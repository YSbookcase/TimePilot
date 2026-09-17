using System.Globalization;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppSettingsLanguageTests
    {
        [Theory]
        [InlineData("ko-KR", "Korean")]
        [InlineData("ko", "Korean")]
        [InlineData("en-US", "English")]
        [InlineData("ja-JP", "English")]
        public void GetDefaultUiLanguage_UsesKoreanOnlyForKoreanWindows(
            string cultureName,
            string expectedLanguage)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);

            Assert.Equal(expectedLanguage, AppSettings.GetDefaultUiLanguage(culture).ToString());
        }
    }
}
