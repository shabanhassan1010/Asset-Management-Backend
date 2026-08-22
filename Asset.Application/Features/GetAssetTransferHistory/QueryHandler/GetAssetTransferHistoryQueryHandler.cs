#region 
using Asset.Application.Bases;
using Asset.Application.Common.Responses;
using Asset.Application.Features.GetAssetTransferHistory.QueryModels;
using Asset.Application.Features.GetAssetTransferHistory.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion

namespace Asset.Application.Features.GetAssetTransferHistory.QueryHandler
{
    public class GetAssetTransferHistoryQueryHandler : BaseResponseHandler,
                                                       IRequestHandler<GetAssetTransferHistoryQueryModel, BaseResponse<IReadOnlyList<GetAssetTransferHistoryResponse>>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public GetAssetTransferHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #endregion


        #region Handlers
        public async Task<BaseResponse<IReadOnlyList<GetAssetTransferHistoryResponse>>> Handle(GetAssetTransferHistoryQueryModel request, CancellationToken cancellationToken)
        {
            // Use ExistsAsync instead of ExistsActiveAsync:
            // A retired asset remains in the system history, and its transfer history must remain accessible (R2.5 / R3.2).
            // If we use ExistsActiveAsync, the history will return 404 as soon as the asset is retired.
            var assetExists = await _unitOfWork.Assets.ExistsAsync(request.AssetId, cancellationToken);
            if (!assetExists)
                throw new NotFoundException($"Asset {request.AssetId} was not found.");

            var transfers = await _unitOfWork.AssetTransfers.GetByAssetIdAsync(request.AssetId, cancellationToken);

            // Two separate steps:
            // 1) AutoMapper -> maps the entities to DTOs using the mapping defined in the Profile.
            // 2) Success    -> wraps the result in ApiResponse through BaseResponseHandler.

            var data = _mapper.Map<IReadOnlyList<GetAssetTransferHistoryResponse>>(transfers);

            return Success(data);
        }
        #endregion
    }
}
