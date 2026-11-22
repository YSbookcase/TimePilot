using TimePilot.WinForms.KYS24.Features;
using TimePilot.WinForms.Menus;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private MainMenuController CreateMainMenuController()
        {
            var controller = new MainMenuController(
                new MainMenuControls(
                    mainMenuStrip,
                    fileMenuItem,
                    exportCsvMenuItem,
                    exportRawDataMenuItem,
                    createDataBackupMenuItem,
                    restoreDataBackupMenuItem,
                    importAppCategoriesMenuItem,
                    exportAppCategoriesMenuItem,
                    exitMenuItem,
                    settingsMenuItem,
                    preferencesMenuItem,
                    appCategoryManagementMenuItem,
                    resetTableSortMenuItem,
                    helpMenuItem,
                    runtimeDiagnosticsMenuItem,
                    sponsorMenuItem,
                    aboutMenuItem),
                new MainMenuActions(
                    OnExportCsvMenuItemClick,
                    OnExportRawDataMenuItemClick,
                    OnCreateDataBackupMenuItemClick,
                    OnRestoreDataBackupMenuItemClick,
                    OnImportAppCategoriesMenuItemClick,
                    OnExportAppCategoriesMenuItemClick,
                    OnExitMenuItemClick,
                    OnPreferencesMenuItemClick,
                    OnAppCategoryManagementMenuItemClick,
                    OnResetTableSortMenuItemClick,
                    OnRuntimeDiagnosticsMenuItemClick,
                    OnSponsorMenuItemClick,
                    OnAboutMenuItemClick));

            var featureRegistry = TimePilotFeatureRegistry.CreateCommunityRegistry();
            var featureRegistrationFilter = new TimePilotFeatureRegistrationFilter(
                new CommunityFeatureAvailabilityProvider(featureRegistry));
            var featureRegistrationSnapshot = featureRegistrationFilter.CreateSnapshot(featureRegistry);
            controller.ApplyFeatureRegistrations(featureRegistrationSnapshot.Menus);
            return controller;
        }

        private MainMenuText CreateMainMenuText()
        {
            return new MainMenuText(
                UiText.Main.FileMenu,
                UiText.Main.ExportCsv,
                UiText.Main.ExportRawData,
                UiText.Main.CreateDataBackup,
                UiText.Main.RestoreDataBackup,
                new CategoryImportText(settings.UiLanguage).Title,
                new CategoryImportText(settings.UiLanguage).ExportTitle,
                UiText.Main.Exit,
                UiText.Main.SettingsMenu,
                UiText.Main.Preferences,
                GetAppCategoryManagementMenuText(),
                GetResetTableSortMenuText(),
                UiText.Main.HelpMenu,
                UiText.Main.RuntimeDiagnostics,
                UiText.Main.Sponsor,
                UiText.Main.About);
        }
    }
}
