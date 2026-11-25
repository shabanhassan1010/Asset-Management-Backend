#region
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.IService;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.Queries.QueryHandler;
using Asset.Application.Features.AI.Queries.QueryModel;
using Asset.Application.Features.AI.ServiceImplementation;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Application.Resources;
using Asset.Domain.Enum;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using AssetEntity = Asset.Domain.Models.Asset;
#endregion

namespace Asset.XUnitTest.ApplicationTests.AI
{
    // ============================================================  The security rules of the AI handler
    // The AI only turns the question into filters; WHO may see WHAT is decided by code.
    // The parser is mocked here, so each test controls exactly what "the AI understood".
    public class AskAssetQuestionQueryHandlerTests
    {
        private readonly Mock<IAssetQuestionParserService> _parser = new();
        private readonly Mock<IAssetAnswerWriterService> _writer = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAssetRepository> _assets = new();
        private readonly Mock<IAiLookupRepository> _lookup = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IConversationStoreService> _store = new();
        private readonly Mock<IStringLocalizer<SharedResources>> _localizer = new();

        private AssetFilter? _usedFilter;
        private AssetAnswerContext? _writerInput;

        public AskAssetQuestionQueryHandlerTests()
        {
            _uow.Setup(x => x.Assets).Returns(_assets.Object);
            _uow.Setup(x => x.AiLookup).Returns(_lookup.Object);
            _currentUser.Setup(x => x.UserId).Returns("u1");
            _store.Setup(s => s.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatTurn>());

            // The localizer just returns the English text (with its {0} filled), so answers are readable in asserts.
            _localizer.Setup(l => l[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, k));
            _localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
                      .Returns((string k, object[] a) => new LocalizedString(k, string.Format(k, a)));

            _assets.Setup(a => a.GetPaginationAsync(It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()))
                   .Callback<AssetFilter, CancellationToken>((f, _) => _usedFilter = f)
                   .ReturnsAsync(new PagedResult<AssetEntity>
                   {
                       Items = new() { new AssetEntity { Id = 1, AssetCode = "LT-1", Status = (int)AssetStatus.Assigned, PurchaseCost = 1200 } },
                       TotalCount = 1, Page = 1, PageSize = 20
                   });
            _writer.Setup(w => w.WriteAnswerAsync(It.IsAny<AssetAnswerContext>(), It.IsAny<CancellationToken>()))
                   .Callback<AssetAnswerContext, CancellationToken>((c, _) => _writerInput = c)
                   .ReturnsAsync("answer");
        }

        private AskAssetQuestionQueryHandler Handler() =>
            new(_parser.Object, _writer.Object, _uow.Object, _currentUser.Object, _store.Object, _localizer.Object);

        private void AiUnderstood(ParsedAssetQuestion parsed) =>
            _parser.Setup(p => p.ParseAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatTurn>>(), It.IsAny<ParsedAssetQuestion?>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(parsed);

        private static AskAssetQuestionQuery Ask(string q) => new() { Question = q, SessionId = "s1" };

        [Fact]
        public async Task My_Assets_Should_Filter_By_The_Employee_In_The_Token()
        {
            _currentUser.Setup(x => x.EmployeeId).Returns(15);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.ListAssets, IsAboutSelf = true });

            await Handler().Handle(Ask("show my assets"), CancellationToken.None);

            _usedFilter!.EmployeeId.Should().Be(15);
        }

