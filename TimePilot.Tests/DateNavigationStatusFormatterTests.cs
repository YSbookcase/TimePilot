using System.Globalization;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DateNavigationStatusFormatterTests
    {
        [Theory]
        [InlineData("en-US", "2026-01-02")]
        [InlineData("ko-KR", "2026-01-02")]
        [InlineData("th-TH", "2569-01-02")]
        public void BuildAutoAdvancedStatus_PreservesLanguageAndCurrentCulture(
            string cultureName,
            string expectedDate)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var date = new DateTime(2026, 1, 2, 23, 45, 0);

                Assert.Equal($"Moved today's views to {expectedDate}",
                    DateNavigationStatusFormatter.BuildAutoAdvancedStatus(date, UiLanguage.English));
                Assert.Equal($"오늘 보기 날짜를 {expectedDate}(으)로 갱신했습니다.",
                    DateNavigationStatusFormatter.BuildAutoAdvancedStatus(date, UiLanguage.Korean));
                Assert.Equal($"오늘 보기 날짜를 {expectedDate}(으)로 갱신했습니다.",
                    DateNavigationStatusFormatter.BuildAutoAdvancedStatus(date, (UiLanguage)(-1)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
