using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Refresh
{
    internal sealed class ViewRefreshCache
    {
        private static readonly TimeSpan LiveViewRefreshInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan PastViewRefreshInterval = TimeSpan.FromMinutes(5);

        private long timelineDataVersion;
        private long processRuntimeDataVersion;
        private CacheEntry<SummaryViewRefreshKey>? summaryEntry;
        private CacheEntry<HeavyViewRefreshKey>? timelineEntry;
        private CacheEntry<HeavyViewRefreshKey>? detailEntry;

        public void MarkTimelineDataChanged()
        {
            Interlocked.Increment(ref timelineDataVersion);
        }

        public void MarkProcessRuntimeDataChanged()
        {
            Interlocked.Increment(ref processRuntimeDataVersion);
        }

        public void InvalidateCategoryDependentViews()
        {
            MarkTimelineDataChanged();
            MarkProcessRuntimeDataChanged();
            Clear();
        }

        public void Clear()
        {
            summaryEntry = null;
            timelineEntry = null;
            detailEntry = null;
        }

        public bool TryGetSummary(
            SummaryPeriodRange range,
            DateTimeOffset observedAt,
            out ViewRefreshSnapshot snapshot)
        {
            var key = SummaryViewRefreshKey.FromRange(range, Interlocked.Read(ref timelineDataVersion));
            if (summaryEntry is null
                || summaryEntry.Key != key
                || IsSummaryExpired(summaryEntry.CachedAt, range, observedAt))
            {
                snapshot = default!;
                return false;
            }

            snapshot = summaryEntry.Snapshot with { ReadElapsedMs = 0 };
            return true;
        }

        public bool TryGetTimeline(
            DateTime date,
            int categoryBucketMinutes,
            DateTimeOffset observedAt,
            out ViewRefreshSnapshot snapshot)
        {
            var key = HeavyViewRefreshKey.ForTimeline(
                date,
                categoryBucketMinutes,
                Interlocked.Read(ref timelineDataVersion));
            if (timelineEntry is null
                || timelineEntry.Key != key
                || IsHeavyViewExpired(timelineEntry.CachedAt, date, observedAt))
            {
                snapshot = default!;
                return false;
            }

            snapshot = LiveViewSnapshotRefresher.Refresh(timelineEntry.Snapshot, observedAt) with
            {
                ReadElapsedMs = 0
            };
            return true;
        }

        public bool TryGetDetail(
            DateTime date,
            long? selectedAppId,
            DateTimeOffset observedAt,
            out ViewRefreshSnapshot snapshot)
        {
            var key = HeavyViewRefreshKey.ForDetail(
                date,
                selectedAppId,
                Interlocked.Read(ref processRuntimeDataVersion));
            if (detailEntry is null
                || detailEntry.Key != key
                || IsHeavyViewExpired(detailEntry.CachedAt, date, observedAt))
            {
                snapshot = default!;
                return false;
            }

            snapshot = LiveViewSnapshotRefresher.Refresh(detailEntry.Snapshot, observedAt) with
            {
                ReadElapsedMs = 0
            };
            return true;
        }

        public void StoreSummary(
            SummaryPeriodRange range,
            DateTimeOffset observedAt,
            ViewRefreshSnapshot snapshot)
        {
            summaryEntry = new CacheEntry<SummaryViewRefreshKey>(
                SummaryViewRefreshKey.FromRange(range, Interlocked.Read(ref timelineDataVersion)),
                observedAt,
                snapshot);
        }

        public void StoreTimeline(
            DateTime date,
            int categoryBucketMinutes,
            DateTimeOffset observedAt,
            ViewRefreshSnapshot snapshot)
        {
            timelineEntry = new CacheEntry<HeavyViewRefreshKey>(
                HeavyViewRefreshKey.ForTimeline(
                    date,
                    categoryBucketMinutes,
                    Interlocked.Read(ref timelineDataVersion)),
                observedAt,
                snapshot);
        }

        public void StoreDetail(
            DateTime date,
            long? selectedAppId,
            DateTimeOffset observedAt,
            ViewRefreshSnapshot snapshot)
        {
            detailEntry = new CacheEntry<HeavyViewRefreshKey>(
                HeavyViewRefreshKey.ForDetail(
                    date,
                    selectedAppId,
                    Interlocked.Read(ref processRuntimeDataVersion)),
                observedAt,
                snapshot);
        }

        private static bool IsHeavyViewExpired(
            DateTimeOffset cachedAt,
            DateTime selectedDate,
            DateTimeOffset observedAt)
        {
            var interval = selectedDate.Date == observedAt.ToLocalTime().Date
                ? LiveViewRefreshInterval
                : PastViewRefreshInterval;
            return observedAt - cachedAt >= interval;
        }

        private static bool IsSummaryExpired(
            DateTimeOffset cachedAt,
            SummaryPeriodRange range,
            DateTimeOffset observedAt)
        {
            var today = observedAt.ToLocalTime().Date;
            var todayStart = new DateTimeOffset(today, TimeZoneInfo.Local.GetUtcOffset(today));
            var includesToday = range.Start < observedAt && range.End > todayStart;
            var interval = includesToday ? LiveViewRefreshInterval : PastViewRefreshInterval;
            return observedAt - cachedAt >= interval;
        }

        private sealed record CacheEntry<TKey>(
            TKey Key,
            DateTimeOffset CachedAt,
            ViewRefreshSnapshot Snapshot);

        private sealed record HeavyViewRefreshKey(
            string ViewName,
            DateTime Date,
            long? SelectedAppId,
            int CategoryBucketMinutes,
            long DataVersion)
        {
            public static HeavyViewRefreshKey ForTimeline(
                DateTime date,
                int categoryBucketMinutes,
                long dataVersion)
            {
                return new HeavyViewRefreshKey(
                    "timeline",
                    date.Date,
                    null,
                    categoryBucketMinutes,
                    dataVersion);
            }

            public static HeavyViewRefreshKey ForDetail(
                DateTime date,
                long? selectedAppId,
                long dataVersion)
            {
                return new HeavyViewRefreshKey("detail", date.Date, selectedAppId, 0, dataVersion);
            }
        }

        private sealed record SummaryViewRefreshKey(
            DateTime StartDate,
            DateTime EndDate,
            long DataVersion)
        {
            public static SummaryViewRefreshKey FromRange(
                SummaryPeriodRange range,
                long dataVersion)
            {
                return new SummaryViewRefreshKey(
                    range.Start.ToLocalTime().Date,
                    range.End.ToLocalTime().Date,
                    dataVersion);
            }
        }
    }
}
