using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class SummaryOverviewFormatterTests
    {
        public SummaryOverviewFormatterTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void Build_ReturnsZeroStateMetricsWhenNoRowsExist()
        {
            var metrics = SummaryOverviewFormatter.Build(Array.Empty<UsageSummaryRow>());

            Assert.Collection(
                metrics,
                metric =>
                {
                    Assert.Equal("Total active usage", metric.Label);
                    Assert.Equal("00:00", metric.Value);
                },
                metric =>
                {
                    Assert.Equal("Apps", metric.Label);
                    Assert.Equal("0", metric.Value);
                },
                metric =>
                {
                    Assert.Equal("Top app", metric.Label);
                    Assert.Equal("No records", metric.Value);
                },
                metric =>
                {
                    Assert.Equal("Switches", metric.Label);
                    Assert.Equal("0", metric.Value);
                });
        }

        [Fact]
        public void Build_SummarizesActiveRows()
        {
            var metrics = SummaryOverviewFormatter.Build(
            [
                CreateRow("Editor", "editor", 3_600_000, 0.75, 8),
                CreateRow("Browser", "browser", 1_200_000, 0.25, 3),
                CreateRow("Ignored", "ignored", 0, 0, 2)
            ]);

            Assert.Collection(
                metrics,
                metric =>
                {
                    Assert.Equal("Total active usage", metric.Label);
                    Assert.Equal("01:20:00", metric.Value);
                },
                metric =>
                {
                    Assert.Equal("Apps", metric.Label);
                    Assert.Equal("2", metric.Value);
                },
                metric =>
                {
                    Assert.Equal("Top app", metric.Label);
                    Assert.Equal("Editor", metric.Value);
                    Assert.Equal("01:00:00", metric.Detail);
                },
                metric =>
                {
                    Assert.Equal("Switches", metric.Label);
                    Assert.Equal("11", metric.Value);
                });
        }

        private static UsageSummaryRow CreateRow(
            string appName,
            string processName,
            long activeUsageMs,
            double usageRatio,
            int switchCount)
        {
            return new UsageSummaryRow(
                null,
                appName,
                processName,
                null,
                null,
                null,
                activeUsageMs,
                usageRatio,
                switchCount);
        }
    }
}
