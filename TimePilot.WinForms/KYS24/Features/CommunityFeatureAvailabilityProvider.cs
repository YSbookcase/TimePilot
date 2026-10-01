namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed class CommunityFeatureAvailabilityProvider : ITimePilotFeatureAvailabilityProvider
    {
        private readonly TimePilotFeatureRegistry registry;

        public CommunityFeatureAvailabilityProvider(TimePilotFeatureRegistry registry)
        {
            this.registry = registry;
        }

        public TimePilotFeatureAvailability GetAvailability(string featureId)
        {
            var feature = registry.FindFeature(featureId)
                ?? TimePilotFeatureCatalog.Find(featureId)
                ?? new TimePilotFeatureDefinition(
                    featureId,
                    featureId,
                    TimePilotEdition.Pro,
                    "Unknown feature.");

            if (feature.Edition == TimePilotEdition.Community)
                return new TimePilotFeatureAvailability(feature, true, null);

            return new TimePilotFeatureAvailability(
                feature,
                false,
                "This feature is not available in the Community edition.");
        }
    }
}
