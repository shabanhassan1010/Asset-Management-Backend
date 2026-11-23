#region 
using Asset.Application.Bases;
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AssetTransfers.Queries.QueryModels;
using Asset.Application.Features.AssetTransfers.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resources;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion

namespace Asset.Application.Features.AssetTransfers.Queries.QueryHandler
{
    public class GetAssetTransferHistoryQueryHandler : BaseResponseHandler,
                                                       IRequestHandler<GetAssetTransferHistoryQueryModel, BaseResponse<IReadOnlyList<GetAssetTransferHistoryResponse>>> ,
                                                       IRequestHandler<GetTransferDetailsQueryModel, BaseResponse<GetTransferDetailsResponse>>
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
            var assetExists = await _unitOfWork.Assets.ExistsAsync(request.AssetId, cancellationToken);
            if (!assetExists)
                throw new NotFoundException("Asset {0} was not found.", request.AssetId);

            var transfers = await _unitOfWork.AssetTransfers.GetByAssetIdAsync(request.AssetId, cancellationToken);

            var data = _mapper.Map<IReadOnlyList<GetAssetTransferHistoryResponse>>(transfers);

            return Success(data);
        }

        public async Task<BaseResponse<GetTransferDetailsResponse>> Handle(GetTransferDetailsQueryModel request, CancellationToken cancellationToken)
        {
            var transfer = await _unitOfWork.AssetTransfers.GetDetailsAsync(request.AssetId, request.TransferId, cancellationToken);

            if (transfer is null)
                // Thrown (not returned) like every other "not found": the middleware turns it into a
                // translated 404 — a message with the ids built in could not be translated.
                throw new NotFoundException("Transfer {0} was not found for asset {1}.", request.TransferId, request.AssetId);

            var response = _mapper.Map<GetTransferDetailsResponse>(transfer);

            return Success(response);
        }
        #endregion
    }
}