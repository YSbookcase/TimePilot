namespace TimePilot.WinForms.KYS24.Analytics
{
    internal sealed class DailyAnalyticsProvider : IDailyAnalyticsProvider
    {
        private readonly IDailyAnalyticsDataSource dataSource;

        public DailyAnalyticsProvider(IDailyAnalyticsDataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public DailyAnalyticsSnapshot GetSnapshot(
            DailyAnalyticsRange range,
            DateTimeOffset observedAt)
        {
            ArgumentNullException.ThrowIfNull(range);

            var periodStart = CreateLocalDateTimeOffset(range.StartDate);
            var periodEnd = CreateLocalDateTimeOffset(range.EndDate.AddDays(1));
            var usageTrendByDate = dataSource
                .GetDailyUsageTrend(periodStart, periodEnd)
                .GroupBy(row => row.Date.Date)
                .ToDictionary(group => group.Key, group => group.First());

            var days = range
                .EnumerateDates()
                .Select(date => CreateDay(date, observedAt, usageTrendByDate))
                .ToList();

            return new DailyAnalyticsSnapshot(range, days);
        }

        private DailyAnalyticsDay CreateDay(
            DateTime date,
            DateTimeOffset observedAt,
            IReadOnlyDictionary<DateTime, DailyUsageTrendRow> usageTrendByDate)
        {
            var dayStart = CreateLocalDateTimeOffset(date);
            var dayEnd = CreateLocalDateTimeOffset(date.AddDays(1));
            usageTrendByDate.TryGetValue(date.Date, out var usageTrend);
            var idleUsage = dataSource.GetIdleUsage(dayStart, dayEnd);
            var coverage = dataSource.GetRuntimeCoverage(dayStart, dayEnd, observedAt);

            return new DailyAnalyticsDay(
                date.Date,
                usageTrend?.ActiveUsageMs ?? 0,
                idleUsage.IdleMs,
                usageTrend?.TopAppName,
                usageTrend?.TopAppUsageMs ?? 0,
                DailyRuntimeCoverageMetrics.FromSummary(coverage));
        }

        private static DateTimeOffset CreateLocalDateTimeOffset(DateTime localDate)
        {
            return new DateTimeOffset(
                localDate.Date,
                TimeZoneInfo.Local.GetUtcOffset(localDate.Date));
        }
    }
}
