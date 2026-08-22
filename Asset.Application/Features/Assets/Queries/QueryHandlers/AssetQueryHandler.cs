using Asset.Application.Bases;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Features.Assets.Queries.QueryModels;
using Asset.Application.Features.Assets.Queries.QueryResponses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resoures;
using Asset.Domain.Common;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;

namespace Asset.Application.Features.Assets.Queries.QueryHandlers
{
    public class AssetQueryHandler : BaseResponseHandler, IRequestHandler<GetAssetByIdQueryModel, GetByIdQueryResponse>
                                                        , IRequestHandler<GetAssetListQueryModel, List<GetAssetListQueryResponse>>
                                                        , IRequestHandler<GetAssetPaginatedListQueryModel, PaginatedResponse<GetAssetPaginatedListQueryResponse>>
    {
        #region Fields
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public AssetQueryHandler(ICurrentUser currentUser, IUnitOfWork unitOfWork, IMapper mapper, IStringLocalizer<SharedResources> localizer) : base(localizer)
        {
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #endregion

        #region Handlers
        public async Task<GetByIdQueryResponse> Handle(GetAssetByIdQueryModel request, CancellationToken cancellationToken)
        {
            var asset = await _unitOfWork.Assets.GetByIdWithDetailsAsync(request.Id, cancellationToken);

            if (asset == null)
            {
                throw new NotFoundException($"Asset {request.Id} was not found.");
            }

            var dto = _mapper.Map<GetByIdQueryResponse>(asset);
            return dto;
        }

        public async Task<List<GetAssetListQueryResponse>> Handle(GetAssetListQueryModel request, CancellationToken cancellationToken)
        {
            var assets = await _unitOfWork.Assets.ListAllAsync(cancellationToken);

            var response = _mapper.Map<List<GetAssetListQueryResponse>>(assets);

            return response;
        }

        public async Task<PaginatedResponse<GetAssetPaginatedListQueryResponse>> Handle(GetAssetPaginatedListQueryModel request, CancellationToken cancellationToken)
        {
            var filter = new AssetFilter
            {
                Page = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
                CategoryId = request.CategoryId,
                AssetTypeId = request.AssetTypeId,
                StatusId = request.StatusId,
                DepartmentId = request.DepartmentId,
                LocationId = request.LocationId,
                EmployeeId = request.EmployeeId,
                SortBy = request.SortBy,
                SortDesc = request.SortDesc
            };

            var result = await _unitOfWork.Assets.GetPaginationAsync(filter, cancellationToken);

            var items = _mapper.Map<List<GetAssetPaginatedListQueryResponse>>(result.Items);
            if (!_currentUser.IsAdmin)
            {
                foreach (var item in items)
                {
                    item.PurchaseCost = null;
                }
            }
            return new PaginatedResponse<GetAssetPaginatedListQueryResponse>(result.Page, result.PageSize, result.TotalCount, items);
        }
        #endregion
    }
}