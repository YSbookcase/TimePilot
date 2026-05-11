namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsageSummary(
        string AppName,
        long ActiveUsageMs,
        DateTimeOffset FirstStartedAt,
        DateTimeOffset LastObservedAt);
}
