using Microsoft.Win32;
using System.Text;

namespace TimePilot.WinForms.KYS24
{
    internal enum DeploymentChannel
    {
        StoreMsix,
        InstalledExe,
        Portable
    }

    internal sealed record DeploymentDataLocation(
        string DirectoryPath,
        string DatabasePath,
        bool DatabaseExists);

    internal sealed record DeploymentDiagnosticsSnapshot(
        DeploymentChannel Channel,
        string Version,
        string ExecutablePath,
        string LogicalDataDirectory,
        string PhysicalDataDirectory,
        string ActiveDatabasePath,
        bool ActiveDatabaseExists,
        string LegacyDataDirectory,
        bool? LegacyDatabaseExists,
        string? InstalledExeDirectory,
        IReadOnlyList<DeploymentDataLocation> StoreDataLocations);

    internal static class DeploymentDiagnosticsService
    {
        private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string InstallerAppId = "B1C2D7C2-0B18-4F41-9B72-9D1B6B92F412";
        private const string PackageDirectoryPrefix = "YSBookcase.ActiveLogbook_";

        public static DeploymentDiagnosticsSnapshot Collect()
        {
            var executablePath = Application.ExecutablePath;
            var isPackaged = WindowsStartupRegistration.IsPackagedApp();
            var installedExeDirectory = TryGetInstalledExeDirectory();
            var channel = ResolveChannel(isPackaged, executablePath, installedExeDirectory);
            var localAppDataDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            var logicalDataDirectory = AppDataPaths.DataDirectory;
            var physicalDataDirectory = AppDataPaths.DataDirectoryForShell;
            var activeDatabasePath = Path.Combine(physicalDataDirectory, "timepilot.db");
            var legacyDataDirectory = Path.Combine(localAppDataDirectory, "TimePilot");
            var legacyDatabasePath = Path.Combine(legacyDataDirectory, "timepilot.db");

            return new DeploymentDiagnosticsSnapshot(
                channel,
                Application.ProductVersion,
                executablePath,
                logicalDataDirectory,
                physicalDataDirectory,
                activeDatabasePath,
                File.Exists(activeDatabasePath),
                legacyDataDirectory,
                isPackaged ? null : File.Exists(legacyDatabasePath),
                installedExeDirectory,
                FindStoreDataLocations(localAppDataDirectory));
        }

        internal static DeploymentChannel ResolveChannel(
            bool isPackaged,
            string executablePath,
            string? installedExeDirectory)
        {
            if (isPackaged)
                return DeploymentChannel.StoreMsix;

            return IsPathInsideDirectory(executablePath, installedExeDirectory)
                ? DeploymentChannel.InstalledExe
                : DeploymentChannel.Portable;
        }

        internal static bool IsPathInsideDirectory(string path, string? directory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
                return false;

            try
            {
                var fullPath = Path.GetFullPath(path);
                var fullDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
                return fullPath.StartsWith(
                    fullDirectory + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }

        internal static IReadOnlyList<DeploymentDataLocation> FindStoreDataLocations(
            string localAppDataDirectory)
        {
            var packagesDirectory = Path.Combine(localAppDataDirectory, "Packages");
            if (!Directory.Exists(packagesDirectory))
                return Array.Empty<DeploymentDataLocation>();

            try
            {
                return Directory.EnumerateDirectories(
                        packagesDirectory,
                        PackageDirectoryPrefix + "*",
                        SearchOption.TopDirectoryOnly)
                    .Select(packageDirectory => Path.Combine(
                        packageDirectory,
                        "LocalCache",
                        "Local",
                        "TimePilot"))
                    .Where(Directory.Exists)
                    .Select(directory => new DeploymentDataLocation(
                        directory,
                        Path.Combine(directory, "timepilot.db"),
                        File.Exists(Path.Combine(directory, "timepilot.db"))))
                    .OrderBy(location => location.DirectoryPath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException)
            {
                return Array.Empty<DeploymentDataLocation>();
            }
        }

        private static string? TryGetInstalledExeDirectory()
        {
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                        using var uninstallKey = baseKey.OpenSubKey(UninstallKeyPath, writable: false);
                        if (uninstallKey is null)
                            continue;

                        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                        {
                            if (!subKeyName.Contains(InstallerAppId, StringComparison.OrdinalIgnoreCase))
                                continue;

                            using var appKey = uninstallKey.OpenSubKey(subKeyName, writable: false);
                            var installLocation = appKey?.GetValue("InstallLocation") as string;
                            if (!string.IsNullOrWhiteSpace(installLocation))
                                return Path.TrimEndingDirectorySeparator(installLocation.Trim());
                        }
                    }
                    catch (Exception ex) when (ex is IOException
                        or UnauthorizedAccessException
                        or System.Security.SecurityException)
                    {
                        // A missing or inaccessible registry view is not proof that the app is portable.
                    }
                }
            }

            return null;
        }
    }

