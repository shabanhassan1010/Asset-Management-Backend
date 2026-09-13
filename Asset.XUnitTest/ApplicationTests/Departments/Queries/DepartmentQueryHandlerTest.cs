using Asset.Application.Features.Departments.Queries.QueryHandlers;
using Asset.Application.Features.Departments.Queries.QueryModels;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using Microsoft.Extensions.Localization;
using Moq;

namespace Asset.XUnitTest.ApplicationTests.Departments.Queries
{
    public class DepartmentQueryHandlerTest
    {
        #region Fields
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<IStringLocalizer> localizeMock;
        private readonly DepartmentQueryHandler _handler;
        private readonly CancellationToken _ct;
        #endregion

        #region Constructor
        public DepartmentQueryHandlerTest()
        {
            _unitOfWork = new();
            _mapper = new();
            localizeMock = new ();
            _handler = new DepartmentQueryHandler(_unitOfWork.Object, _mapper.Object , localizeMock.Object);
            _ct = CancellationToken.None;
        }
        #endregion

        #region GetList

        [Fact]
        public async Task GetDepartmentList_Should_Return_Successful_Response_With_Departments()
        {
            // Arrange
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

            // بنقول للـ mock: لو الـ handler نادى GetAllProjectedAsync رجّعله الليستة دي ومتروحش الداتابيز
            _unitOfWork.Setup(x => x.Departments.GetAllProjectedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(departments);

            var query = new GetDepartmentListQueryModel();

            // Act
            var result = await _handler.Handle(query, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);

            Assert.NotNull(result.data);
            Assert.NotEmpty(result.data);
            Assert.Equal(2, result.data.Count);

            Assert.Equal(1, result.data[0].Id);
            Assert.Equal("IT", result.data[0].DepartmentName);
            Assert.Equal(5, result.data[0].AssetsCount);

            Assert.Equal(2, result.data[1].Id);
            Assert.Equal("HR", result.data[1].DepartmentName);
            Assert.Equal(10, result.data[1].AssetsCount);

            _unitOfWork.Verify(x => x.Departments.GetAllProjectedAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDepartmentList_Should_Return_Empty_List_When_No_Departments_Exist()
        {
            // Arrange
            // حالة مهمة: لازم يرجّع Success مع ليستة فاضية، مش null ولا exception
            _unitOfWork.Setup(x => x.Departments.GetAllProjectedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<GetDepartmentListResponse>());

            var query = new GetDepartmentListQueryModel();

            // Act
            var result = await _handler.Handle(query, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);
            Assert.Empty(result.data);

            _unitOfWork.Verify(x => x.Departments.GetAllProjectedAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetById

        [Fact]
        public async Task GetDepartmentById_Should_Return_Department_When_Department_Exists_And_Active()
        {
            // Arrange
            var id = 1;

            var department = new Department
            {
                Id = id,
                DepartmentName = "IT Department",
                Code = "IT",
                IsActive = true
            };

            var response = new GetDepartmentByIdResponse
            {
                Id = id,
                DepartmentName = "IT Department",
                Code = "IT",
            };

            _unitOfWork.Setup(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(department);

            _mapper.Setup(x => x.Map<GetDepartmentByIdResponse>(department)).Returns(response);

            var query = new GetDepartmentByIdQueryModel(id);

            // Act
            var result = await _handler.Handle(query, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.data);

            Assert.Equal(id, result.data.Id);
            Assert.Equal("IT Department", result.data.DepartmentName);
            Assert.Equal("IT", result.data.Code);

            _unitOfWork.Verify(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(x => x.Map<GetDepartmentByIdResponse>(department), Times.Once);
        }

        [Fact]
        public async Task GetDepartmentById_Should_Throw_NotFoundException_When_Department_Is_Inactive()
        {
            // Arrange
            var id = 1;

            // موجود في الداتابيز بس متعطّل (soft deleted) → المفروض يتعامل معاه كأنه مش موجود
            var department = new Department
            {
                Id = id,
                DepartmentName = "IT Department",
                Code = "IT",
                IsActive = false
            };

            _unitOfWork.Setup(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(department);

            var query = new GetDepartmentByIdQueryModel(id);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, _ct));

            _unitOfWork.Verify(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);

            _mapper.Verify(x => x.Map<GetDepartmentByIdResponse>(It.IsAny<Department>()), Times.Never);
        }

        [Fact]
        public async Task GetDepartmentById_Should_Throw_NotFoundException_When_Department_Does_Not_Exist()
        {
            // Arrange
            var id = 999;

            _unitOfWork.Setup(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Department?)null);

            var query = new GetDepartmentByIdQueryModel(id);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, _ct));

            _unitOfWork.Verify(x => x.Departments.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(x => x.Map<GetDepartmentByIdResponse>(It.IsAny<Department>()), Times.Never);
        }

        #endregion
    }
}
