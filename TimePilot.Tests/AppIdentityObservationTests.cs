using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppIdentityObservationTests
    {
        [Fact]
        public void AppWithSameProcessAndMultiplePathsNeedsIdentityReview()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"timepilot-test-{Guid.NewGuid():N}.db");
            try
            {
                var now = new DateTimeOffset(2026, 6, 12, 9, 0, 0, TimeSpan.Zero);
                using var storage = new TimePilotStorage(databasePath);
                storage.Initialize(now, now.AddMinutes(-5));

                storage.UpdateAppMetadata(
                    new AppMetadata("launcher", "Launcher", @"C:\Apps\One\launcher.exe"),
                    now);
                storage.UpdateAppMetadata(
                    new AppMetadata("launcher", "Launcher", @"D:\Portable\Other\launcher.exe"),
                    now.AddSeconds(10));

                var row = Assert.Single(storage.GetAppCategoryManagementRows(now.AddSeconds(10)));
                Assert.True(row.NeedsIdentityReview);
                Assert.Equal(2, row.ObservationPathCount);

                var observations = storage.GetAppIdentityObservations(row.AppId);
                Assert.Equal(2, observations.Count);
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
