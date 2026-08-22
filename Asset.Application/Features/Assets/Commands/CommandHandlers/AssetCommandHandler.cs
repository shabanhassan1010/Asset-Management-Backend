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
            request.DepartmentId = request.DepartmentId is null or 0 ? null : request.DepartmentId;
            request.AssignedEmployeeId = request.AssignedEmployeeId is null or 0 ? null : request.AssignedEmployeeId;
            request.LocationId = request.LocationId is null or 0 ? null : request.LocationId;

            var entity = _mapper.Map<AssetEntity>(request);
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
            var entity = await _unitOfWork.Assets.GetForUpdateAsync(request.AssetId, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Asset {request.AssetId} was not found.");

            request.DepartmentId = request.DepartmentId is null or 0 ? null : request.DepartmentId;
            request.AssignedEmployeeId = request.AssignedEmployeeId is null or 0 ? null : request.AssignedEmployeeId;
            request.LocationId = request.LocationId is null or 0 ? null : request.LocationId;


            _mapper.Map(request, entity);

            entity.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Assets.SetOriginalRowVersion(entity, Convert.FromBase64String(request.RowVersion));

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
        #endregion
    }
}