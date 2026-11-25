#region
using Asset.Application.Common;
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
                                            IRequestHandler<DeactivateDepartmentCommandModel, ApiResponse<string>>,
                                            IRequestHandler<ActivateDepartmentCommandModel, ApiResponse<string>>
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
                throw new BusinessException("Department with name '{0}' already exists.", name);


            var nameAr = LocalizedName.TrimOrNull(request.DepartmentNameAr);
            if (nameAr is not null && await _unitOfWork.Departments.IsNameArExistsAsync(nameAr, null, cancellationToken))
                throw new BusinessException("Arabic department name '{0}' is already in use.", nameAr);

            var entity = _mapper.Map<Department>(request);
            entity.DepartmentName = name;
            entity.DepartmentNameAr = nameAr;
            entity.IsActive = true;

            _unitOfWork.Departments.Add(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

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
                throw new NotFoundException("Department {0} does not exist.", request.Id);

            var name = request.DepartmentName.Trim();                                            

            if (await _unitOfWork.Departments.IsNameExistsAsync(name, request.Id, cancellationToken)) 
                throw new BusinessException("A department with this name already exists.");

            var nameAr = LocalizedName.TrimOrNull(request.DepartmentNameAr);
            if (nameAr is not null && await _unitOfWork.Departments.IsNameArExistsAsync(nameAr, request.Id, cancellationToken))
                throw new BusinessException("Arabic department name '{0}' is already in use.", nameAr);

            _mapper.Map(request, entity);
            entity.DepartmentName = name;
            entity.DepartmentNameAr = nameAr;    // "" from the form is saved as NULL

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

            return new ApiResponse<UpdateDepartmentResponseDto>
            {
                data = _mapper.Map<UpdateDepartmentResponseDto>(entity),
                Success = true,
                Message = "Department Updated Successfully"
            };
        }

        public async Task<ApiResponse<string>> Handle(DeactivateDepartmentCommandModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Departments.GetByIdAsync(request.Id, cancellationToken);

            if (entity is null || !entity.IsActive)
                throw new NotFoundException("Department {0} was not found.", request.Id);

            var employeesCount = await _unitOfWork.Employees.CountEmployeesAsync(request.Id, cancellationToken);
            var assetsCount   = await _unitOfWork.Assets.CountAssetsAsync(request.Id, cancellationToken);

            if (employeesCount > 0 || assetsCount > 0)
            {
                throw new BusinessException("Cannot deactivate this department: it has {0} employee(s) and {1} asset(s). Reassign them first.", employeesCount, assetsCount);
            }

            _unitOfWork.Departments.DeActivate(entity);  
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateAsync();

            return new ApiResponse<string>
            {
                data = null,
                Success = true,
                Message = "Department Deactivated Successfully"
            };
        }
        #endregion

        public async Task<ApiResponse<string>> Handle(ActivateDepartmentCommandModel request, CancellationToken cancellationToken)
        {
            // GetByIdAsync returns inactive rows too (it is a plain find by id), and the entity is tracked,
            // so changing IsActive and calling SaveChanges is enough.
            var entity = await _unitOfWork.Departments.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException("Department {0} was not found.", request.Id);

            // Nothing to do: answer OK instead of an error, so clicking twice is harmless.
            if (entity.IsActive)
                return new ApiResponse<string>
                {
                    data = entity.DepartmentName,
                    Success = true,
                    Message = "Department is already active"
                };

            // The names are UNIQUE among ACTIVE rows only (filtered indexes ..._Active).
            // While this row was inactive someone may have created a new one with the same name.
            // Turning this one back on would then break the index, so we refuse with a clear message.
            if (await _unitOfWork.Departments.IsNameExistsAsync(entity.DepartmentName, entity.Id, cancellationToken))
                throw new ConflictException("Another active department is already named '{0}'. Rename one of them first.", entity.DepartmentName);

            if (entity.DepartmentNameAr is not null &&
                await _unitOfWork.Departments.IsNameArExistsAsync(entity.DepartmentNameAr, entity.Id, cancellationToken))
                throw new ConflictException("Another active department already uses the Arabic name '{0}'. Rename one of them first.", entity.DepartmentNameAr);

            entity.IsActive = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();     // the dropdowns and every page of the list must show it again

            return new ApiResponse<string>
            {
                data = entity.DepartmentName,
                Success = true,
                Message = "Department Activated Successfully"
            };
        }

        #region Private Methods
        private async Task InvalidateAsync()
        {
            await _cache.RemoveAsync(CacheKeys.DepartmentList);           // the full list (dropdowns)
            await _cache.BumpVersionAsync(CacheKeys.DepartmentsVersion);  // every page of the paged list
            await _cache.BumpVersionAsync(CacheKeys.AssetsVersion);
        }
        #endregion
    }
}