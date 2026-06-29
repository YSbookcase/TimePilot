using System.Diagnostics;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private IReadOnlyList<UsageSummaryRow> AddIcons(
            IReadOnlyList<UsageSummaryRow> rows)
        {
            return rows
                .Select(row => row with
                {
                    AppIcon = appIconCache.GetIcon(row.ExecutablePath)
                })
                .ToList();
        }

        private void OnUsageGridCellMouseDown(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            if (SelectGridRow<UsageSummaryRow>(
                    usageGrid,
                    e.RowIndex,
                    e.ColumnIndex) is not { } row)
                return;

            usageGridMenu.Items.Clear();
            var showInTimelineItem =
                new ToolStripMenuItem(UiText.Main.ShowInTimeline);
            showInTimelineItem.Click += (_, _) => HighlightUsageRowInTimeline(row);
            usageGridMenu.Items.Add(showInTimelineItem);
            if (row.AppId is { } appId)
            {
                usageGridMenu.Items.Add(CreateSetCategoryMenuItem(
                    row.PrimaryCategoryId,
                    categoryId => SetAppCategory(
                        appId,
                        row.AppName,
                        categoryId)));
            }

            usageGridMenu.Items.Add(
                CreateSearchWebMenuItem(row.AppName, row.ProcessName));
            usageGridMenu.Show(
                usageGrid,
                usageGrid.PointToClient(Cursor.Position));
        }

        private ToolStripMenuItem CreateSetCategoryMenuItem(
            long? currentCategoryId,
            Action<long?> setCategory)
        {
            var setCategoryMenuItem =
                new ToolStripMenuItem(UiText.Main.SetCategory);
            var uncategorizedItem =
                new ToolStripMenuItem(UiText.Main.Uncategorized)
                {
                    Checked = currentCategoryId is null,
                    Tag = (long?)null
                };
            uncategorizedItem.Click += (_, _) => setCategory(null);
            setCategoryMenuItem.DropDownItems.Add(uncategorizedItem);

            if (storage is null)
                return setCategoryMenuItem;

            var categories = storage.GetAppCategoryOptions();
            if (categories.Count > 0)
                setCategoryMenuItem.DropDownItems.Add(new ToolStripSeparator());

            foreach (var category in categories)
            {
                var categoryItem = new ToolStripMenuItem(
                    AppCategoryDisplay.GetDisplayName(category))
                {
                    Checked = currentCategoryId == category.Id,
                    Tag = category.Id
                };
                categoryItem.Click += (_, _) => setCategory(category.Id);
                setCategoryMenuItem.DropDownItems.Add(categoryItem);
            }

            return setCategoryMenuItem;
        }

        private ToolStripMenuItem CreateSearchWebMenuItem(
            string appName,
            string processName)
        {
            var text = settings.UiLanguage == UiLanguage.English
                ? "Search web"
                : "웹에서 검색";
            var item = new ToolStripMenuItem(text);
            item.Click += (_, _) => OpenAppWebSearch(appName, processName);
            return item;
        }

        private void OpenAppWebSearch(string appName, string processName)
        {
            var query = BuildAppWebSearchQuery(appName, processName);
            if (string.IsNullOrWhiteSpace(query))
                return;

            if (settings.UiLanguage != UiLanguage.English)
                query += " 이란";

            var url =
                "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
            try
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    this,
                    settings.UiLanguage == UiLanguage.English
                        ? "Unable to open the browser."
                        : "브라우저를 열 수 없습니다.",
                    UiText.AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string BuildAppWebSearchQuery(
            string appName,
            string processName)
        {
            var parts = new[] { appName, processName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Take(2);
            return string.Join(" ", parts);
        }

        private void SetRuntimeAppCategory(
            long appId,
            string appName,
            long? categoryId)
        {
            SetAppCategory(
                appId,
                appName,
                categoryId,
                selectRuntimeApp: true);
        }

        private void SetAppCategory(
            long appId,
            string appName,
            long? categoryId,
            bool selectRuntimeApp = false)
        {
            if (storage is null)
                return;

            storage.SetAppPrimaryCategory(appId, categoryId);
            var category = categoryId is null
                ? null
                : storage.GetAppCategoryOptions()
                    .FirstOrDefault(x => x.Id == categoryId);
            var categoryName = category is null
                ? UiText.Main.Uncategorized
                : AppCategoryDisplay.GetDisplayName(category);

            if (selectRuntimeApp)
                selectedRuntimeAppId = appId;

            InvalidateCategoryDependentViewCaches();
            SetStatusText(UiText.Main.CategoryUpdated(appName, categoryName));
            RefreshViews(DateTimeOffset.UtcNow);
        }
    }
}
