using TimePilot.WinForms.KYS24.Features;

namespace TimePilot.WinForms.Menus
{
    internal sealed class MainMenuController
    {
        private readonly MenuStrip menuStrip;
        private readonly ToolStripMenuItem fileMenuItem;
        private readonly ToolStripMenuItem exportCsvMenuItem;
        private readonly ToolStripMenuItem exportRawDataMenuItem;
        private readonly ToolStripMenuItem createDataBackupMenuItem;
        private readonly ToolStripMenuItem restoreDataBackupMenuItem;
        private readonly ToolStripMenuItem exitMenuItem;
        private readonly ToolStripMenuItem settingsMenuItem;
        private readonly ToolStripMenuItem preferencesMenuItem;
        private readonly ToolStripMenuItem appCategoryManagementMenuItem;
        private readonly ToolStripMenuItem resetTableSortMenuItem;
        private readonly ToolStripMenuItem helpMenuItem;
        private readonly ToolStripMenuItem runtimeDiagnosticsMenuItem;
        private readonly ToolStripMenuItem sponsorMenuItem;
        private readonly ToolStripMenuItem aboutMenuItem;
        private readonly List<ToolStripMenuItem> featureMenuItems = new();
        private readonly List<ToolStripMenuItem> featureRootMenuItems = new();

        public MainMenuController(MainMenuControls controls, MainMenuActions actions)
        {
            menuStrip = controls.MenuStrip;
            fileMenuItem = controls.File;
            exportCsvMenuItem = controls.ExportCsv;
            exportRawDataMenuItem = controls.ExportRawData;
            createDataBackupMenuItem = controls.CreateDataBackup;
            restoreDataBackupMenuItem = controls.RestoreDataBackup;
            exitMenuItem = controls.Exit;
            settingsMenuItem = controls.Settings;
            preferencesMenuItem = controls.Preferences;
            appCategoryManagementMenuItem = controls.AppCategoryManagement;
            resetTableSortMenuItem = controls.ResetTableSort;
            helpMenuItem = controls.Help;
            runtimeDiagnosticsMenuItem = controls.RuntimeDiagnostics;
            sponsorMenuItem = controls.Sponsor;
            aboutMenuItem = controls.About;

            exportCsvMenuItem.Click += actions.ExportCsv;
            exportRawDataMenuItem.Click += actions.ExportRawData;
            createDataBackupMenuItem.Click += actions.CreateDataBackup;
            restoreDataBackupMenuItem.Click += actions.RestoreDataBackup;
            exitMenuItem.Click += actions.Exit;
            preferencesMenuItem.Click += actions.Preferences;
            appCategoryManagementMenuItem.Click += actions.AppCategoryManagement;
            resetTableSortMenuItem.Click += actions.ResetTableSort;
            runtimeDiagnosticsMenuItem.Click += actions.RuntimeDiagnostics;
            sponsorMenuItem.Click += actions.Sponsor;
            aboutMenuItem.Click += actions.About;
        }

        public event EventHandler<TimePilotFeatureMenuRequestedEventArgs>? FeatureMenuRequested;

        public void ApplyText(MainMenuText text)
        {
            fileMenuItem.Text = text.File;
            exportCsvMenuItem.Text = text.ExportCsv;
            exportRawDataMenuItem.Text = text.ExportRawData;
            createDataBackupMenuItem.Text = text.CreateDataBackup;
            restoreDataBackupMenuItem.Text = text.RestoreDataBackup;
            exitMenuItem.Text = text.Exit;
            settingsMenuItem.Text = text.Settings;
            preferencesMenuItem.Text = text.Preferences;
            appCategoryManagementMenuItem.Text = text.AppCategoryManagement;
            resetTableSortMenuItem.Text = text.ResetTableSort;
            helpMenuItem.Text = text.Help;
            runtimeDiagnosticsMenuItem.Text = text.RuntimeDiagnostics;
            sponsorMenuItem.Text = text.Sponsor;
            aboutMenuItem.Text = text.About;
        }

        public void SetDataOperationsEnabled(bool isEnabled)
        {
            exportCsvMenuItem.Enabled = isEnabled;
            exportRawDataMenuItem.Enabled = isEnabled;
            createDataBackupMenuItem.Enabled = isEnabled;
            restoreDataBackupMenuItem.Enabled = isEnabled;
        }

        public void ApplyFeatureRegistrations(IReadOnlyList<TimePilotMenuRegistration> registrations)
        {
            ClearFeatureMenus();

            foreach (var registration in registrations.OrderBy(item => item.SortOrder))
            {
                var parent = ResolveParentMenu(registration.MenuPath);
                var menuItem = new ToolStripMenuItem(registration.Label)
                {
                    Tag = registration
                };
                menuItem.Click += OnFeatureMenuItemClick;
                parent.DropDownItems.Add(menuItem);
                featureMenuItems.Add(menuItem);
            }
        }

        private void ClearFeatureMenus()
        {
            foreach (var menuItem in featureMenuItems.AsEnumerable().Reverse())
            {
                if (menuItem.OwnerItem is ToolStripDropDownItem ownerItem)
                    ownerItem.DropDownItems.Remove(menuItem);
                menuItem.Dispose();
            }

            foreach (var rootMenuItem in featureRootMenuItems)
            {
                menuStrip.Items.Remove(rootMenuItem);
                rootMenuItem.Dispose();
            }

            featureMenuItems.Clear();
            featureRootMenuItems.Clear();
        }

        private ToolStripMenuItem ResolveParentMenu(string menuPath)
        {
            var segments = menuPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                return settingsMenuItem;

            var current = ResolveRootMenu(segments[0]);
            foreach (var segment in segments.Skip(1))
            {
                var child = current.DropDownItems
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => string.Equals(item.Text, segment, StringComparison.CurrentCultureIgnoreCase));
                if (child is null)
                {
                    child = new ToolStripMenuItem(segment);
                    current.DropDownItems.Add(child);
                    featureMenuItems.Add(child);
                }

                current = child;
            }

            return current;
        }

        private ToolStripMenuItem ResolveRootMenu(string segment)
        {
            if (MatchesRoot(segment, "file", fileMenuItem))
                return fileMenuItem;
            if (MatchesRoot(segment, "settings", settingsMenuItem))
                return settingsMenuItem;
            if (MatchesRoot(segment, "help", helpMenuItem))
                return helpMenuItem;

            var existing = featureRootMenuItems.FirstOrDefault(
                item => string.Equals(item.Text, segment, StringComparison.CurrentCultureIgnoreCase));
            if (existing is not null)
                return existing;

            var rootMenuItem = new ToolStripMenuItem(segment);
            menuStrip.Items.Add(rootMenuItem);
            featureRootMenuItems.Add(rootMenuItem);
            return rootMenuItem;
        }

        private static bool MatchesRoot(string segment, string key, ToolStripMenuItem menuItem)
        {
            return string.Equals(segment, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(segment, menuItem.Name, StringComparison.OrdinalIgnoreCase);
        }

        private void OnFeatureMenuItemClick(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: TimePilotMenuRegistration registration })
                return;

            FeatureMenuRequested?.Invoke(this, new TimePilotFeatureMenuRequestedEventArgs(registration));
        }
    }
}
