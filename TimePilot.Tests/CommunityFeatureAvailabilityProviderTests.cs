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
            }
        }
    }
}
