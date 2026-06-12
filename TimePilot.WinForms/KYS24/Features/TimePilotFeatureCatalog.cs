namespace TimePilot.WinForms.KYS24.Features
{
    internal static class TimePilotFeatureCatalog
    {
        public const string CoreAppUsageTracking = "community.core-app-usage-tracking";

        public const string OptionalDetailTracking = "pro.optional-detail-tracking";

        public static IReadOnlyList<TimePilotFeatureDefinition> All { get; } =
            new[]
            {
                new TimePilotFeatureDefinition(
                    CoreAppUsageTracking,
                    "Core app usage tracking",
                    TimePilotEdition.Community,
                    "Records foreground app usage, idle state, timeline, summary, and detail views."),
                new TimePilotFeatureDefinition(
                    OptionalDetailTracking,
                    "Optional detail tracking",
                    TimePilotEdition.Pro,
                    "Opt-in tracking for sensitive details such as window titles, domains, URLs, or document names.")
            };

        public static TimePilotFeatureDefinition? Find(string featureId)
        {
            return All.FirstOrDefault(feature => string.Equals(feature.Id, featureId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
