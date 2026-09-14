#region
using Asset.Application.Common.Caching;
using Asset.Application.Features.Departments.Commands.CommandHandlers;
using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Features.Departments.Commands.CommandResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using FluentAssertions;
using Moq;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Departments.Commands
{
    public class DepartmentCommandHandlerTest
    {
        #region Fields
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDepartmentRepository> _DepartmentRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly DepartmentCommandHandler _handler;
        #endregion

        #region Constructor
        public DepartmentCommandHandlerTest()
        {
            _unitOfWorkMock = new();
            _DepartmentRepositoryMock = new();
            _mapperMock = new();
            _cacheMock = new();

            // UnitOfWork.Departments returns our mock repository
            _unitOfWorkMock.Setup(x => x.Departments).Returns(_DepartmentRepositoryMock.Object);

            // Create the real handler using mocked dependencies
            _handler = new DepartmentCommandHandler(_unitOfWorkMock.Object, _mapperMock.Object, _cacheMock.Object);
        }
        #endregion

        #region Create
        [Fact]
        public async Task CreateDepartment_Should_Return_Successful_Response_When_Department_Is_Created()
        {
            // Arrange

            // Create fake request
            var request = new CreateDepartmentCommandModel
            {
                DepartmentName = "IT Department", Code = "IT"
            };

            // Fake entity that AutoMapper should create
            var entity = new Department
            {
                Id = 1, DepartmentName = "IT Department", Code = "IT", IsActive = true
            };

            // Fake response returned after mapping entity
            var response = new CreateDepartmentResponseDto
            {
                Id = 1, DepartmentName = "IT Department", Code = "IT"
            };

            // When mapper maps request -> Department >>  return our fake entity
            _mapperMock.Setup(x => x.Map<Department>(request)).Returns(entity);

            // When SaveChangesAsync is called
            // pretend that database save succeeded
            //_unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            // When mapper maps Department -> Response DTO  return our fake response
            _mapperMock.Setup(x => x.Map<CreateDepartmentResponseDto>(entity)).Returns(response);
            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert: handler Behivor
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Department Created Successfully");
            result.data.Should().BeSameAs(response);
            entity.IsActive.Should().BeTrue();

            // Assert: how I work with dependencies
            _mapperMock.Verify(x => x.Map<Department>(request), Times.Once);
            _unitOfWorkMock.Verify(x => x.Departments.Add(entity), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
            _cacheMock.Verify(x => x.RemoveAsync(CacheKeys.DepartmentList, CancellationToken.None), Times.Once);
        }
        #endregion

        #region Update
        [Fact]
        public async Task UpdateDepartment_When_Department_Exists_And_IsActive()
        {
            // Arrange 
            var request = new UpdateDepartmentCommandModel
            {
                Id = 1, DepartmentName = "IT Department - Updated", Code = "IT",
            };
            var response = new UpdateDepartmentResponseDto
            {
                DepartmentName = "IT Department - Updated", Code = "IT",
            };
            var entity = new Department
            {
                Id = 1, DepartmentName = "IT Department",  Code = "IT", IsActive = true
            };

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(request.Id, CancellationToken.None))
                                                    .ReturnsAsync(entity);

            _mapperMock.Setup(x => x.Map(request, entity)).Returns(entity);
            _mapperMock.Setup(x => x.Map<UpdateDepartmentResponseDto>(entity)).Returns(response);

            // Act
            var result = _handler.Handle(request, CancellationToken.None);
            // Assert
            result.Result.Success.Should().BeTrue();
            result.Result.Message.Should().Be("Department Updated Successfully");
            result.Result.data.Should().BeSameAs(response);

            // Check if Mapping is work good or not
            _mapperMock.Verify(x => x.Map(request, entity), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);

            _cacheMock.Verify(x => x.RemoveAsync(It.Is<string[]>(keys =>  keys.Length == 2 && 
                                                                 keys.Contains(CacheKeys.DepartmentById(request.Id)) && 
                                                                 keys.Contains(CacheKeys.DepartmentList)),
                                                                 CancellationToken.None),Times.Once);
        }

        [Fact]
        public async Task UpdateDepartment_Should_Throw_NotFoundException_When_Department_Does_NotExist()
        {
            // Arrange
            var request = new UpdateDepartmentCommandModel 
            { 
                Id = 99, DepartmentName = "X", Code = "X" 
            };

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync((Department?)null);

            // Act
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage("Department 99 does not exist.");

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateDepartment_Should_Throw_NotFoundException_When_Department_Is_Inactive()
        {
            // Arrange
            var request = new UpdateDepartmentCommandModel { Id = 1, DepartmentName = "X", Code = "X" };

            var inactiveEntity = new Department
            {
                Id = 1,
                DepartmentName = "IT Department",
                Code = "IT",
                IsActive = false
            };

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(request.Id, CancellationToken.None))
                                                    .ReturnsAsync(inactiveEntity);

            // Act
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _mapperMock.Verify(x => x.Map(It.IsAny<UpdateDepartmentCommandModel>(), It.IsAny<Department>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        #endregion

        #region Delete
        [Fact]
        public async Task DeleteDepartment_Should_DeactivateDepartment_When_ItHasNoEmployees_And_NoAssets()
        {
            // Arrange
            var request = new DeleteDepartmentCommandModel(1);
            var entity = new Department
            {
                Id = 1, DepartmentName = "IT Department", Code = "IT", IsActive = true
            };

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync(entity);
            _unitOfWorkMock.Setup(x => x.Employees.CountEmployeesAsync(request.Id, CancellationToken.None)).ReturnsAsync(0);
            _unitOfWorkMock.Setup(x => x.Assets.CountAssetsAsync(request.Id, CancellationToken.None)).ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Department Deactivated Successfully");
            result.data.Should().BeNull();

            _unitOfWorkMock.Verify(x => x.Departments.Remove(entity), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);

            _cacheMock.Verify(x => x.RemoveAsync(
                It.Is<string[]>(keys =>
                    keys.Length == 2 &&
                    keys.Contains(CacheKeys.DepartmentById(request.Id)) &&
                    keys.Contains(CacheKeys.DepartmentList)),
                CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task DeleteDepartment_Should_Throw_NotFoundException_When_Department_Does_NotExist()
        {
            // Arrange
            var request = new DeleteDepartmentCommandModel(99);

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(request.Id, CancellationToken.None))
                           .ReturnsAsync((Department?)null);

            // Act
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Department 99 does not exist.");

            _unitOfWorkMock.Verify(x => x.Departments.Remove(It.IsAny<Department>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteDepartment_Should_Throw_BusinessException_When_Department_IsAlready_Deactivated()
        {
            // Arrange
            var request = new DeleteDepartmentCommandModel(1);
            var entity = new Department
            {
                Id = 1, DepartmentName = "IT Department", Code = "IT", IsActive = true
            };
            entity.IsActive = false;

            _unitOfWorkMock.Setup(x => x.Departments.GetByIdAsync(request.Id, CancellationToken.None)).ReturnsAsync(entity);

            // Act
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>().WithMessage("Department 1 is already deactivated.");

            _unitOfWorkMock.Verify(x => x.Employees.CountEmployeesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.Departments.Remove(It.IsAny<Department>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        #endregion
    }
}