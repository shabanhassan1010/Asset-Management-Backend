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
    public class DepartmentCommandHandler : IRequestHandler<CreateDepartmentCommandModel, ApiResponse<CreateDepartmentResponseDto>>,
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
            var name = request.DepartmentName.Trim();
            if(await _unitOfWork.Departments.IsNameExistsAsync(name, null, cancellationToken))
                throw new BusinessException($"Department with name '{name}' already exists.");


            var entity = _mapper.Map<Department>(request);
            entity.DepartmentName = name;
            entity.IsActive = true;

            _unitOfWork.Departments.Add(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync(CacheKeys.DepartmentList);

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
            if (entity is null || !entity.IsActive)
                throw new NotFoundException($"Department {request.Id} does not exist.");

            var name = request.DepartmentName.Trim();                                            

            if (await _unitOfWork.Departments.IsNameExistsAsync(name, request.Id, cancellationToken)) 
                throw new BusinessException("A location with this name already exists.");

            _mapper.Map(request, entity);
            entity.DepartmentName = name;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync( new[] { CacheKeys.DepartmentById(request.Id), CacheKeys.DepartmentList });
            await _cache.BumpVersionAsync(CacheKeys.AssetsVersion);

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

            if (entity is null || !entity.IsActive)
                throw new NotFoundException($"Department {request.Id} was not found.");

            var employeesCount = await _unitOfWork.Employees.CountEmployeesAsync(request.Id, cancellationToken);
            var assetsCount   = await _unitOfWork.Assets.CountAssetsAsync(request.Id, cancellationToken);

            if (employeesCount > 0 || assetsCount > 0)
            {
                throw new BusinessException($"Cannot deactivate this department: it has {employeesCount} employee(s) and {assetsCount} asset(s). Reassign them first.");
            }

            _unitOfWork.Departments.Remove(entity);  
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync(new[] { CacheKeys.DepartmentById(request.Id), CacheKeys.DepartmentList });

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