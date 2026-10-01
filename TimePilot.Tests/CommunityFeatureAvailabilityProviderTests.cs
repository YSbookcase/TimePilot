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
            var registry = new TimePilotFeatureRegistry();
            var module = new TestFeatureModule();

            registry.RegisterModule(module);

            Assert.Same(module, Assert.Single(registry.Modules));
            Assert.NotNull(registry.FindFeature(TestFeatureModule.FeatureId));
        }

        [Fact]
        public void Registry_AllowsFutureModulesToRegisterExtensionPoints()
        {
            var registry = new TimePilotFeatureRegistry();

            registry.RegisterModule(new TestFeatureModule());

            Assert.Equal("Tools", Assert.Single(registry.MenuRegistrations).MenuPath);
            Assert.Equal("detail-tracking", Assert.Single(registry.TabRegistrations).TabKey);
            Assert.Equal("detail-tracking", Assert.Single(registry.SettingsSectionRegistrations).SectionKey);
            Assert.Equal("detail-summary", Assert.Single(registry.AnalyticsPanelRegistrations).PanelKey);
            Assert.Equal("export-detail-data", Assert.Single(registry.ExportActionRegistrations).ActionKey);
        }

        [Fact]
        public void RegistrationFilter_ReturnsCommunityRegistrationsAsAvailable()
        {
            var registry = new TimePilotFeatureRegistry();
            registry.RegisterFeature(TimePilotFeatureCatalog.Find(TimePilotFeatureCatalog.CoreAppUsageTracking)!);
            registry.RegisterModule(new CommunityFeatureModule());

            var snapshot = CreateRegistrationFilter(registry).CreateSnapshot(registry);

            Assert.Equal("summary-help", Assert.Single(snapshot.Menus).Label);
            Assert.Equal("summary", Assert.Single(snapshot.Tabs).TabKey);
            Assert.Equal("general", Assert.Single(snapshot.SettingsSections).SectionKey);
            Assert.Equal("summary-panel", Assert.Single(snapshot.AnalyticsPanels).PanelKey);
            Assert.Equal("export-summary", Assert.Single(snapshot.ExportActions).ActionKey);
            Assert.Empty(snapshot.UnavailableMenus);
            Assert.Empty(snapshot.UnavailableTabs);
            Assert.Empty(snapshot.UnavailableSettingsSections);
            Assert.Empty(snapshot.UnavailableAnalyticsPanels);
            Assert.Empty(snapshot.UnavailableExportActions);
        }

        [Fact]
        public void RegistrationFilter_HidesProRegistrationsFromCommunitySnapshot()
        {
            var registry = new TimePilotFeatureRegistry();
            registry.RegisterModule(new TestFeatureModule());

            var snapshot = CreateRegistrationFilter(registry).CreateSnapshot(registry);

            Assert.Empty(snapshot.Menus);
            Assert.Empty(snapshot.Tabs);
            Assert.Empty(snapshot.SettingsSections);
            Assert.Empty(snapshot.AnalyticsPanels);
            Assert.Empty(snapshot.ExportActions);
            Assert.Equal("Tools", Assert.Single(snapshot.UnavailableMenus).Registration.MenuPath);
            Assert.Equal("detail-tracking", Assert.Single(snapshot.UnavailableTabs).Registration.TabKey);
            Assert.Equal("detail-tracking", Assert.Single(snapshot.UnavailableSettingsSections).Registration.SectionKey);
            Assert.Equal("detail-summary", Assert.Single(snapshot.UnavailableAnalyticsPanels).Registration.PanelKey);
            Assert.Equal("export-detail-data", Assert.Single(snapshot.UnavailableExportActions).Registration.ActionKey);
        }

        [Fact]
        public void CommunityRegistry_RegistersOptionalDetailTrackingAsUnavailableCandidate()
        {
            var registry = TimePilotFeatureRegistry.CreateCommunityRegistry();

            var snapshot = CreateRegistrationFilter(registry).CreateSnapshot(registry);

            Assert.Contains(
                registry.Modules,
                module => module is OptionalDetailTrackingFeatureModule);
            Assert.Empty(snapshot.Menus);
            Assert.Empty(snapshot.Tabs);
            Assert.Empty(snapshot.SettingsSections);
            Assert.Empty(snapshot.AnalyticsPanels);
            Assert.Empty(snapshot.ExportActions);
            Assert.Equal("Optional Detail Tracking", Assert.Single(snapshot.UnavailableMenus).Registration.Label);
            Assert.Equal("optional-detail-tracking", Assert.Single(snapshot.UnavailableTabs).Registration.TabKey);
            Assert.Equal("optional-detail-tracking", Assert.Single(snapshot.UnavailableSettingsSections).Registration.SectionKey);
            Assert.Equal("detail-activity-panel", Assert.Single(snapshot.UnavailableAnalyticsPanels).Registration.PanelKey);
            Assert.Equal("export-detail-activity", Assert.Single(snapshot.UnavailableExportActions).Registration.ActionKey);
            Assert.All(
                snapshot.UnavailableMenus,
                registration => Assert.Equal(TimePilotEdition.Pro, registration.Availability.Feature.Edition));
        }

        [Fact]
        public void RegistrationFilter_SortsAvailableRegistrationsBySortOrder()
        {
            var registry = new TimePilotFeatureRegistry();
            registry.RegisterFeature(TimePilotFeatureCatalog.Find(TimePilotFeatureCatalog.CoreAppUsageTracking)!);
            registry.RegisterMenu(new TimePilotMenuRegistration(
                TimePilotFeatureCatalog.CoreAppUsageTracking,
                "Tools/Late",
                "late",
                20));
            registry.RegisterMenu(new TimePilotMenuRegistration(
                TimePilotFeatureCatalog.CoreAppUsageTracking,
                "Tools/Early",
                "early",
                10));

            var snapshot = CreateRegistrationFilter(registry).CreateSnapshot(registry);

            Assert.Equal(new[] { "early", "late" }, snapshot.Menus.Select(menu => menu.Label));
        }

        private static CommunityFeatureAvailabilityProvider CreateProvider()
        {
            return new CommunityFeatureAvailabilityProvider(TimePilotFeatureRegistry.CreateCommunityRegistry());
        }

        private static TimePilotFeatureRegistrationFilter CreateRegistrationFilter(TimePilotFeatureRegistry registry)
        {
            return new TimePilotFeatureRegistrationFilter(new CommunityFeatureAvailabilityProvider(registry));
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
                    "Tools",
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

        private sealed class CommunityFeatureModule : ITimePilotFeatureModule
        {
            public string Id => "test.community-module";

            public void Register(TimePilotFeatureRegistry registry)
            {
                registry.RegisterMenu(new TimePilotMenuRegistration(
                    TimePilotFeatureCatalog.CoreAppUsageTracking,
                    "Help/Summary",
                    "summary-help",
                    10));
                registry.RegisterTab(new TimePilotTabRegistration(
                    TimePilotFeatureCatalog.CoreAppUsageTracking,
                    "summary",
                    "Summary",
                    10));
                registry.RegisterSettingsSection(new TimePilotSettingsSectionRegistration(
                    TimePilotFeatureCatalog.CoreAppUsageTracking,
                    "general",
                    "General",
                    10));
                registry.RegisterAnalyticsPanel(new TimePilotAnalyticsPanelRegistration(
                    TimePilotFeatureCatalog.CoreAppUsageTracking,
                    "summary-panel",
                    "Summary",
                    10));
                registry.RegisterExportAction(new TimePilotExportActionRegistration(
                    TimePilotFeatureCatalog.CoreAppUsageTracking,
                    "export-summary",
                    "Export Summary",
                    10));
            }
        }
    }
}
