namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed class OptionalDetailTrackingFeatureModule : ITimePilotFeatureModule
    {
        public string Id => "pro.optional-detail-tracking-module";

        public void Register(TimePilotFeatureRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            registry.RegisterMenu(new TimePilotMenuRegistration(
                TimePilotFeatureCatalog.OptionalDetailTracking,
                "Tools/Detail Tracking",
                "Optional Detail Tracking",
                200));
            registry.RegisterTab(new TimePilotTabRegistration(
                TimePilotFeatureCatalog.OptionalDetailTracking,
                "optional-detail-tracking",
                "Detail Tracking",
                200));
            registry.RegisterSettingsSection(new TimePilotSettingsSectionRegistration(
                TimePilotFeatureCatalog.OptionalDetailTracking,
                "optional-detail-tracking",
                "Optional Detail Tracking",
                200));
            registry.RegisterAnalyticsPanel(new TimePilotAnalyticsPanelRegistration(
                TimePilotFeatureCatalog.OptionalDetailTracking,
                "detail-activity-panel",
                "Detail Activity",
                200));
            registry.RegisterExportAction(new TimePilotExportActionRegistration(
                TimePilotFeatureCatalog.OptionalDetailTracking,
                "export-detail-activity",
                "Export Detail Activity",
                200));
        }
    }
}
