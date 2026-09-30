#region
using Asset.Application.Bases;
using Asset.Application.Features.AssetTypes.Queries.QueryModels;
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using System.Diagnostics.Metrics;
#endregion
namespace Asset.Application.Features.AssetTypes.Queries.QueryHandlers
{
    public class GetAssetTypeListQueryHandler : BaseResponseHandler,
                                                IRequestHandler<GetAssetTypeListQueryModel, BaseResponse<IReadOnlyList<GetAssetTypeListQueryResponse>>>,
                                                IRequestHandler<GetAssetTypeByIdQueryModel, BaseResponse<GetAssetTypeByIdQueryResponse>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public GetAssetTypeListQueryHandler(IUnitOfWork unitOfWork,IMapper mapper,IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #endregion

        #region Handler
        public async Task<BaseResponse<IReadOnlyList<GetAssetTypeListQueryResponse>>> Handle(GetAssetTypeListQueryModel request, CancellationToken cancellationToken)
        {
            var assetTypes = await _unitOfWork.AssetTypes.GetAllAsync(cancellationToken);
            var data = _mapper.Map<IReadOnlyList<GetAssetTypeListQueryResponse>>(assetTypes);
            var AssetsCount = await _unitOfWork.Assets.GetCountsByAssetTypeAsync(cancellationToken);
            foreach (var assetType in data)
            {
                var match = AssetsCount.FirstOrDefault(c => c.AssetTypeId == assetType.Id);
                assetType.AssetsCount = match?.Count ?? 0;  
            }
            return Success(data);
        }

        public async Task<BaseResponse<GetAssetTypeByIdQueryResponse>> Handle(GetAssetTypeByIdQueryModel request, CancellationToken cancellationToken)
        {
            var assetType = await _unitOfWork.AssetTypes.GetByIdAsync(request.Id, cancellationToken);

            // Throw, don't return: this query is cached, and a returned "not found"
            // would be saved in Redis. An exception is never saved.
            if (assetType is null)
                throw new NotFoundException($"Asset type {request.Id} was not found.");

            var data = _mapper.Map<GetAssetTypeByIdQueryResponse>(assetType);
            return Success(data);
        }
        #endregion
    }
}