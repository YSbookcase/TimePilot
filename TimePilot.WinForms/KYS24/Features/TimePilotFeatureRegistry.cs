namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed class TimePilotFeatureRegistry
    {
        private readonly Dictionary<string, TimePilotFeatureDefinition> features =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<ITimePilotFeatureModule> modules = new();

        public IReadOnlyCollection<TimePilotFeatureDefinition> Features => features.Values;

        public IReadOnlyList<ITimePilotFeatureModule> Modules => modules;

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
