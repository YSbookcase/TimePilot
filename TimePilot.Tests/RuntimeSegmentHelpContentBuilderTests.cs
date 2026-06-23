using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class RuntimeSegmentHelpContentBuilderTests
    {
        [Fact]
        public void GetHelpTitle_ReturnsEnglishTitle()
        {
            UiText.UseLanguage(UiLanguage.English);

            var title = RuntimeSegmentHelpContentBuilder.GetHelpTitle();

            Assert.Equal("Selected App Runtime Chart Help", title);
        }

        [Fact]
        public void GetHelpMessage_IncludesRuntimeSegmentSelectionGuidance()
        {
            UiText.UseLanguage(UiLanguage.English);

            var message = RuntimeSegmentHelpContentBuilder.GetHelpMessage();

            Assert.Contains("Selecting a runtime segment row", message);
        }
    }
}
