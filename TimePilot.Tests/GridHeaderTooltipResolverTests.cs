using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class GridHeaderTooltipResolverTests
    {
        public GridHeaderTooltipResolverTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void GetTooltipText_ReturnsUsageGridTooltip()
        {
            var tooltip = GridHeaderTooltipResolver.GetTooltipText("usageGrid", "usageRatioColumn");

            Assert.Equal(UiText.Main.UsageRatioTooltip, tooltip);
        }

        [Fact]
        public void GetTooltipText_ReturnsRuntimeGridTooltip()
        {
            var tooltip = GridHeaderTooltipResolver.GetTooltipText("runtimeGrid", "runtimeDurationColumn");

            Assert.Equal(UiText.Main.RuntimeDurationTooltip, tooltip);
        }

        [Fact]
        public void GetTooltipText_ReturnsNullForUnsupportedColumn()
        {
            var tooltip = GridHeaderTooltipResolver.GetTooltipText("timelineGrid", "timelineDurationColumn");

            Assert.Null(tooltip);
        }
    }
}
