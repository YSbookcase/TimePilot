using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DataStorageLocationServiceTests
    {
        [Fact]
        public void BuildPlan_UsesLocalStateAsPackagedTargetWithoutChangingCurrentLocalCache()
        {
            var localAppData = @"C:\Users\tester\AppData\Local";
            var legacyDirectory = Path.Combine(localAppData, "TimePilot");
            var localCache = Path.Combine(localAppData, "Packages", "ActiveLogbook_test", "LocalCache");
            var localState = Path.Combine(localAppData, "Packages", "ActiveLogbook_test", "LocalState");

            var plan = DataStorageLocationService.BuildPlan(
                isPackaged: true,
                legacyDirectory,
                localCache,
                localState);

            Assert.Equal(Path.Combine(localCache, "Local", "TimePilot"), plan.CurrentDirectory);
            Assert.Equal(Path.Combine(localState, "TimePilot"), plan.TargetDirectory);
            Assert.True(plan.RequiresMigration);
            Assert.Contains(plan.Candidates, candidate =>
                candidate.Kind == DataStorageLocationKind.MsixVirtualizedLocalCache
                && candidate.IsCurrent);
            Assert.Contains(plan.Candidates, candidate =>
                candidate.Kind == DataStorageLocationKind.MsixLocalState
                && candidate.IsTarget);
        }

        [Fact]
        public void BuildPlan_KeepsLegacyDirectoryForUnpackagedApp()
        {
            var legacyDirectory = @"C:\Users\tester\AppData\Local\TimePilot";

            var plan = DataStorageLocationService.BuildPlan(
                isPackaged: false,
                legacyDirectory,
                packagedLocalCacheDirectory: null,
                packagedLocalStateDirectory: null);

            Assert.Equal(legacyDirectory, plan.CurrentDirectory);
            Assert.Equal(legacyDirectory, plan.TargetDirectory);
            Assert.False(plan.RequiresMigration);
            var candidate = Assert.Single(plan.Candidates);
            Assert.True(candidate.IsCurrent);
            Assert.True(candidate.IsTarget);
        }

        [Fact]
        public void BuildPlan_InspectsDatabaseSettingsAndBackupCandidates()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotStoragePlan-{Guid.NewGuid():N}");
            var legacyDirectory = Path.Combine(root, "Legacy", "TimePilot");
            var localCache = Path.Combine(root, "Package", "LocalCache");
            var localState = Path.Combine(root, "Package", "LocalState");
            var currentDirectory = Path.Combine(localCache, "Local", "TimePilot");

            try
            {
                Directory.CreateDirectory(Path.Combine(currentDirectory, "backups"));
                File.WriteAllText(Path.Combine(currentDirectory, "timepilot.db"), string.Empty);
                File.WriteAllText(Path.Combine(currentDirectory, "settings.json"), "{}");

                var plan = DataStorageLocationService.BuildPlan(
                    isPackaged: true,
                    legacyDirectory,
                    localCache,
                    localState);

                var current = Assert.Single(plan.Candidates, candidate => candidate.IsCurrent);
                Assert.True(current.DatabaseExists);
                Assert.True(current.SettingsExists);
                Assert.True(current.BackupDirectoryExists);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void Formatter_WarnsWithoutMovingWhenCurrentAndTargetDatabasesBothExist()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotStorageConflict-{Guid.NewGuid():N}");
            var legacyDirectory = Path.Combine(root, "Legacy", "TimePilot");
            var localCache = Path.Combine(root, "Package", "LocalCache");
            var localState = Path.Combine(root, "Package", "LocalState");
            var currentDirectory = Path.Combine(localCache, "Local", "TimePilot");
            var targetDirectory = Path.Combine(localState, "TimePilot");

            try
            {
                Directory.CreateDirectory(currentDirectory);
                Directory.CreateDirectory(targetDirectory);
                File.WriteAllText(Path.Combine(currentDirectory, "timepilot.db"), string.Empty);
                File.WriteAllText(Path.Combine(targetDirectory, "timepilot.db"), string.Empty);
                var plan = DataStorageLocationService.BuildPlan(
                    isPackaged: true,
                    legacyDirectory,
                    localCache,
                    localState);
                var snapshot = new DeploymentDiagnosticsSnapshot(
                    DeploymentChannel.StoreMsix,
                    "0.2.12",
                    @"C:\Program Files\WindowsApps\ActiveLogbook.exe",
                    legacyDirectory,
                    currentDirectory,
                    Path.Combine(currentDirectory, "timepilot.db"),
                    true,
                    legacyDirectory,
                    null,
                    null,
                    Array.Empty<DeploymentDataLocation>(),
                    plan);

                var text = DeploymentDiagnosticsFormatter.Format(snapshot, UiLanguage.Korean);

                Assert.Contains("LocalCache와 목표 LocalState에 데이터베이스가 모두 있습니다", text);
                Assert.Contains("필요함 (아직 수행하지 않음)", text);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}
