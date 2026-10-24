using System.Globalization;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppWebSearchQueryFormatterTests
    {
        [Theory]
        [InlineData("이란", "Editor editor.exe 이란")]
        [InlineData(" 이란? ", "Editor editor.exe 이란?")]
        [InlineData("사용법", "Editor editor.exe 사용법")]
        [InlineData("", "Editor editor.exe")]
        [InlineData(" \t ", "Editor editor.exe")]
        [InlineData(null, "Editor editor.exe 이란")]
        public void Format_KoreanSearchUsesConfiguredSuffix(string? suffix, string expected)
        {
            Assert.Equal(expected,
                AppWebSearchQueryFormatter.Format("Editor", "editor.exe", UiLanguage.Korean, suffix));
        }

        [Fact]
        public void Format_EnglishSearchDoesNotUseKoreanSuffix()
        {
            Assert.Equal("Editor editor.exe",
                AppWebSearchQueryFormatter.Format("Editor", "editor.exe", UiLanguage.English, "이란?"));
        }

        [Fact]
        public void Format_EmptyAppNamesDoNotProduceSuffixOnlySearch()
        {
            Assert.Equal(string.Empty,
                AppWebSearchQueryFormatter.Format(" ", "", UiLanguage.Korean, "이란?"));
        }

        [Theory]
        [InlineData("  Sample Editor  ", " editor.exe ", "Sample Editor editor.exe")]
        [InlineData("", "editor.exe", "editor.exe")]
        [InlineData("Sample", " \t ", "Sample")]
        [InlineData(" \t ", "", "")]
        [InlineData(" 한글 앱 ", "프로세스.exe", "한글 앱 프로세스.exe")]
        [InlineData("A & B", "a+b.exe", "A & B a+b.exe")]
        public void Format_TrimsAndCombinesNonblankNamesInOrder(
            string appName, string processName, string expected)
        {
            Assert.Equal(expected, AppWebSearchQueryFormatter.Format(appName, processName));
        }

        [Theory]
        [InlineData("en-US", "FILE", "file", "FILE")]
        [InlineData("tr-TR", "FILE", "file", "FILE file")]
        [InlineData("tr-TR", "FILE", "fıle", "FILE")]
        public void Format_RemovesDuplicatesUsingCurrentCulture(
            string cultureName, string appName, string processName, string expected)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                Assert.Equal(expected, AppWebSearchQueryFormatter.Format(appName, processName));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