    internal static class DeploymentDiagnosticsFormatter
    {
        public static string Format(DeploymentDiagnosticsSnapshot snapshot, UiLanguage language)
        {
            var isEnglish = language == UiLanguage.English;
            var builder = new StringBuilder();
            builder.AppendLine($"{Label(isEnglish, "Installation type", "설치 유형")}: {FormatChannel(snapshot.Channel, isEnglish)}");
            builder.AppendLine($"{Label(isEnglish, "Version", "버전")}: {snapshot.Version}");
            builder.AppendLine($"{Label(isEnglish, "Executable", "실행 파일")}: {snapshot.ExecutablePath}");
            builder.AppendLine();
            builder.AppendLine($"{Label(isEnglish, "Logical data folder", "논리 데이터 폴더")}: {snapshot.LogicalDataDirectory}");
            builder.AppendLine($"{Label(isEnglish, "Physical data folder", "실제 데이터 폴더")}: {snapshot.PhysicalDataDirectory}");
            builder.AppendLine($"{Label(isEnglish, "Active database", "사용 중인 데이터베이스")}: {snapshot.ActiveDatabasePath}");
            builder.AppendLine($"{Label(isEnglish, "Database status", "데이터베이스 상태")}: {FormatExists(snapshot.ActiveDatabaseExists, isEnglish)}");
            builder.AppendLine();
            builder.AppendLine($"{Label(isEnglish, "Legacy EXE data folder", "기존 EXE 데이터 폴더")}: {snapshot.LegacyDataDirectory}");
            builder.AppendLine($"{Label(isEnglish, "Legacy database", "기존 EXE 데이터베이스")}: {FormatNullableExists(snapshot.LegacyDatabaseExists, isEnglish)}");
            builder.AppendLine($"{Label(isEnglish, "Installed EXE", "설치형 EXE")}: {FormatInstalledExe(snapshot.InstalledExeDirectory, isEnglish)}");
            builder.AppendLine();
            builder.AppendLine(Label(isEnglish, "Microsoft Store data locations", "Microsoft Store 데이터 위치"));

            if (snapshot.StoreDataLocations.Count == 0)
            {
                builder.AppendLine(Label(isEnglish, "- No package data folder was found.", "- 패키지 데이터 폴더가 발견되지 않았습니다."));
            }
            else
            {
                foreach (var location in snapshot.StoreDataLocations)
                {
                    builder.AppendLine($"- {location.DatabasePath} ({FormatExists(location.DatabaseExists, isEnglish)})");
                }
            }

            var warnings = BuildWarnings(snapshot, isEnglish);
            if (warnings.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine(Label(isEnglish, "Attention", "주의"));
                foreach (var warning in warnings)
                    builder.AppendLine($"- {warning}");
            }

            return builder.ToString().TrimEnd();
        }

        internal static IReadOnlyList<string> BuildWarnings(
            DeploymentDiagnosticsSnapshot snapshot,
            bool isEnglish)
        {
            var warnings = new List<string>();
            if (snapshot.Channel == DeploymentChannel.StoreMsix
                && !string.IsNullOrWhiteSpace(snapshot.InstalledExeDirectory))
            {
                warnings.Add(Label(
                    isEnglish,
                    "An installed EXE was also detected. Do not enable startup for both versions.",
                    "설치형 EXE도 감지되었습니다. 두 버전의 자동 시작을 동시에 켜지 마세요."));
            }

            var currentDataDirectory = Path.TrimEndingDirectorySeparator(snapshot.PhysicalDataDirectory);
            var hasOtherStoreDatabase = snapshot.StoreDataLocations.Any(location =>
                location.DatabaseExists
                && !Path.TrimEndingDirectorySeparator(location.DirectoryPath).Equals(
                    currentDataDirectory,
                    StringComparison.OrdinalIgnoreCase));
            if (snapshot.Channel != DeploymentChannel.StoreMsix && hasOtherStoreDatabase)
            {
                warnings.Add(Label(
                    isEnglish,
                    "Microsoft Store data also exists. Back up both data locations before changing distribution channels.",
                    "Microsoft Store 데이터도 존재합니다. 배포판을 전환하기 전에 두 데이터 위치를 모두 백업하세요."));
            }

            if (!snapshot.ActiveDatabaseExists)
            {
                warnings.Add(Label(
                    isEnglish,
                    "No database was found at the active location. This can be normal before the first record is saved.",
                    "현재 사용 위치에 데이터베이스가 없습니다. 첫 기록이 저장되기 전이라면 정상일 수 있습니다."));
            }

            return warnings;
        }

        private static string FormatChannel(DeploymentChannel channel, bool isEnglish)
        {
            return channel switch
            {
                DeploymentChannel.StoreMsix => "Microsoft Store (MSIX)",
                DeploymentChannel.InstalledExe => Label(isEnglish, "Installed EXE", "설치형 EXE"),
                _ => Label(isEnglish, "Portable or development build", "포터블 또는 개발 빌드")
            };
        }

        private static string FormatExists(bool exists, bool isEnglish)
        {
            return exists
                ? Label(isEnglish, "Found", "있음")
                : Label(isEnglish, "Not found", "없음");
        }

        private static string FormatNullableExists(bool? exists, bool isEnglish)
        {
            return exists.HasValue
                ? FormatExists(exists.Value, isEnglish)
                : Label(
                    isEnglish,
                    "Not inspected from the Store process",
                    "Store 프로세스에서는 검사하지 않음");
        }

        private static string FormatInstalledExe(string? directory, bool isEnglish)
        {
            return string.IsNullOrWhiteSpace(directory)
                ? Label(isEnglish, "Not detected", "감지되지 않음")
                : directory;
        }

        private static string Label(bool isEnglish, string english, string korean)
        {
            return isEnglish ? english : korean;
        }
    }
}
