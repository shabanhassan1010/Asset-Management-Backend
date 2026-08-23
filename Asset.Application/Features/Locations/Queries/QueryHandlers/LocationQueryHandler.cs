using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Queries.QueryModels;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;

namespace Asset.Application.Features.Locations.Queries.QueryHandlers
{
    public class LocationQueryHandler :
                                        IRequestHandler<GetLocationListQueryModel, ApiResponse<IReadOnlyList<GetLocationListResponse>>>,
                                        IRequestHandler<GetLocationByIdQueryModel, ApiResponse<GetLocationByIdResponse>>
    {
        #region Fields
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public LocationQueryHandler(ILocationRepository locationRepository, IMapper mapper)
        {
            _locationRepository = locationRepository;
            _mapper = mapper;
        }
        #endregion

        public async Task<ApiResponse<IReadOnlyList<GetLocationListResponse>>> Handle(GetLocationListQueryModel request, CancellationToken cancellationToken)
        {
            var list = await _locationRepository.GetAllProjectedAsync(cancellationToken);

            return new ApiResponse<IReadOnlyList<GetLocationListResponse>>
            {
                data = list,
                Success = true,
                Message = "Locations Retrieved Successfully"
            };
        }

        public async Task<ApiResponse<GetLocationByIdResponse>> Handle(GetLocationByIdQueryModel request, CancellationToken cancellationToken)
        {
            var entity = await _locationRepository.GetByIdAsync(request.Id, cancellationToken);

            if (entity == null || entity.IsActive == false)
                throw new NotFoundException($"Location {request.Id} was not found.");

            return new ApiResponse<GetLocationByIdResponse>
            {
                data = _mapper.Map<GetLocationByIdResponse>(entity),
                Success = true,
                Message = "Location Retrieved Successfully"
            };
        }
    }
}