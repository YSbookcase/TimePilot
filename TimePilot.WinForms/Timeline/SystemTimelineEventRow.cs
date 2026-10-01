namespace TimePilot.WinForms.Timeline
{
    internal sealed record SystemTimelineEventRow(
        DateTimeOffset OccurredAt,
        string OccurredAtText,
        string EventTypeText,
        long PreviousIntervalMs,
        string PreviousIntervalText,
        string RelationText,
        string DetailsText);
}
