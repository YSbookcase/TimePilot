using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineHighlightMenuTextTests
    {
        [Theory]
        [InlineData((int)UiLanguage.English, "Highlight this segment", "Highlight this app")]
        [InlineData((int)UiLanguage.Korean, "이 구간 강조", "이 앱 강조")]
        [InlineData(-1, "이 구간 강조", "이 앱 강조")]
        public void MenuText_PreservesBothLabelsAndFallback(
            int languageValue, string expectedSegment, string expectedApp)
        {
            var language = (UiLanguage)languageValue;
            Assert.Equal(expectedSegment, TimelineHighlightMenuText.ForSegment(language));
            Assert.Equal(expectedApp, TimelineHighlightMenuText.ForApp(language));
        }
    }
}
