namespace TimePilot.WinForms.KYS24.Analytics
{
    internal sealed class TimePilotStorageDailyAnalyticsDataSource : IDailyAnalyticsDataSource
    {
        private readonly TimePilotStorage storage;

        public TimePilotStorageDailyAnalyticsDataSource(TimePilotStorage storage)
        {
            this.storage = storage;
        }

        public IReadOnlyList<DailyUsageTrendRow> GetDailyUsageTrend(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd)
        {
            return storage.GetDailyUsageTrendForPeriod(periodStart, periodEnd);
        }

        public IdleUsageSummary GetIdleUsage(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd)
        {
            return storage.GetIdleUsageForPeriod(periodStart, periodEnd);
        }

        public RuntimeCoverageSummary GetRuntimeCoverage(
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            DateTimeOffset observedAt)
        {
            return storage.GetRuntimeCoverageForPeriod(periodStart, periodEnd, observedAt);
        }
    }
}
