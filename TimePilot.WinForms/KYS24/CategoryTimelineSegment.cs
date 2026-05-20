namespace TimePilot.WinForms.KYS24
{
    internal sealed record CategoryTimelineSegment(
        DateTimeOffset StartedAt,
        DateTimeOffset EndedAt,
        string CategoryName,
        string? Color,
        bool IsMixed,
        long ActiveUsageMs,
        string DetailText);
}
