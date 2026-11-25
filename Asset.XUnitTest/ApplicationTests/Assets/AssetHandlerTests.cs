#region
using System.Linq.Expressions;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Assets.Commands.CommandHandlers;
using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Features.Assets.Queries.QueryHandlers;
using Asset.Application.Features.Assets.Queries.QueryModels;
using Asset.Application.Features.Assets.Queries.QueryResponses;
using Asset.Application.Features.AssetTransfers.Commands.CommandHandler;
using Asset.Application.Features.AssetTransfers.Commands.CommandModel;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resources;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Assets
{
    // Shared fakes for the asset tests.
    public abstract class AssetTestBase
    {
        protected readonly Mock<IUnitOfWork> Uow = new();
        protected readonly Mock<IAssetRepository> Assets = new();
        protected readonly Mock<IDepartmentRepository> Departments = new();
        protected readonly Mock<ILocationRepository> Locations = new();
        protected readonly Mock<IEmployeeRepository> Employees = new();
        protected readonly Mock<ICacheService> Cache = new();
        protected readonly Mock<ICurrentUserService> CurrentUser = new();
        protected readonly Mock<IMapper> Mapper = new();

        // The row version the database holds, and the same value as the client sends it (Base64).
        protected static readonly byte[] DbVersion = { 0, 0, 0, 0, 0, 0, 7, 209 };
        protected static readonly string ClientVersion = Convert.ToBase64String(DbVersion);

        protected AssetTestBase()
        {
            Uow.Setup(x => x.Assets).Returns(Assets.Object);
            Uow.Setup(x => x.Departments).Returns(Departments.Object);
            Uow.Setup(x => x.Locations).Returns(Locations.Object);
            Uow.Setup(x => x.Employees).Returns(Employees.Object);
            CurrentUser.Setup(x => x.UserId).Returns("admin-id");
            Departments.Setup(d => d.ExistsActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            Locations.Setup(l => l.ExistsActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        protected void NoDuplicates() =>
            Assets.Setup(a => a.AnyAsync(It.IsAny<Expression<Func<AssetEntity, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        protected AssetEntity StoredAsset(int status = (int)AssetStatus.Available) => new()
        {
            Id = 5, AssetCode = "LT-001", Status = status, RowVersion = (byte[])DbVersion.Clone()
        };
    }

    public class AssetCommandHandlerTests : AssetTestBase
    {
        private readonly AssetCommandHandler _handler;

        public AssetCommandHandlerTests()
        {
            _handler = new AssetCommandHandler(CurrentUser.Object, Uow.Object, Cache.Object, Mapper.Object,
                                               new Mock<IStringLocalizer<SharedResources>>().Object);
        }

        private UpdateAssetCommandModel Update(int status = (int)AssetStatus.Available, int? employeeId = null, int? departmentId = null) => new()
        {
            AssetId = 5, AssetCode = "LT-001", AssetName = "Laptop", Status = status,
            AssignedEmployeeId = employeeId, DepartmentId = departmentId, RowVersion = ClientVersion
        };

        // ---- Create ----
        [Fact]
        public async Task Create_Should_Save_With_The_User_From_The_Token_And_Bump_The_Asset_Cache()
        {
            NoDuplicates();
            var request = new CreateAssetCommandModel { AssetCode = "LT-002", DepartmentId = 0, LocationId = 0 };
            var entity = new AssetEntity();
            Mapper.Setup(m => m.Map<AssetEntity>(request)).Returns(entity);

            var result = await _handler.Handle(request, CancellationToken.None);

            result.Success.Should().BeTrue();
            request.DepartmentId.Should().BeNull();           // 0 from the form means "none"
            entity.CreatedByUserId.Should().Be("admin-id");
            Assets.Verify(a => a.Add(entity), Times.Once);
            Cache.Verify(c => c.BumpVersionAsync(CacheKeys.AssetsVersion), Times.Once);
        }

        [Fact]
        public async Task Create_Should_Refuse_A_Duplicate_Code()
        {
            Assets.Setup(a => a.AnyAsync(It.IsAny<Expression<Func<AssetEntity, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new CreateAssetCommandModel { AssetCode = "LT-001" }, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
            Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---- Update ----
        [Fact]
        public async Task Update_Should_Save_When_The_RowVersion_Matches()
        {
            NoDuplicates();
            var entity = StoredAsset();
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(Update(), CancellationToken.None);

            result.Success.Should().BeTrue();
            Assets.Verify(a => a.SetOriginalRowVersion(entity, It.Is<byte[]>(v => v.SequenceEqual(DbVersion))), Times.Once);
            Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_Should_Return_Conflict_When_Someone_Else_Saved_First()
        {
            var entity = StoredAsset();
            entity.RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 9, 99 };     // the database moved on
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var act = () => _handler.Handle(Update(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
            Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_Should_Turn_A_Save_Time_Concurrency_Error_Into_Conflict()
        {
            // Someone saved between our check and our UPDATE: EF throws, we answer 409.
            NoDuplicates();
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());
            Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateConcurrencyException("stale"));

            var act = () => _handler.Handle(Update(), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Update_Should_Reject_A_Malformed_RowVersion()
        {
            var request = Update();
            request.RowVersion = "not-base64!";

            var act = () => _handler.Handle(request, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Update_Should_Refuse_Setting_Retired_Through_Edit()
        {
            var act = () => _handler.Handle(Update(status: (int)AssetStatus.Retired), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Update_Should_Refuse_Editing_A_Retired_Asset()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset((int)AssetStatus.Retired));

            var act = () => _handler.Handle(Update(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Update_Assigned_Without_Employee_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());

            var act = () => _handler.Handle(Update(status: (int)AssetStatus.Assigned), CancellationToken.None);

            (await act.Should().ThrowAsync<BusinessException>()).WithMessage("An assigned asset must have an employee.");
        }

        [Fact]
        public async Task Update_Available_With_Employee_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());
            Employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, DepartmentId = 2, IsActive = true });

            var act = () => _handler.Handle(Update(status: (int)AssetStatus.Available, employeeId: 15), CancellationToken.None);

            (await act.Should().ThrowAsync<BusinessException>()).WithMessage("An available asset cannot have an employee assigned.");
        }

        [Fact]
        public async Task Update_Employee_From_Another_Department_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());
            Employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, DepartmentId = 2, IsActive = true });

            var act = () => _handler.Handle(Update(status: (int)AssetStatus.Assigned, employeeId: 15, departmentId: 3), CancellationToken.None);

            (await act.Should().ThrowAsync<BusinessException>()).WithMessage("The selected employee does not belong to the selected department.");
        }

        [Fact]
        public async Task Update_With_Employee_And_No_Department_Should_Take_The_Employees_Department()
        {
            NoDuplicates();
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());
            Employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, DepartmentId = 2, IsActive = true });
            var request = Update(status: (int)AssetStatus.Assigned, employeeId: 15);

            await _handler.Handle(request, CancellationToken.None);

            request.DepartmentId.Should().Be(2);
        }

        // ---- Retire ----
        [Fact]
        public async Task Retire_Should_Mark_The_Asset_Retired_With_Who_And_Why()
        {
            var entity = StoredAsset((int)AssetStatus.Assigned);
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            await _handler.Handle(new RetireAssetCommandModel { AssetId = 5, Reason = "Broken screen", RowVersion = ClientVersion }, CancellationToken.None);

            entity.Status.Should().Be((int)AssetStatus.Retired);
            entity.RetirementReason.Should().Be("Broken screen");
            entity.RetiredByUserId.Should().Be("admin-id");
            entity.RetiredAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Retire_Twice_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset((int)AssetStatus.Retired));

            var act = () => _handler.Handle(new RetireAssetCommandModel { AssetId = 5, Reason = "x", RowVersion = ClientVersion }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }
    }

    public class AssetQueryHandlerTests : AssetTestBase
    {
        private AssetQueryHandler Handler() =>
            new(CurrentUser.Object, Uow.Object, Mapper.Object, new Mock<IStringLocalizer<SharedResources>>().Object);

        [Theory]
        [InlineData(true, 1500)]
        [InlineData(false, null)]     // a normal user never sees the purchase cost
        public async Task GetById_Should_Hide_The_Cost_From_Non_Admins(bool isAdmin, int? expectedCost)
        {
            CurrentUser.Setup(x => x.IsAdmin).Returns(isAdmin);
            var asset = new AssetEntity { Id = 5 };
            Assets.Setup(a => a.GetByIdWithDetailsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
            Mapper.Setup(m => m.Map<GetByIdQueryResponse>(asset)).Returns(new GetByIdQueryResponse { PurchaseCost = 1500 });

            var result = await Handler().Handle(new GetAssetByIdQueryModel(5), CancellationToken.None);

            result.PurchaseCost.Should().Be(expectedCost);
        }

        [Fact]
        public async Task GetById_Should_Throw_NotFound_For_An_Unknown_Id()
        {
            var act = () => Handler().Handle(new GetAssetByIdQueryModel(404), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Paginated_Should_Pass_The_Filters_And_Hide_Costs_For_Users()
        {
            CurrentUser.Setup(x => x.IsAdmin).Returns(false);
            AssetFilter? used = null;
            Assets.Setup(a => a.GetPaginationAsync(It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()))
                  .Callback<AssetFilter, CancellationToken>((f, _) => used = f)
                  .ReturnsAsync(new PagedResult<AssetEntity> { Items = new() { new AssetEntity() }, TotalCount = 1, Page = 2, PageSize = 10 });
            Mapper.Setup(m => m.Map<List<GetAssetPaginatedListQueryResponse>>(It.IsAny<object>()))
                  .Returns(new List<GetAssetPaginatedListQueryResponse> { new() { PurchaseCost = 900 } });

            var result = await Handler().Handle(new GetAssetPaginatedListQueryModel { PageNumber = 2, StatusId = 1, Search = "dell" }, CancellationToken.None);

            used!.Page.Should().Be(2);
            used.StatusId.Should().Be(1);
            used.Search.Should().Be("dell");
            result.TotalItems.Should().Be(1);
            result.Data.Single().PurchaseCost.Should().BeNull();
        }
    }

    public class TransferAssetCommandHandlerTests : AssetTestBase
    {
        private readonly TransferAssetCommandHandler _handler;

        public TransferAssetCommandHandlerTests()
        {
            _handler = new TransferAssetCommandHandler(CurrentUser.Object, Uow.Object, Cache.Object);
        }

        private TransferAssetCommandModel Transfer(int? employeeId = null, int? departmentId = null, int? locationId = null) => new()
        {
            AssetId = 5, ToEmployeeId = employeeId, ToDepartmentId = departmentId, ToLocationId = locationId,
            TransferDate = DateTime.UtcNow, Reason = " Moved desk ", RowVersion = ClientVersion
        };

        [Fact]
        public async Task Transfer_To_An_Employee_Should_Assign_Log_And_Save_Once()
        {
            var asset = StoredAsset();
            asset.DepartmentId = 1;
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
            Employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, DepartmentId = 2, IsActive = true });
            AssetTransfer? log = null;
            Assets.Setup(a => a.AddTransferAsync(It.IsAny<AssetTransfer>(), It.IsAny<CancellationToken>()))
                  .Callback<AssetTransfer, CancellationToken>((t, _) => log = t);

            var result = await _handler.Handle(Transfer(employeeId: 15), CancellationToken.None);

            result.Success.Should().BeTrue();
            asset.AssignedEmployeeId.Should().Be(15);
            asset.DepartmentId.Should().Be(2);                       // taken from the employee
            asset.Status.Should().Be((int)AssetStatus.Assigned);
            log!.FromDepartmentId.Should().Be(1);
            log.ToDepartmentId.Should().Be(2);
            log.Reason.Should().Be("Moved desk");
            Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Cache.Verify(c => c.BumpVersionAsync(CacheKeys.AssetsVersion), Times.Once);
        }

        [Fact]
        public async Task Transfer_Away_From_Everyone_Should_Make_The_Asset_Available()
        {
            var asset = StoredAsset((int)AssetStatus.Assigned);
            asset.AssignedEmployeeId = 15;
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

            await _handler.Handle(Transfer(locationId: 3), CancellationToken.None);

            asset.AssignedEmployeeId.Should().BeNull();
            asset.Status.Should().Be((int)AssetStatus.Available);
        }

        [Fact]
        public async Task Transfer_Should_Keep_UnderMaintenance()
        {
            var asset = StoredAsset((int)AssetStatus.UnderMaintenance);
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

            await _handler.Handle(Transfer(locationId: 3), CancellationToken.None);

            asset.Status.Should().Be((int)AssetStatus.UnderMaintenance);
        }

        [Fact]
        public async Task Transfer_That_Changes_Nothing_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());

            var act = () => _handler.Handle(Transfer(), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Transfer_Of_A_Retired_Asset_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset((int)AssetStatus.Retired));

            var act = () => _handler.Handle(Transfer(locationId: 3), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Transfer_In_The_Future_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());
            var request = Transfer(locationId: 3);
            request.TransferDate = DateTime.UtcNow.AddDays(2);

            var act = () => _handler.Handle(request, CancellationToken.None);

            (await act.Should().ThrowAsync<BusinessException>()).WithMessage("A transfer date cannot be in the future.");
        }

        [Fact]
        public async Task Transfer_With_A_Stale_RowVersion_Should_Throw_Concurrency()
        {
            var asset = StoredAsset();
            asset.RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

            var act = () => _handler.Handle(Transfer(locationId: 3), CancellationToken.None);

            await act.Should().ThrowAsync<ConcurrencyException>();
        }

        [Fact]
        public async Task Transfer_To_An_Inactive_Employee_Should_Fail()
        {
            Assets.Setup(a => a.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(StoredAsset());
            Employees.Setup(e => e.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(new Employee { Id = 15, FullName = "Ali", IsActive = false });

            var act = () => _handler.Handle(Transfer(employeeId: 15), CancellationToken.None);

            (await act.Should().ThrowAsync<BusinessException>()).Which.Args.Should().Equal("Ali");
        }
    }
}
