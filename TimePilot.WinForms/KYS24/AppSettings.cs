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
        public const ProcessRuntimeTrackingScope DefaultProcessRuntimeTrackingScope = ProcessRuntimeTrackingScope.WindowedApps;
        public const int DefaultProcessRuntimeSampleIntervalSeconds = 60;
        public const int MinProcessRuntimeSampleIntervalSeconds = 1;
        public const int MaxProcessRuntimeSampleIntervalSeconds = 3600;
        public const int WarningProcessRuntimeSampleIntervalSeconds = 10;

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

        public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; set; } = DefaultProcessRuntimeTrackingScope;

        public int ProcessRuntimeSampleIntervalSeconds { get; set; } = DefaultProcessRuntimeSampleIntervalSeconds;

        public int ProcessRuntimeSampleIntervalMs => ProcessRuntimeSampleIntervalSeconds * 1000;

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
                settings.ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(
                    persisted?.ProcessRuntimeTrackingScope);
                settings.ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(
                    persisted?.ProcessRuntimeSampleIntervalSeconds);
            }
            catch
            {
                settings.IdleThresholdMinutes = DefaultIdleThresholdMinutes;
                settings.ProcessRuntimeTrackingEnabled = DefaultProcessRuntimeTrackingEnabled;
                settings.StartWithWindows = DefaultStartWithWindows;
                settings.StartupPromptShown = DefaultStartupPromptShown;
                settings.ProcessRuntimeTrackingScope = DefaultProcessRuntimeTrackingScope;
                settings.ProcessRuntimeSampleIntervalSeconds = DefaultProcessRuntimeSampleIntervalSeconds;
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
                ProcessRuntimeTrackingEnabled = ProcessRuntimeTrackingEnabled,
                ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(ProcessRuntimeTrackingScope),
                ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(
                    ProcessRuntimeSampleIntervalSeconds)
            };
            IdleThresholdMinutes = persisted.IdleThresholdMinutes;
            StartWithWindows = persisted.StartWithWindows;
            StartupPromptShown = persisted.StartupPromptShown;
            ProcessRuntimeTrackingEnabled = persisted.ProcessRuntimeTrackingEnabled;
            ProcessRuntimeTrackingScope = persisted.ProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = persisted.ProcessRuntimeSampleIntervalSeconds;

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
            int sampleIntervalSeconds)
        {
            ProcessRuntimeTrackingEnabled = isEnabled;
            ProcessRuntimeTrackingScope = NormalizeProcessRuntimeTrackingScope(scope);
            ProcessRuntimeSampleIntervalSeconds = NormalizeProcessRuntimeSampleIntervalSeconds(sampleIntervalSeconds);
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

        private static int NormalizeProcessRuntimeSampleIntervalSeconds(int? seconds)
        {
            return Math.Clamp(
                seconds ?? DefaultProcessRuntimeSampleIntervalSeconds,
                MinProcessRuntimeSampleIntervalSeconds,
                MaxProcessRuntimeSampleIntervalSeconds);
        }

        private sealed class PersistedSettings
        {
            public int IdleThresholdMinutes { get; set; } = DefaultIdleThresholdMinutes;

            public bool StartWithWindows { get; set; } = DefaultStartWithWindows;

            public bool StartupPromptShown { get; set; } = DefaultStartupPromptShown;

            public bool ProcessRuntimeTrackingEnabled { get; set; } = DefaultProcessRuntimeTrackingEnabled;

            public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; set; } =
                DefaultProcessRuntimeTrackingScope;

            public int ProcessRuntimeSampleIntervalSeconds { get; set; } =
                DefaultProcessRuntimeSampleIntervalSeconds;
        }
    }
}
