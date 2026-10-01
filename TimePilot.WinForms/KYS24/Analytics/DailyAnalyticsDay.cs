namespace TimePilot.WinForms.KYS24.Analytics
{
    internal sealed record DailyAnalyticsDay(
        DateTime Date,
        long ActiveUsageMs,
        long IdleRecordedMs,
        string? TopAppName,
        long TopAppUsageMs,
        DailyRuntimeCoverageMetrics Coverage);
}
