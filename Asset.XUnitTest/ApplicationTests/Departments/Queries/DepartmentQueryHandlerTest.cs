#region
using Asset.Application.Features.Departments.Queries.QueryHandlers;
using Asset.Application.Features.Departments.Queries.QueryModels;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Resoures;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using Microsoft.Extensions.Localization;
using Moq;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Departments.Queries
{
    public class DepartmentQueryHandlerTest
    {
        #region Fields
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDepartmentRepository> _DepartmentRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IStringLocalizer<SharedResources>> localizeMock;
        private readonly DepartmentQueryHandler _handlerMock;
        #endregion

        #region Constructor
        public DepartmentQueryHandlerTest()
        {
            _unitOfWorkMock = new();
            _DepartmentRepositoryMock = new();
            _mapperMock = new();
            localizeMock = new ();
            // IMPORTANT: Make UnitOfWork.Departments return our mock repository
            _unitOfWorkMock.Setup(x => x.Departments).Returns(_DepartmentRepositoryMock.Object);
            _handlerMock = new DepartmentQueryHandler(_unitOfWorkMock.Object, _mapperMock.Object , localizeMock.Object);         
        }
        #endregion

        #region GetList

        [Fact]
        public async Task GetDepartmentList_Should_Return_Successful_Response_With_Departments()
        {
            // Arrange : Create Fake list and do not go into Databse
            var departments = new List<GetDepartmentListResponse>
            {
                new GetDepartmentListResponse
                {
                    Id = 1, DepartmentName = "IT Department", Code = "IT", AssetsCount = 5 , EmployeesCount = 2
                },

                new GetDepartmentListResponse
                {
                    Id = 2, DepartmentName = "HR Department", Code = "HR", AssetsCount = 10 , EmployeesCount = 4
                }
            };

            // When the handler asks the repository for departments, return my fake list.
            // and in this line he will call    >>>>>    [var list = await _unitOfWork.Departments.GetAllProjectedAsync(cancellationToken);]  and it will excute in fake list not database
            _unitOfWorkMock.Setup(x => x.Departments.GetAllProjectedAsync(It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(departments);
            // Create Request
            var request = new GetDepartmentListQueryModel();

            // Act : Call or go to handler
            var result = await _handlerMock.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);

            Assert.NotNull(result.data);
            Assert.NotEmpty(result.data);
            Assert.Equal(2, result.data.Count);

            Assert.Equal(1, result.data[0].Id);
            Assert.Equal("IT Department", result.data[0].DepartmentName);
            Assert.Equal(5, result.data[0].AssetsCount);
            Assert.Equal(2, result.data[0].EmployeesCount);

            Assert.Equal(2, result.data[1].Id);
            Assert.Equal("HR Department", result.data[1].DepartmentName);
            Assert.Equal(10, result.data[1].AssetsCount);
            Assert.Equal(4, result.data[1].EmployeesCount);
            Assert.Equal("Departments Retrieved Successfully",result.Message);
        }

        [Fact]
        public async Task GetDepartmentList_Should_Return_Empty_List_When_No_Departments_Exist()
        {
            // Arrange
            _unitOfWorkMock.Setup(x => x.Departments.GetAllProjectedAsync(It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(new List<GetDepartmentListResponse>());
            // Create Request
            var request = new GetDepartmentListQueryModel();

            // Act : Call or go to handler
            var result = await _handlerMock.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);
            Assert.Empty(result.data);
        }

        #endregion

        #region GetById
        [Fact]
        public async Task GetDepartmentById_Should_Return_Department_When_Department_Exists_And_Active()
        {
            // Arrange
            var id = 1;
            // Create Fake Department entity
            var department = new Department
            {
                Id = id,
                DepartmentName = "IT Department",
                Code = "IT",
                IsActive = true
            };
            // Create the expected DTO returned by AutoMapper
            var response = new GetDepartmentByIdResponse
            {
                Id = id,
                DepartmentName = "IT Department",
                Code = "IT",
            };

            // Tell the mock repository:  When GetByIdAsync(1, ...) is called, >>> return our fake department instead of going to the database.
            // It.IsAny<CancellationToken>()): Mean Accept any CancellationToken
            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                           .ReturnsAsync(department);

            // Tell the mock mapper: When this department is mapped, return our fake response.
            _mapperMock.Setup(x => x.Map<GetDepartmentByIdResponse>(department)).Returns(response);

            // Create Request
            var request = new GetDepartmentByIdQueryModel(id);

            // Act: Call the real handler
            var result = await _handlerMock.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);                    // The response should not be null
            Assert.True(result.Success);               // The operation should be successful
            Assert.NotNull(result.data);               // Data should exist

            Assert.Equal(id, result.data.Id);
            Assert.Equal("IT Department", result.data.DepartmentName);
            Assert.Equal("IT", result.data.Code);
        }

        [Fact]
        public async Task GetDepartmentById_Should_Throw_NotFoundException_When_Department_Is_Inactive()
        {
            // Arrange
            var id = 1;
            // He is Exit but not active
            var department = new Department
            {
                Id = id,
                DepartmentName = "IT Department",
                Code = "IT",
                IsActive = false
            };

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(department);

            var request = new GetDepartmentByIdQueryModel(id);

            // Act 
            var action = async () => await _handlerMock.Handle(request, CancellationToken.None);
            // Assert
            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        [Fact]
        public async Task GetDepartmentById_Should_Throw_NotFoundException_When_Department_Does_Not_Exist()
        {
            // Arrange
            var id = 999;

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Department?)null);

            var request = new GetDepartmentByIdQueryModel(id);

            // Act

            var action = async () => await _handlerMock.Handle(request,CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<NotFoundException>(action);
        }

        #endregion
    }
}