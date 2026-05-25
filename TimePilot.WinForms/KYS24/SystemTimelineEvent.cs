namespace TimePilot.WinForms.KYS24
{
    internal sealed record SystemTimelineEvent(
        DateTimeOffset OccurredAt,
        string EventType,
        string? Details);
}
