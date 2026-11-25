#region
using Asset.Application.Behaviors;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Resources;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Behaviors
{
    // Fake requests used only by these tests — each one opts into a different caching rule.
    public record PlainCachedQuery(string Name) : IRequest<string>, ICachedQuery
    {
        public string CacheKey => $"test:{Name}";
        public CacheDuration Duration => CacheDuration.Lookup;
    }

    public record RoleCachedQuery : IRequest<string>, ICachedQuery, IVaryByRole
    {
        public string CacheKey => "test:role";
        public CacheDuration Duration => CacheDuration.Volatile;
    }

    public record VersionedCachedQuery : IRequest<string>, ICachedQuery, IVersionedCache
    {
        public string CacheKey => "test:list";
        public CacheDuration Duration => CacheDuration.Volatile;
        public string VersionKey => "test:version";
    }

    public class CachingBehaviorTests
    {
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly CacheSettings _settings = new() { LookupMinutes = 20, VolatileMinutes = 2 };

        private CachingBehavior<TReq, string> Create<TReq>() where TReq : notnull, ICachedQuery
            => new(_cache.Object, NullLogger<CachingBehavior<TReq, string>>.Instance, Options.Create(_settings), _currentUser.Object);

        [Fact]
        public async Task Hit_Should_Return_The_Cached_Value_Without_Calling_The_Handler()
        {
            _cache.Setup(c => c.GetAsync<string>("test:a", It.IsAny<CancellationToken>())).ReturnsAsync("from cache");
            var handlerCalled = false;

            var result = await Create<PlainCachedQuery>().Handle(new PlainCachedQuery("a"),
                () => { handlerCalled = true; return Task.FromResult("from db"); }, CancellationToken.None);

            result.Should().Be("from cache");
            handlerCalled.Should().BeFalse();
        }

        [Fact]
        public async Task Miss_Should_Call_The_Handler_And_Store_The_Result_With_The_Group_Duration()
        {
            var result = await Create<PlainCachedQuery>().Handle(new PlainCachedQuery("a"),
                () => Task.FromResult("from db"), CancellationToken.None);

            result.Should().Be("from db");
            _cache.Verify(c => c.SetAsync("test:a", "from db", TimeSpan.FromMinutes(20), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(true, "test:role:role:admin")]
        [InlineData(false, "test:role:role:user")]
        public async Task VaryByRole_Should_Put_The_Role_In_The_Key(bool isAdmin, string expectedKey)
        {
            // Without this, an admin's response (with PurchaseCost) could be served to a normal user.
            _currentUser.Setup(u => u.IsAdmin).Returns(isAdmin);

            await Create<RoleCachedQuery>().Handle(new RoleCachedQuery(), () => Task.FromResult("x"), CancellationToken.None);

            _cache.Verify(c => c.GetAsync<string>(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Versioned_Should_Put_The_Current_Version_In_The_Key()
        {
            _cache.Setup(c => c.GetVersionAsync("test:version", It.IsAny<CancellationToken>())).ReturnsAsync("7");

            await Create<VersionedCachedQuery>().Handle(new VersionedCachedQuery(), () => Task.FromResult("x"), CancellationToken.None);

            _cache.Verify(c => c.GetAsync<string>("test:list:v7", It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class ValidationBehaviorTests
    {
        public record SaveName(string Name) : IRequest<string>;

        private class SaveNameValidator : AbstractValidator<SaveName>
        {
            public SaveNameValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }

        [Fact]
        public async Task Invalid_Request_Should_Throw_And_Never_Reach_The_Handler()
        {
            var behavior = new ValidationBehavior<SaveName, string>(new[] { new SaveNameValidator() });
            var handlerCalled = false;

            var act = () => behavior.Handle(new SaveName(""), () => { handlerCalled = true; return Task.FromResult("ok"); }, CancellationToken.None);

            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Name is required.");
            handlerCalled.Should().BeFalse();
        }

        [Fact]
        public async Task Valid_Request_Should_Reach_The_Handler()
        {
            var behavior = new ValidationBehavior<SaveName, string>(new[] { new SaveNameValidator() });

            var result = await behavior.Handle(new SaveName("IT"), () => Task.FromResult("ok"), CancellationToken.None);

            result.Should().Be("ok");
        }

        [Fact]
        public async Task Request_Without_Validators_Should_Pass_Through()
        {
            var behavior = new ValidationBehavior<SaveName, string>(Array.Empty<IValidator<SaveName>>());

            var result = await behavior.Handle(new SaveName(""), () => Task.FromResult("ok"), CancellationToken.None);

            result.Should().Be("ok");
        }
    }

    public class LocalizationBehaviorTests
    {
        public record AnyRequest : IRequest<ApiResponse<int>>;

        [Fact]
        public async Task Should_Translate_The_Message_Of_The_Response()
        {
            var localizer = new Mock<IStringLocalizer<SharedResources>>();
            localizer.Setup(l => l["Done"]).Returns(new LocalizedString("Done", "تم"));
            var behavior = new LocalizationBehavior<AnyRequest, ApiResponse<int>>(localizer.Object);

            var result = await behavior.Handle(new AnyRequest(),
                () => Task.FromResult(new ApiResponse<int> { Success = true, Message = "Done" }), CancellationToken.None);

            result.Message.Should().Be("تم");
        }

        [Fact]
        public async Task Should_Leave_An_Empty_Message_Alone()
        {
            var localizer = new Mock<IStringLocalizer<SharedResources>>(MockBehavior.Strict);   // any call would fail the test
            var behavior = new LocalizationBehavior<AnyRequest, ApiResponse<int>>(localizer.Object);

            var result = await behavior.Handle(new AnyRequest(),
                () => Task.FromResult(new ApiResponse<int> { Success = true, Message = "" }), CancellationToken.None);

            result.Message.Should().BeEmpty();
        }
    }
}
