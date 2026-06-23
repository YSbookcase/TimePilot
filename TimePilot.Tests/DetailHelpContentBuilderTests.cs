using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DetailHelpContentBuilderTests
    {
        public DetailHelpContentBuilderTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void GetFilterText_ReturnsSelectedFilterLabel()
        {
            var text = DetailHelpContentBuilder.GetFilterText(DetailRuntimeFilter.VisibleApps);

            Assert.Equal(UiText.Main.DetailFilterVisibleApps, text);
        }

        [Fact]
        public void GetFilterDescription_ReturnsSelectedFilterDescription()
        {
            var description = DetailHelpContentBuilder.GetFilterDescription(DetailRuntimeFilter.UserProcesses);

            Assert.Equal(UiText.Main.DetailFilterUserProcessesDescription, description);
        }

        [Fact]
        public void BuildMessage_IncludesCurrentSelectionAndBaseHelp()
        {
            var message = DetailHelpContentBuilder.BuildMessage(DetailRuntimeFilter.AllRecords);

            Assert.Contains(UiText.Main.DetailFilterAllRecords, message);
            Assert.Contains(UiText.Main.DetailFilterAllRecordsDescription, message);
            Assert.Contains(UiText.Main.DetailHelpMessage, message);
        }
    }
}
