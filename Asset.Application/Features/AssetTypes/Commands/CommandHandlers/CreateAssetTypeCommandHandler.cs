#region
using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Resoures;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion

namespace Asset.Application.Features.AssetTypes.Commands.CommandHandlers
{
    public class CreateAssetTypeCommandHandler : BaseResponseHandler, IRequestHandler<CreateAssetTypeCommandModel, BaseResponse<int>> ,
                                                                      IRequestHandler<UpdateAssetTypeCommandModel, BaseResponse<string>> ,
                                                                      IRequestHandler<DeleteAssetTypeCommandModel, BaseResponse<string>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        #endregion

        #region Constructor
        public CreateAssetTypeCommandHandler(IUnitOfWork unitOfWork, IMapper mapper,
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
            var exists = await _unitOfWork.AssetTypes.AnyAsync(t => t.TypeName == request.TypeName, cancellationToken);

            if (exists)
                return BadRequest<int>("Asset type name already exists");

            var assetType = _mapper.Map<AssetType>(request);

            assetType.IsActive = true;
            await _unitOfWork.AssetTypes.AddAsync(assetType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKeys.AssetTypeList, cancellationToken);

            return Created(assetType.Id);
        }
        public async Task<BaseResponse<string>> Handle(UpdateAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);

            if (assetType is null)
                return NotFound<string>("Asset type not found");

            var duplicated = await _unitOfWork.AssetTypes.AnyAsync(t => t.TypeName == request.TypeName && t.Id != request.Id, cancellationToken);

            if (duplicated)
                return BadRequest<string>("Asset type name already exists");

            _mapper.Map(request, assetType);

            _unitOfWork.AssetTypes.UpdateAsync(assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKeys.AssetTypeList, cancellationToken);

            return Success("Updated successfully");
        }
        public async Task<BaseResponse<string>> Handle(DeleteAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);

            if (assetType is null)
                return NotFound<string>("Asset type not found");

            var isUsed = await _unitOfWork.Assets.AnyAsync(a => a.AssetTypeId == request.Id, cancellationToken);

            if (isUsed)
                return BadRequest<string>("Cannot delete this asset type because it is used by ex");

            _unitOfWork.AssetTypes.Remove(assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.AssetTypeList, cancellationToken);

            return Deleted<string>("Deleted successfully");
        }
    }
}