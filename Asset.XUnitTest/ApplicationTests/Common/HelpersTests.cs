#region
using System.Globalization;
using Asset.Application.Common;
using Asset.Application.Common.Caching;
using Asset.Application.Features.Employees;
using Asset.Domain.Exceptions;
using FluentAssertions;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Common
{
    // Small pure helpers: no mocks needed, just input → output.
    public class LocalizedNameTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]          // only spaces = no Arabic name → NULL (the filtered unique index ignores NULL)
        [InlineData("  حسن  ", "حسن")]
        public void TrimOrNull_Should_Return_Null_For_Empty_And_Trimmed_Text_Otherwise(string? input, string? expected)
        {
            LocalizedName.TrimOrNull(input).Should().Be(expected);
        }

        [Fact]
        public void Pick_Should_Return_Arabic_When_UI_Is_Arabic_And_Arabic_Exists()
        {
            using var _ = new UiCulture("ar");
            LocalizedName.Pick("Laptop", "لابتوب").Should().Be("لابتوب");
        }

        [Fact]
        public void Pick_Should_Fall_Back_To_English_When_There_Is_No_Arabic()
        {
            using var _ = new UiCulture("ar");
            LocalizedName.Pick("Laptop", null).Should().Be("Laptop");
            LocalizedName.Pick("Laptop", "  ").Should().Be("Laptop");
        }

        [Fact]
        public void Pick_Should_Return_English_When_UI_Is_English()
        {
            using var _ = new UiCulture("en");
            LocalizedName.Pick("Laptop", "لابتوب").Should().Be("Laptop");
        }

        // Sets CurrentUICulture for one test and puts the old one back (tests must not leak state).
        private sealed class UiCulture : IDisposable
        {
            private readonly CultureInfo _old = CultureInfo.CurrentUICulture;
            public UiCulture(string name) => CultureInfo.CurrentUICulture = new CultureInfo(name);
            public void Dispose() => CultureInfo.CurrentUICulture = _old;
        }
    }

    public class EmployeeCodesTests
    {
        [Fact]
        public void Next_Should_Start_At_EMP_001_When_There_Are_No_Codes()
        {
            EmployeeCodes.Next(Array.Empty<string>()).Should().Be("EMP-001");
        }

        [Fact]
        public void Next_Should_Be_The_Biggest_Number_Plus_One_Not_The_Count()
        {
            // 3 codes, but the biggest is 8 → 9 (a gap in the numbers must not be reused)
            EmployeeCodes.Next(new[] { "EMP-001", "EMP-008", "EMP-003" }).Should().Be("EMP-009");
        }

        [Fact]
        public void Next_Should_Grow_Past_Three_Digits()
        {
            EmployeeCodes.Next(new[] { "EMP-999" }).Should().Be("EMP-1000");
        }

        [Fact]
        public void Next_Should_Ignore_Codes_That_Are_Not_Prefix_Plus_Digits()
        {
            EmployeeCodes.Next(new[] { "EMP-ABC", "EMP-", "EMP-005" }).Should().Be("EMP-006");
        }
    }

    public class CacheSettingsTests
    {
        [Fact]
        public void GetDuration_Should_Map_Each_Group_To_Its_Minutes()
        {
            var settings = new CacheSettings { LookupMinutes = 20, VolatileMinutes = 2 };

            settings.GetDuration(CacheDuration.Lookup).Should().Be(TimeSpan.FromMinutes(20));
            settings.GetDuration(CacheDuration.Volatile).Should().Be(TimeSpan.FromMinutes(2));
        }
    }

    public class AppExceptionTests
    {
        [Fact]
        public void Exception_Should_Keep_The_Template_And_The_Args_Separately()
        {
            // The template is the translation key, so the id must NOT be inside the message.
            var ex = new NotFoundException("Asset {0} was not found.", 42);

            ex.Message.Should().Be("Asset {0} was not found.");
            ex.Args.Should().Equal(42);
        }
    }
}
