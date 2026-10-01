using System.ComponentModel;
using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelineSystemEventPresenter
    {
        public static IReadOnlyList<SystemTimelineEvent> FilterEvents(
            IReadOnlyList<SystemTimelineEvent> events,
            TimelineSystemEventFilter filter)
        {
            if (filter == TimelineSystemEventFilter.All)
                return events;

            return events
                .Where(systemEvent => TimelineSystemEventFilterMatcher.Matches(systemEvent.EventType, filter))
                .ToList();
        }

        public static IReadOnlyList<SystemTimelineRange> FilterRanges(
            IReadOnlyList<SystemTimelineRange> ranges,
            TimelineSystemEventFilter filter)
        {
            return filter switch
            {
                TimelineSystemEventFilter.All => ranges,
                TimelineSystemEventFilter.Lock => ranges
                    .Where(range => range.RangeType == SystemTimelineRangeType.LockSession)
                    .ToList(),
                TimelineSystemEventFilter.Power => ranges
                    .Where(range => range.RangeType == SystemTimelineRangeType.SleepEstimate)
                    .ToList(),
                _ => Array.Empty<SystemTimelineRange>()
            };
        }

        public static IReadOnlyList<SystemTimelineEventRow> BuildRows(
            IReadOnlyList<SystemTimelineEvent> events)
        {
            var orderedEvents = events.OrderBy(systemEvent => systemEvent.OccurredAt).ToList();
            var rows = new List<SystemTimelineEventRow>(orderedEvents.Count);
            SystemTimelineEvent? previousEvent = null;

            foreach (var systemEvent in orderedEvents)
            {
                var previousIntervalMs = previousEvent is null
                    ? -1
                    : (long)(systemEvent.OccurredAt - previousEvent.OccurredAt).TotalMilliseconds;
                var intervalText = previousEvent is null
                    ? "-"
                    : RuntimeDiagnosticsMessageBuilder.FormatDuration(previousIntervalMs);

                rows.Add(new SystemTimelineEventRow(
                    systemEvent.OccurredAt,
                    systemEvent.OccurredAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture),
                    RuntimeDiagnosticsMessageBuilder.GetSystemEventTypeText(systemEvent.EventType),
                    previousIntervalMs,
                    intervalText,
                    SystemTimelineEventTextFormatter.GetRelationText(systemEvent.EventType),
                    SystemTimelineEventTextFormatter.FormatDetails(systemEvent)));
                previousEvent = systemEvent;
            }

            return rows.OrderByDescending(row => row.OccurredAt).ToList();
        }

        public static IReadOnlyList<SystemTimelineEventRow> SortRows(
            IReadOnlyList<SystemTimelineEventRow> rows,
            string propertyName,
            ListSortDirection direction)
        {
            IOrderedEnumerable<SystemTimelineEventRow> orderedRows = propertyName switch
            {
                nameof(SystemTimelineEventRow.OccurredAtText) => rows.OrderBy(row => row.OccurredAt),
                nameof(SystemTimelineEventRow.PreviousIntervalText) => rows.OrderBy(row => row.PreviousIntervalMs),
                nameof(SystemTimelineEventRow.EventTypeText) => rows.OrderBy(row => row.EventTypeText, StringComparer.CurrentCulture),
                nameof(SystemTimelineEventRow.RelationText) => rows.OrderBy(row => row.RelationText, StringComparer.CurrentCulture),
                nameof(SystemTimelineEventRow.DetailsText) => rows.OrderBy(row => row.DetailsText, StringComparer.CurrentCulture),
                _ => rows.OrderBy(row => row.OccurredAt)
            };

            return direction == ListSortDirection.Ascending
                ? orderedRows.ToList()
                : orderedRows.Reverse().ToList();
        }
    }
}
