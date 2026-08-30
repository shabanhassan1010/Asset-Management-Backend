#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AssetTransfers.CommandModel;
using Asset.Application.Features.AssetTransfers.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Application.Features.AssetTransfers.CommandHandler
{
    public class TransferAssetCommandHandler: IRequestHandler<TransferAssetCommandModel, ApiResponse<TransferAssetResponseDto>>
    {
        #region Fields
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        public TransferAssetCommandHandler(ICurrentUserService currentUser,IUnitOfWork unitOfWork)
        {
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<ApiResponse<TransferAssetResponseDto>> Handle(TransferAssetCommandModel request,CancellationToken cancellationToken)
        {
            var asset = await _unitOfWork.Assets.GetForUpdateAsync(request.AssetId, cancellationToken)
                        ?? throw new NotFoundException($"Asset {request.AssetId} was not found.");

            // 2) Concurrency check FIRST - before any business rule can short-circuit it
            if (string.IsNullOrWhiteSpace(request.RowVersion))
                throw new BusinessException("RowVersion is required.");

            byte[] clientRowVersion;
            try
            {
                clientRowVersion = Convert.FromBase64String(request.RowVersion);
            }
            catch (FormatException)
            {
                throw new BusinessException("RowVersion is not a valid base64 value.");
            }

            if (asset.RowVersion is null || !asset.RowVersion.SequenceEqual(clientRowVersion))
                throw new ConcurrencyException("This asset was modified by another user. Reload it and try again.");

            // 3) Normalize incoming ids
            var toEmployeeId = request.ToEmployeeId is null or 0 ? null : request.ToEmployeeId;
            var toDepartmentId = request.ToDepartmentId is null or 0 ? null : request.ToDepartmentId;
            var toLocationId = request.ToLocationId is null or 0 ? null : request.ToLocationId;


            if (asset.Status == (int)AssetStatus.Retired)
                throw new BusinessException($"{asset.AssetCode} is retired and cannot be transferred.");


            if (request.TransferDate.Date > DateTime.UtcNow.Date)
                throw new BusinessException("A transfer date cannot be in the future.");
        
            if (toDepartmentId.HasValue && !await _unitOfWork.Departments.ExistsActiveAsync(toDepartmentId.Value, cancellationToken))
                throw new BusinessException("The target department does not exist or is inactive.");

            if (toLocationId.HasValue && !await _unitOfWork.Locations.ExistsActiveAsync(toLocationId.Value, cancellationToken))
                throw new BusinessException("The target location does not exist or is inactive.");

            if (toEmployeeId.HasValue)
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(toEmployeeId.Value, cancellationToken)
                               ?? throw new BusinessException("The target employee does not exist.");

                if (!employee.IsActive)
                    throw new BusinessException($"{employee.FullName} is not an active employee.");

                // If the caller didn't send a department, inherit the employee's own department
                toDepartmentId ??= employee.DepartmentId;

                if (employee.DepartmentId != toDepartmentId)
                    throw new BusinessException("The selected employee does not belong to the target department.");
            }

            // 6) No-op check AFTER the department has been resolved
            if (asset.AssignedEmployeeId == toEmployeeId && asset.DepartmentId == toDepartmentId && asset.LocationId == toLocationId)
                throw new BusinessException("A transfer must change the employee, department or location.");


            var transfer = new AssetTransfer
            {
                AssetId = asset.Id,

                FromEmployeeId = asset.AssignedEmployeeId,
                FromDepartmentId = asset.DepartmentId,
                FromLocationId = asset.LocationId,

                ToEmployeeId = toEmployeeId,
                ToDepartmentId = toDepartmentId,
                ToLocationId = toLocationId,

                TransferDate = request.TransferDate,
                Reason = request.Reason.Trim(),
                TransferredByUserId = _currentUser.UserId!
            };

            await _unitOfWork.Assets.AddTransferAsync(transfer, cancellationToken);


            asset.AssignedEmployeeId = toEmployeeId;
            asset.DepartmentId = toDepartmentId;
            asset.LocationId = toLocationId;

            if (asset.Status != (int)AssetStatus.UnderMaintenance)
                asset.Status = toEmployeeId.HasValue ? (int)AssetStatus.Assigned : (int)AssetStatus.Available;

            asset.UpdatedAt = DateTime.UtcNow;
            asset.UpdatedByUserId = _currentUser.UserId;

            _unitOfWork.Assets.SetOriginalRowVersion(asset, Convert.FromBase64String(request.RowVersion));

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("This asset was modified by another user. Reload it and try again.");
            }

            return new ApiResponse<TransferAssetResponseDto>
            {
                Success = true,
                Message = "Asset Transferred Successfully",
                data = new TransferAssetResponseDto
                {
                    TransferId = transfer.Id,

                    AssetId = asset.Id,
                    AssetCode = asset.AssetCode,
                    ToEmployeeId = asset.AssignedEmployeeId,
                    ToDepartmentId = asset.DepartmentId,
                    ToLocationId = asset.LocationId,
                    Status = asset.Status,
                    TransferDate = transfer.TransferDate,
                    RowVersion = Convert.ToBase64String(asset.RowVersion)
                }
            };
        }
    }
}