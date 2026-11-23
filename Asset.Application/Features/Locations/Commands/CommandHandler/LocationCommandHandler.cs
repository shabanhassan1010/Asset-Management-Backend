#region
using Asset.Application.Common;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Features.Locations.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
#endregion
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
        public async Task<ApiResponse<CreateLocationResponseDto>> Handle(CreateLocationCommandModel request, CancellationToken cancellationToken)
        {
            var name = request.LocationName.Trim();

            if (await _unitOfWork.Locations.LocationNameExistsAsync(name, null, cancellationToken))
                throw new ConflictException("Location with name '{0}' already exists.", name);

            var nameAr = LocalizedName.TrimOrNull(request.LocationNameAr);
            if (nameAr is not null && await _unitOfWork.Locations.LocationNameArExistsAsync(nameAr, null, cancellationToken))
                throw new ConflictException("Arabic location name '{0}' is already in use.", nameAr);

            var entity = _mapper.Map<Location>(request);
            entity.LocationName = name;
            entity.LocationNameAr = nameAr;
            entity.IsActive = true;

            _unitOfWork.Locations.Add(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

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
            if (entity is null || !entity.IsActive)
                throw new NotFoundException("Location {0} was not found.", request.Id);

            var name = request.LocationName.Trim();

            if (await _unitOfWork.Locations.LocationNameExistsAsync(name, request.Id, cancellationToken))
                throw new BusinessException("A location with this name already exists.");

            var nameAr = LocalizedName.TrimOrNull(request.LocationNameAr);
            if (nameAr is not null && await _unitOfWork.Locations.LocationNameArExistsAsync(nameAr, request.Id, cancellationToken))
                throw new BusinessException("Arabic location name '{0}' is already in use.", nameAr);

            _mapper.Map(request, entity);
            entity.LocationName = name;
            entity.LocationNameAr = nameAr;      // "" from the form is saved as NULL

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

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
            if (entity is null || !entity.IsActive)
                throw new NotFoundException("Location {0} was not found.", request.Id);

            // Assets associated with this location must be unassigned before deactivation.
            var assets = await _unitOfWork.Locations.GetTrackedAssetsByLocationAsync(request.Id, cancellationToken);

            foreach (var asset in assets)
            {
                asset.LocationId = null;
            }

            _unitOfWork.Locations.DeActivate(entity);   // IsActive = false
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();

            return new ApiResponse<string>
            {
                data = $"{assets.Count} asset(s) unassigned.",
                Success = true,
                Message = "Location Deactivated Successfully"
            };
        }
        #endregion

        #region Private Methods
        private async Task InvalidateAsync()
        {
            await _cache.RemoveAsync(CacheKeys.LocationList);
            await _cache.BumpVersionAsync(CacheKeys.LocationsVersion);
            // Asset rows and asset details show this location's name and are cached under "assets:version".
            // Without this bump, the asset list keeps the OLD name until its cache expires.
            await _cache.BumpVersionAsync(CacheKeys.AssetsVersion);
        }
        #endregion
    }
}