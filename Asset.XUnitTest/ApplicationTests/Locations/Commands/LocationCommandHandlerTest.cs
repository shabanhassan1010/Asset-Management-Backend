#region
using Asset.Application.Common.Caching;
using Asset.Application.Features.Locations.Commands.CommandHandler;
using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Features.Locations.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using AssetEntity = Asset.Domain.Models.Asset;
using AutoMapper;
using Moq;
using Asset.Domain.Models;
using FluentAssertions;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Locations.Commands
{
    public class LocationCommandHandlerTest
    {
        #region Fields
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly LocationCommandHandler _handlerMock;
        private readonly CancellationToken _ct;
        #endregion

        #region Constructor
        public LocationCommandHandlerTest()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _cacheServiceMock = new();
            _locationRepositoryMock = new();

            _handlerMock = new LocationCommandHandler(_unitOfWorkMock.Object, _mapperMock.Object, _cacheServiceMock.Object);
            _unitOfWorkMock.Setup(x => x.Locations).Returns(_locationRepositoryMock.Object);
            _ct = CancellationToken.None;
        }
        #endregion

        #region Create
        [Fact]
        public async Task CreateLocation_Should_CreateLocation_Successfully()
        {
            // Arrange
            var request = new CreateLocationCommandModel
            {
                LocationName = "Alexandria", Address = "Alexandria Address"
            };

            var response = new CreateLocationResponseDto
            {
                Id = 1,  LocationName = "Alexandria", Address = "Alexandria Address", IsActive = true
            };

            var location = new Location
            {
                Id = 1, LocationName = "Alexandria", Address = "Alexandria Address" , IsActive = true
            };

           
            _mapperMock.Setup(x => x.Map<Location>(request)).Returns(location);                                 // request -> Entity
            _mapperMock.Setup(x => x.Map<CreateLocationResponseDto>(location)).Returns(response);               // Entity -> Response

            // Act
            var result = await _handlerMock.Handle(request, _ct);

            // Asserte
            result.data.Should().BeOfType<CreateLocationResponseDto>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);

            result.data.Should().BeSameAs(response);
            Assert.True(result.data.IsActive);
            Assert.True(location.IsActive);

            _mapperMock.Verify( x => x.Map<Location>(request),Times.Once);                            // Verify Mapper
            _locationRepositoryMock.Verify( x => x.Add(location), Times.Once);                        // Verify Repository
            _unitOfWorkMock.Verify( x => x.SaveChangesAsync(_ct), Times.Once);                        // Verify Save
            _cacheServiceMock.Verify( x => x.RemoveAsync(CacheKeys.LocationList, _ct), Times.Once);   // Verify Cache
            _mapperMock.Verify( x => x.Map<CreateLocationResponseDto>(location), Times.Once );        // Verify Entity -> DTO
        }
        #endregion

        #region Update

        [Fact]
        public async Task UpdateLocation_Should_UpdateLocation_Successfully()
        {
            // Arrange
            var id = 1;

            var request = new UpdateLocationCommandModel
            {
                Id = id, LocationName = "Alexandria Updated", Address = "New Address"
            };

            var response = new UpdateLocationResponseDto
            {
                LocationName = "Alexandria Updated", Address = "New Address", IsActive = true
            };

            var location = new Location
            {
                Id = id, LocationName = "Alexandria", Address = "Alexandria Address", IsActive = true
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(location);

            _mapperMock.Setup(x => x.Map<UpdateLocationResponseDto>(location)).Returns(response);

            // Act
            var result = await _handlerMock.Handle(request, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);

            Assert.Equal("Alexandria Updated", result.data.LocationName);
            Assert.Equal("New Address", result.data.Address);

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(x => x.Map(request, location), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct), Times.Once);

            // Remove keys 
            _cacheServiceMock.Verify(
                x => x.RemoveAsync(
                        It.Is<string[]>(keys => keys.Length == 2
                                             && keys.Contains(CacheKeys.LocationById(id))
                                             && keys.Contains(CacheKeys.LocationList)), _ct), Times.Once);
        }

        [Fact]
        public async Task UpdateLocation_Should_Throw_NotFoundException_When_Location_Does_Not_Exist()
        {
            // Arrange
            var id = 999;

            var request = new UpdateLocationCommandModel
            {
                Id = id,
                LocationName = "Alexandria Updated",
                Address = "New Address"
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Location?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _handlerMock.Handle(request, _ct));

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(x => x.Map(It.IsAny<UpdateLocationCommandModel>(), It.IsAny<Location>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _cacheServiceMock.Verify(x => x.RemoveAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        #endregion

        #region Delete

        [Fact]
        public async Task DeleteLocation_Should_Deactivate_Location_And_Unassign_Assets()
        {
            // Arrange
            var id = 1;

            var location = new Location
            {
                Id = id,
                LocationName = "Alexandria",
                Address = "Alexandria Address",
                IsActive = true
            };

            var assets = new List<AssetEntity>
            {
                new() { Id = 1, LocationId = id },
                new() { Id = 2, LocationId = id }
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(location);

            _unitOfWorkMock.Setup(x => x.Locations.GetTrackedAssetsByLocationAsync(id, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(assets);

            var request = new DeleteLocationCommandModel(id);

            // Act
            var result = await _handlerMock.Handle(request, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("2 asset(s) unassigned.", result.data);

            Assert.All(assets, asset => Assert.Null(asset.LocationId));

            _unitOfWorkMock.Verify(x => x.Locations.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.Locations.GetTrackedAssetsByLocationAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.Locations.Remove(location), Times.Once);              // soft delete → IsActive = false
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct), Times.Once);

            _cacheServiceMock.Verify(
                x => x.RemoveAsync(
                        It.Is<string[]>(keys => keys.Length == 2
                                             && keys.Contains(CacheKeys.LocationById(id))
                                             && keys.Contains(CacheKeys.LocationList)),
                        _ct),Times.Once);
        }

        [Fact]
        public async Task DeleteLocation_Should_Succeed_When_Location_Has_No_Assets()
        {
            // Arrange
            var id = 1;

            var location = new Location
            {
                Id = id,
                LocationName = "Alexandria",
                Address = "Alexandria Address",
                IsActive = true
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(location);

            _unitOfWorkMock.Setup(x => x.Locations.GetTrackedAssetsByLocationAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<AssetEntity>());

            var request = new DeleteLocationCommandModel(id);

            // Act
            var result = await _handlerMock.Handle(request, _ct);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("0 asset(s) unassigned.", result.data);

            _locationRepositoryMock.Verify(x => x.Remove(location), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task DeleteLocation_Should_Throw_NotFoundException_When_Location_Does_Not_Exist()
        {
            // Arrange
            var id = 999;

            _locationRepositoryMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Location?)null);

            var request = new DeleteLocationCommandModel(id);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _handlerMock.Handle(request, _ct));

            _locationRepositoryMock.Verify(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);

            _locationRepositoryMock.Verify(x => x.GetTrackedAssetsByLocationAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _locationRepositoryMock.Verify(x => x.Remove(It.IsAny<Location>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _cacheServiceMock.Verify(x => x.RemoveAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}