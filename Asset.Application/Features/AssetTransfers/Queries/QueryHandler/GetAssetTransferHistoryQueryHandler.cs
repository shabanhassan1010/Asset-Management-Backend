#region 
using Asset.Application.Bases;
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AssetTransfers.Queries.QueryModels;
using Asset.Application.Features.AssetTransfers.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resoures;
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
        private readonly IUserRepository _userRepository;
        #endregion

        #region Constructor
        public GetAssetTransferHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, IUserRepository userRepository, IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userRepository = userRepository;
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

        public async Task<BaseResponse<GetTransferDetailsResponse>> Handle(GetTransferDetailsQueryModel request, CancellationToken cancellationToken)
        {
            // 1. Transfer + asset + departments + employees + locations (AssetManagementDbContext)
            var transfer = await _unitOfWork.AssetTransfers
                .GetDetailsAsync(request.AssetId, request.TransferId, cancellationToken);

            if (transfer is null)
                return NotFound<GetTransferDetailsResponse>(
                    $"Transfer {request.TransferId} was not found for asset {request.AssetId}.");

            var response = _mapper.Map<GetTransferDetailsResponse>(transfer);

            // 2. Admin name lives in AppIdentityDbContext → can't Include it,
            //    so we fetch it with one separate call.
            if (!string.IsNullOrEmpty(transfer.TransferredByUserId))
            {
                var admin = await _userRepository.GetByIdAsync(transfer.TransferredByUserId, cancellationToken);
                response.TransferredByName = admin?.UserName;
            }

            return Success(response);
        }
        #endregion
    }
}
