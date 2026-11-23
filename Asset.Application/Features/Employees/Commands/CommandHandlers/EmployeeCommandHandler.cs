#region
using Asset.Application.Bases;
using Asset.Application.Common;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Commands.CommandModels;
using Asset.Application.Features.Employees.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Resources;
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
        private readonly ICurrentUserService _currentUser;   // who moved the assets (saved in the transfer history)
        #endregion

        #region Constructor
        public EmployeeCommandHandler(IUnitOfWork unitOfWork, ICacheService cache,
                                      IMapper mapper,
                                      ICurrentUserService currentUser,
                                      IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _mapper = mapper;
            _currentUser = currentUser;
        }
        #endregion

        #region Handlers
        public async Task<ApiResponse<CreateEmployeeCommandResponse>> Handle(CreateEmployeeCommandModel request, CancellationToken cancellationToken)
        {
            var employeeCode = request.EmployeeCode.Trim();
            var email = request.Email?.Trim();

            var departmentExists = await _unitOfWork.Departments.ExistsActiveAsync(request.DepartmentId, cancellationToken);
            if (!departmentExists)
                throw new NotFoundException("Department {0} does not exist.", request.DepartmentId);

            var codeExists = await _unitOfWork.Employees.IsCodeExistsAsync(employeeCode, null, cancellationToken);
            if (codeExists)
                throw new ConflictException("Employee code '{0}' is already used.", employeeCode);

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailExists = await _unitOfWork.Employees.IsEmailExistsAsync(email, null, cancellationToken);
                if (emailExists)
                    throw new ConflictException("Email '{0}' is already used by another employee.", email);

            }
            var employee = new Employee
            {
                EmployeeCode = employeeCode,
                FullName = request.FullName.Trim(),
                FullNameAr = LocalizedName.TrimOrNull(request.FullNameAr),   // "" from the form is saved as NULL
                Email = email,
                Phone = request.Phone?.Trim(),
                DepartmentId = request.DepartmentId,
                IsActive = true
            };

            await _unitOfWork.Employees.AddAsync(employee, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

            return new ApiResponse<CreateEmployeeCommandResponse>
            {
                data = _mapper.Map<CreateEmployeeCommandResponse>(employee),
                Success = true,
                Message = "Employee Created Successfully"
            };
        }
        public async Task<ApiResponse<UpdateEmployeeCommandResponse>> Handle(UpdateEmployeeCommandModel request, CancellationToken cancellationToken)
        {
            var email = request.Email?.Trim();

            var employee = await _unitOfWork.Employees.GetByIdAsync(request.Id, cancellationToken);
            if (employee is null)
                throw new NotFoundException("Employee {0} does not exist.", request.Id);

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailExists = await _unitOfWork.Employees.IsEmailExistsAsync(email, request.Id, cancellationToken);
                if (emailExists)
                    throw new ConflictException("Email '{0}' is already used by another employee.", email);
            }

            if (employee.DepartmentId != request.DepartmentId)
            {
                var departmentExists = await _unitOfWork.Departments.ExistsActiveAsync(request.DepartmentId, cancellationToken);
                if (!departmentExists)
                    throw new NotFoundException("Department {0} does not exist.", request.DepartmentId);

                employee.DepartmentId = request.DepartmentId;

                // The bug this fixes: Assets has its OWN DepartmentId column, so changing the
                // employee's department left their assets in the old one (and the asset list showed it).
                await MoveAssignedAssetsAsync(employee, cancellationToken);
            }

            employee.FullName = request.FullName.Trim();
            employee.FullNameAr = LocalizedName.TrimOrNull(request.FullNameAr);
            employee.Email = email;
            employee.Phone = request.Phone?.Trim();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

            var saved = await _unitOfWork.Employees.GetByIdWithDepartmentAsync(employee.Id, cancellationToken);
            return new ApiResponse<UpdateEmployeeCommandResponse>
            {
                data = _mapper.Map<UpdateEmployeeCommandResponse>(saved),
                Success = true,
                Message = "Employee Updated Successfully"
            };
        }
        public async Task<ApiResponse<SetEmployeeStatusCommandResponse>> Handle(SetEmployeeStatusCommandModel request, CancellationToken cancellationToken)
        {
            var employee = await _unitOfWork.Employees.GetByIdWithDepartmentAsync(request.Id, cancellationToken);
            if (employee is null)
                throw new NotFoundException("Employee {0} does not exist.", request.Id);

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
                    throw new BusinessException("Cannot disable employee '{0}' while assets are still assigned to them. Transfer or return the assets first.",
                                                LocalizedName.Pick(employee.FullName, employee.FullNameAr));
            }

            employee.IsActive = request.IsActive;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

            return new ApiResponse<SetEmployeeStatusCommandResponse>
            {
                data = _mapper.Map<SetEmployeeStatusCommandResponse>(employee),
                Success = true,
                Message = request.IsActive ? "Employee Enabled Successfully" : "Employee Disabled Successfully"
            };
        }
        #endregion

        #region Private Methods
        // Rule used everywhere in the app (TransferAssetCommandHandler checks the same thing):
        // an asset assigned to a person sits in that person's department.
        // So when the person moves, their assets move with them, and each move is written to the
        // transfer history like a normal transfer, so the audit trail stays complete.
        // Nothing is saved here: the caller's single SaveChangesAsync saves the employee, the assets
        // and the history together (one transaction — all or nothing).
        private async Task MoveAssignedAssetsAsync(Employee employee, CancellationToken cancellationToken)
        {
            var assets = await _unitOfWork.Assets.GetActiveAssignedToEmployeeAsync(employee.Id, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var asset in assets.Where(a => a.DepartmentId != employee.DepartmentId))
            {
                await _unitOfWork.Assets.AddTransferAsync(new AssetTransfer
                {
                    AssetId = asset.Id,

                    FromEmployeeId = asset.AssignedEmployeeId,
                    FromDepartmentId = asset.DepartmentId,
                    FromLocationId = asset.LocationId,

                    ToEmployeeId = asset.AssignedEmployeeId,     // same person
                    ToDepartmentId = employee.DepartmentId,      // new department
                    ToLocationId = asset.LocationId,             // same place

                    TransferDate = now,
                    // Kept in English in the database; the Angular app translates it when it shows the history.
                    Reason = "Moved automatically: the assigned employee changed department.",
                    TransferredByUserId = _currentUser.UserId!
                }, cancellationToken);

                asset.DepartmentId = employee.DepartmentId;
                asset.UpdatedAt = now;
                asset.UpdatedByUserId = _currentUser.UserId;
            }
        }

        private async Task InvalidateAsync()
        {
            await _cache.RemoveAsync(new[] { CacheKeys.EmployeeList, CacheKeys.DepartmentList });
            // Employee paged list is cached by version → bump the version
            await _cache.BumpVersionAsync(CacheKeys.EmployeesVersion);
            await _cache.BumpVersionAsync(CacheKeys.DepartmentsVersion);
            // Asset rows and asset details show this employee's name and are cached under "assets:version".
            // Without this bump, the asset list keeps the OLD name until its cache expires.
            // It lives here (not only in Update) so Create and Enable/Disable are covered too.
            await _cache.BumpVersionAsync(CacheKeys.AssetsVersion);
        }
        #endregion
    }
}