#region
using Asset.Application.Common.Models;
using Asset.Application.Common.Interfaces;
using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Features.Assets.Commands.CommandValidators;
using Asset.Application.Features.AssetTransfers.Commands.CommandModel;
using Asset.Application.Features.AssetTransfers.Commands.CommandValidator;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Features.Auth.Queries.GetCurrentUser;
using Asset.Application.Features.Employees.Commands.CommandModels;
using Asset.Application.Features.Employees.Commands.CommandValidators;
using Asset.Application.Features.Employees.DTos;
using Asset.Application.Features.Users.Commands.CreateUser;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resources;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Validators
{
    // Validators run BEFORE the handler (ValidationBehavior). These tests check the rules one by one.
    public abstract class ValidatorTestBase
    {
        protected static IStringLocalizer<SharedResources> Localizer()
        {
            var l = new Mock<IStringLocalizer<SharedResources>>();
            l.Setup(x => x[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, k));
            l.Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string k, object[] a) => new LocalizedString(k, string.Format(k, a)));
            return l.Object;
        }
    }

    public class CreateAssetCommandValidatorTests : ValidatorTestBase
    {
        private readonly Mock<IAssetRepository> _repo = new();

        private static CreateAssetCommandModel Valid() => new()
        {
            AssetCode = "LT-010", AssetName = "Dell XPS", CategoryId = 1, AssetTypeId = 1,
            Status = 1, LocationId = 2, PurchaseDate = new DateOnly(2025, 1, 1), WarrantyExpiryDate = new DateOnly(2027, 1, 1)
        };

        private Task<FluentValidation.Results.ValidationResult> Validate(CreateAssetCommandModel m) =>
            new CreateAssetCommandValidator(_repo.Object, Localizer()).ValidateAsync(m);

        [Fact]
        public async Task A_Complete_Asset_Should_Be_Valid()
        {
            (await Validate(Valid())).IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Assigned_Without_Employee_Should_Fail()
        {
            var m = Valid(); m.Status = 2;

            var result = await Validate(m);

            result.Errors.Should().Contain(e => e.PropertyName == "AssignedEmployeeId");
        }

        [Fact]
        public async Task Retired_Is_Not_Allowed_On_Create()
        {
            var m = Valid(); m.Status = 4;

            (await Validate(m)).Errors.Should().Contain(e => e.PropertyName == "Status");
        }

        [Fact]
        public async Task Warranty_Before_Purchase_Should_Fail()
        {
            var m = Valid(); m.WarrantyExpiryDate = new DateOnly(2024, 1, 1);

            (await Validate(m)).Errors.Should().Contain(e => e.ErrorMessage == "Warranty expiry date cannot be before the purchase date.");
        }

        [Fact]
        public async Task Missing_Location_Should_Fail()
        {
            var m = Valid(); m.LocationId = null;

            (await Validate(m)).Errors.Should().Contain(e => e.PropertyName == "LocationId");
        }

        [Fact]
        public async Task A_Code_Already_In_Use_Should_Fail_With_The_Code_In_The_Message()
        {
            _repo.Setup(r => r.IsCodeExistsAsync("LT-010", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            (await Validate(Valid())).Errors.Should().Contain(e => e.ErrorMessage == "Asset code 'LT-010' is already in use.");
        }
    }

    public class TransferAssetCommandValidatorTests : ValidatorTestBase
    {
        private static TransferAssetCommandModel Valid() => new()
        {
            AssetId = 5, ToLocationId = 3, TransferDate = DateTime.UtcNow, Reason = "Desk move",
            RowVersion = Convert.ToBase64String(new byte[] { 0, 0, 0, 0, 0, 0, 7, 209 })
        };

        private static FluentValidation.Results.ValidationResult Validate(TransferAssetCommandModel m) =>
            new TransferAssetCommandValidator(Localizer()).Validate(m);

        [Fact]
        public void A_Complete_Transfer_Should_Be_Valid() => Validate(Valid()).IsValid.Should().BeTrue();

        [Fact]
        public void Missing_Reason_Should_Fail()
        {
            var m = Valid(); m.Reason = "";
            Validate(m).Errors.Should().Contain(e => e.PropertyName == "Reason");
        }

        [Fact]
        public void Employee_Without_Department_Should_Fail()
        {
            var m = Valid(); m.ToEmployeeId = 15;
            Validate(m).Errors.Should().Contain(e => e.PropertyName == "ToDepartmentId");
        }

        [Theory]
        [InlineData("")]
        [InlineData("not base64 !!")]
        public void Bad_RowVersion_Should_Fail(string rowVersion)
        {
            var m = Valid(); m.RowVersion = rowVersion;
            Validate(m).Errors.Should().Contain(e => e.PropertyName == "RowVersion");
        }
    }

    public class CreateEmployeeCommandValidatorTests : ValidatorTestBase
    {
        private static FluentValidation.Results.ValidationResult Validate(CreateEmployeeCommandModel m) =>
            new CreateEmployeeCommandValidator(Localizer()).Validate(m);

        [Fact]
        public void A_Complete_Employee_Should_Be_Valid()
        {
            Validate(new CreateEmployeeCommandModel { FullName = "Hassan", Email = "h@x.com", DepartmentId = 1 })
                .IsValid.Should().BeTrue();
        }

        [Fact]
        public void No_Code_Is_Needed_Because_The_Server_Makes_It()
        {
            // The old validator required EmployeeCode; the code is now generated, so the model has no code at all.
            typeof(CreateEmployeeCommandModel).GetProperty("EmployeeCode").Should().BeNull();
        }

        [Fact]
        public void Bad_Email_And_Missing_Department_Should_Fail()
        {
            var result = Validate(new CreateEmployeeCommandModel { FullName = "Hassan", Email = "not-an-email", DepartmentId = 0 });

            result.Errors.Should().Contain(e => e.PropertyName == "Email");
            result.Errors.Should().Contain(e => e.PropertyName == "DepartmentId");
        }

        [Fact]
        public void Too_Long_Arabic_Name_Should_Fail()
        {
            var result = Validate(new CreateEmployeeCommandModel { FullName = "H", FullNameAr = new string('ح', 201), Email = "h@x.com", DepartmentId = 1 });

            result.Errors.Should().Contain(e => e.PropertyName == "FullNameAr");
        }
    }

    public class CreateUserCommandValidatorTests
    {
        [Theory]
        [InlineData("ab")]            // too short
        [InlineData("has space")]     // not allowed characters
        public void Bad_Usernames_Should_Fail(string userName)
        {
            var result = new CreateUserCommandValidator().Validate(new CreateUserCommand
            {
                UserName = userName, Email = "a@x.com", Password = "Passw0rd!", EmployeeId = 1, Role = Role.User
            });

            result.Errors.Should().Contain(e => e.PropertyName == "UserName");
        }

        [Fact]
        public void A_User_Must_Be_Linked_To_An_Employee()
        {
            var result = new CreateUserCommandValidator().Validate(new CreateUserCommand
            {
                UserName = "hassan", Email = "a@x.com", Password = "Passw0rd!", EmployeeId = 0
            });

            result.Errors.Should().Contain(e => e.PropertyName == "EmployeeId");
        }
    }

    // ============================================================  GET /auth/me
    public class GetCurrentUserQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IEmployeeRepository> _employees = new();

        public GetCurrentUserQueryHandlerTests()
        {
            _currentUser.Setup(x => x.UserId).Returns("u1");
            _uow.Setup(x => x.Employees).Returns(_employees.Object);
            _mapper.Setup(m => m.Map<CurrentUserDto>(It.IsAny<UserWithRole>())).Returns(new CurrentUserDto());
        }

        private GetCurrentUserQueryHandler Handler() => new(_users.Object, _currentUser.Object, _mapper.Object, _uow.Object);

        [Fact]
        public async Task Should_Include_The_Linked_Employee()
        {
            var user = new ApplicationUser { Id = "u1", IsActive = true, EmployeeId = 15 };
            _users.Setup(u => u.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
            var employee = new EmployeeInfo { Id = 15 };
            _employees.Setup(e => e.GetProjectedByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

            await Handler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

            _mapper.Verify(m => m.Map<CurrentUserDto>(It.Is<UserWithRole>(x => x.Employee == employee)), Times.Once);
        }

        [Fact]
        public async Task An_Account_Without_Employee_Should_Not_Crash()
        {
            // This used to be a 500 (EmployeeId.Value on null).
            _users.Setup(u => u.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = "u1", IsActive = true, EmployeeId = null });

            await Handler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

            _mapper.Verify(m => m.Map<CurrentUserDto>(It.Is<UserWithRole>(x => x.Employee == null)), Times.Once);
            _employees.Verify(e => e.GetProjectedByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task A_Disabled_Account_Should_Get_401()
        {
            _users.Setup(u => u.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(new ApplicationUser { Id = "u1", IsActive = false });

            var act = () => Handler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

            await act.Should().ThrowAsync<AuthenticationFailedException>();
        }
    }
}
