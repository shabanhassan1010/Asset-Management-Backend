#region
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Departments.Commands.CommandHandlers
{
    public class DepartmentCommandHandler :
                                            IRequestHandler<CreateDepartmentCommandModel, ApiResponse<CreateDepartmentResponseDto>>,
                                            IRequestHandler<UpdateDepartmentCommandModel, ApiResponse<UpdateDepartmentResponseDto>>,
                                            IRequestHandler<DeleteDepartmentCommandModel, ApiResponse<string>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        #endregion

        #region Constructor
        public DepartmentCommandHandler(IUnitOfWork unitOfWork,IMapper mapper, ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }
        #endregion

        #region Handlers
        public async Task<ApiResponse<CreateDepartmentResponseDto>> Handle(CreateDepartmentCommandModel request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Department>(request);
            entity.IsActive = true;

            await _unitOfWork.Departments.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // After the save, never before: a failed save would have cleared the
            // cache for nothing, and a concurrent read could refill it with old data.
            await _cache.RemoveAsync(CacheKeys.DepartmentList, cancellationToken);

            return new ApiResponse<CreateDepartmentResponseDto>
            {
                data = _mapper.Map<CreateDepartmentResponseDto>(entity),
                Success = true,
                Message = "Department Created Successfully"
            };
        }

        public async Task<ApiResponse<UpdateDepartmentResponseDto>> Handle(UpdateDepartmentCommandModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Departments.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null || entity.IsActive == false)
                throw new NotFoundException($"Department {request.Id} does not exist.");

            _mapper.Map(request, entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Two keys affected: the single item and the list it appears in.
            await _cache.RemoveAsync(CacheKeys.DepartmentById(request.Id), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.DepartmentList, cancellationToken);
            return new ApiResponse<UpdateDepartmentResponseDto>
            {
                data = _mapper.Map<UpdateDepartmentResponseDto>(entity),
                Success = true,
                Message = "Department Updated Successfully"
            };
        }

        public async Task<ApiResponse<string>> Handle(DeleteDepartmentCommandModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Departments.GetByIdAsync(request.Id, cancellationToken);

            if (entity is null || entity.IsActive == false)
                throw new NotFoundException($"Department {request.Id} does not exist.");

            var employeesCount = await _unitOfWork.Departments.CountEmployeesAsync(request.Id, cancellationToken);
            var assetsCount   = await _unitOfWork.Departments.CountAssetsAsync(request.Id, cancellationToken);

            if (employeesCount > 0 || assetsCount > 0)
            {
                throw new BusinessException($"Cannot deactivate this department: it has {employeesCount} employee(s) and {assetsCount} asset(s). Reassign them first.");
            }

            _unitOfWork.Departments.Remove(entity);  
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.DepartmentById(request.Id), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.DepartmentList, cancellationToken);
            return new ApiResponse<string>
            {
                data = null,
                Success = true,
                Message = "Department Deactivated Successfully"
            };
        }
        #endregion
    }
}
