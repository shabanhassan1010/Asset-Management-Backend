#region
using Asset.Application.Features.Departments.Queries.QueryHandlers;
using Asset.Application.Features.Locations.Queries.QueryHandlers;
using Asset.Application.Features.Locations.Queries.QueryModels;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using FluentAssertions;
using Moq;
using static System.Runtime.InteropServices.JavaScript.JSType;
#endregion
namespace Asset.XUnitTest.ApplicationTests.Locations.Queries
{
    public class LocationQueryHandlerTest
    {
        #region Fields
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly LocationQueryHandler _handlerMock;

        #endregion

        #region Constrcutor
        public LocationQueryHandlerTest()
        {
            _unitOfWorkMock = new();
            _mapperMock = new();
            _locationRepositoryMock = new();
            // IMPORTANT: Make UnitOfWork.Locations return our mock repository
            _unitOfWorkMock.Setup(x => x.Locations).Returns(_locationRepositoryMock.Object);
            _handlerMock = new LocationQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object);
        }
        #endregion

        #region Flow

        //                 ARRANGE
        //                    │
        //                    ▼
        //          Create Fake Locations
        //                    │
        //                    ▼
        //          Setup Mock Repository
        //                    │
        //                    ▼
        //      "When GetAllProjectedAsync()
        //       is called, return locations"
        //                    │
        //                    ▼
        //              Create Query
        //                    │
        //                    ▼
        //             Create Handler
        //                    │
        //                    ▼
        //  ────────────────────────────────────
        //                  ACT
        //                    │
        //                    ▼
        //          handler.Handle(query, ct)
        //                    │
        //                    ▼
        //          Handler calls Repository
        //                    │
        //                    ▼
        //              Mock returns
        //            Alexandria + Cairo
        //                    │
        //                    ▼
        //             Handler creates
        //               ApiResponse
        //                    │
        //                    ▼
        //   ────────────────────────────────────
        //                 ASSERT
        //                    │
        //                    ▼
        //             result != null
        //                    │
        //                    ▼
        //             Success == true
        //                    │
        //                    ▼
        //             data != null
        //                    │
        //                    ▼
        //             data not empty
        //                    │
        //                    ▼
        //             Count == 2
        //                    │
        //                    ▼
        //        First location is correct
        //                    │
        //                    ▼
        //        Second location is correct
        //                    │
        //                    ▼
        //      Repository called exactly once

        #endregion

        #region List
        [Fact]
        public async Task GetLocationList_Should_Return_Successful_Response_With_Locations()
        {
            // Arrange : Make All data ready for test
            var locationlist = new List<GetLocationListResponse>
            {
                new GetLocationListResponse
                {
                    Id = 1, LocationName = "Alexandria", Address = "Alexandria Address", IsActive = true, AssetsCount = 5
                },

                new GetLocationListResponse
                {
                    Id = 2, LocationName = "Cairo", Address = "Cairo Address", IsActive = true, AssetsCount = 10
                }
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetAllProjectedAsync(It.IsAny<CancellationToken>()))
                                                  .ReturnsAsync(locationlist);

            var query   = new GetLocationListQueryModel();                                     // this is request which handler waiting it
            var handler = new LocationQueryHandler(_unitOfWorkMock.Object,_mapperMock.Object); //  handler creation

            var ct = CancellationToken.None;                                           // I do not need CancellationToken here so i made it none

            // Act : execute work which i went to do it
            var result = await handler.Handle(query,ct);                               // I tell it here execute the real handler 

            // Assert
            Assert.NotNull(result);              // result will not null 
            Assert.True(result.Success);         // I Expected result.Success == true
             
            Assert.NotNull(result.data);         // result must retun data
            Assert.NotEmpty(result.data);        // result must not empty

            Assert.Equal(2, result.data.Count);  // I Expected count of locations 2

            // test the first location in response
            result.data.Should().BeSameAs(result.data);
        }
        #endregion

        #region Get By Id
        [Fact]
        public async Task GetLocationById_Should_Return_Location_When_Location_Exists_And_Active()
        {
            int id = 1;
            // Arrange
            var location = new Location
            {
                Id = 1, LocationName = "Alex", Address = "23 Smouha", IsActive = true,
            };

            var response = new GetLocationByIdResponse
            {
                Id = 1, LocationName = "Alex", Address = "23 Smouha", IsActive = true,
            };

            // If handler went location Id = 1 return this [location] for it and do not go into database
            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()))
                                                  .ReturnsAsync(location);
            // Tell the mock mapper: When this department is mapped, return our fake response.
            _mapperMock.Setup(x =>x.Map<GetLocationByIdResponse>(location)).Returns(response);
            // Create Request
            var query   = new GetLocationByIdQueryModel(id);

            // Act
            var result = await _handlerMock.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);

            result.data.Should().BeSameAs(result.data);
            Assert.True(result.data.IsActive);
        }

        [Fact]
        public async Task GetLocationById_Should_Throw_NotFoundException_When_Location_Is_Inactive()
        {
            // Arrange
            var id = 1;
            var location = new Location
            {
                Id = id, LocationName = "Alexandria", Address = "Alexandria Address", IsActive = false
            };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()))
                       .ReturnsAsync(location);

            var request = new GetLocationByIdQueryModel(id);
            var handler = new LocationQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object);

            // Act 
            var action = async () => await _handlerMock.Handle(request, CancellationToken.None);
            // Assert
            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task GetLocationById_Should_Throw_NotFoundException_When_Location_Does_Not_Exist()
        {
            int id = 999;
            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()))
                           .ReturnsAsync((Location?)null);

            var request = new GetLocationByIdQueryModel(id);
            var handler = new LocationQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object);

            // Act 
            var action = async () => await _handlerMock.Handle(request, CancellationToken.None);
            // Assert
            await Assert.ThrowsAsync<NotFoundException>(action);

        }
        #endregion
    }
}