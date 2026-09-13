#region
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
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        #endregion

        #region Constrcutor
        public LocationQueryHandlerTest()
        {
            _unitOfWork = new();
            _mapper = new();
            _locationRepositoryMock = new();
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

            _unitOfWork.Setup(x => x.Locations.GetAllProjectedAsync(It.IsAny<CancellationToken>()))
                                              .ReturnsAsync(locationlist);

            var query   = new GetLocationListQueryModel();                              // this is request which handler waiting it
            var handler = new LocationQueryHandler(_unitOfWork.Object,_mapper.Object); //  handler creation

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
            Assert.Equal(1, result.data[0].Id);   
            Assert.Equal("Alexandria", result.data[0].LocationName);
            Assert.Equal(5, result.data[0].AssetsCount);

            // test the second location in response
            Assert.Equal(2, result.data[1].Id);
            Assert.Equal("Cairo", result.data[1].LocationName);
            Assert.Equal(10, result.data[1].AssetsCount);

            // I check if Handler call repository or not
            _unitOfWork.Verify(x => x.Locations.GetAllProjectedAsync(It.IsAny<CancellationToken>()),Times.Once);   // Times.Once: call it only one call 
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
            _unitOfWork.Setup(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()))
                                              .ReturnsAsync(location); 

            _mapper.Setup(x =>x.Map<GetLocationByIdResponse>(location)).Returns(response);
            var query   = new GetLocationByIdQueryModel(id);
            var handler = new LocationQueryHandler(_unitOfWork.Object,_mapper.Object);

            var ct = CancellationToken.None;

            // Act
            var result = await handler.Handle(query, ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);

            Assert.Equal(1, result.data.Id);
            Assert.Equal("Alex", result.data.LocationName);
            Assert.Equal("23 Smouha", result.data.Address);
            Assert.True(result.data.IsActive);

            _unitOfWork.Verify(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()),Times.Once);
            _mapper.Verify(x => x.Map<GetLocationByIdResponse>(location),Times.Once);
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

            _unitOfWork.Setup(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()))
                       .ReturnsAsync(location);

            var query = new GetLocationByIdQueryModel(id);
            var handler = new LocationQueryHandler(_unitOfWork.Object, _mapper.Object);
            var ct = CancellationToken.None;


            // Act 
            await Assert.ThrowsAsync<NotFoundException>( () => handler.Handle(query, ct));

            // Assert
            _unitOfWork.Verify(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()), Times.Once );
            _mapper.Verify( x => x.Map<GetLocationByIdResponse>( It.IsAny<Location>()), Times.Never );  // in case location is not active must handler do not use mapping
        }

        [Fact]
        public async Task GetLocationById_Should_Throw_NotFoundException_When_Location_Does_Not_Exist()
        {
            int id = 999;
            _unitOfWork.Setup(x => x.Locations.GetByIdAsync(id,It.IsAny<CancellationToken>()))
                                   .ReturnsAsync((Location?)null);

            var query = new GetLocationByIdQueryModel(id);
            var handler = new LocationQueryHandler(_unitOfWork.Object,_mapper.Object);

            var ct = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, ct));

            _unitOfWork.Verify(x => x.Locations.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(x => x.Map<GetLocationByIdResponse>(It.IsAny<Location>()), Times.Never);

        }
        #endregion
    }
}