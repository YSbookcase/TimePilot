namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed class TimePilotFeatureRegistrationFilter
    {
        private readonly ITimePilotFeatureAvailabilityProvider availabilityProvider;

        public TimePilotFeatureRegistrationFilter(ITimePilotFeatureAvailabilityProvider availabilityProvider)
        {
            this.availabilityProvider = availabilityProvider;
        }

        public TimePilotFeatureRegistrationSnapshot CreateSnapshot(TimePilotFeatureRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            var menus = Filter(
                registry.MenuRegistrations,
                registration => registration.FeatureId,
                registration => registration.SortOrder,
                out var unavailableMenus);
            var tabs = Filter(
                registry.TabRegistrations,
                registration => registration.FeatureId,
                registration => registration.SortOrder,
                out var unavailableTabs);
            var settingsSections = Filter(
                registry.SettingsSectionRegistrations,
                registration => registration.FeatureId,
                registration => registration.SortOrder,
                out var unavailableSettingsSections);
            var analyticsPanels = Filter(
                registry.AnalyticsPanelRegistrations,
                registration => registration.FeatureId,
                registration => registration.SortOrder,
                out var unavailableAnalyticsPanels);
            var exportActions = Filter(
                registry.ExportActionRegistrations,
                registration => registration.FeatureId,
                registration => registration.SortOrder,
                out var unavailableExportActions);

            return new TimePilotFeatureRegistrationSnapshot(
                menus,
                tabs,
                settingsSections,
                analyticsPanels,
                exportActions,
                unavailableMenus,
                unavailableTabs,
                unavailableSettingsSections,
                unavailableAnalyticsPanels,
                unavailableExportActions);
        }

        private IReadOnlyList<TRegistration> Filter<TRegistration>(
            IEnumerable<TRegistration> registrations,
            Func<TRegistration, string> getFeatureId,
            Func<TRegistration, int> getSortOrder,
            out IReadOnlyList<TimePilotFeatureRegistrationAvailability<TRegistration>> unavailableRegistrations)
        {
            var available = new List<TRegistration>();
            var unavailable = new List<TimePilotFeatureRegistrationAvailability<TRegistration>>();

            foreach (var registration in registrations.OrderBy(getSortOrder))
            {
                var availability = availabilityProvider.GetAvailability(getFeatureId(registration));
                if (availability.IsAvailable)
                {
                    available.Add(registration);
                    continue;
                }

                unavailable.Add(new TimePilotFeatureRegistrationAvailability<TRegistration>(
                    registration,
                    availability));
            }

            unavailableRegistrations = unavailable;
            return available;
        }
    }
}
