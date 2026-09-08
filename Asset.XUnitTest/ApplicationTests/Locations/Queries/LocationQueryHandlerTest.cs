using Asset.Application.Features.Locations.Queries.QueryHandlers;
using Asset.Application.Interfaces.Comman;
using AutoMapper;
using Moq;

namespace Asset.XUnitTest.ApplicationTests.Locations.Queries
{
    public class LocationQueryHandlerTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<IMapper> _mapper;
        public LocationQueryHandlerTest()
        {
            _unitOfWork = new();
            _mapper = new();
        }
        [Fact]
        public Task StudentList_Should_notNUll_And_NotEmpty()
        {
            var handler = new LocationQueryHandler(_unitOfWork.Object,_mapper.Object);
        }
    }
}
