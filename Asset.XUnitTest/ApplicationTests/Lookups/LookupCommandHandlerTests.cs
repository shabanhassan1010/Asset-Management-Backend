#region
using System.Linq.Expressions;
using System.Net;
using Asset.Application.Common.Caching;
using Asset.Application.Features.AssetTypes.Commands.CommandHandlers;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Features.Category.Commands.CommandHandlers;
using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Features.Category.Commands.CommandResponse;
using Asset.Application.Features.Departments.Commands.CommandHandlers;
using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Features.Locations.Commands.CommandHandler;
using Asset.Application.Features.Locations.Commands.CommandModels;
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
using CategoryEntity = Asset.Domain.Models.Category;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Lookups
{
    // =====================================================================  Category
    public class CategoryCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICategoryRepository> _repo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly CategoryCommandHandler _handler;

        public CategoryCommandHandlerTests()
        {
            _uow.Setup(x => x.Categories).Returns(_repo.Object);
            _handler = new CategoryCommandHandler(_uow.Object, _mapper.Object, _cache.Object,
                                                  new Mock<IStringLocalizer<SharedResources>>().Object);
        }

        [Fact]
        public async Task Create_Should_Trim_Names_Save_And_Clear_The_Cache()
        {
            var request = new CreateCategoryCommandModel { CategoryName = "  Laptops ", CategoryNameAr = "  ", Description = "d" };
            var entity = new CategoryEntity();
            _mapper.Setup(m => m.Map<CategoryEntity>(request)).Returns(entity);

            var result = await _handler.Handle(request, CancellationToken.None);

            result.Success.Should().BeTrue();
            entity.CategoryName.Should().Be("Laptops");
            entity.CategoryNameAr.Should().BeNull();          // spaces only → NULL
            entity.IsActive.Should().BeTrue();
            _repo.Verify(r => r.Add(entity), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.CategoryList), Times.Once);
            _cache.Verify(c => c.BumpVersionAsync(CacheKeys.CategoriesVersion), Times.Once);
        }

        [Fact]
        public async Task Create_Should_Reject_A_Name_Used_By_An_Active_Category()
        {
            _repo.Setup(r => r.CategoryNameExistsAsync("Laptops", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new CreateCategoryCommandModel { CategoryName = "Laptops" }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_Should_Reject_An_Arabic_Name_Already_In_Use()
        {
            _repo.Setup(r => r.CategoryNameArExistsAsync("لابتوب", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new CreateCategoryCommandModel { CategoryName = "Laptops", CategoryNameAr = "لابتوب" }, CancellationToken.None);

            (await act.Should().ThrowAsync<BusinessException>()).Which.Args.Should().Equal("لابتوب");
        }

        [Fact]
        public async Task Update_Should_Throw_NotFound_For_An_Inactive_Category()
        {
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CategoryEntity { Id = 5, IsActive = false });

            var act = () => _handler.Handle(new UpdateCategoryCommandModel { Id = 5, CategoryName = "X" }, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Update_Should_Exclude_Its_Own_Id_From_The_Duplicate_Check()
        {
            var entity = new CategoryEntity { Id = 5, CategoryName = "Old", IsActive = true };
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new UpdateCategoryCommandModel { Id = 5, CategoryName = " New ", CategoryNameAr = "جديد" }, CancellationToken.None);

            result.Success.Should().BeTrue();
            entity.CategoryName.Should().Be("New");
            entity.CategoryNameAr.Should().Be("جديد");
            _repo.Verify(r => r.CategoryNameExistsAsync("New", 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Refuse_When_The_Category_Has_Assets()
        {
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CategoryEntity { Id = 5, IsActive = true });
            _repo.Setup(r => r.HasAssetsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new DeleteCategoryCommandModel(5), CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
            _repo.Verify(r => r.DeActivate(It.IsAny<CategoryEntity>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Deactivate_When_The_Category_Is_Not_Used()
        {
            var entity = new CategoryEntity { Id = 5, IsActive = true };
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new DeleteCategoryCommandModel(5), CancellationToken.None);

            result.Success.Should().BeTrue();
            _repo.Verify(r => r.DeActivate(entity), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---- Activate ----
        [Fact]
        public async Task Activate_Should_Turn_The_Category_Back_On_And_Clear_The_Cache()
        {
            var entity = new CategoryEntity { Id = 5, CategoryName = "Laptops", CategoryNameAr = "لابتوب", IsActive = false };
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new ActivateCategoryCommandModel(5), CancellationToken.None);

            result.Success.Should().BeTrue();
            result.Message.Should().Be("Category Activated Successfully");
            entity.IsActive.Should().BeTrue();
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _cache.Verify(c => c.RemoveAsync(CacheKeys.CategoryList), Times.Once);
            _cache.Verify(c => c.BumpVersionAsync(CacheKeys.CategoriesVersion), Times.Once);
        }

        [Fact]
        public async Task Activate_Should_Return_OK_Without_Saving_When_Already_Active()
        {
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CategoryEntity { Id = 5, CategoryName = "X", IsActive = true });

            var result = await _handler.Handle(new ActivateCategoryCommandModel(5), CancellationToken.None);

            result.Message.Should().Be("Category is already active");
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Activate_Should_Throw_NotFound_For_An_Unknown_Id()
        {
            var act = () => _handler.Handle(new ActivateCategoryCommandModel(99), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Activate_Should_Refuse_When_An_Active_Category_Has_The_Same_Name()
        {
            // While "Laptops" (id 5) was inactive, a new active "Laptops" was created.
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CategoryEntity { Id = 5, CategoryName = "Laptops", IsActive = false });
            _repo.Setup(r => r.CategoryNameExistsAsync("Laptops", 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new ActivateCategoryCommandModel(5), CancellationToken.None);

            (await act.Should().ThrowAsync<ConflictException>()).Which.Args.Should().Equal("Laptops");
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Activate_Should_Refuse_When_An_Active_Category_Has_The_Same_Arabic_Name()
        {
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new CategoryEntity { Id = 5, CategoryName = "Laptops", CategoryNameAr = "لابتوب", IsActive = false });
            _repo.Setup(r => r.CategoryNameArExistsAsync("لابتوب", 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new ActivateCategoryCommandModel(5), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }

    // =====================================================================  Asset type
    public class AssetTypeCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAssetTypeRepository> _repo = new();
        private readonly Mock<IAssetRepository> _assets = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly AssetTypeCommandHandler _handler;

        public AssetTypeCommandHandlerTests()
        {
            _uow.Setup(x => x.AssetTypes).Returns(_repo.Object);
            _uow.Setup(x => x.Assets).Returns(_assets.Object);
            _handler = new AssetTypeCommandHandler(_uow.Object, _mapper.Object, _cache.Object,
                                                   new Mock<IStringLocalizer<SharedResources>>().Object);
        }

        [Fact]
        public async Task Create_Should_Save_And_Return_201()
        {
            var request = new CreateAssetTypeCommandModel { assetTypeName = " Laptop ", assetTypeNameAr = "لابتوب" };
            var entity = new AssetType { Id = 3 };
            _mapper.Setup(m => m.Map<AssetType>(request)).Returns(entity);

            var result = await _handler.Handle(request, CancellationToken.None);

            result.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Data.Should().Be(3);
            entity.TypeName.Should().Be("Laptop");
            entity.IsActive.Should().BeTrue();
            _cache.Verify(c => c.RemoveAsync(CacheKeys.AssetTypeList), Times.Once);
        }

        [Fact]
        public async Task Create_Should_Reject_A_Duplicate_Name()
        {
            _repo.Setup(r => r.AssetTypeNameExistsAsync("Laptop", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new CreateAssetTypeCommandModel { assetTypeName = "Laptop" }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
        }

        [Fact]
        public async Task Update_Should_Change_The_Names()
        {
            var entity = new AssetType { Id = 3, TypeName = "Old", IsActive = true };
            _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new UpdateAssetTypeCommandModel { Id = 3, assetTypeName = "New", assetTypeNameAr = "" }, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            entity.TypeName.Should().Be("New");
            entity.TypeNameAr.Should().BeNull();
        }

        [Fact]
        public async Task Deactivate_Should_Refuse_While_Non_Retired_Assets_Use_It()
        {
            _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new AssetType { Id = 3, IsActive = true });
            _assets.Setup(a => a.AnyAsync(It.IsAny<Expression<Func<AssetEntity, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new DeactivateAssetTypeCommandModel { Id = 3 }, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessException>();
            _repo.Verify(r => r.Deactivate(It.IsAny<AssetType>()), Times.Never);
        }

        [Fact]
        public async Task Deactivate_Should_Deactivate_An_Unused_Type()
        {
            var entity = new AssetType { Id = 3, IsActive = true };
            _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new DeactivateAssetTypeCommandModel { Id = 3 }, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            _repo.Verify(r => r.Deactivate(entity), Times.Once);
        }

        [Fact]
        public async Task Activate_Should_Turn_The_Type_Back_On()
        {
            var entity = new AssetType { Id = 3, TypeName = "Laptop", IsActive = false };
            _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new ActivateAssetTypeCommandModel(3), CancellationToken.None);

            result.Success.Should().BeTrue();
            entity.IsActive.Should().BeTrue();
            _cache.Verify(c => c.BumpVersionAsync(CacheKeys.AssetTypesVersion), Times.Once);
        }

        [Fact]
        public async Task Activate_Should_Refuse_A_Name_Taken_By_An_Active_Type()
        {
            _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new AssetType { Id = 3, TypeName = "Laptop", IsActive = false });
            _repo.Setup(r => r.AssetTypeNameExistsAsync("Laptop", 3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new ActivateAssetTypeCommandModel(3), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }

    // =====================================================================  Department & Location: Activate
    public class DepartmentActivateTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IDepartmentRepository> _repo = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly DepartmentCommandHandler _handler;

        public DepartmentActivateTests()
        {
            _uow.Setup(x => x.Departments).Returns(_repo.Object);
            _handler = new DepartmentCommandHandler(_uow.Object, new Mock<IMapper>().Object, _cache.Object);
        }

        [Fact]
        public async Task Activate_Should_Turn_The_Department_Back_On()
        {
            var entity = new Department { Id = 4, DepartmentName = "IT", IsActive = false };
            _repo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new ActivateDepartmentCommandModel(4), CancellationToken.None);

            result.Success.Should().BeTrue();
            entity.IsActive.Should().BeTrue();
            _cache.Verify(c => c.RemoveAsync(CacheKeys.DepartmentList), Times.Once);
            _cache.Verify(c => c.BumpVersionAsync(CacheKeys.DepartmentsVersion), Times.Once);
        }

        [Fact]
        public async Task Activate_Should_Refuse_A_Name_Taken_By_An_Active_Department()
        {
            _repo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new Department { Id = 4, DepartmentName = "IT", IsActive = false });
            _repo.Setup(r => r.IsNameExistsAsync("IT", 4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new ActivateDepartmentCommandModel(4), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Activate_Should_Throw_NotFound_For_An_Unknown_Id()
        {
            var act = () => _handler.Handle(new ActivateDepartmentCommandModel(404), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }

    public class LocationActivateTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ILocationRepository> _repo = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly LocationCommandHandler _handler;

        public LocationActivateTests()
        {
            _uow.Setup(x => x.Locations).Returns(_repo.Object);
            _handler = new LocationCommandHandler(_uow.Object, new Mock<IMapper>().Object, _cache.Object);
        }

        [Fact]
        public async Task Activate_Should_Turn_The_Location_Back_On()
        {
            var entity = new Location { Id = 2, LocationName = "HQ", IsActive = false };
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _handler.Handle(new ActivateLocationCommandModel(2), CancellationToken.None);

            result.Success.Should().BeTrue();
            entity.IsActive.Should().BeTrue();
            _cache.Verify(c => c.BumpVersionAsync(CacheKeys.LocationsVersion), Times.Once);
        }

        [Fact]
        public async Task Activate_Should_Refuse_An_Arabic_Name_Taken_By_An_Active_Location()
        {
            _repo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Location { Id = 2, LocationName = "HQ", LocationNameAr = "المقر", IsActive = false });
            _repo.Setup(r => r.LocationNameArExistsAsync("المقر", 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var act = () => _handler.Handle(new ActivateLocationCommandModel(2), CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }
    }
}
