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
                                            IRequestHandler<DeleteLocationCommandModel, ApiResponse<string>>,
                                            IRequestHandler<ActivateLocationCommandModel, ApiResponse<string>>
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

        public async Task<ApiResponse<string>> Handle(ActivateLocationCommandModel request, CancellationToken cancellationToken)
        {
            // GetByIdAsync returns inactive rows too (it is a plain find by id), and the entity is tracked,
            // so changing IsActive and calling SaveChanges is enough.
            var entity = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
                throw new NotFoundException("Location {0} was not found.", request.Id);

            // Nothing to do: answer OK instead of an error, so clicking twice is harmless.
            if (entity.IsActive)
                return new ApiResponse<string>
                {
                    data = entity.LocationName,
                    Success = true,
                    Message = "Location is already active"
                };

            // The names are UNIQUE among ACTIVE rows only (filtered indexes ..._Active).
            // While this row was inactive someone may have created a new one with the same name.
            // Turning this one back on would then break the index, so we refuse with a clear message.
            if (await _unitOfWork.Locations.LocationNameExistsAsync(entity.LocationName, entity.Id, cancellationToken))
                throw new ConflictException("Another active location is already named '{0}'. Rename one of them first.", entity.LocationName);

            if (entity.LocationNameAr is not null &&
                await _unitOfWork.Locations.LocationNameArExistsAsync(entity.LocationNameAr, entity.Id, cancellationToken))
                throw new ConflictException("Another active location already uses the Arabic name '{0}'. Rename one of them first.", entity.LocationNameAr);

            entity.IsActive = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateAsync();     // the dropdowns and every page of the list must show it again

            return new ApiResponse<string>
            {
                data = entity.LocationName,
                Success = true,
                Message = "Location Activated Successfully"
            };
        }

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