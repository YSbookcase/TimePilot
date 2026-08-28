using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimePilotStorageIdleUsageTests
    {
        [Fact]
        public void GetIdleUsageForPeriod_MergesOverlappingIdleSessions()
        {
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"timepilot-idle-usage-{Guid.NewGuid():N}.db");
            try
            {
                using var storage = new TimePilotStorage(databasePath);
                var dayStart = new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero);
                storage.Initialize(dayStart, dayStart.AddHours(-1));

                var firstIdleId = storage.StartIdleSession(
                    dayStart,
                    thresholdMs: 120_000,
                    foregroundApp: null);
                storage.EndIdleSession(firstIdleId, dayStart.AddHours(10));

                var secondIdleId = storage.StartIdleSession(
                    dayStart.AddHours(5),
                    thresholdMs: 120_000,
                    foregroundApp: null);
                storage.EndIdleSession(secondIdleId, dayStart.AddHours(20));

                var summary = storage.GetIdleUsageForPeriod(
                    dayStart,
                    dayStart.AddDays(1));

                Assert.Equal((long)TimeSpan.FromHours(20).TotalMilliseconds, summary.IdleMs);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(databasePath))
                    File.Delete(databasePath);
            }
        }
    }
}
