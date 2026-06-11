namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsageSummary(
        long AppId,
        string AppName,
        string ProcessName,
        string? ExecutablePath,
        long? PrimaryCategoryId,
        string? CategoryName,
        string? CategoryColor,
        long ActiveUsageMs,
        long IdleRecordedMs,
        int SwitchCount,
        DateTimeOffset FirstStartedAt,
        DateTimeOffset LastObservedAt);
}
