using TimePilot.WinForms.KYS24.Features;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class CommunityFeatureAvailabilityProviderTests
    {
        [Fact]
        public void CommunityFeature_IsAvailableInCommunityEdition()
        {
            var provider = CreateProvider();

            var availability = provider.GetAvailability(TimePilotFeatureCatalog.CoreAppUsageTracking);

            Assert.True(availability.IsAvailable);
            Assert.Null(availability.UnavailableReason);
            Assert.Equal(TimePilotEdition.Community, availability.Feature.Edition);
        }

        [Fact]
        public void ProCandidateFeature_IsUnavailableInCommunityEdition()
        {
            var provider = CreateProvider();

            var availability = provider.GetAvailability(TimePilotFeatureCatalog.OptionalDetailTracking);

            Assert.False(availability.IsAvailable);
            Assert.Equal(TimePilotEdition.Pro, availability.Feature.Edition);
            Assert.Contains("Community", availability.UnavailableReason);
        }

        [Fact]
        public void Registry_AllowsFutureModulesToRegisterFeatures()
        {
            var registry = TimePilotFeatureRegistry.CreateCommunityRegistry();
            var module = new TestFeatureModule();

            registry.RegisterModule(module);

            Assert.Same(module, Assert.Single(registry.Modules));
            Assert.NotNull(registry.FindFeature(TestFeatureModule.FeatureId));
        }

        [Fact]
        public void Registry_AllowsFutureModulesToRegisterExtensionPoints()
        {
            var registry = TimePilotFeatureRegistry.CreateCommunityRegistry();

            registry.RegisterModule(new TestFeatureModule());

            Assert.Equal("Tools/Detail Tracking", Assert.Single(registry.MenuRegistrations).MenuPath);
            Assert.Equal("detail-tracking", Assert.Single(registry.TabRegistrations).TabKey);
            Assert.Equal("detail-tracking", Assert.Single(registry.SettingsSectionRegistrations).SectionKey);
            Assert.Equal("detail-summary", Assert.Single(registry.AnalyticsPanelRegistrations).PanelKey);
            Assert.Equal("export-detail-data", Assert.Single(registry.ExportActionRegistrations).ActionKey);
        }

        private static CommunityFeatureAvailabilityProvider CreateProvider()
        {
            return new CommunityFeatureAvailabilityProvider(TimePilotFeatureRegistry.CreateCommunityRegistry());
        }

        private sealed class TestFeatureModule : ITimePilotFeatureModule
        {
            public const string FeatureId = "test.future-module-feature";

            public string Id => "test.future-module";

            public void Register(TimePilotFeatureRegistry registry)
            {
                registry.RegisterFeature(new TimePilotFeatureDefinition(
                    FeatureId,
                    "Future module feature",
                    TimePilotEdition.Pro,
                    "Feature registered by a future module."));
                registry.RegisterMenu(new TimePilotMenuRegistration(
                    FeatureId,
                    "Tools/Detail Tracking",
                    "Detail Tracking",
                    100));
                registry.RegisterTab(new TimePilotTabRegistration(
                    FeatureId,
                    "detail-tracking",
                    "Detail Tracking",
                    100));
                registry.RegisterSettingsSection(new TimePilotSettingsSectionRegistration(
                    FeatureId,
                    "detail-tracking",
                    "Detail Tracking",
                    100));
                registry.RegisterAnalyticsPanel(new TimePilotAnalyticsPanelRegistration(
                    FeatureId,
                    "detail-summary",
                    "Detail Summary",
                    100));
                registry.RegisterExportAction(new TimePilotExportActionRegistration(
                    FeatureId,
                    "export-detail-data",
                    "Export Detail Data",
                    100));
            }
        }
    }
}