        [Fact]
        public async Task A_User_Asking_About_Another_Employee_Should_Be_Refused_Without_Querying()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(false);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.ListAssets, EmployeeName = "Ahmed" });

            var result = await Handler().Handle(Ask("show Ahmed's assets"), CancellationToken.None);

            result.data.Assets.Should().BeEmpty();
            _lookup.Verify(l => l.FindEmployeesByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _assets.Verify(a => a.GetPaginationAsync(It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task A_Question_Without_Filters_From_A_User_Should_Become_My_Assets()
        {
            // A normal user may only see their own assets, even if they ask "list all assets".
            _currentUser.Setup(x => x.IsAdmin).Returns(false);
            _currentUser.Setup(x => x.EmployeeId).Returns(15);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.ListAssets });

            await Handler().Handle(Ask("list all assets"), CancellationToken.None);

            _usedFilter!.EmployeeId.Should().Be(15);
        }

        [Theory]
        [InlineData(true, 1200)]
        [InlineData(false, null)]
        public async Task The_Purchase_Cost_Should_Reach_The_AI_Only_For_Admins(bool isAdmin, int? expected)
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(isAdmin);
            _currentUser.Setup(x => x.EmployeeId).Returns(15);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.ListAssets, IsAboutSelf = true });

            await Handler().Handle(Ask("my assets"), CancellationToken.None);

            _writerInput!.Assets.Single().PurchaseCost.Should().Be(expected);
        }

        [Fact]
        public async Task An_Admin_Asking_For_An_Ambiguous_Name_Should_Get_A_Question_Back()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(true);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.ListAssets, EmployeeName = "Ali" });
            _lookup.Setup(l => l.FindEmployeesByNameAsync("Ali", It.IsAny<CancellationToken>())).ReturnsAsync(new List<EmployeeLookup>
            {
                new(1, "Ali Hassan", null),
                new(2, "Ali Mahmoud", null),
            });

            var result = await Handler().Handle(Ask("Ali's assets"), CancellationToken.None);

            result.data.Suggestions.Should().HaveCount(2);           // one chip per matching employee
            _assets.Verify(a => a.GetPaginationAsync(It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Count_Questions_Should_Only_Ask_For_One_Row()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(true);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.CountAssets, Status = AssetStatus.Available });

            var result = await Handler().Handle(Ask("how many available assets?"), CancellationToken.None);

            _usedFilter!.PageSize.Should().Be(1);           // only TotalCount is needed
            _usedFilter.StatusId.Should().Be((int)AssetStatus.Available);
            result.data.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task An_Unknown_Asset_Type_Should_Be_Answered_Without_Querying_Assets()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(true);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.ListAssets, AssetTypeName = "Spaceship" });

            await Handler().Handle(Ask("show spaceships"), CancellationToken.None);

            _assets.Verify(a => a.GetPaginationAsync(It.IsAny<AssetFilter>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task The_Chat_Key_Should_Contain_The_User_Id_So_Nobody_Reads_Another_Users_Chat()
        {
            _currentUser.Setup(x => x.IsAdmin).Returns(true);
            AiUnderstood(new ParsedAssetQuestion { Intent = AssetQuestionIntent.Greeting });

            await Handler().Handle(Ask("hi"), CancellationToken.None);

            _store.Verify(s => s.AddTurnAsync("u1:s1", "hi", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // ============================================================  Chat memory in Redis
    public class RedisConversationStoreTests
    {
        private readonly Mock<ICacheService> _cache = new();

        [Fact]
        public async Task AddTurn_Should_Keep_Only_The_Last_10_Turns()
        {
            var old = Enumerable.Range(1, 10).Select(i => new ChatTurn($"q{i}", $"a{i}")).ToList();
            _cache.Setup(c => c.GetAsync<List<ChatTurn>>("asset-chat:u1:s1:history", It.IsAny<CancellationToken>())).ReturnsAsync(old);
            List<ChatTurn>? saved = null;
            _cache.Setup(c => c.SetAsync("asset-chat:u1:s1:history", It.IsAny<List<ChatTurn>>(), TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()))
                  .Callback<string, List<ChatTurn>, TimeSpan, CancellationToken>((_, t, _, _) => saved = t);

            await new RedisConversationStore(_cache.Object).AddTurnAsync("u1:s1", "q11", "a11", CancellationToken.None);

            saved.Should().HaveCount(10);
            saved!.First().Question.Should().Be("q2");      // the oldest one was dropped
            saved.Last().Question.Should().Be("q11");
        }

        [Fact]
        public async Task AddTurn_Should_Cut_Very_Long_Answers()
        {
            List<ChatTurn>? saved = null;
            _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<ChatTurn>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                  .Callback<string, List<ChatTurn>, TimeSpan, CancellationToken>((_, t, _, _) => saved = t);

            await new RedisConversationStore(_cache.Object).AddTurnAsync("u1:s1", "q", new string('x', 5000), CancellationToken.None);

            saved!.Single().Answer.Length.Should().Be(1000 + "...".Length);
        }

        [Fact]
        public async Task GetHistory_Should_Return_An_Empty_List_When_Nothing_Is_Stored()
        {
            var history = await new RedisConversationStore(_cache.Object).GetHistoryAsync("u1:s1", CancellationToken.None);

            history.Should().BeEmpty();
        }
    }

    // ============================================================  Rule-based parser (the fallback without Gemini)
    public class RuleBasedAssetQuestionParserTests
    {
        private static Task<ParsedAssetQuestion> Parse(string q) =>
            new RuleBasedAssetQuestionParser().ParseAsync(q, Array.Empty<ChatTurn>(), null, CancellationToken.None);

        [Theory]
        [InlineData("how many laptops do we have", AssetQuestionIntent.CountAssets)]
        [InlineData("show me the available assets", AssetQuestionIntent.ListAssets)]
        public async Task Should_Detect_The_Intent(string question, AssetQuestionIntent expected)
        {
            (await Parse(question)).Intent.Should().Be(expected);
        }

        [Theory]
        [InlineData("show available assets", AssetStatus.Available)]
        [InlineData("list assets under maintenance", AssetStatus.UnderMaintenance)]
        [InlineData("show retired assets", AssetStatus.Retired)]
        public async Task Should_Detect_The_Status(string question, AssetStatus expected)
        {
            (await Parse(question)).Status.Should().Be(expected);
        }

        [Fact]
        public async Task Should_Detect_Questions_About_Myself()
        {
            (await Parse("show my assets")).IsAboutSelf.Should().BeTrue();
        }
    }
}
