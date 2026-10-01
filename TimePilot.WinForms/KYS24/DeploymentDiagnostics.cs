using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

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

    internal sealed record DeploymentStartupInfo(
        bool? StorePackageInstalled,
        string? ActiveLogbookRunCommand,
        string? LegacyRunCommand,
        StartupTaskState? StoreStartupTaskState);

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
        IReadOnlyList<DeploymentDataLocation> StoreDataLocations,
        DeploymentStartupInfo StartupInfo,
        DataStorageLocationPlan StoragePlan);

    internal static class DeploymentDiagnosticsService
    {
        private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string InstallerAppId = "B1C2D7C2-0B18-4F41-9B72-9D1B6B92F412";
        private const string PackageDirectoryPrefix = "YSBookcase.ActiveLogbook_";
        private const string PackageFamilyName = "YSBookcase.ActiveLogbook_qx0xt5p8pr0jp";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ActiveLogbookRunValueName = "ActiveLogbook";
        private const string LegacyRunValueName = "TimePilot";

        public static async Task<DeploymentDiagnosticsSnapshot> CollectAsync()
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
            var storagePlan = DataStorageLocationService.Collect();
            var storeDataLocations = MergeStoreDataLocations(
                FindStoreDataLocations(localAppDataDirectory),
                storagePlan.Candidates);
            var startupInfo = new DeploymentStartupInfo(
                isPackaged ? true : TryIsStorePackageInstalled(),
                TryGetRunCommand(ActiveLogbookRunValueName),
                TryGetRunCommand(LegacyRunValueName),
                isPackaged
                    ? await TryGetPackagedStartupStateAsync()
                    : null);

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
                storeDataLocations,
                startupInfo,
                storagePlan);
        }

        internal static bool? TryIsStorePackageInstalled()
        {
            try
            {
                var packageManager = new PackageManager();
                return packageManager
                    .FindPackagesForUser(string.Empty, PackageFamilyName)
                    .Any(package => package.Id.Name.Equals(
                        "YSBookcase.ActiveLogbook",
                        StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                or System.Security.SecurityException
                or InvalidOperationException
                or ArgumentException
                or COMException)
            {
                return null;
            }
        }

        internal static string? TryGetRunCommand(string valueName)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(valueName) as string;
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException
                or System.Security.SecurityException)
            {
                return null;
            }
        }

        private static async Task<StartupTaskState?> TryGetPackagedStartupStateAsync()
        {
            try
            {
                return await WindowsStartupRegistration.GetPackagedStateAsync();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException
                or InvalidOperationException
                or System.Security.SecurityException
                or COMException)
            {
                return null;
            }
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
                    .SelectMany(packageDirectory => new[]
                    {
                        Path.Combine(packageDirectory, "LocalState", "TimePilot"),
                        Path.Combine(packageDirectory, "LocalCache", "Local", "TimePilot")
                    })
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

        internal static IReadOnlyList<DeploymentDataLocation> MergeStoreDataLocations(
            IReadOnlyList<DeploymentDataLocation> discoveredLocations,
            IReadOnlyList<DataStorageCandidate> candidates)
        {
            var locations = discoveredLocations.ToDictionary(
                location => Path.TrimEndingDirectorySeparator(location.DirectoryPath),
                location => location,
                StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates.Where(candidate => candidate.Kind is
                DataStorageLocationKind.MsixLocalState or
                DataStorageLocationKind.MsixVirtualizedLocalCache))
            {
                var directory = Path.TrimEndingDirectorySeparator(candidate.DirectoryPath);
                var hasArtifacts = candidate.DatabaseExists == true
                    || candidate.SettingsExists == true
                    || candidate.BackupDirectoryExists == true;
                if (!hasArtifacts && !Directory.Exists(directory))
                    continue;

                locations[directory] = new DeploymentDataLocation(
                    directory,
                    Path.Combine(directory, "timepilot.db"),
                    candidate.DatabaseExists == true);
            }

            return locations.Values
                .OrderBy(location => location.DirectoryPath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
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
            AppendStartupInfo(builder, snapshot.StartupInfo, snapshot.Channel, isEnglish);
            builder.AppendLine();
            AppendStoragePlan(builder, snapshot.StoragePlan, isEnglish);
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

        private static void AppendStartupInfo(
            StringBuilder builder,
            DeploymentStartupInfo startupInfo,
            DeploymentChannel channel,
            bool isEnglish)
        {
            builder.AppendLine(Label(isEnglish, "Distribution and startup status", "배포판 및 자동 시작 상태"));
            builder.AppendLine(
                $"{Label(isEnglish, "- Microsoft Store package", "- Microsoft Store 패키지")}: " +
                FormatNullableDetected(startupInfo.StorePackageInstalled, isEnglish));
            builder.AppendLine(
                $"{Label(isEnglish, "- ActiveLogbook EXE startup", "- ActiveLogbook EXE 자동 시작")}: " +
                FormatRunCommand(startupInfo.ActiveLogbookRunCommand, isEnglish));
            builder.AppendLine(
                $"{Label(isEnglish, "- Legacy TimePilot startup", "- 레거시 TimePilot 자동 시작")}: " +
                FormatRunCommand(startupInfo.LegacyRunCommand, isEnglish));
            builder.AppendLine(
                $"{Label(isEnglish, "- Store startup task", "- Store 시작 작업")}: " +
                FormatStartupTaskState(
                    startupInfo.StoreStartupTaskState,
                    channel,
                    startupInfo.StorePackageInstalled == true,
                    isEnglish));
        }

        private static void AppendStoragePlan(
            StringBuilder builder,
            DataStorageLocationPlan plan,
            bool isEnglish)
        {
            builder.AppendLine(Label(isEnglish, "Storage transition plan", "저장 위치 전환 계획"));
            builder.AppendLine($"{Label(isEnglish, "- Migration source folder", "- 이전 원본 폴더")}: {plan.CurrentDirectory}");
            builder.AppendLine($"{Label(isEnglish, "- Target folder", "- 목표 폴더")}: {plan.TargetDirectory}");
            builder.AppendLine($"{Label(isEnglish, "- Migration", "- 이전 상태")}: {FormatMigrationState(plan, isEnglish)}");
            builder.AppendLine($"{Label(isEnglish, "- Decision", "- 판단 결과")}: {FormatMigrationDecision(plan.MigrationDecision.Kind, isEnglish)}");
            builder.AppendLine(Label(isEnglish, "- Candidates", "- 이전 후보"));
            foreach (var candidate in plan.Candidates)
            {
                builder.AppendLine(
                    $"  · {FormatCandidateKind(candidate.Kind, isEnglish)}: {candidate.DirectoryPath}");
                builder.AppendLine(
                    $"    DB {FormatDatabaseState(candidate.DatabaseState, isEnglish)}, " +
                    $"{Label(isEnglish, "settings", "설정")} {FormatNullableExists(candidate.SettingsExists, isEnglish)}, " +
                    $"{Label(isEnglish, "backups", "백업")} {FormatNullableExists(candidate.BackupDirectoryExists, isEnglish)}" +
                    FormatCandidateRole(candidate, plan, isEnglish));
            }
        }

        internal static IReadOnlyList<string> BuildWarnings(
            DeploymentDiagnosticsSnapshot snapshot,
            bool isEnglish)
        {
            var warnings = new List<string>();
            var hasExeStartup = !string.IsNullOrWhiteSpace(snapshot.StartupInfo.ActiveLogbookRunCommand)
                || !string.IsNullOrWhiteSpace(snapshot.StartupInfo.LegacyRunCommand);
            var storeStartupEnabled = snapshot.StartupInfo.StoreStartupTaskState is
                StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;

            if (snapshot.Channel == DeploymentChannel.StoreMsix && hasExeStartup)
            {
                warnings.Add(storeStartupEnabled
                    ? Label(
                        isEnglish,
                        "Both the Store startup task and an EXE startup entry are enabled. Disable the EXE startup entry before relying on Store startup.",
                        "Store 시작 작업과 EXE 자동 시작 항목이 모두 활성화되어 있습니다. Store 자동 시작을 사용하기 전에 EXE 자동 시작 항목을 해제하세요.")
                    : Label(
                        isEnglish,
                        "An EXE startup entry remains. Remove it after confirming that the Store startup task works correctly.",
                        "EXE 자동 시작 항목이 남아 있습니다. Store 시작 작업이 정상 동작하는지 확인한 후 제거하세요."));
            }

            if (snapshot.Channel != DeploymentChannel.StoreMsix
                && snapshot.StartupInfo.StorePackageInstalled == true)
            {
                warnings.Add(Label(
                    isEnglish,
                    "The Microsoft Store version is also installed. Back up both data locations and choose one distribution before changing startup settings.",
                    "Microsoft Store 버전도 설치되어 있습니다. 자동 시작 설정을 바꾸기 전에 두 데이터 위치를 백업하고 사용할 배포판을 하나 선택하세요."));
            }

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

            if (snapshot.StoragePlan.RequiresMigration)
            {
                var decision = snapshot.StoragePlan.MigrationDecision;
                if (decision.Kind == DataStorageMigrationDecisionKind.ConflictRequiresUserChoice)
                {
                    warnings.Add(Label(
                        isEnglish,
                        "Multiple storage locations contain data. Do not choose one automatically; back up and compare them first.",
                        "둘 이상의 저장 위치에 데이터가 있습니다. 자동으로 하나를 선택하지 말고 먼저 백업하고 비교해야 합니다."));
                }
                else if (decision.Kind == DataStorageMigrationDecisionKind.MigrateCurrentToTarget)
                {
                    warnings.Add(Label(
                        isEnglish,
                        "The Store database still uses its previous storage location. Migration to LocalState has not been performed yet.",
                        "Store 데이터베이스가 아직 이전 저장 위치를 사용합니다. LocalState 이전은 아직 수행되지 않았습니다."));
                }
                else if (decision.Kind is DataStorageMigrationDecisionKind.BlockedCurrentDatabaseInvalid
                    or DataStorageMigrationDecisionKind.BlockedTargetDatabaseInvalid)
                {
                    warnings.Add(Label(
                        isEnglish,
                        "A migration database failed validation. Migration must remain blocked until the data is backed up and repaired.",
                        "이전 대상 데이터베이스가 검증을 통과하지 못했습니다. 데이터를 백업하고 복구하기 전까지 이전을 중단해야 합니다."));
                }
                else if (decision.Kind is DataStorageMigrationDecisionKind.InspectionFailed
                    or DataStorageMigrationDecisionKind.TargetUnavailable)
                {
                    warnings.Add(Label(
                        isEnglish,
                        "The migration state could not be determined safely. Keep using the current location and do not create a new database at the target.",
                        "이전 상태를 안전하게 판단하지 못했습니다. 현재 위치를 계속 사용하고 목표 위치에 새 데이터베이스를 만들지 않아야 합니다."));
                }
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

        private static string FormatMigrationState(DataStorageLocationPlan plan, bool isEnglish)
        {
            if (plan.IsPackaged && !plan.IsTargetAvailable)
                return Label(isEnglish, "Blocked: target unavailable", "중단: 목표 위치 확인 불가");

            if (plan.HasCompletedMigration)
                return Label(isEnglish, "Completed (LocalState is active)", "완료됨 (LocalState 사용 중)");

            return plan.RequiresMigration
                ? Label(isEnglish, "Required (not performed)", "필요함 (아직 수행하지 않음)")
                : Label(isEnglish, "Not required", "필요 없음");
        }

        private static string FormatMigrationDecision(
            DataStorageMigrationDecisionKind kind,
            bool isEnglish)
        {
            return kind switch
            {
                DataStorageMigrationDecisionKind.NoMigrationRequired =>
                    Label(isEnglish, "Keep current location", "현재 위치 유지"),
                DataStorageMigrationDecisionKind.InitializeTarget =>
                    Label(isEnglish, "New install: initialize LocalState", "새 설치: LocalState 초기화 가능"),
                DataStorageMigrationDecisionKind.MigrateCurrentToTarget =>
                    Label(isEnglish, "Migration can be prepared", "이전 준비 가능"),
                DataStorageMigrationDecisionKind.UseExistingTarget =>
                    Label(isEnglish, "Use existing LocalState", "기존 LocalState 사용"),
                DataStorageMigrationDecisionKind.ConflictRequiresUserChoice =>
                    Label(isEnglish, "Conflict: user choice required", "충돌: 사용자 선택 필요"),
                DataStorageMigrationDecisionKind.BlockedCurrentDatabaseInvalid =>
                    Label(isEnglish, "Blocked: current database is invalid", "중단: 현재 데이터베이스 검증 실패"),
                DataStorageMigrationDecisionKind.BlockedTargetDatabaseInvalid =>
                    Label(isEnglish, "Blocked: target database is invalid", "중단: 목표 데이터베이스 검증 실패"),
                DataStorageMigrationDecisionKind.TargetUnavailable =>
                    Label(isEnglish, "Blocked: LocalState is unavailable", "중단: LocalState 경로 확인 불가"),
                _ => Label(isEnglish, "Blocked: inspection failed", "중단: 상태 검사 실패")
            };
        }

        private static string FormatDatabaseState(DataStorageDatabaseState state, bool isEnglish)
        {
            return state switch
            {
                DataStorageDatabaseState.Valid => Label(isEnglish, "valid", "정상"),
                DataStorageDatabaseState.Invalid => Label(isEnglish, "invalid", "검증 실패"),
                DataStorageDatabaseState.Unavailable => Label(isEnglish, "unavailable", "접근 불가"),
                DataStorageDatabaseState.NotPresent => Label(isEnglish, "not found", "없음"),
                _ => Label(isEnglish, "not inspected", "검사하지 않음")
            };
        }

        private static string FormatCandidateKind(DataStorageLocationKind kind, bool isEnglish)
        {
            return kind switch
            {
                DataStorageLocationKind.MsixLocalState => "MSIX LocalState",
                DataStorageLocationKind.MsixVirtualizedLocalCache => "MSIX LocalCache",
                _ => Label(isEnglish, "Legacy EXE", "기존 EXE")
            };
        }

        private static string FormatCandidateRole(
            DataStorageCandidate candidate,
            DataStorageLocationPlan plan,
            bool isEnglish)
        {
            var roles = new List<string>();
            if (candidate.IsCurrent)
                roles.Add(Label(isEnglish, "source", "원본"));
            if (candidate.IsTarget)
                roles.Add(Label(isEnglish, "target", "목표"));
            if (candidate.IsTarget && plan.HasCompletedMigration)
                roles.Add(Label(isEnglish, "active", "사용 중"));

            return roles.Count == 0 ? string.Empty : $" [{string.Join(", ", roles)}]";
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

        private static string FormatNullableDetected(bool? detected, bool isEnglish)
        {
            return detected.HasValue
                ? detected.Value
                    ? Label(isEnglish, "Installed", "설치됨")
                    : Label(isEnglish, "Not detected", "감지되지 않음")
                : Label(isEnglish, "Could not inspect", "검사할 수 없음");
        }

        private static string FormatRunCommand(string? command, bool isEnglish)
        {
            return string.IsNullOrWhiteSpace(command)
                ? Label(isEnglish, "Not registered", "등록되지 않음")
                : command;
        }

        private static string FormatStartupTaskState(
            StartupTaskState? state,
            DeploymentChannel channel,
            bool storePackageInstalled,
            bool isEnglish)
        {
            if (!state.HasValue)
            {
                if (channel == DeploymentChannel.StoreMsix)
                    return Label(isEnglish, "Could not inspect", "검사할 수 없음");

                return storePackageInstalled
                    ? Label(
                        isEnglish,
                        "Inspect from the running Store version",
                        "실행 중인 Store 버전에서 확인 가능")
                    : Label(isEnglish, "Not available", "확인할 수 없음");
            }

            return state.Value.ToString();
        }

        private static string Label(bool isEnglish, string english, string korean)
        {
            return isEnglish ? english : korean;
        }
    }
}
