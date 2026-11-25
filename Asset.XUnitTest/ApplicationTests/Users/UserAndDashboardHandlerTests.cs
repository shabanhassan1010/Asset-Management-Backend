#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Dashboard.DTos;
using Asset.Application.Features.Dashboard.Queries.QueryHandlers;
using Asset.Application.Features.Users.Commands.ChangeUserRole;
using Asset.Application.Features.Users.Commands.ChangeUserStatus;
using Asset.Application.Features.Users.Commands.CreateUser;
using Asset.Application.Features.Users.DTOs;
using Asset.Application.Features.Users.Queries.GetUsers;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using FluentAssertions;
using Moq;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Users
{
    public abstract class UserTestBase
    {
        protected readonly Mock<IUserRepository> Users = new();
        protected readonly Mock<IRefreshTokenRepository> RefreshTokens = new();
        protected readonly Mock<ICurrentUserService> CurrentUser = new();
        protected readonly Mock<IUnitOfWork> Uow = new();

        protected UserTestBase()
        {
            CurrentUser.Setup(x => x.UserId).Returns("me");
            // Run the code passed to the transaction, as the real UnitOfWork does.
            Uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
               .Returns<Func<Task>, CancellationToken>((action, _) => action());
        }

        protected ApplicationUser Existing(string id, Role role, bool active = true)
        {
            var user = new ApplicationUser { Id = id, UserName = id, IsActive = active };
            Users.Setup(u => u.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            Users.Setup(u => u.GetRoleAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(role);
            return user;
        }
    }

    public class ChangeUserRoleCommandHandlerTests : UserTestBase
    {
        private ChangeUserRoleCommandHandler Handler() => new(Users.Object, RefreshTokens.Object, CurrentUser.Object, Uow.Object);

        [Fact]
        public async Task Should_Replace_The_Role_And_Revoke_All_Sessions_In_One_Transaction()
        {
            var user = Existing("u1", Role.User);

            await Handler().Handle(new ChangeUserRoleCommand("u1", Role.Admin), CancellationToken.None);

            Users.Verify(u => u.ReplaceRoleAsync(user, Role.User, Role.Admin, It.IsAny<CancellationToken>()), Times.Once);
            RefreshTokens.Verify(r => r.RevokeAllForUserAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
            Uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Same_Role_Should_Do_Nothing()
        {
            Existing("u1", Role.Admin);

            await Handler().Handle(new ChangeUserRoleCommand("u1", Role.Admin), CancellationToken.None);

            Users.Verify(u => u.ReplaceRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<Role>(), It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Admin_Should_Not_Change_Their_Own_Role()
        {
            Existing("me", Role.Admin);

            var act = () => Handler().Handle(new ChangeUserRoleCommand("me", Role.User), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Demoting_The_Last_Active_Admin_Should_Fail()
        {
            Existing("u1", Role.Admin);
            Users.Setup(u => u.CountActiveAdminsExcludingAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var act = () => Handler().Handle(new ChangeUserRoleCommand("u1", Role.User), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
            RefreshTokens.Verify(r => r.RevokeAllForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Unknown_User_Should_Throw_NotFound()
        {
            var act = () => Handler().Handle(new ChangeUserRoleCommand("ghost", Role.User), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class ChangeUserStatusCommandHandlerTests : UserTestBase
    {
        private ChangeUserStatusCommandHandler Handler() => new(Users.Object, RefreshTokens.Object, CurrentUser.Object, Uow.Object);

        [Fact]
        public async Task Disabling_Should_Save_And_Revoke_All_Sessions()
        {
            var user = Existing("u1", Role.User);

            await Handler().Handle(new ChangeUserStatusCommand("u1", false), CancellationToken.None);

            user.IsActive.Should().BeFalse();
            Users.Verify(u => u.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
            RefreshTokens.Verify(r => r.RevokeAllForUserAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Enabling_Should_Save_Without_Revoking()
        {
            var user = Existing("u1", Role.User, active: false);

            await Handler().Handle(new ChangeUserStatusCommand("u1", true), CancellationToken.None);

            user.IsActive.Should().BeTrue();
            RefreshTokens.Verify(r => r.RevokeAllForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Admin_Should_Not_Disable_Themselves()
        {
            Existing("me", Role.Admin);

            var act = () => Handler().Handle(new ChangeUserStatusCommand("me", false), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Disabling_The_Last_Active_Admin_Should_Fail()
        {
            Existing("u1", Role.Admin);
            Users.Setup(u => u.CountActiveAdminsExcludingAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var act = () => Handler().Handle(new ChangeUserStatusCommand("u1", false), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }
    }

    public class CreateUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IEmployeeRepository> _employees = new();
        private readonly CreateUserCommandHandler _handler;

        public CreateUserCommandHandlerTests()
        {
            _uow.Setup(x => x.Employees).Returns(_employees.Object);
            _employees.Setup(e => e.ExistsAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _users.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<Role>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(Array.Empty<string>());
            _handler = new CreateUserCommandHandler(_uow.Object, _users.Object, new Mock<IMapper>().Object);
        }

        private static CreateUserCommand Request() => new()
        {
            UserName = "hassan", Email = "h@x.com", Password = "Passw0rd!", Role = Role.User, EmployeeId = 15
        };

        [Fact]
        public async Task Should_Create_An_Active_User_Linked_To_The_Employee()
        {
            ApplicationUser? created = null;
            _users.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "Passw0rd!", Role.User, It.IsAny<CancellationToken>()))
                  .Callback<ApplicationUser, string, Role, CancellationToken>((u, _, _, _) => created = u)
                  .ReturnsAsync(Array.Empty<string>());

            await _handler.Handle(Request(), CancellationToken.None);

            created!.UserName.Should().Be("hassan");
            created.EmployeeId.Should().Be(15);
            created.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Taken_Username_Should_Conflict()
        {
            _users.Setup(u => u.UserNameExistsAsync("hassan", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(Request(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Employee_That_Already_Has_An_Account_Should_Conflict()
        {
            _users.Setup(u => u.EmployeeHasUserAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(Request(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Unknown_Employee_Should_Throw_NotFound()
        {
            _employees.Setup(e => e.ExistsAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var act = () => _handler.Handle(Request(), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Identity_Errors_Should_Become_A_Conflict_With_All_Messages()
        {
            _users.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<Role>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new[] { "Password too short.", "Needs a digit." });

            var act = () => _handler.Handle(Request(), CancellationToken.None);

            (await act.Should().ThrowAsync<ConflictException>()).WithMessage("Password too short. Needs a digit.");
        }
    }

    public class GetUsersQueryHandlerTests
    {
        [Fact]
        public async Task Should_Pass_The_Filters_And_Return_The_Page()
        {
            var users = new Mock<IUserRepository>();
            users.Setup(u => u.GetPagedAsync("ali", Role.Admin, true, 2, 5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(((IReadOnlyList<UserWithRole>)new List<UserWithRole>(), 12));
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<List<UserListItemDto>>(It.IsAny<object>())).Returns(new List<UserListItemDto>());

            var result = await new GetUsersQueryHandler(users.Object, mapper.Object)
                .Handle(new GetUsersQuery("ali", Role.Admin, true, 2, 5), CancellationToken.None);

            result.TotalCount.Should().Be(12);
            result.Page.Should().Be(2);
            result.TotalPages.Should().Be(3);
        }
    }

    public class DashboardHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IDashboardRepository> _dashboard = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();

        public DashboardHandlerTests()
        {
            _uow.Setup(x => x.Dashboard).Returns(_dashboard.Object);
            _dashboard.Setup(d => d.GetStatusCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AssetStatusCounts(10, 2, 4, 5, 1));
            _dashboard.Setup(d => d.GetTotalPurchaseCostAsync(It.IsAny<CancellationToken>())).ReturnsAsync(25000m);
        }

        [Fact]
        public async Task Admin_Should_See_The_Counts_And_The_Portfolio_Value()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(true);

            var result = await new GetDashboardSummaryHandler(_uow.Object, _currentUser.Object)
                .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

            result.ActiveAssets.Should().Be(10);
            result.AssignedAssets.Should().Be(5);
            result.PortfolioValue.Should().Be(25000m);
        }

        [Fact]
        public async Task User_Should_Not_See_The_Portfolio_Value_And_It_Is_Not_Even_Queried()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(false);

            var result = await new GetDashboardSummaryHandler(_uow.Object, _currentUser.Object)
                .Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

            result.PortfolioValue.Should().BeNull();
            _dashboard.Verify(d => d.GetTotalPurchaseCostAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
