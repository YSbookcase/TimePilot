namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotFeatureRegistrationSnapshot(
        IReadOnlyList<TimePilotMenuRegistration> Menus,
        IReadOnlyList<TimePilotTabRegistration> Tabs,
        IReadOnlyList<TimePilotSettingsSectionRegistration> SettingsSections,
        IReadOnlyList<TimePilotAnalyticsPanelRegistration> AnalyticsPanels,
        IReadOnlyList<TimePilotExportActionRegistration> ExportActions,
        IReadOnlyList<TimePilotFeatureRegistrationAvailability<TimePilotMenuRegistration>> UnavailableMenus,
        IReadOnlyList<TimePilotFeatureRegistrationAvailability<TimePilotTabRegistration>> UnavailableTabs,
        IReadOnlyList<TimePilotFeatureRegistrationAvailability<TimePilotSettingsSectionRegistration>> UnavailableSettingsSections,
        IReadOnlyList<TimePilotFeatureRegistrationAvailability<TimePilotAnalyticsPanelRegistration>> UnavailableAnalyticsPanels,
        IReadOnlyList<TimePilotFeatureRegistrationAvailability<TimePilotExportActionRegistration>> UnavailableExportActions);
}
