using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Refresh;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class LiveViewSnapshotRefresherTests
    {
        [Fact]
        public void Refresh_AdvancesRunningRowsAndClampsOpenRanges()
        {
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.FromHours(9));
            var startedAt = observedAt.AddMinutes(-5);
            var snapshot = CreateSnapshot(
                timelineRows:
                [
                    new ActivityTimelineRow("App", startedAt, null, 0, "App")
                ],
                windowsRuntimeRanges:
                [
                    new TimelineRange(startedAt, observedAt.AddMinutes(10))
                ],
                runtimeRows:
                [
                    CreateRuntimeRow(startedAt, 60_000)
                ],
                runtimeSegmentRows:
                [
                    new ProcessRuntimeSegmentRow(startedAt, null, 0, 1, true, true)
                ]);

            var result = LiveViewSnapshotRefresher.Refresh(snapshot, observedAt);

            Assert.Equal(300_000, Assert.Single(result.TimelineRows!).DurationMs);
            Assert.Equal(observedAt, Assert.Single(result.WindowsRuntimeRanges!).EndedAt);
            Assert.Equal(360_000, Assert.Single(result.RuntimeRows!).RuntimeMs);
            Assert.Equal(300_000, Assert.Single(result.RuntimeSegmentRows!).DurationMs);
        }

        [Fact]
        public void Refresh_DoesNotChangeCompletedRows()
        {
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.FromHours(9));
            var endedAt = observedAt.AddMinutes(-2);
            var row = new ActivityTimelineRow(
                "App",
                observedAt.AddMinutes(-5),
                endedAt,
                180_000,
                "App");
            var snapshot = CreateSnapshot(timelineRows: [row]);

            var result = LiveViewSnapshotRefresher.Refresh(snapshot, observedAt);

            Assert.Equal(row, Assert.Single(result.TimelineRows!));
        }

        private static ProcessRuntimeSummaryRow CreateRuntimeRow(
            DateTimeOffset lastObservedAt,
            long runtimeMs)
        {
            return new ProcessRuntimeSummaryRow(
                1,
                "App",
                "app",
                null,
                null,
                null,
                runtimeMs,
                0,
                0,
                null,
                1,
                true,
                true,
                true,
                LastObservedAt: lastObservedAt);
        }

        private static ViewRefreshSnapshot CreateSnapshot(
            IReadOnlyList<ActivityTimelineRow>? timelineRows = null,
            IReadOnlyList<TimelineRange>? windowsRuntimeRanges = null,
            IReadOnlyList<ProcessRuntimeSummaryRow>? runtimeRows = null,
            IReadOnlyList<ProcessRuntimeSegmentRow>? runtimeSegmentRows = null)
        {
            return new ViewRefreshSnapshot(
                null,
                null,
                null,
                null,
                false,
                null,
                null,
                timelineRows,
                windowsRuntimeRanges,
                null,
                null,
                null,
                null,
                null,
                runtimeRows,
                null,
                runtimeSegmentRows,
                0);
        }
    }
}
