using TimePilot.WinForms.KYS24.Features;
using TimePilot.WinForms.Menus;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class MainMenuControllerTests
    {
        [Fact]
        public void BuiltInMenuClick_IsForwardedToConfiguredAction()
        {
            using var menuStrip = CreateMenuStrip();
            var exportRequested = false;
            _ = new MainMenuController(
                CreateControls(menuStrip),
                CreateActions((_, _) => exportRequested = true));

            FindMenuItem(menuStrip, "exportCsvMenuItem").PerformClick();

            Assert.True(exportRequested);
        }

        [Fact]
        public void ApplyText_UpdatesBuiltInMenuLabels()
        {
            using var menuStrip = CreateMenuStrip();
            var controller = new MainMenuController(CreateControls(menuStrip), CreateActions());

            controller.ApplyText(new MainMenuText(
                "File",
                "Export CSV",
                "Export Raw Data",
                "Create Backup",
                "Restore Backup",
                "Exit",
                "Settings",
                "Preferences",
                "App Categories",
                "Reset Sorting",
                "Help",
                "Diagnostics",
                "Sponsor",
                "About"));

            Assert.Equal("File", FindMenuItem(menuStrip, "fileMenuItem").Text);
            Assert.Equal("App Categories", FindMenuItem(menuStrip, "appCategoryManagementMenuItem").Text);
            Assert.Equal("About", FindMenuItem(menuStrip, "aboutMenuItem").Text);
        }

        [Fact]
        public void ApplyFeatureRegistrations_AddsAvailableFeatureMenuAndRaisesRequest()
        {
            using var menuStrip = CreateMenuStrip();
            var controller = new MainMenuController(CreateControls(menuStrip), CreateActions());
            var registration = new TimePilotMenuRegistration(
                TimePilotFeatureCatalog.CoreAppUsageTracking,
                "settings",
                "Community Tool",
                10);
            TimePilotMenuRegistration? requestedRegistration = null;
            controller.FeatureMenuRequested += (_, e) => requestedRegistration = e.Registration;

            controller.ApplyFeatureRegistrations(new[] { registration });
            var settingsMenu = FindMenuItem(menuStrip, "settingsMenuItem");
            var featureMenu = Assert.Single(
                settingsMenu.DropDownItems.OfType<ToolStripMenuItem>(),
                item => item.Text == "Community Tool");
            featureMenu.PerformClick();

            Assert.Same(registration, requestedRegistration);
        }

        [Fact]
        public void SetDataOperationsEnabled_UpdatesAllDataOperationMenus()
        {
            using var menuStrip = CreateMenuStrip();
            var controller = new MainMenuController(CreateControls(menuStrip), CreateActions());

            controller.SetDataOperationsEnabled(false);

            Assert.False(FindMenuItem(menuStrip, "exportCsvMenuItem").Enabled);
            Assert.False(FindMenuItem(menuStrip, "exportRawDataMenuItem").Enabled);
            Assert.False(FindMenuItem(menuStrip, "createDataBackupMenuItem").Enabled);
            Assert.False(FindMenuItem(menuStrip, "restoreDataBackupMenuItem").Enabled);
        }

        [Fact]
        public void ApplyFeatureRegistrations_ReplacesPreviouslyRegisteredMenus()
        {
            using var menuStrip = CreateMenuStrip();
            var controller = new MainMenuController(CreateControls(menuStrip), CreateActions());
            var first = new TimePilotMenuRegistration(
                TimePilotFeatureCatalog.CoreAppUsageTracking,
                "settings",
                "First Tool",
                10);
            var second = new TimePilotMenuRegistration(
                TimePilotFeatureCatalog.CoreAppUsageTracking,
                "settings",
                "Second Tool",
                20);

            controller.ApplyFeatureRegistrations(new[] { first });
            controller.ApplyFeatureRegistrations(new[] { second });

            var settingsMenu = FindMenuItem(menuStrip, "settingsMenuItem");
            Assert.DoesNotContain(
                settingsMenu.DropDownItems.OfType<ToolStripMenuItem>(),
                item => item.Text == "First Tool");
            Assert.Single(
                settingsMenu.DropDownItems.OfType<ToolStripMenuItem>(),
                item => item.Text == "Second Tool");
        }

        private static MenuStrip CreateMenuStrip()
        {
            var menuStrip = new MenuStrip();
            var file = CreateMenu("fileMenuItem",
                "exportCsvMenuItem",
                "exportRawDataMenuItem",
                "createDataBackupMenuItem",
                "restoreDataBackupMenuItem",
                "exitMenuItem");
            var settings = CreateMenu("settingsMenuItem",
                "preferencesMenuItem",
                "appCategoryManagementMenuItem",
                "resetTableSortMenuItem");
            var help = CreateMenu("helpMenuItem",
                "runtimeDiagnosticsMenuItem",
                "sponsorMenuItem",
                "aboutMenuItem");
            menuStrip.Items.AddRange(new ToolStripItem[] { file, settings, help });
            return menuStrip;
        }

        private static ToolStripMenuItem CreateMenu(string name, params string[] childNames)
        {
            var menuItem = new ToolStripMenuItem { Name = name };
            menuItem.DropDownItems.AddRange(
                childNames
                    .Select(childName => new ToolStripMenuItem { Name = childName })
                    .ToArray());
            return menuItem;
        }

        private static ToolStripMenuItem FindMenuItem(MenuStrip menuStrip, string name)
        {
            return EnumerateMenuItems(menuStrip.Items)
                .Single(item => item.Name == name);
        }

        private static MainMenuControls CreateControls(MenuStrip menuStrip)
        {
            return new MainMenuControls(
                menuStrip,
                FindMenuItem(menuStrip, "fileMenuItem"),
                FindMenuItem(menuStrip, "exportCsvMenuItem"),
                FindMenuItem(menuStrip, "exportRawDataMenuItem"),
                FindMenuItem(menuStrip, "createDataBackupMenuItem"),
                FindMenuItem(menuStrip, "restoreDataBackupMenuItem"),
                FindMenuItem(menuStrip, "exitMenuItem"),
                FindMenuItem(menuStrip, "settingsMenuItem"),
                FindMenuItem(menuStrip, "preferencesMenuItem"),
                FindMenuItem(menuStrip, "appCategoryManagementMenuItem"),
                FindMenuItem(menuStrip, "resetTableSortMenuItem"),
                FindMenuItem(menuStrip, "helpMenuItem"),
                FindMenuItem(menuStrip, "runtimeDiagnosticsMenuItem"),
                FindMenuItem(menuStrip, "sponsorMenuItem"),
                FindMenuItem(menuStrip, "aboutMenuItem"));
        }

        private static IEnumerable<ToolStripMenuItem> EnumerateMenuItems(ToolStripItemCollection items)
        {
            foreach (var menuItem in items.OfType<ToolStripMenuItem>())
            {
                yield return menuItem;

                foreach (var child in EnumerateMenuItems(menuItem.DropDownItems))
                    yield return child;
            }
        }

        private static MainMenuActions CreateActions(EventHandler? exportCsv = null)
        {
            EventHandler noOp = (_, _) => { };
            return new MainMenuActions(
                exportCsv ?? noOp,
                noOp,
                noOp,
                noOp,
                noOp,
                noOp,
                noOp,
                noOp,
                noOp,
                noOp,
                noOp);
        }
    }
}
