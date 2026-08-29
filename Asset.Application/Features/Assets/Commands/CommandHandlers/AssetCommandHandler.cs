#region
using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Features.Assets.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resoures;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Net.Http.Headers;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion

namespace Asset.Application.Features.Assets.Commands.CommandHandlers
{
    public class AssetCommandHandler : BaseResponseHandler, IRequestHandler<CreateAssetCommandModel, ApiResponse<CreateAssetResponseDto>>,
                                                            IRequestHandler<UpdateAssetCommandModel, ApiResponse<UpdateAssetResponseDto>>,
                                                            IRequestHandler<RetireAssetCommandModel, ApiResponse<RetireAssetResponseDto>>
    {
        #region Fields
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cache;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public AssetCommandHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork, ICacheService cache,
                                   IMapper mapper, IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _mapper = mapper;
        }
        #endregion

        #region Methods
        public async Task<ApiResponse<CreateAssetResponseDto>> Handle(CreateAssetCommandModel request, CancellationToken cancellationToken)
        {
            // The database has a unique index on AssetCode. We check here first so the user gets a clear message instead of a raw SqlException
            var codeExists = await _unitOfWork.Assets.AnyAsync(a => a.AssetCode == request.AssetCode, cancellationToken);

            if (codeExists)
                throw new ConflictException("An asset with this code already exists.");

            // Serial numbers are optional — not every asset has one — so this is only checked when the user actually supplied one.
            if (!string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                var serialExists = await _unitOfWork.Assets.AnyAsync(a => a.SerialNumber == request.SerialNumber, cancellationToken);

                if (serialExists)
                    throw new ConflictException("An asset with this serial number already exists.");
            }
            request.DepartmentId = request.DepartmentId is null or 0 ? null : request.DepartmentId;
            request.AssignedEmployeeId = request.AssignedEmployeeId is null or 0 ? null : request.AssignedEmployeeId;
            request.LocationId = request.LocationId is null or 0 ? null : request.LocationId;

            var entity = _mapper.Map<AssetEntity>(request);

            // Identity comes from the validated token, never from the request body.
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedByUserId = _currentUser.UserId;

            await _unitOfWork.Assets.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateLookupCountsAsync(cancellationToken);
            return new ApiResponse<CreateAssetResponseDto>
            {
                data = _mapper.Map<CreateAssetResponseDto>(entity),
                Success = true,
                Message = "AssetCreatedSuccessfully"
            };
        }

        public async Task<ApiResponse<UpdateAssetResponseDto>> Handle(UpdateAssetCommandModel request, CancellationToken cancellationToken)
        {
            // Parsed before anything else: a malformed value is the client's  mistake, and Convert.FromBase64String would otherwise surface it as a 500.
            if (!TryParseRowVersion(request.RowVersion, out var rowVersion))
                throw new ConflictException("The row version is missing or malformed. Please reload the asset.");

            var entity = await _unitOfWork.Assets.GetForUpdateAsync(request.AssetId, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Asset {request.AssetId} was not found.");

            // a.Id != request.AssetId matters: without it, saving an asset whose
            // code did not change would be rejected for clashing with itself.
            var codeExists = await _unitOfWork.Assets.AnyAsync(a => a.AssetCode == request.AssetCode && a.Id != request.AssetId, cancellationToken);

            if (codeExists)
                throw new ConflictException("An asset with this code already exists.");


            if (!string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                var serialExists = await _unitOfWork.Assets
                    .AnyAsync(a => a.SerialNumber == request.SerialNumber && a.Id != request.AssetId, cancellationToken);

                if (serialExists)
                    throw new ConflictException("An asset with this serial number already exists.");
            }

            request.DepartmentId = request.DepartmentId is null or 0 ? null : request.DepartmentId;
            request.AssignedEmployeeId = request.AssignedEmployeeId is null or 0 ? null : request.AssignedEmployeeId;
            request.LocationId = request.LocationId is null or 0 ? null : request.LocationId;


            _mapper.Map(request, entity);

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByUserId = _currentUser.UserId;

            _unitOfWork.Assets.SetOriginalRowVersion(entity, rowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateLookupCountsAsync(cancellationToken);

            return new ApiResponse<UpdateAssetResponseDto>
            {
                data = _mapper.Map<UpdateAssetResponseDto>(entity),
                Success = true,
                Message = "AssetUpdatedSuccessfully"
            };
        }

        public async Task<ApiResponse<RetireAssetResponseDto>> Handle(RetireAssetCommandModel request, CancellationToken cancellationToken)
        {
            var asset = await _unitOfWork.Assets.GetForUpdateAsync(request.AssetId, cancellationToken);

            if (asset is null)
                throw new NotFoundException($"Asset {request.AssetId} was not found.");

            if (asset.Status == (int)AssetStatus.Retired)
                throw new BusinessException("Asset is already retired.");

            asset.Status = (int)AssetStatus.Retired;
            asset.RetiredAt = DateTime.UtcNow;
            asset.RetirementReason = request.Reason;
            asset.RetiredByUserId = _currentUser.UserId;

            // to now which use did the retire for assets 
            asset.UpdatedAt = DateTime.UtcNow;
            asset.UpdatedByUserId = _currentUser.UserId;

            _unitOfWork.Assets.SetOriginalRowVersion(asset, Convert.FromBase64String(request.RowVersion));
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await InvalidateLookupCountsAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("This asset was modified by another user. Reload it and try again.");
            }
            return new ApiResponse<RetireAssetResponseDto>
            {
                Success = true,
                Message = "Asset Retired Successfully",
                data = new RetireAssetResponseDto
                {
                    AssetId = asset.Id,
                    AssetCode = asset.AssetCode,
                    Status = asset.Status,
                    RetiredAt = asset.RetiredAt!.Value,
                    RowVersion = Convert.ToBase64String(asset.RowVersion)
                }
            };
        }
        #endregion

        #region Private 
        private async Task InvalidateLookupCountsAsync(CancellationToken ct)
        {
            foreach (var key in CacheKeys.ListsAffectedByAssetChanges)
                await _cache.RemoveAsync(key, ct);
        }
        private static bool TryParseRowVersion(string value, out byte[] result)
        {
            result = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                result = Convert.FromBase64String(value);
                return result.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        #endregion
    }
}