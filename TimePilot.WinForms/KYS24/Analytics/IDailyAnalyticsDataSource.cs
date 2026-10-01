namespace TimePilot.WinForms.KYS24.Analytics
{
    internal interface IDailyAnalyticsDataSource
    {
        IReadOnlyList<DailyUsageTrendRow> GetDailyUsageTrend(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd);

        IdleUsageSummary GetIdleUsage(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd);

        RuntimeCoverageSummary GetRuntimeCoverage(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            DateTimeOffset observedAt);
    }
}
