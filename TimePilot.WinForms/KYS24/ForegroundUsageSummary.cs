namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsageSummary(
        string AppName,
        string? ExecutablePath,
        long ActiveUsageMs,
        int SwitchCount,
        DateTimeOffset FirstStartedAt,
        DateTimeOffset LastObservedAt);
}
