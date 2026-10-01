using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Refresh;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class ViewRefreshSnapshotReaderTests
    {
        [Fact]
        public void Read_SummaryTargetPopulatesOnlySummaryData()
        {
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"timepilot-refresh-reader-{Guid.NewGuid():N}.db");
            try
            {
                var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.Zero);
                using var storage = new TimePilotStorage(databasePath);
                storage.Initialize(observedAt, observedAt.AddHours(-1));
                var request = CreateRequest(ViewRefreshTarget.Summary, observedAt);

                var result = ViewRefreshSnapshotReader.Read(storage, request);

                Assert.NotNull(result.ForegroundUsage);
                Assert.NotNull(result.DailyUsageTrendRows);
                Assert.NotNull(result.RuntimeCoverage);
                Assert.Null(result.TimelineRows);
                Assert.Null(result.RuntimeRows);
                Assert.True(result.ShowDateInUsageTimestamps);
                Assert.True(result.ReadElapsedMs >= 0);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(databasePath))
                    File.Delete(databasePath);
            }
        }

        [Fact]
        public void Read_NoneTargetDoesNotOpenDatabase()
        {
            var observedAt = new DateTimeOffset(2026, 6, 28, 10, 0, 0, TimeSpan.Zero);
            using var storage = new TimePilotStorage(
                Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "timepilot.db"));

            var result = ViewRefreshSnapshotReader.Read(
                storage,
                CreateRequest(ViewRefreshTarget.None, observedAt));

            Assert.Null(result.ForegroundUsage);
            Assert.Null(result.TimelineRows);
            Assert.Null(result.RuntimeRows);
        }

        private static ViewRefreshRequest CreateRequest(
            ViewRefreshTarget target,
            DateTimeOffset observedAt)
        {
            return new ViewRefreshRequest(
                target,
                new SummaryPeriodRange(
                    observedAt.AddDays(-1),
                    observedAt,
                    true),
                observedAt.Date,
                observedAt.Date,
                null,
                30,
                observedAt);
        }
    }
}
