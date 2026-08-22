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

            // An unselected dropdown posts 0, which is not a valid key. Normalised
            // once here so every comparison below works on the same value the
            // database will store - the same treatment create and update give it.
            var toEmployeeId = request.ToEmployeeId is null or 0 ? null : request.ToEmployeeId;
            var toDepartmentId = request.ToDepartmentId is null or 0 ? null : request.ToDepartmentId;
            var toLocationId = request.ToLocationId is null or 0 ? null : request.ToLocationId;

            // ---- R3.4 : business rules, server-side, meaningful errors (422) ----

            if (asset.Status == (int)AssetStatus.Retired)
                throw new BusinessException($"{asset.AssetCode} is retired and cannot be transferred.");

            // Compared on Date, not on the full timestamp: a client in UTC+2
            // sending today's date at 00:00 local is "yesterday 22:00" in UTC,
            // and comparing instants would reject a perfectly valid same-day
            // transfer.
            if (request.TransferDate.Date > DateTime.UtcNow.Date)
                throw new BusinessException("A transfer date cannot be in the future.");

            if (asset.AssignedEmployeeId == toEmployeeId && asset.DepartmentId == toDepartmentId && asset.LocationId == toLocationId)
                throw new BusinessException("A transfer must change the employee, department or location.");

            // ---- targets must exist and be usable ----
            //
            // The foreign keys would catch a missing row anyway, but as a SQL
            // 547 that the middleware can only report generically. Checking here
            // is what lets the message name the field the admin got wrong.

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

                // Without this the asset could sit in Finance while the person
                // holding it works in IT - two columns describing the same fact
                // and disagreeing. The prototype enforces it in the form; this
                // is the authoritative copy.
                if (employee.DepartmentId != toDepartmentId)
                    throw new BusinessException("The selected employee does not belong to the target department.");
            }

            // ---- R3.1 : the history row, written BEFORE the asset changes ----
            //
            // Order matters for readability, not for EF: reading asset.* here
            // makes it obvious the From* values are the OLD assignment. Move
            // this block below the assignments and From* silently becomes a copy
            // of To*, with nothing to show for it in a diff.
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

                // R3.1 "who performed it" - from the validated token, never from
                // the request body. The Admin role check on the endpoint
                // guarantees this is populated.
                TransferredByUserId = _currentUser.UserId!
            };

            await _unitOfWork.Assets.AddTransferAsync(transfer, cancellationToken);

            // ---- the asset's new current assignment ----

            asset.AssignedEmployeeId = toEmployeeId;
            asset.DepartmentId = toDepartmentId;
            asset.LocationId = toLocationId;

            // Status follows the assignment: holding an employee means Assigned,
            // holding none means Available.
            //
            // UnderMaintenance is left alone on purpose. It describes the
            // physical condition of the asset, not who has it, so moving a
            // machine that is in for repair must not silently mark it as ready
            // to use. Retired never reaches this line - it was rejected above.
            if (asset.Status != (int)AssetStatus.UnderMaintenance)
                asset.Status = toEmployeeId.HasValue ? (int)AssetStatus.Assigned : (int)AssetStatus.Available;

            // R2.7 - audit fields from the authenticated principal.
            asset.UpdatedAt = DateTime.UtcNow;
            asset.UpdatedByUserId = _currentUser.UserId;

            // R3.5 - tells EF to put the client's stamp in the WHERE clause.
            // If another admin transferred this asset in the meantime the UPDATE
            // matches zero rows, EF throws DbUpdateConcurrencyException, and the
            // middleware answers 409. Both transfers cannot succeed.
            _unitOfWork.Assets.SetOriginalRowVersion(asset, Convert.FromBase64String(request.RowVersion));

            // R3.3 - ONE SaveChanges for the history insert and the asset update.
            // EF wraps a single SaveChanges in an implicit transaction, and both
            // entities belong to the same DbContext, so an explicit
            // BeginTransaction would add a second transaction around one that
            // already exists. If the concurrency check fails, neither write lands.
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
                    // Populated by EF from the identity column during SaveChanges.
                    TransferId = transfer.Id,

                    AssetId = asset.Id,
                    AssetCode = asset.AssetCode,
                    ToEmployeeId = asset.AssignedEmployeeId,
                    ToDepartmentId = asset.DepartmentId,
                    ToLocationId = asset.LocationId,
                    Status = asset.Status,
                    TransferDate = transfer.TransferDate,

                    // Refreshed by SQL Server during the UPDATE, so the client can
                    // transfer again without re-reading the asset first.
                    RowVersion = Convert.ToBase64String(asset.RowVersion)
                }
            };
        }
    }
}
