using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Refresh
{
    internal static class LiveViewSnapshotRefresher
    {
        public static ViewRefreshSnapshot Refresh(
            ViewRefreshSnapshot snapshot,
            DateTimeOffset observedAt)
        {
            return snapshot with
            {
                TimelineRows = RefreshTimelineRows(snapshot.TimelineRows, observedAt),
                WindowsRuntimeRanges = ClampTimelineRanges(snapshot.WindowsRuntimeRanges, observedAt),
                SystemTimelineRanges = ClampSystemTimelineRanges(snapshot.SystemTimelineRanges, observedAt),
                CategoryTimelineSegments = ClampCategorySegments(snapshot.CategoryTimelineSegments, observedAt),
                RuntimeRows = RefreshRuntimeRows(snapshot.RuntimeRows, observedAt),
                RuntimeSegmentRows = RefreshRuntimeSegmentRows(snapshot.RuntimeSegmentRows, observedAt)
            };
        }

        private static IReadOnlyList<ActivityTimelineRow>? RefreshTimelineRows(
            IReadOnlyList<ActivityTimelineRow>? rows,
            DateTimeOffset observedAt)
        {
            return rows?.Select(row =>
            {
                if (row.EndedAt is not null)
                    return row;

                return row with { DurationMs = GetDurationMs(row.StartedAt, observedAt) };
            }).ToList();
        }

        private static IReadOnlyList<TimelineRange>? ClampTimelineRanges(
            IReadOnlyList<TimelineRange>? ranges,
            DateTimeOffset observedAt)
        {
            return ranges?.Select(range =>
                range.EndedAt > observedAt
                    ? range with { EndedAt = observedAt }
                    : range).ToList();
        }

        private static IReadOnlyList<SystemTimelineRange>? ClampSystemTimelineRanges(
            IReadOnlyList<SystemTimelineRange>? ranges,
            DateTimeOffset observedAt)
        {
            return ranges?.Select(range =>
                range.EndedAt > observedAt
                    ? range with { EndedAt = observedAt }
                    : range).ToList();
        }

        private static IReadOnlyList<CategoryTimelineSegment>? ClampCategorySegments(
            IReadOnlyList<CategoryTimelineSegment>? segments,
            DateTimeOffset observedAt)
        {
            return segments?.Select(segment =>
                segment.EndedAt > observedAt
                    ? segment with { EndedAt = observedAt }
                    : segment).ToList();
        }

        private static IReadOnlyList<ProcessRuntimeSummaryRow>? RefreshRuntimeRows(
            IReadOnlyList<ProcessRuntimeSummaryRow>? rows,
            DateTimeOffset observedAt)
        {
            return rows?.Select(row =>
            {
                if (!row.HasRunningSession)
                    return row;

                var baseEnd = row.LastObservedAt ?? row.FirstObservedAt ?? observedAt;
                var deltaMs = Math.Max(0, (long)(observedAt - baseEnd).TotalMilliseconds);
                return row with { RuntimeMs = row.RuntimeMs + deltaMs };
            }).ToList();
        }

        private static IReadOnlyList<ProcessRuntimeSegmentRow>? RefreshRuntimeSegmentRows(
            IReadOnlyList<ProcessRuntimeSegmentRow>? rows,
            DateTimeOffset observedAt)
        {
            return rows?.Select(row =>
            {
                if (row.EndedAt is not null)
                    return row;

                return row with { DurationMs = GetDurationMs(row.StartedAt, observedAt) };
            }).ToList();
        }

        private static long GetDurationMs(DateTimeOffset startedAt, DateTimeOffset endedAt)
        {
            return Math.Max(0, (long)(endedAt - startedAt).TotalMilliseconds);
        }
    }
}
