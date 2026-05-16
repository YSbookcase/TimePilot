namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsageSummary(
        long AppId,
        string AppName,
        string ProcessName,
        string? ExecutablePath,
        long ActiveUsageMs,
        int SwitchCount,
        DateTimeOffset FirstStartedAt,
        DateTimeOffset LastObservedAt);
}
