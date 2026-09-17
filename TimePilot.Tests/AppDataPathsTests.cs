using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class AppDataPathsTests
    {
        [Fact]
        public void ResolveDataDirectoryForShell_UsesLogicalDirectoryWhenUnpackaged()
        {
            var localAppData = @"C:\Users\tester\AppData\Local";
            var logicalDirectory = Path.Combine(localAppData, "TimePilot");

            var result = AppDataPaths.ResolveDataDirectoryForShell(
                localAppData,
                logicalDirectory,
                packagedLocalCacheDirectory: null);

            Assert.Equal(logicalDirectory, result);
        }

        [Fact]
        public void ResolveDataDirectoryForShell_MapsPackagedDirectoryToLocalCache()
        {
            var localAppData = @"C:\Users\tester\AppData\Local";
            var logicalDirectory = Path.Combine(localAppData, "TimePilot");
            var localCacheDirectory = Path.Combine(
                localAppData,
                "Packages",
                "YSBookcase.ActiveLogbook_test",
                "LocalCache");

            var result = AppDataPaths.ResolveDataDirectoryForShell(
                localAppData,
                logicalDirectory,
                localCacheDirectory);

            Assert.Equal(
                Path.Combine(localCacheDirectory, "Local", "TimePilot"),
                result);
        }

        [Fact]
        public void ResolveDataDirectoryForShell_DoesNotMapDirectoryOutsideLocalAppData()
        {
            var localAppData = @"C:\Users\tester\AppData\Local";
            var logicalDirectory = @"D:\TimePilot";
            var localCacheDirectory = Path.Combine(
                localAppData,
                "Packages",
                "YSBookcase.ActiveLogbook_test",
                "LocalCache");

            var result = AppDataPaths.ResolveDataDirectoryForShell(
                localAppData,
                logicalDirectory,
                localCacheDirectory);

            Assert.Equal(logicalDirectory, result);
        }
    }
}
