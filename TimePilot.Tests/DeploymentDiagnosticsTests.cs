using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DeploymentDiagnosticsTests
    {
        [Fact]
        public void ResolveChannel_PrefersStoreIdentity()
        {
            var result = DeploymentDiagnosticsService.ResolveChannel(
                isPackaged: true,
                executablePath: @"C:\Program Files\WindowsApps\ActiveLogbook.exe",
                installedExeDirectory: @"C:\Program Files\ActiveLogbook");

            Assert.Equal(DeploymentChannel.StoreMsix, result);
        }

        [Fact]
        public void ResolveChannel_UsesInstallerLocationForInstalledExe()
        {
            var result = DeploymentDiagnosticsService.ResolveChannel(
                isPackaged: false,
                executablePath: @"C:\Program Files\ActiveLogbook\ActiveLogbook.exe",
                installedExeDirectory: @"C:\Program Files\ActiveLogbook");

            Assert.Equal(DeploymentChannel.InstalledExe, result);
        }

        [Fact]
        public void ResolveChannel_TreatsOtherUnpackagedLocationAsPortable()
        {
            var result = DeploymentDiagnosticsService.ResolveChannel(
                isPackaged: false,
                executablePath: @"D:\Tools\ActiveLogbook.exe",
                installedExeDirectory: @"C:\Program Files\ActiveLogbook");

            Assert.Equal(DeploymentChannel.Portable, result);
        }

        [Fact]
        public void Format_WarnsWhenStoreAndInstalledExeCoexist()
        {
            var snapshot = CreateSnapshot(
                DeploymentChannel.StoreMsix,
                installedExeDirectory: @"C:\Program Files\ActiveLogbook");

            var text = DeploymentDiagnosticsFormatter.Format(snapshot, UiLanguage.Korean);

            Assert.Contains("설치형 EXE도 감지되었습니다", text);
            Assert.Contains("두 버전의 자동 시작을 동시에 켜지 마세요", text);
        }

        [Fact]
        public void Format_WarnsUnpackagedChannelAboutOtherStoreDatabase()
        {
            var snapshot = CreateSnapshot(
                DeploymentChannel.InstalledExe,
                storeDataLocations:
                [
                    new DeploymentDataLocation(
                        @"C:\Users\tester\AppData\Local\Packages\YSBookcase.ActiveLogbook_test\LocalCache\Local\TimePilot",
                        @"C:\Users\tester\AppData\Local\Packages\YSBookcase.ActiveLogbook_test\LocalCache\Local\TimePilot\timepilot.db",
                        true)
                ]);

            var text = DeploymentDiagnosticsFormatter.Format(snapshot, UiLanguage.Korean);

            Assert.Contains("Microsoft Store 데이터도 존재합니다", text);
            Assert.Contains("두 데이터 위치를 모두 백업하세요", text);
        }

        [Fact]
        public void FindStoreDataLocations_FindsDatabaseInPackageLocalCache()
        {
            var root = Path.Combine(Path.GetTempPath(), $"TimePilotDiagnostics-{Guid.NewGuid():N}");
            var dataDirectory = Path.Combine(
                root,
                "Packages",
                "YSBookcase.ActiveLogbook_test",
                "LocalCache",
                "Local",
                "TimePilot");

            try
            {
                Directory.CreateDirectory(dataDirectory);
                File.WriteAllText(Path.Combine(dataDirectory, "timepilot.db"), string.Empty);

                var locations = DeploymentDiagnosticsService.FindStoreDataLocations(root);

                var location = Assert.Single(locations);
                Assert.Equal(dataDirectory, location.DirectoryPath);
                Assert.True(location.DatabaseExists);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        private static DeploymentDiagnosticsSnapshot CreateSnapshot(
            DeploymentChannel channel,
            string? installedExeDirectory = null,
            IReadOnlyList<DeploymentDataLocation>? storeDataLocations = null)
        {
            return new DeploymentDiagnosticsSnapshot(
                channel,
                "0.2.12",
                @"C:\Program Files\ActiveLogbook\ActiveLogbook.exe",
                @"C:\Users\tester\AppData\Local\TimePilot",
                @"C:\Users\tester\AppData\Local\TimePilot",
                @"C:\Users\tester\AppData\Local\TimePilot\timepilot.db",
                true,
                @"C:\Users\tester\AppData\Local\TimePilot",
                true,
                installedExeDirectory,
                storeDataLocations ?? Array.Empty<DeploymentDataLocation>(),
                DataStorageLocationService.BuildPlan(
                    isPackaged: channel == DeploymentChannel.StoreMsix,
                    legacyDirectory: @"C:\Users\tester\AppData\Local\TimePilot",
                    packagedLocalCacheDirectory: channel == DeploymentChannel.StoreMsix
                        ? @"C:\Users\tester\AppData\Local\Packages\YSBookcase.ActiveLogbook_test\LocalCache"
                        : null,
                    packagedLocalStateDirectory: channel == DeploymentChannel.StoreMsix
                        ? @"C:\Users\tester\AppData\Local\Packages\YSBookcase.ActiveLogbook_test\LocalState"
                        : null));
        }
    }
}
