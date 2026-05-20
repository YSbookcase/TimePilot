namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsageSummary(
        long AppId,
        string AppName,
        string ProcessName,
        string? ExecutablePath,
        long? PrimaryCategoryId,
        string? CategoryName,
        long ActiveUsageMs,
        int SwitchCount,
        DateTimeOffset FirstStartedAt,
        DateTimeOffset LastObservedAt);
}
