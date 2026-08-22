#region
using Asset.Application.Bases;
using Asset.Application.Features.AssetTypes.Queries.QueryModels;
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resoures;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
#endregion
namespace Asset.Application.Features.AssetTypes.Queries.QueryHandlers
{
    public class GetAssetTypeListQueryHandler : BaseResponseHandler,
                                                IRequestHandler<GetAssetTypeListQueryModel, BaseResponse<IReadOnlyList<GetAssetTypeListQueryResponse>>>
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

            return Success(data);
        }
        #endregion
    }
}
