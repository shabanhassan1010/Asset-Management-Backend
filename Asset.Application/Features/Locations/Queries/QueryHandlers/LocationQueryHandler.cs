#region
using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using Asset.Application.Features.Locations.Queries.QueryModels;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Locations.Queries.QueryHandlers
{
    public class LocationQueryHandler :
                                        IRequestHandler<GetLocationListQueryModel,      ApiResponse<IReadOnlyList<GetLocationListResponse>>>,
                                        IRequestHandler<GetLocationByIdQueryModel,      ApiResponse<GetLocationByIdResponse>> ,
                                        IRequestHandler<GetLocationPaginatedModelQuery, ApiResponse<PagedResult<GetLocationListResponse>>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public LocationQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        #endregion

        public async Task<ApiResponse<IReadOnlyList<GetLocationListResponse>>> Handle(GetLocationListQueryModel request, CancellationToken cancellationToken)
        {
            var list = await _unitOfWork.Locations.GetAllProjectedAsync(cancellationToken);

            return new ApiResponse<IReadOnlyList<GetLocationListResponse>>
            {
                data = list,
                Success = true,
                Message = "Locations Retrieved Successfully"
            };
        }

        public async Task<ApiResponse<GetLocationByIdResponse>> Handle(GetLocationByIdQueryModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);

            if (entity == null || entity.IsActive == false)
                throw new NotFoundException($"Location {request.Id} was not found.");

            return new ApiResponse<GetLocationByIdResponse>
            {
                data = _mapper.Map<GetLocationByIdResponse>(entity),
                Success = true,
                Message = "Location Retrieved Successfully"
            };
        }

        public async Task<ApiResponse<PagedResult<GetLocationListResponse>>> Handle(GetLocationPaginatedModelQuery request, CancellationToken cancellationToken)
        {
            var (locations, totalCount) = await _unitOfWork.Locations.GetPagedAsync(request.Search, request.IsActive,
                                                                      request.PageNumber, request.PageSize, cancellationToken);
            var mapper = _mapper.Map<List<GetLocationListResponse>>(locations);
            var result = new PagedResult<GetLocationListResponse>
            {
                Items = mapper,
                TotalCount = totalCount,
                Page = request.PageNumber,
                PageSize = request.PageSize
            };

            return new ApiResponse<PagedResult<GetLocationListResponse>>
            {
                data = result,
                Success = true,
                Message = "Employees Retrieved Successfully"
            };
        }
    }
}