#region
using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
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
            var assetType = _mapper.Map<AssetType>(request);
            assetType.IsActive = true;

            _unitOfWork.AssetTypes.Add(assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.AssetTypeList, cancellationToken);

            return Created(assetType.Id);
        }
        public async Task<BaseResponse<string>> Handle(UpdateAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);

            if (assetType is null)
                throw new NotFoundException("Asset type not found");

            _mapper.Map(request, assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveAsync(new[] { CacheKeys.AssetTypeById(request.Id), CacheKeys.AssetTypeList }, cancellationToken);

            return Success("Updated successfully");
        }
        public async Task<BaseResponse<string>> Handle(DeleteAssetTypeCommandModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);
            if (assetType is null)
                return NotFound<string>("Asset type not found");

            var isUsed = await _unitOfWork.Assets.AnyAsync(a => a.AssetTypeId == request.Id, cancellationToken);
            if (isUsed)
                return BadRequest<string>("This asset type is in use. Delete or reassign its assets first.");

            _unitOfWork.AssetTypes.Remove(assetType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cacheService.RemoveAsync(new[] { CacheKeys.AssetTypeById(request.Id), CacheKeys.AssetTypeList }, cancellationToken);

            return Deleted<string>("Deleted successfully");
        }
    }
}