using Microsoft.Data.Sqlite;
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class PublicDailyAnalyticsCompatibilityTests
{
    [Fact]
    public void CurrentRecordingSchemaIsAcceptedWithoutInitializingItAgain()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TimePilotPublicCompatibility-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "synthetic.db");
        try
        {
            var now = new DateTimeOffset(2024, 2, 28, 10, 0, 0, TimeSpan.Zero);
            long expectedActive;
            long expectedIdle;
            double communityWindowsRecordableRatio;
            using (var storage = new TimePilotStorage(path))
            {
                storage.Initialize(now, now.AddHours(-1));
                var app = new AppMetadata("synthetic-editor", "Synthetic editor", null);
                storage.BeginRuntimeSession(now, now.AddHours(-1), "fixture-v1");
                var session = storage.StartForegroundSession(app, now);
                storage.UpdateForegroundSessionObservation(session, app, now.AddHours(1));
                storage.EndForegroundSession(session, now.AddHours(1));
                var idle = storage.StartIdleSession(now.AddHours(1), 120_000, app);
                storage.EndIdleSession(idle, now.AddHours(2));
                storage.EndRuntimeSession(now.AddHours(2), "normal");
                expectedActive = storage.GetForegroundUsageForPeriod(now.Date, now.AddDays(1).Date)
                    .Sum(row => row.ActiveUsageMs);
                expectedIdle = storage.GetIdleUsageForPeriod(now.Date, now.AddDays(1).Date).IdleMs;
                communityWindowsRecordableRatio = storage.GetRuntimeCoverageForPeriod(
                    new DateTimeOffset(2024, 2, 28, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero), now.AddDays(1)).CoverageRatio;
            }
            SqliteConnection.ClearAllPools();
            using var reader = SqliteDailyAnalyticsReader.Open(path, now.AddDays(1));
            var day = Assert.Single(reader.Read(new DailyQuery(new(2024, 2, 28), new(2024, 2, 29),
                TimeZoneInfo.Utc, now.AddDays(1))).Days);
            Assert.Equal(3_600_000, day.ActiveMilliseconds);
            Assert.Equal(expectedActive, day.ActiveMilliseconds);
            Assert.Equal(expectedIdle, day.IdleMilliseconds);
            // Public calendar coverage includes the whole elapsed day; UI coverage estimates Windows recordable time.
            Assert.Equal(24 * 3_600_000, day.RecorderCalendarCoverage.ElapsedCalendarMilliseconds);
            Assert.Equal(2 * 3_600_000, day.RecorderCalendarCoverage.RecorderRunningMilliseconds);
            Assert.Equal(2.0 / 24, day.RecorderCalendarCoverage.RecorderToCalendarRatio);
            Assert.NotEqual(communityWindowsRecordableRatio, day.RecorderCalendarCoverage.RecorderToCalendarRatio);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}
