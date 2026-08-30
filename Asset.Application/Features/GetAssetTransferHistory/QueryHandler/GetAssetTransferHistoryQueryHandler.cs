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
            var assetExists = await _unitOfWork.Assets.ExistsAsync(request.AssetId, cancellationToken);
            if (!assetExists)
                throw new NotFoundException($"Asset {request.AssetId} was not found.");

            var transfers = await _unitOfWork.AssetTransfers.GetByAssetIdAsync(request.AssetId, cancellationToken);

            var data = _mapper.Map<IReadOnlyList<GetAssetTransferHistoryResponse>>(transfers);

            return Success(data);
        }
        #endregion
    }
}
