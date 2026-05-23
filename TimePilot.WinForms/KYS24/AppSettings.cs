using System.Text.Json;
using System.Drawing;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class AppSettings
    {
        public const int DefaultIdleThresholdMinutes = 2;
        public const int MinIdleThresholdMinutes = 1;
        public const int MaxIdleThresholdMinutes = 60;
        public const bool DefaultProcessRuntimeTrackingEnabled = true;
        public const bool DefaultStartWithWindows = false;
        public const bool DefaultStartupPromptShown = false;
        public const bool DefaultPerformanceDiagnosticsEnabled = false;
        public const UiLanguage DefaultUiLanguage = UiLanguage.Korean;
        public const ProcessRuntimeTrackingScope DefaultProcessRuntimeTrackingScope = ProcessRuntimeTrackingScope.WindowedApps;
        public const int DefaultProcessRuntimeSampleIntervalSeconds = 60;
        public const int MinProcessRuntimeSampleIntervalSeconds = 1;
        public const int MaxProcessRuntimeSampleIntervalSeconds = 3600;
        public const int WarningProcessRuntimeSampleIntervalSeconds = 10;
        public const int DangerousAllProcessesSampleIntervalSeconds = 10;
        public const int DangerousUserProcessesSampleIntervalSeconds = 5;
        public const int DangerousAnyScopeSampleIntervalSeconds = 3;

        private readonly string settingsPath;

        private AppSettings(string settingsPath)
        {
            this.settingsPath = settingsPath;
        }

        public int IdleThresholdMinutes { get; set; } = DefaultIdleThresholdMinutes;

        public int IdleThresholdMs => IdleThresholdMinutes * 60 * 1000;

        public bool ProcessRuntimeTrackingEnabled { get; set; } = DefaultProcessRuntimeTrackingEnabled;

        public bool StartWithWindows { get; set; } = DefaultStartWithWindows;

        public bool StartupPromptShown { get; set; } = DefaultStartupPromptShown;

        public bool PerformanceDiagnosticsEnabled { get; set; } = DefaultPerformanceDiagnosticsEnabled;

        public UiLanguage UiLanguage { get; set; } = DefaultUiLanguage;

        public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; set; } = DefaultProcessRuntimeTrackingScope;

        public int ProcessRuntimeSampleIntervalSeconds { get; set; } = DefaultProcessRuntimeSampleIntervalSeconds;

        public int ProcessRuntimeSampleIntervalMs => ProcessRuntimeSampleIntervalSeconds * 1000;

        public ProcessRuntimeTrackingScope? ApprovedRiskyProcessRuntimeTrackingScope { get; set; }

        public int? ApprovedRiskyProcessRuntimeSampleIntervalSeconds { get; set; }

        public DateTimeOffset? ProcessRuntimeRiskAcceptedAt { get; set; }

        public int? WindowLeft { get; set; }

        public int? WindowTop { get; set; }

        public int? WindowWidth { get; set; }

        public int? WindowHeight { get; set; }

        public bool WindowMaximized { get; set; }

        public string? UsageSortProperty { get; set; }

        public bool? UsageSortDescending { get; set; }

        public string? DailyUsageTrendSortProperty { get; set; }

        public bool? DailyUsageTrendSortDescending { get; set; }

        public string? TimelineSortProperty { get; set; }

        public bool? TimelineSortDescending { get; set; }

        public string? RuntimeSortProperty { get; set; }

        public bool? RuntimeSortDescending { get; set; }

        public string? RuntimeSegmentSortProperty { get; set; }

        public bool? RuntimeSegmentSortDescending { get; set; }

        public Dictionary<string, List<TableColumnLayout>> TableColumnLayouts { get; set; } = new();

        public bool IsCurrentProcessRuntimeRiskAccepted =>
            !IsDangerousProcessRuntimeTracking(
                ProcessRuntimeTrackingEnabled,
                ProcessRuntimeTrackingScope,
                ProcessRuntimeSampleIntervalSeconds)
            || (ProcessRuntimeRiskAcceptedAt is not null
                && ApprovedRiskyProcessRuntimeTrackingScope == ProcessRuntimeTrackingScope
                && ApprovedRiskyProcessRuntimeSampleIntervalSeconds == ProcessRuntimeSampleIntervalSeconds);

        public static AppSettings LoadDefault()
        {
            var settings = new AppSettings(AppDataPaths.SettingsPath);

            if (!File.Exists(AppDataPaths.SettingsPath))
                return settings;

            try
            {
                var persisted = JsonSerializer.Deserialize<PersistedSettings>(File.ReadAllText(AppDataPaths.SettingsPath));
                settings.IdleThresholdMinutes = NormalizeIdleThresholdMinutes(persisted?.IdleThresholdMinutes);
                settings.ProcessRuntimeTrackingEnabled = persisted?.ProcessRuntimeTrackingEnabled
                    ?? DefaultProcessRuntimeTrackingEnabled;
                settings.StartWithWindows = persisted?.StartWithWindows ?? DefaultStartWithWindows;
                settings.StartupPromptShown = persisted?.StartupPromptShown ?? DefaultStartupPromptShown;
                settings.PerformanceDiagnosticsEnabled = persisted?.PerformanceDiagnosticsEnabled
                    ?? DefaultPerformanceDiagnosticsEnabled;
                settings.UiLanguage = NormalizeUiLanguage(persisted?.UiLanguage);
                settings.ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(
                    persisted?.ProcessRuntimeTrackingScope);
                settings.ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(
                    persisted?.ProcessRuntimeSampleIntervalSeconds);
                settings.ApprovedRiskyProcessRuntimeTrackingScope = NormalizeNullableProcessRuntimeTrackingScope(
                    persisted?.ApprovedRiskyProcessRuntimeTrackingScope);
                settings.ApprovedRiskyProcessRuntimeSampleIntervalSeconds =
                    NormalizeNullableProcessRuntimeSampleIntervalSeconds(
                        persisted?.ApprovedRiskyProcessRuntimeSampleIntervalSeconds);
                settings.ProcessRuntimeRiskAcceptedAt = persisted?.ProcessRuntimeRiskAcceptedAt;
                settings.WindowLeft = NormalizeNullableWindowCoordinate(persisted?.WindowLeft);
                settings.WindowTop = NormalizeNullableWindowCoordinate(persisted?.WindowTop);
                settings.WindowWidth = NormalizeNullableWindowSize(persisted?.WindowWidth);
                settings.WindowHeight = NormalizeNullableWindowSize(persisted?.WindowHeight);
                settings.WindowMaximized = persisted?.WindowMaximized ?? false;
                settings.UsageSortProperty = NormalizeNullableText(persisted?.UsageSortProperty);
                settings.UsageSortDescending = persisted?.UsageSortDescending;
                settings.DailyUsageTrendSortProperty = NormalizeNullableText(persisted?.DailyUsageTrendSortProperty);
                settings.DailyUsageTrendSortDescending = persisted?.DailyUsageTrendSortDescending;
                settings.TimelineSortProperty = NormalizeNullableText(persisted?.TimelineSortProperty);
                settings.TimelineSortDescending = persisted?.TimelineSortDescending;
                settings.RuntimeSortProperty = NormalizeNullableText(persisted?.RuntimeSortProperty);
                settings.RuntimeSortDescending = persisted?.RuntimeSortDescending;
                settings.RuntimeSegmentSortProperty = NormalizeNullableText(persisted?.RuntimeSegmentSortProperty);
                settings.RuntimeSegmentSortDescending = persisted?.RuntimeSegmentSortDescending;
                settings.TableColumnLayouts = NormalizeTableColumnLayouts(persisted?.TableColumnLayouts);
            }
            catch
            {
                settings.IdleThresholdMinutes = DefaultIdleThresholdMinutes;
                settings.ProcessRuntimeTrackingEnabled = DefaultProcessRuntimeTrackingEnabled;
                settings.StartWithWindows = DefaultStartWithWindows;
                settings.StartupPromptShown = DefaultStartupPromptShown;
                settings.PerformanceDiagnosticsEnabled = DefaultPerformanceDiagnosticsEnabled;
                settings.UiLanguage = DefaultUiLanguage;
                settings.ProcessRuntimeTrackingScope = DefaultProcessRuntimeTrackingScope;
                settings.ProcessRuntimeSampleIntervalSeconds = DefaultProcessRuntimeSampleIntervalSeconds;
                settings.ApprovedRiskyProcessRuntimeTrackingScope = null;
                settings.ApprovedRiskyProcessRuntimeSampleIntervalSeconds = null;
                settings.ProcessRuntimeRiskAcceptedAt = null;
                settings.WindowLeft = null;
                settings.WindowTop = null;
                settings.WindowWidth = null;
                settings.WindowHeight = null;
                settings.WindowMaximized = false;
                settings.UsageSortProperty = null;
                settings.UsageSortDescending = null;
                settings.DailyUsageTrendSortProperty = null;
                settings.DailyUsageTrendSortDescending = null;
                settings.TimelineSortProperty = null;
                settings.TimelineSortDescending = null;
                settings.RuntimeSortProperty = null;
                settings.RuntimeSortDescending = null;
                settings.RuntimeSegmentSortProperty = null;
                settings.RuntimeSegmentSortDescending = null;
                settings.TableColumnLayouts = new Dictionary<string, List<TableColumnLayout>>();
            }

            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var persisted = new PersistedSettings
            {
                IdleThresholdMinutes = NormalizeIdleThresholdMinutes(IdleThresholdMinutes),
                StartWithWindows = StartWithWindows,
                StartupPromptShown = StartupPromptShown,
                PerformanceDiagnosticsEnabled = PerformanceDiagnosticsEnabled,
                UiLanguage = NormalizeUiLanguage(UiLanguage),
                ProcessRuntimeTrackingEnabled = ProcessRuntimeTrackingEnabled,
                ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(ProcessRuntimeTrackingScope),
                ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(
                    ProcessRuntimeSampleIntervalSeconds),
                ApprovedRiskyProcessRuntimeTrackingScope = NormalizeNullableProcessRuntimeTrackingScope(
                    ApprovedRiskyProcessRuntimeTrackingScope),
                ApprovedRiskyProcessRuntimeSampleIntervalSeconds =
                    NormalizeNullableProcessRuntimeSampleIntervalSeconds(
                        ApprovedRiskyProcessRuntimeSampleIntervalSeconds),
                ProcessRuntimeRiskAcceptedAt = ProcessRuntimeRiskAcceptedAt,
                WindowLeft = NormalizeNullableWindowCoordinate(WindowLeft),
                WindowTop = NormalizeNullableWindowCoordinate(WindowTop),
                WindowWidth = NormalizeNullableWindowSize(WindowWidth),
                WindowHeight = NormalizeNullableWindowSize(WindowHeight),
                WindowMaximized = WindowMaximized,
                UsageSortProperty = NormalizeNullableText(UsageSortProperty),
                UsageSortDescending = UsageSortDescending,
                DailyUsageTrendSortProperty = NormalizeNullableText(DailyUsageTrendSortProperty),
                DailyUsageTrendSortDescending = DailyUsageTrendSortDescending,
                TimelineSortProperty = NormalizeNullableText(TimelineSortProperty),
                TimelineSortDescending = TimelineSortDescending,
                RuntimeSortProperty = NormalizeNullableText(RuntimeSortProperty),
                RuntimeSortDescending = RuntimeSortDescending,
                RuntimeSegmentSortProperty = NormalizeNullableText(RuntimeSegmentSortProperty),
                RuntimeSegmentSortDescending = RuntimeSegmentSortDescending,
                TableColumnLayouts = NormalizeTableColumnLayouts(TableColumnLayouts)
            };
            IdleThresholdMinutes = persisted.IdleThresholdMinutes;
            StartWithWindows = persisted.StartWithWindows;
            StartupPromptShown = persisted.StartupPromptShown;
            PerformanceDiagnosticsEnabled = persisted.PerformanceDiagnosticsEnabled;
            UiLanguage = persisted.UiLanguage;
            ProcessRuntimeTrackingEnabled = persisted.ProcessRuntimeTrackingEnabled;
            ProcessRuntimeTrackingScope = persisted.ProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = persisted.ProcessRuntimeSampleIntervalSeconds;
            ApprovedRiskyProcessRuntimeTrackingScope = persisted.ApprovedRiskyProcessRuntimeTrackingScope;
            ApprovedRiskyProcessRuntimeSampleIntervalSeconds =
                persisted.ApprovedRiskyProcessRuntimeSampleIntervalSeconds;
            ProcessRuntimeRiskAcceptedAt = persisted.ProcessRuntimeRiskAcceptedAt;
            WindowLeft = persisted.WindowLeft;
            WindowTop = persisted.WindowTop;
            WindowWidth = persisted.WindowWidth;
            WindowHeight = persisted.WindowHeight;
            WindowMaximized = persisted.WindowMaximized;
            UsageSortProperty = persisted.UsageSortProperty;
            UsageSortDescending = persisted.UsageSortDescending;
            DailyUsageTrendSortProperty = persisted.DailyUsageTrendSortProperty;
            DailyUsageTrendSortDescending = persisted.DailyUsageTrendSortDescending;
            TimelineSortProperty = persisted.TimelineSortProperty;
            TimelineSortDescending = persisted.TimelineSortDescending;
            RuntimeSortProperty = persisted.RuntimeSortProperty;
            RuntimeSortDescending = persisted.RuntimeSortDescending;
            RuntimeSegmentSortProperty = persisted.RuntimeSegmentSortProperty;
            RuntimeSegmentSortDescending = persisted.RuntimeSegmentSortDescending;
            TableColumnLayouts = persisted.TableColumnLayouts;

            File.WriteAllText(
                settingsPath,
                JsonSerializer.Serialize(persisted, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void SetIdleThresholdMinutes(int minutes)
        {
            IdleThresholdMinutes = NormalizeIdleThresholdMinutes(minutes);
            Save();
        }

        public void SetProcessRuntimeTracking(
            bool isEnabled,
            ProcessRuntimeTrackingScope scope,
            int sampleIntervalSeconds,
            bool riskAccepted = false)
        {
            ProcessRuntimeTrackingEnabled = isEnabled;
            ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(scope);
            ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(sampleIntervalSeconds);
            if (IsDangerousProcessRuntimeTracking(
                    ProcessRuntimeTrackingEnabled,
                    ProcessRuntimeTrackingScope,
                    ProcessRuntimeSampleIntervalSeconds)
                && riskAccepted)
            {
                ApprovedRiskyProcessRuntimeTrackingScope = ProcessRuntimeTrackingScope;
                ApprovedRiskyProcessRuntimeSampleIntervalSeconds = ProcessRuntimeSampleIntervalSeconds;
                ProcessRuntimeRiskAcceptedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                ClearProcessRuntimeRiskAcceptance();
            }

            Save();
        }

        public void DisableProcessRuntimeTrackingForSafeMode()
        {
            ProcessRuntimeTrackingEnabled = false;
            ClearProcessRuntimeRiskAcceptance();
            Save();
        }

        public void SetStartWithWindows(bool isEnabled)
        {
            WindowsStartupRegistration.SetEnabled(isEnabled);
            StartWithWindows = isEnabled;
            Save();
        }

        public void SetStartupPromptResult(bool startWithWindows)
        {
            WindowsStartupRegistration.SetEnabled(startWithWindows);
            StartWithWindows = startWithWindows;
            StartupPromptShown = true;
            Save();
        }

        public void SetPerformanceDiagnosticsEnabled(bool isEnabled)
        {
            PerformanceDiagnosticsEnabled = isEnabled;
            Save();
        }

        public void SetUiLanguage(UiLanguage language)
        {
            UiLanguage = NormalizeUiLanguage(language);
            Save();
        }

        public void SetWindowPlacement(Rectangle normalBounds, bool isMaximized)
        {
            WindowLeft = normalBounds.Left;
            WindowTop = normalBounds.Top;
            WindowWidth = normalBounds.Width;
            WindowHeight = normalBounds.Height;
            WindowMaximized = isMaximized;
            Save();
        }

        public void ResetTableSortStates()
        {
            UsageSortProperty = null;
            UsageSortDescending = null;
            DailyUsageTrendSortProperty = null;
            DailyUsageTrendSortDescending = null;
            TimelineSortProperty = null;
            TimelineSortDescending = null;
            RuntimeSortProperty = null;
            RuntimeSortDescending = null;
            RuntimeSegmentSortProperty = null;
            RuntimeSegmentSortDescending = null;
            TableColumnLayouts.Clear();
            Save();
        }

        private static int NormalizeIdleThresholdMinutes(int? minutes)
        {
            return Math.Clamp(
                minutes ?? DefaultIdleThresholdMinutes,
                MinIdleThresholdMinutes,
                MaxIdleThresholdMinutes);
        }

        private static ProcessRuntimeTrackingScope NormalizeProcessRuntimeTrackingScope(
            ProcessRuntimeTrackingScope? scope)
        {
            return Enum.IsDefined(scope ?? DefaultProcessRuntimeTrackingScope)
                ? scope ?? DefaultProcessRuntimeTrackingScope
                : DefaultProcessRuntimeTrackingScope;
        }

        private static UiLanguage NormalizeUiLanguage(UiLanguage? language)
        {
            return Enum.IsDefined(language ?? DefaultUiLanguage)
                ? language ?? DefaultUiLanguage
                : DefaultUiLanguage;
        }

        private static ProcessRuntimeTrackingScope? NormalizeNullableProcessRuntimeTrackingScope(
            ProcessRuntimeTrackingScope? scope)
        {
            return Enum.IsDefined(scope ?? DefaultProcessRuntimeTrackingScope) ? scope : null;
        }

        private static int NormalizeProcessRuntimeSampleIntervalSeconds(int? seconds)
        {
            return Math.Clamp(
                seconds ?? DefaultProcessRuntimeSampleIntervalSeconds,
                MinProcessRuntimeSampleIntervalSeconds,
                MaxProcessRuntimeSampleIntervalSeconds);
        }

        private static int? NormalizeNullableProcessRuntimeSampleIntervalSeconds(int? seconds)
        {
            return seconds is null
                ? null
                : NormalizeProcessRuntimeSampleIntervalSeconds(seconds);
        }

        private static int? NormalizeNullableWindowCoordinate(int? value)
        {
            return value is null ? null : Math.Clamp(value.Value, -100000, 100000);
        }

        private static int? NormalizeNullableWindowSize(int? value)
        {
            return value is null ? null : Math.Clamp(value.Value, 1, 100000);
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static Dictionary<string, List<TableColumnLayout>> NormalizeTableColumnLayouts(
            Dictionary<string, List<TableColumnLayout>>? layouts)
        {
            if (layouts is null || layouts.Count == 0)
                return new Dictionary<string, List<TableColumnLayout>>();

            return layouts
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value.Count > 0)
                .ToDictionary(
                    x => x.Key.Trim(),
                    x => x.Value
                        .Where(column => !string.IsNullOrWhiteSpace(column.Name))
                        .Select(column => new TableColumnLayout
                        {
                            Name = column.Name.Trim(),
                            DisplayIndex = Math.Clamp(column.DisplayIndex, 0, 1000),
                            Width = Math.Clamp(column.Width, 1, 10000)
                        })
                        .GroupBy(column => column.Name, StringComparer.Ordinal)
                        .Select(group => group.First())
                        .OrderBy(column => column.DisplayIndex)
                        .ToList(),
                    StringComparer.Ordinal);
        }

        public static bool IsDangerousProcessRuntimeTracking(
            bool isEnabled,
            ProcessRuntimeTrackingScope scope,
            int sampleIntervalSeconds)
        {
            if (!isEnabled)
                return false;

            var normalizedSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(sampleIntervalSeconds);
            if (normalizedSeconds <= DangerousAnyScopeSampleIntervalSeconds)
                return true;

            return scope switch
            {
                ProcessRuntimeTrackingScope.AllProcesses =>
                    normalizedSeconds <= DangerousAllProcessesSampleIntervalSeconds,
                ProcessRuntimeTrackingScope.UserProcesses =>
                    normalizedSeconds <= DangerousUserProcessesSampleIntervalSeconds,
                _ => false
            };
        }

        private void ClearProcessRuntimeRiskAcceptance()
        {
            ApprovedRiskyProcessRuntimeTrackingScope = null;
            ApprovedRiskyProcessRuntimeSampleIntervalSeconds = null;
            ProcessRuntimeRiskAcceptedAt = null;
        }

        private sealed class PersistedSettings
        {
            public int IdleThresholdMinutes { get; set; } = DefaultIdleThresholdMinutes;

            public bool StartWithWindows { get; set; } = DefaultStartWithWindows;

            public bool StartupPromptShown { get; set; } = DefaultStartupPromptShown;

            public bool PerformanceDiagnosticsEnabled { get; set; } = DefaultPerformanceDiagnosticsEnabled;

            public UiLanguage UiLanguage { get; set; } = DefaultUiLanguage;

            public bool ProcessRuntimeTrackingEnabled { get; set; } = DefaultProcessRuntimeTrackingEnabled;

            public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; set; } =
                DefaultProcessRuntimeTrackingScope;

            public int ProcessRuntimeSampleIntervalSeconds { get; set; } =
                DefaultProcessRuntimeSampleIntervalSeconds;

            public ProcessRuntimeTrackingScope? ApprovedRiskyProcessRuntimeTrackingScope { get; set; }

            public int? ApprovedRiskyProcessRuntimeSampleIntervalSeconds { get; set; }

            public DateTimeOffset? ProcessRuntimeRiskAcceptedAt { get; set; }

            public int? WindowLeft { get; set; }

            public int? WindowTop { get; set; }

            public int? WindowWidth { get; set; }

            public int? WindowHeight { get; set; }

            public bool WindowMaximized { get; set; }

            public string? UsageSortProperty { get; set; }

            public bool? UsageSortDescending { get; set; }

            public string? DailyUsageTrendSortProperty { get; set; }

            public bool? DailyUsageTrendSortDescending { get; set; }

            public string? TimelineSortProperty { get; set; }

            public bool? TimelineSortDescending { get; set; }

            public string? RuntimeSortProperty { get; set; }

            public bool? RuntimeSortDescending { get; set; }

            public string? RuntimeSegmentSortProperty { get; set; }

            public bool? RuntimeSegmentSortDescending { get; set; }

            public Dictionary<string, List<TableColumnLayout>> TableColumnLayouts { get; set; } = new();
        }

        public sealed class TableColumnLayout
        {
            public string Name { get; set; } = string.Empty;

            public int DisplayIndex { get; set; }

            public int Width { get; set; }
        }
    }
}
