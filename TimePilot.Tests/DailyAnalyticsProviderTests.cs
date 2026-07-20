using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.KYS24.Analytics;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DailyAnalyticsProviderTests
    {
        [Fact]
        public void RangeCreate_NormalizesDatesAndCountsInclusiveDays()
        {
            var range = DailyAnalyticsRange.Create(
                new DateTime(2026, 1, 1, 12, 30, 0),
                new DateTime(2026, 1, 3, 8, 0, 0));

            Assert.Equal(new DateTime(2026, 1, 1), range.StartDate);
            Assert.Equal(new DateTime(2026, 1, 3), range.EndDate);
            Assert.Equal(3, range.DayCount);
            Assert.Equal(
                new[]
                {
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 1, 2),
                    new DateTime(2026, 1, 3)
                },
                range.EnumerateDates());
        }

        [Fact]
        public void RangeCreate_RejectsEndBeforeStart()
        {
            Assert.Throws<ArgumentException>(() =>
                DailyAnalyticsRange.Create(
                    new DateTime(2026, 1, 2),
                    new DateTime(2026, 1, 1)));
        }

        [Fact]
        public void GetSnapshot_ReturnsOneRowForEveryDateInRange()
        {
            var source = new TestDailyAnalyticsDataSource();
            source.UsageRows.Add(new DailyUsageTrendRow(
                new DateTime(2026, 1, 2),
                3_600_000,
                "Code",
                2_400_000));
            source.IdleByDate[new DateTime(2026, 1, 2)] = 600_000;
            source.CoverageByDate[new DateTime(2026, 1, 2)] = new RuntimeCoverageSummary(
                10_000,
                8_000,
                6_000,
                2_000,
                1_000,
                500,
                300,
                200);
            var provider = new DailyAnalyticsProvider(source);

            var snapshot = provider.GetSnapshot(
                DailyAnalyticsRange.Create(
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 1, 3)),
                new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero));

            Assert.Equal(3, snapshot.Days.Count);
            Assert.Equal(new DateTime(2026, 1, 1), snapshot.Days[0].Date);
            Assert.Equal(0, snapshot.Days[0].ActiveUsageMs);
            Assert.Null(snapshot.Days[0].TopAppName);

            Assert.Equal(new DateTime(2026, 1, 2), snapshot.Days[1].Date);
            Assert.Equal(3_600_000, snapshot.Days[1].ActiveUsageMs);
            Assert.Equal(600_000, snapshot.Days[1].IdleRecordedMs);
            Assert.Equal("Code", snapshot.Days[1].TopAppName);
            Assert.Equal(2_400_000, snapshot.Days[1].TopAppUsageMs);
            Assert.Equal(8_000, snapshot.Days[1].Coverage.RecordableRuntimeMs);
            Assert.Equal(0.75, snapshot.Days[1].Coverage.CoverageRatio);

            Assert.Equal(new DateTime(2026, 1, 3), snapshot.Days[2].Date);
            Assert.Equal(0, snapshot.Days[2].Coverage.RecordableRuntimeMs);
        }

        [Fact]
        public void GetSnapshot_QueriesUsageTrendOnceForWholeRange()
        {
            var source = new TestDailyAnalyticsDataSource();
            var provider = new DailyAnalyticsProvider(source);

            provider.GetSnapshot(
                DailyAnalyticsRange.Create(
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 1, 5)),
                new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero));

            Assert.Equal(1, source.UsageTrendCallCount);
            Assert.Equal(5, source.IdleCallCount);
            Assert.Equal(5, source.CoverageCallCount);
        }

        private sealed class TestDailyAnalyticsDataSource : IDailyAnalyticsDataSource
        {
            public List<DailyUsageTrendRow> UsageRows { get; } = new();

            public Dictionary<DateTime, long> IdleByDate { get; } = new();

            public Dictionary<DateTime, RuntimeCoverageSummary> CoverageByDate { get; } = new();

            public int UsageTrendCallCount { get; private set; }

            public int IdleCallCount { get; private set; }

            public int CoverageCallCount { get; private set; }

            public IReadOnlyList<DailyUsageTrendRow> GetDailyUsageTrend(
                DateTimeOffset periodStart,
                DateTimeOffset periodEnd)
            {
                UsageTrendCallCount++;
                return UsageRows
                    .Where(row => row.Date >= periodStart.Date && row.Date < periodEnd.Date)
                    .ToList();
            }

            public IdleUsageSummary GetIdleUsage(
                DateTimeOffset periodStart,
                DateTimeOffset periodEnd)
            {
                IdleCallCount++;
                return new IdleUsageSummary(
                    IdleByDate.TryGetValue(periodStart.Date, out var idleMs) ? idleMs : 0);
            }

            public RuntimeCoverageSummary GetRuntimeCoverage(
                DateTimeOffset periodStart,
                DateTimeOffset periodEnd,
                DateTimeOffset observedAt)
            {
                CoverageCallCount++;
                return CoverageByDate.TryGetValue(periodStart.Date, out var coverage)
                    ? coverage
                    : new RuntimeCoverageSummary(0, 0, 0, 0, 0, 0, 0, null);
            }
        }
    }
}
