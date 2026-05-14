using System.Text.Json;

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

        public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; set; } = DefaultProcessRuntimeTrackingScope;

        public int ProcessRuntimeSampleIntervalSeconds { get; set; } = DefaultProcessRuntimeSampleIntervalSeconds;

        public int ProcessRuntimeSampleIntervalMs => ProcessRuntimeSampleIntervalSeconds * 1000;

        public ProcessRuntimeTrackingScope? ApprovedRiskyProcessRuntimeTrackingScope { get; set; }

        public int? ApprovedRiskyProcessRuntimeSampleIntervalSeconds { get; set; }

        public DateTimeOffset? ProcessRuntimeRiskAcceptedAt { get; set; }

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
            }
            catch
            {
                settings.IdleThresholdMinutes = DefaultIdleThresholdMinutes;
                settings.ProcessRuntimeTrackingEnabled = DefaultProcessRuntimeTrackingEnabled;
                settings.StartWithWindows = DefaultStartWithWindows;
                settings.StartupPromptShown = DefaultStartupPromptShown;
                settings.PerformanceDiagnosticsEnabled = DefaultPerformanceDiagnosticsEnabled;
                settings.ProcessRuntimeTrackingScope = DefaultProcessRuntimeTrackingScope;
                settings.ProcessRuntimeSampleIntervalSeconds = DefaultProcessRuntimeSampleIntervalSeconds;
                settings.ApprovedRiskyProcessRuntimeTrackingScope = null;
                settings.ApprovedRiskyProcessRuntimeSampleIntervalSeconds = null;
                settings.ProcessRuntimeRiskAcceptedAt = null;
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
                ProcessRuntimeTrackingEnabled = ProcessRuntimeTrackingEnabled,
                ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(ProcessRuntimeTrackingScope),
                ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(
                    ProcessRuntimeSampleIntervalSeconds),
                ApprovedRiskyProcessRuntimeTrackingScope = NormalizeNullableProcessRuntimeTrackingScope(
                    ApprovedRiskyProcessRuntimeTrackingScope),
                ApprovedRiskyProcessRuntimeSampleIntervalSeconds =
                    NormalizeNullableProcessRuntimeSampleIntervalSeconds(
                        ApprovedRiskyProcessRuntimeSampleIntervalSeconds),
                ProcessRuntimeRiskAcceptedAt = ProcessRuntimeRiskAcceptedAt
            };
            IdleThresholdMinutes = persisted.IdleThresholdMinutes;
            StartWithWindows = persisted.StartWithWindows;
            StartupPromptShown = persisted.StartupPromptShown;
            PerformanceDiagnosticsEnabled = persisted.PerformanceDiagnosticsEnabled;
            ProcessRuntimeTrackingEnabled = persisted.ProcessRuntimeTrackingEnabled;
            ProcessRuntimeTrackingScope = persisted.ProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = persisted.ProcessRuntimeSampleIntervalSeconds;
            ApprovedRiskyProcessRuntimeTrackingScope = persisted.ApprovedRiskyProcessRuntimeTrackingScope;
            ApprovedRiskyProcessRuntimeSampleIntervalSeconds =
                persisted.ApprovedRiskyProcessRuntimeSampleIntervalSeconds;
            ProcessRuntimeRiskAcceptedAt = persisted.ProcessRuntimeRiskAcceptedAt;

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

            public bool ProcessRuntimeTrackingEnabled { get; set; } = DefaultProcessRuntimeTrackingEnabled;

            public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; set; } =
                DefaultProcessRuntimeTrackingScope;

            public int ProcessRuntimeSampleIntervalSeconds { get; set; } =
                DefaultProcessRuntimeSampleIntervalSeconds;

            public ProcessRuntimeTrackingScope? ApprovedRiskyProcessRuntimeTrackingScope { get; set; }

            public int? ApprovedRiskyProcessRuntimeSampleIntervalSeconds { get; set; }

            public DateTimeOffset? ProcessRuntimeRiskAcceptedAt { get; set; }
        }
    }
}
