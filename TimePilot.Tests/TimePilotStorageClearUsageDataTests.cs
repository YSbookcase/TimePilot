using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimePilotStorageClearUsageDataTests
    {
        [Fact]
        public void ClearUsageData_RemovesOldRecordsAndKeepsNewRecords()
        {
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"timepilot-clear-usage-{Guid.NewGuid():N}.db");
            try
            {
                using var storage = new TimePilotStorage(databasePath);
                var startedAt = new DateTimeOffset(2026, 9, 29, 9, 0, 0, TimeSpan.Zero);
                storage.Initialize(startedAt, startedAt.AddHours(-1));

                AddForegroundSession(storage, "BeforeClear", startedAt, startedAt.AddMinutes(5));
                storage.ClearUsageData();
                AddForegroundSession(
                    storage,
                    "AfterClear",
                    startedAt.AddMinutes(10),
                    startedAt.AddMinutes(15));

                var rows = storage.GetForegroundUsageForPeriod(
                    startedAt,
                    startedAt.AddDays(1));

                var row = Assert.Single(rows);
                Assert.Equal("AfterClear", row.AppName);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(databasePath))
                    File.Delete(databasePath);
            }
        }

        private static void AddForegroundSession(
            TimePilotStorage storage,
            string name,
            DateTimeOffset startedAt,
            DateTimeOffset endedAt)
        {
            var app = new AppMetadata(name, name, null);
            var sessionId = storage.StartForegroundSession(app, startedAt);
            storage.UpdateForegroundSessionObservation(sessionId, app, endedAt);
            storage.EndForegroundSession(sessionId, endedAt);
        }
    }
}
