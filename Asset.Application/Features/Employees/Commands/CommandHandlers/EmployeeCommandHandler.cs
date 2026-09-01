#region
using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using Asset.Application.Features.Employees.Commands.CommandModels;
using Asset.Application.Features.Employees.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion

namespace Asset.Application.Features.Employees.Commands.CommandHandlers
{
    public class EmployeeCommandHandler : BaseResponseHandler,
                                          IRequestHandler<CreateEmployeeCommandModel, ApiResponse<CreateEmployeeCommandResponse>>,
                                          IRequestHandler<UpdateEmployeeCommandModel, ApiResponse<UpdateEmployeeCommandResponse>>,
                                          IRequestHandler<SetEmployeeStatusCommandModel, ApiResponse<SetEmployeeStatusCommandResponse>>

    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cache;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public EmployeeCommandHandler(IUnitOfWork unitOfWork, ICacheService cache,
                                      IMapper mapper,
                                      IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _mapper = mapper;
        }
        #endregion

        #region Handlers
        public async Task<ApiResponse<CreateEmployeeCommandResponse>> Handle(CreateEmployeeCommandModel request, CancellationToken cancellationToken)
        {
            var employeeCode = request.EmployeeCode.Trim();
            var email = request.Email?.Trim();

            var departmentExists = await _unitOfWork.Departments.ExistsActiveAsync(request.DepartmentId, cancellationToken);
            if (!departmentExists)
                throw new NotFoundException($"Department {request.DepartmentId} does not exist.");

            var codeExists = await _unitOfWork.Employees.IsCodeExistsAsync(employeeCode, null, cancellationToken);
            if (codeExists)
                throw new ConflictException($"Employee code '{employeeCode}' is already used.");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailExists = await _unitOfWork.Employees.IsEmailExistsAsync(email, null, cancellationToken);
                if (emailExists)
                    throw new ConflictException($"Email '{email}' is already used by another employee.");

            }
            var employee = new Employee
            {
                EmployeeCode = employeeCode,
                FullName = request.FullName.Trim(),
                Email = request.Email,
                Phone = request.Phone,
                DepartmentId = request.DepartmentId,
                IsActive = true
            };

            await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync(employee.Id, cancellationToken);

            return new ApiResponse<CreateEmployeeCommandResponse>
            {
                data = _mapper.Map<CreateEmployeeCommandResponse>(employee), 
                Success = true,
                Message = "Employee Created Successfully"
            };
        }
        public async Task<ApiResponse<UpdateEmployeeCommandResponse>> Handle(UpdateEmployeeCommandModel request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();

            var employee = await _unitOfWork.Employees.GetByIdAsync(request.Id, cancellationToken);
            if (employee is null)
                throw new NotFoundException($"Employee {request.Id} does not exist.");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailExists = await _unitOfWork.Employees.IsEmailExistsAsync(email, request.Id, cancellationToken);
                if (emailExists)
                    throw new ConflictException($"Email '{email}' is already used by another employee.");
            }

            if (employee.DepartmentId != request.DepartmentId)
            {
                var departmentExists = await _unitOfWork.Departments.ExistsActiveAsync(request.DepartmentId, cancellationToken);
                if (!departmentExists)
                    throw new NotFoundException($"Department {request.DepartmentId} does not exist.");

                employee.DepartmentId = request.DepartmentId;   
            }

            employee.FullName = request.FullName.Trim();
            employee.Email = email;
            employee.Phone = request.Phone?.Trim();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync(employee.Id, cancellationToken);

            var saved = await _unitOfWork.Employees.GetByIdWithDepartmentAsNoTrackingAsync(employee.Id, cancellationToken);
            return new ApiResponse<UpdateEmployeeCommandResponse>
            {
                data = _mapper.Map<UpdateEmployeeCommandResponse>(saved),
                Success = true,
                Message = "Employee Updated Successfully"
            };
        }
        public async Task<ApiResponse<SetEmployeeStatusCommandResponse>> Handle(SetEmployeeStatusCommandModel request, CancellationToken cancellationToken)
        {
            var employee = await _unitOfWork.Employees.GetByIdWithDepartmentAsNoTrackingAsync(request.Id, cancellationToken);
            if (employee is null)
                throw new NotFoundException($"Employee {request.Id} does not exist.");

            if (employee.IsActive == request.IsActive)
            {
                return new ApiResponse<SetEmployeeStatusCommandResponse>
                {
                    data = _mapper.Map<SetEmployeeStatusCommandResponse>(employee),
                    Success = true,
                    Message = request.IsActive ? "Employee is already active" : "Employee is already disabled"
                };
            }

            if (!request.IsActive)
            {
                var hasAssets = await _unitOfWork.Employees.HasAssignedAssetsAsync(request.Id, cancellationToken);
                if (hasAssets)
                    throw new BusinessException(
                        $"Cannot disable employee '{employee.FullName}' while assets are still assigned to them. Transfer or return the assets first.");
            }

            employee.IsActive = request.IsActive;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync(employee.Id, cancellationToken);

            return new ApiResponse<SetEmployeeStatusCommandResponse>
            {
                data = _mapper.Map<SetEmployeeStatusCommandResponse>(employee),
                Success = true,
                Message = request.IsActive ? "Employee Enabled Successfully" : "Employee Disabled Successfully"
            };
        }
        #endregion
        private async Task InvalidateAsync(int employeeId, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(CacheKeys.EmployeeList, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.EmployeeById(employeeId), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.DepartmentList, cancellationToken);
        }
    }
}