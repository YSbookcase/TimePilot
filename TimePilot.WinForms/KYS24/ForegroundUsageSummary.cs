namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsageSummary(
        string AppName,
        string? ExecutablePath,
        long ActiveUsageMs,
        DateTimeOffset FirstStartedAt,
        DateTimeOffset LastObservedAt);
}
