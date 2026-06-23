using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class GridSortPropertyResolverTests
    {
        [Fact]
        public void GetUsageSortPropertyName_MapsKnownColumns()
        {
            Assert.Equal(
                nameof(UsageSummaryRow.ActiveUsageMs),
                GridSortPropertyResolver.GetUsageSortPropertyName("activeUsageTimeColumn"));
            Assert.Equal(
                nameof(UsageSummaryRow.CategoryText),
                GridSortPropertyResolver.GetUsageSortPropertyName("appCategoryColumn"));
            Assert.Null(GridSortPropertyResolver.GetUsageSortPropertyName("unknownColumn"));
        }

        [Fact]
        public void GetRuntimeSortPropertyName_MapsKnownColumns()
        {
            Assert.Equal(
                nameof(ProcessRuntimeSummaryRow.RuntimeMs),
                GridSortPropertyResolver.GetRuntimeSortPropertyName("runtimeDurationColumn"));
            Assert.Equal(
                nameof(ProcessRuntimeSummaryRow.ActualUsageRatio),
                GridSortPropertyResolver.GetRuntimeSortPropertyName("runtimeActualUsageRatioColumn"));
            Assert.Null(GridSortPropertyResolver.GetRuntimeSortPropertyName("runtimeIconColumn"));
        }

        [Fact]
        public void GetRuntimeSegmentSortPropertyName_MapsKnownColumns()
        {
            Assert.Equal(
                nameof(ProcessRuntimeSegmentRow.StartedAt),
                GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName("runtimeSegmentStartedAtColumn"));
            Assert.Equal(
                nameof(ProcessRuntimeSegmentRow.ObservationTypeText),
                GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName("runtimeSegmentObservationTypeColumn"));
            Assert.Null(GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName("runtimeSegmentUnknownColumn"));
        }

        [Fact]
        public void NormalizeSortProperty_FallsBackToDefaultForUnknownValues()
        {
            Assert.Equal(
                nameof(UsageSummaryRow.ActiveUsageMs),
                GridSortPropertyResolver.NormalizeUsageSortProperty("missing"));
            Assert.Equal(
                nameof(DailyUsageTrendRow.Date),
                GridSortPropertyResolver.NormalizeDailyUsageTrendSortProperty("missing"));
            Assert.Equal(
                nameof(ActivityTimelineRow.StartedAt),
                GridSortPropertyResolver.NormalizeTimelineSortProperty("missing"));
            Assert.Equal(
                nameof(ProcessRuntimeSummaryRow.RuntimeMs),
                GridSortPropertyResolver.NormalizeRuntimeSortProperty("missing"));
            Assert.Equal(
                nameof(ProcessRuntimeSegmentRow.StartedAt),
                GridSortPropertyResolver.NormalizeRuntimeSegmentSortProperty("missing"));
        }

        [Fact]
        public void NormalizeSortProperty_PreservesKnownValues()
        {
            Assert.Equal(
                nameof(UsageSummaryRow.AppName),
                GridSortPropertyResolver.NormalizeUsageSortProperty(nameof(UsageSummaryRow.AppName)));
            Assert.Equal(
                nameof(ActivityTimelineRow.DisplayName),
                GridSortPropertyResolver.NormalizeTimelineSortProperty(nameof(ActivityTimelineRow.DisplayName)));
            Assert.Equal(
                nameof(ProcessRuntimeSummaryRow.StatusText),
                GridSortPropertyResolver.NormalizeRuntimeSortProperty(nameof(ProcessRuntimeSummaryRow.StatusText)));
        }
    }
}
