using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Features.Locations.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
namespace Asset.Application.Features.Locations.Commands.CommandHandler
{
    public class LocationCommandHandler :
                                            IRequestHandler<CreateLocationCommandModel, ApiResponse<CreateLocationResponseDto>>,
                                            IRequestHandler<UpdateLocationCommandModel, ApiResponse<UpdateLocationResponseDto>>,
                                            IRequestHandler<DeleteLocationCommandModel, ApiResponse<string>>
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        #endregion

        #region Constructor
        public LocationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }
        #endregion


        #region handlers
        public async Task<ApiResponse<CreateLocationResponseDto>> Handle( CreateLocationCommandModel request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Location>(request);
            entity.IsActive = true;

            await _unitOfWork.Locations.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.LocationList, cancellationToken);
            return new ApiResponse<CreateLocationResponseDto>
            {
                data = _mapper.Map<CreateLocationResponseDto>(entity),
                Success = true,
                Message = "Location Created Successfully"
            };
        }

        public async Task<ApiResponse<UpdateLocationResponseDto>> Handle(UpdateLocationCommandModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Location {request.Id} was not found.");

            _mapper.Map(request, entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.LocationById(request.Id), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.LocationList, cancellationToken);
            return new ApiResponse<UpdateLocationResponseDto>
            {
                data = _mapper.Map<UpdateLocationResponseDto>(entity),
                Success = true,
                Message = "Location Updated Successfully"
            };
        }

        public async Task<ApiResponse<string>> Handle(DeleteLocationCommandModel request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"Location {request.Id} was not found.");

            // Assets associated with this location must be unassigned before deactivation.
            var assets = await _unitOfWork.Locations.GetTrackedAssetsByLocationAsync(request.Id, cancellationToken);

            foreach (var asset in assets)
            {
                asset.LocationId = null;
            }

            _unitOfWork.Locations.Remove(entity);   // IsActive = false

            // One SaveChanges call => both operations succeed or fail together.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.LocationById(request.Id), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.LocationList, cancellationToken);
            return new ApiResponse<string>
            {
                data = $"{assets.Count} asset(s) unassigned.",
                Success = true,
                Message = "Location Deactivated Successfully"
            };
        }
        #endregion
    }
}
