namespace TimePilot.WinForms.KYS24
{
    internal sealed record SystemTimelineRange(
        DateTimeOffset StartedAt,
        DateTimeOffset EndedAt,
        SystemTimelineRangeType RangeType);
}
