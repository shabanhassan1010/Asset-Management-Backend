#region
using Asset.Application.Bases;
using Asset.Application.Common;
using Asset.Application.Common.Caching;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Resources;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion

namespace Asset.Application.Features.AssetTypes.Commands.CommandHandlers
{
    public class AssetTypeCommandHandler : BaseResponseHandler, IRequestHandler<CreateAssetTypeCommandModel, BaseResponse<int>>,
                                                                IRequestHandler<UpdateAssetTypeCommandModel, BaseResponse<string>>,
                                                                IRequestHandler<DeactivateAssetTypeCommandModel, BaseResponse<string>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        #endregion

        #region Constructor
        public AssetTypeCommandHandler(IUnitOfWork unitOfWork, IMapper mapper,
                                             ICacheService cacheService,
                                             IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }
        #endregion
        public async Task<BaseResponse<int>> Handle(CreateAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var name = request.assetTypeName.Trim();
            if (await _unitOfWork.AssetTypes.AssetTypeNameExistsAsync(name, null, cancellationToken))
                throw new BusinessException("An Asset Type with this name already exists");

            var nameAr = LocalizedName.TrimOrNull(request.assetTypeNameAr);
            if (nameAr is not null && await _unitOfWork.AssetTypes.AssetTypeNameArExistsAsync(nameAr, null, cancellationToken))
                throw new BusinessException("Arabic asset type name '{0}' is already in use.", nameAr);

            var assetType = _mapper.Map<AssetType>(request);
            assetType.TypeName = name;
            assetType.TypeNameAr = nameAr;
            assetType.IsActive = true;

            _unitOfWork.AssetTypes.Add(assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateAsync();

            return Created(assetType.Id);
        }
        public async Task<BaseResponse<string>> Handle(UpdateAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);

            if (assetType is null || !assetType.IsActive)
                throw new NotFoundException("Asset type not found");

            var name = request.assetTypeName.Trim();                                              // NEW

            if (await _unitOfWork.AssetTypes.AssetTypeNameExistsAsync(name, request.Id, cancellationToken))  // NEW
                throw new BusinessException("An Asset Type with this name already exists.");


            var nameAr = LocalizedName.TrimOrNull(request.assetTypeNameAr);
            if (nameAr is not null && await _unitOfWork.AssetTypes.AssetTypeNameArExistsAsync(nameAr, request.Id, cancellationToken))
                throw new BusinessException("Arabic asset type name '{0}' is already in use.", nameAr);

            _mapper.Map(request, assetType);
            assetType.TypeName = name;
            assetType.TypeNameAr = nameAr;       // "" from the form is saved as NULL

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateAsync();

            return Success("Updated successfully");
        }
        public async Task<BaseResponse<string>> Handle(DeactivateAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);
            if (assetType is null || !assetType.IsActive)
                throw new NotFoundException("Asset type not found");

            var isUsed = await _unitOfWork.Assets.AnyAsync(a => a.AssetTypeId == request.Id && a.Status != (int)AssetStatus.Retired, cancellationToken);
            if (isUsed)
                throw new BusinessException("This asset type is in use. Delete or reassign its assets first.");

            _unitOfWork.AssetTypes.Deactivate(assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateAsync();


            return Deleted<string>("Deleted successfully");
        }

        #region Private Methods
        private async Task InvalidateAsync()
        {
            await _cacheService.RemoveAsync(CacheKeys.AssetTypeList);
            await _cacheService.BumpVersionAsync(CacheKeys.AssetTypesVersion);
            // Asset rows and asset details show this asset type's name and are cached under "assets:version".
            // Without this bump, the asset list keeps the OLD name until its cache expires.
            await _cacheService.BumpVersionAsync(CacheKeys.AssetsVersion);
        }
        #endregion
    }
}