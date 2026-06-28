using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Refresh;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class ViewRefreshCacheTests
    {
        [Fact]
        public void TryGetTimeline_ReusesLiveSnapshotBeforeTenSeconds()
        {
            var cache = new ViewRefreshCache();
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.FromHours(9));
            var snapshot = CreateSnapshot(timelineRows: Array.Empty<ActivityTimelineRow>());
            cache.StoreTimeline(observedAt.Date, 30, observedAt, snapshot);

            var found = cache.TryGetTimeline(observedAt.Date, 30, observedAt.AddSeconds(9), out var result);

            Assert.True(found);
            Assert.Equal(0, result.ReadElapsedMs);
        }

        [Fact]
        public void TryGetTimeline_ExpiresLiveSnapshotAtTenSeconds()
        {
            var cache = new ViewRefreshCache();
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.FromHours(9));
            cache.StoreTimeline(
                observedAt.Date,
                30,
                observedAt,
                CreateSnapshot(timelineRows: Array.Empty<ActivityTimelineRow>()));

            var found = cache.TryGetTimeline(observedAt.Date, 30, observedAt.AddSeconds(10), out _);

            Assert.False(found);
        }

        [Fact]
        public void TryGetTimeline_ReusesPastSnapshotBeforeFiveMinutes()
        {
            var cache = new ViewRefreshCache();
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.FromHours(9));
            var pastDate = observedAt.Date.AddDays(-1);
            cache.StoreTimeline(
                pastDate,
                30,
                observedAt,
                CreateSnapshot(timelineRows: Array.Empty<ActivityTimelineRow>()));

            var found = cache.TryGetTimeline(pastDate, 30, observedAt.AddMinutes(4), out _);

            Assert.True(found);
        }

        [Fact]
        public void MarkTimelineDataChanged_InvalidatesTimelineSnapshot()
        {
            var cache = new ViewRefreshCache();
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.FromHours(9));
            cache.StoreTimeline(
                observedAt.Date,
                30,
                observedAt,
                CreateSnapshot(timelineRows: Array.Empty<ActivityTimelineRow>()));

            cache.MarkTimelineDataChanged();

            Assert.False(cache.TryGetTimeline(observedAt.Date, 30, observedAt.AddSeconds(1), out _));
        }

        private static ViewRefreshSnapshot CreateSnapshot(
            IReadOnlyList<ActivityTimelineRow>? timelineRows = null)
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
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                123);
        }
    }
}
