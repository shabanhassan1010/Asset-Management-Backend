#region
using Asset.Application.Common.Caching;
using Asset.Application.Common.Interfaces;
using Asset.Application.Features.Employees.Commands.CommandHandlers;
using Asset.Application.Features.Employees.Commands.CommandModels;
using Asset.Application.Features.Employees.Queries.QueryHandlers;
using Asset.Application.Features.Employees.Queries.QueryModels;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resources;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Employees
{
    public class EmployeeCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IEmployeeRepository> _employees = new();
        private readonly Mock<IDepartmentRepository> _departments = new();
        private readonly Mock<IAssetRepository> _assets = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly EmployeeCommandHandler _handler;

        public EmployeeCommandHandlerTests()
        {
            _uow.Setup(x => x.Employees).Returns(_employees.Object);
            _uow.Setup(x => x.Departments).Returns(_departments.Object);
            _uow.Setup(x => x.Assets).Returns(_assets.Object);
            _currentUser.Setup(x => x.UserId).Returns("admin-id");
            _departments.Setup(d => d.ExistsActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _employees.Setup(e => e.GetCodesStartingWithAsync("EMP-", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<string> { "EMP-001", "EMP-008" });

            _handler = new EmployeeCommandHandler(_uow.Object, _cache.Object, new Mock<IMapper>().Object,
                                                  _currentUser.Object, new Mock<IStringLocalizer<SharedResources>>().Object);
        }

        // ---- Create ----
        [Fact]
        public async Task Create_Should_Generate_The_Next_Code_From_The_Biggest_One()
        {
            Employee? saved = null;
            _employees.Setup(e => e.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
                      .Callback<Employee, CancellationToken>((e, _) => saved = e);

            var result = await _handler.Handle(new CreateEmployeeCommandModel
            {
                FullName = " Hassan Ali ", FullNameAr = "", Email = " h@x.com ", Phone = "010", DepartmentId = 2
            }, CancellationToken.None);

            result.Success.Should().BeTrue();
            saved!.EmployeeCode.Should().Be("EMP-009");
            saved.FullName.Should().Be("Hassan Ali");
            saved.FullNameAr.Should().BeNull();
            saved.Email.Should().Be("h@x.com");
            saved.IsActive.Should().BeTrue();
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_Should_Refuse_An_Inactive_Or_Missing_Department()
        {
            _departments.Setup(d => d.ExistsActiveAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var act = () => _handler.Handle(new CreateEmployeeCommandModel { FullName = "A", DepartmentId = 9 }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
            _employees.Verify(e => e.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_Should_Refuse_An_Email_Used_By_Another_Employee()
        {
            _employees.Setup(e => e.IsEmailExistsAsync("a@x.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new CreateEmployeeCommandModel { FullName = "A", Email = "a@x.com", DepartmentId = 2 }, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        // ---- Update ----
        [Fact]
        public async Task Update_Changing_Department_Should_Move_The_Assets_And_Log_Each_Move()
        {
            var employee = new Employee { Id = 15, DepartmentId = 1, FullName = "Ahmed" };
            _employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

            var laptop = new AssetEntity { Id = 100, AssignedEmployeeId = 15, DepartmentId = 1, LocationId = 3 };
            var phone = new AssetEntity { Id = 101, AssignedEmployeeId = 15, DepartmentId = 1 };
            _assets.Setup(a => a.GetActiveAssignedToEmployeeAsync(15, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new List<AssetEntity> { laptop, phone });

            var transfers = new List<AssetTransfer>();
            _assets.Setup(a => a.AddTransferAsync(It.IsAny<AssetTransfer>(), It.IsAny<CancellationToken>()))
                   .Callback<AssetTransfer, CancellationToken>((t, _) => transfers.Add(t));

            await _handler.Handle(new UpdateEmployeeCommandModel { Id = 15, FullName = "Ahmed", DepartmentId = 2 }, CancellationToken.None);

            employee.DepartmentId.Should().Be(2);
            laptop.DepartmentId.Should().Be(2);
            phone.DepartmentId.Should().Be(2);
            transfers.Should().HaveCount(2);
            transfers[0].FromDepartmentId.Should().Be(1);
            transfers[0].ToDepartmentId.Should().Be(2);
            transfers[0].ToEmployeeId.Should().Be(15);       // same person, new department
            transfers[0].TransferredByUserId.Should().Be("admin-id");
            // One save for the employee, the assets and the history together.
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_Same_Department_Should_Not_Touch_Assets()
        {
            _employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, DepartmentId = 1 });

            await _handler.Handle(new UpdateEmployeeCommandModel { Id = 15, FullName = "Ahmed", DepartmentId = 1 }, CancellationToken.None);

            _assets.Verify(a => a.GetActiveAssignedToEmployeeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_Should_Throw_NotFound_For_An_Unknown_Employee()
        {
            var act = () => _handler.Handle(new UpdateEmployeeCommandModel { Id = 404, FullName = "X", DepartmentId = 1 }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ---- Status ----
        [Fact]
        public async Task Deactivate_Should_Refuse_While_Assets_Are_Assigned()
        {
            _employees.Setup(e => e.GetByIdWithDepartmentAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, FullName = "Ahmed", IsActive = true });
            _employees.Setup(e => e.HasAssignedAssetsAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new SetEmployeeStatusCommandModel { Id = 15, IsActive = false }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Activate_Should_Turn_The_Employee_Back_On()
        {
            var employee = new Employee { Id = 15, FullName = "Ahmed", IsActive = false };
            _employees.Setup(e => e.GetByIdWithDepartmentAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

            var result = await _handler.Handle(new SetEmployeeStatusCommandModel { Id = 15, IsActive = true }, CancellationToken.None);

            result.Message.Should().Be("Employee Enabled Successfully");
            employee.IsActive.Should().BeTrue();
            _cache.Verify(c => c.BumpVersionAsync(CacheKeys.EmployeesVersion), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Status_Unchanged_Should_Return_OK_Without_Saving()
        {
            _employees.Setup(e => e.GetByIdWithDepartmentAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, IsActive = true });

            var result = await _handler.Handle(new SetEmployeeStatusCommandModel { Id = 15, IsActive = true }, CancellationToken.None);

            result.Message.Should().Be("Employee is already active");
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class EmployeeQueryHandlerTests
    {
        [Fact]
        public async Task NextCode_Should_Return_The_Biggest_Code_Plus_One()
        {
            var uow = new Mock<IUnitOfWork>();
            var employees = new Mock<IEmployeeRepository>();
            uow.Setup(x => x.Employees).Returns(employees.Object);
            employees.Setup(e => e.GetCodesStartingWithAsync("EMP-", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<string> { "EMP-007", "EMP-002" });

            var handler = new GetEmployeeListQueryHandler(uow.Object, new Mock<IUserRepository>().Object, new Mock<IMapper>().Object);

            var result = await handler.Handle(new GetNextEmployeeCodeQuery(), CancellationToken.None);

            result.data.Should().Be("EMP-008");
        }
    }
}
