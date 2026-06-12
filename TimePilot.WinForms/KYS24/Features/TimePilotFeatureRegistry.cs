namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed class TimePilotFeatureRegistry
    {
        private readonly Dictionary<string, TimePilotFeatureDefinition> features =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<ITimePilotFeatureModule> modules = new();
        private readonly List<TimePilotMenuRegistration> menuRegistrations = new();
        private readonly List<TimePilotTabRegistration> tabRegistrations = new();
        private readonly List<TimePilotSettingsSectionRegistration> settingsSectionRegistrations = new();
        private readonly List<TimePilotAnalyticsPanelRegistration> analyticsPanelRegistrations = new();
        private readonly List<TimePilotExportActionRegistration> exportActionRegistrations = new();

        public IReadOnlyCollection<TimePilotFeatureDefinition> Features => features.Values;

        public IReadOnlyList<ITimePilotFeatureModule> Modules => modules;

        public IReadOnlyList<TimePilotMenuRegistration> MenuRegistrations => menuRegistrations;

        public IReadOnlyList<TimePilotTabRegistration> TabRegistrations => tabRegistrations;

        public IReadOnlyList<TimePilotSettingsSectionRegistration> SettingsSectionRegistrations => settingsSectionRegistrations;

        public IReadOnlyList<TimePilotAnalyticsPanelRegistration> AnalyticsPanelRegistrations => analyticsPanelRegistrations;

        public IReadOnlyList<TimePilotExportActionRegistration> ExportActionRegistrations => exportActionRegistrations;

        public void RegisterFeature(TimePilotFeatureDefinition feature)
        {
            ArgumentNullException.ThrowIfNull(feature);

            features[feature.Id] = feature;
        }

        public void RegisterModule(ITimePilotFeatureModule module)
        {
            ArgumentNullException.ThrowIfNull(module);

            modules.Add(module);
            module.Register(this);
        }

        public void RegisterMenu(TimePilotMenuRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);

            menuRegistrations.Add(registration);
        }

        public void RegisterTab(TimePilotTabRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);

            tabRegistrations.Add(registration);
        }

        public void RegisterSettingsSection(TimePilotSettingsSectionRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);

            settingsSectionRegistrations.Add(registration);
        }

        public void RegisterAnalyticsPanel(TimePilotAnalyticsPanelRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);

            analyticsPanelRegistrations.Add(registration);
        }

        public void RegisterExportAction(TimePilotExportActionRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);

            exportActionRegistrations.Add(registration);
        }

        public TimePilotFeatureDefinition? FindFeature(string featureId)
        {
            return features.TryGetValue(featureId, out var feature)
                ? feature
                : null;
        }

        public static TimePilotFeatureRegistry CreateCommunityRegistry()
        {
            var registry = new TimePilotFeatureRegistry();
            foreach (var feature in TimePilotFeatureCatalog.All)
                registry.RegisterFeature(feature);

            return registry;
        }
    }
}
