using TimePilot.WinForms.Tables;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void RegisterTableColumnLayoutPersistence()
        {
            foreach (var grid in GetLayoutPersistedGrids())
            {
                grid.ColumnDisplayIndexChanged += OnTableColumnLayoutChanged;
                grid.ColumnWidthChanged += OnTableColumnLayoutChanged;
            }
        }

        private void ApplySavedTableColumnLayouts()
        {
            ApplyTableColumnLayouts(settings.TableColumnLayouts);
        }

        private void ApplyTableColumnLayouts(
            IReadOnlyDictionary<string, List<AppSettings.TableColumnLayout>> layouts)
        {
            isApplyingTableColumnLayouts = true;
            try
            {
                foreach (var grid in GetLayoutPersistedGrids())
                {
                    if (layouts.TryGetValue(grid.Name, out var layout))
                        ApplyTableColumnLayout(grid, layout);
                }
            }
            finally
            {
                isApplyingTableColumnLayouts = false;
            }
        }

        private static void ApplyTableColumnLayout(
            DataGridView grid,
            IReadOnlyList<AppSettings.TableColumnLayout> layout)
        {
            foreach (var columnLayout in layout.OrderBy(x => x.DisplayIndex))
            {
                if (!grid.Columns.Contains(columnLayout.Name))
                    continue;

                var column = grid.Columns[columnLayout.Name];
                column.Width = Math.Clamp(columnLayout.Width, column.MinimumWidth, 10000);
                column.DisplayIndex = Math.Clamp(
                    columnLayout.DisplayIndex,
                    0,
                    grid.Columns.Count - 1);
            }
        }

        private Dictionary<string, List<AppSettings.TableColumnLayout>>
            CaptureTableColumnLayouts()
        {
            return GetLayoutPersistedGrids()
                .ToDictionary(
                    grid => grid.Name,
                    CaptureTableColumnLayout,
                    StringComparer.Ordinal);
        }

        private static List<AppSettings.TableColumnLayout> CaptureTableColumnLayout(
            DataGridView grid)
        {
            return grid.Columns
                .Cast<DataGridViewColumn>()
                .Select(column => new AppSettings.TableColumnLayout
                {
                    Name = column.Name,
                    DisplayIndex = column.DisplayIndex,
                    Width = column.Width
                })
                .OrderBy(column => column.DisplayIndex)
                .ToList();
        }

        private IEnumerable<DataGridView> GetLayoutPersistedGrids()
        {
            yield return usageGrid;
            yield return dailyUsageTrendGrid;
            yield return timelineGrid;
            yield return runtimeGrid;
            yield return runtimeSegmentsGrid;
        }

        private void SaveTableColumnLayouts()
        {
            settings.TableColumnLayouts = CaptureTableColumnLayouts();
            settings.Save();
        }

        private void OnTableColumnLayoutChanged(
            object? sender,
            DataGridViewColumnEventArgs e)
        {
            if (isApplyingTableColumnLayouts)
                return;

            SaveTableColumnLayouts();
        }

        private void ApplySavedTableSortState()
        {
            usageSortProperty = GridSortPropertyResolver.NormalizeUsageSortProperty(
                settings.UsageSortProperty);
            usageSortOrder = GridSortOrderHelper.FromSavedDescending(
                settings.UsageSortDescending,
                SortOrder.Descending);
            dailyUsageTrendSortProperty =
                GridSortPropertyResolver.NormalizeDailyUsageTrendSortProperty(
                    settings.DailyUsageTrendSortProperty);
            dailyUsageTrendSortOrder = GridSortOrderHelper.FromSavedDescending(
                settings.DailyUsageTrendSortDescending,
                SortOrder.Descending);
            timelineSortProperty = GridSortPropertyResolver.NormalizeTimelineSortProperty(
                settings.TimelineSortProperty);
            timelineSortOrder = GridSortOrderHelper.FromSavedDescending(
                settings.TimelineSortDescending,
                SortOrder.Descending);
            runtimeSortProperty = GridSortPropertyResolver.NormalizeRuntimeSortProperty(
                settings.RuntimeSortProperty);
            runtimeSortOrder = GridSortOrderHelper.FromSavedDescending(
                settings.RuntimeSortDescending,
                SortOrder.Descending);
            runtimeSegmentSortProperty =
                GridSortPropertyResolver.NormalizeRuntimeSegmentSortProperty(
                    settings.RuntimeSegmentSortProperty);
            runtimeSegmentSortOrder = GridSortOrderHelper.FromSavedDescending(
                settings.RuntimeSegmentSortDescending,
                SortOrder.Descending);
        }

        private void SaveTableSortState()
        {
            settings.UsageSortProperty = usageSortProperty;
            settings.UsageSortDescending = usageSortOrder == SortOrder.Descending;
            settings.DailyUsageTrendSortProperty = dailyUsageTrendSortProperty;
            settings.DailyUsageTrendSortDescending =
                dailyUsageTrendSortOrder == SortOrder.Descending;
            settings.TimelineSortProperty = timelineSortProperty;
            settings.TimelineSortDescending = timelineSortOrder == SortOrder.Descending;
            settings.RuntimeSortProperty = runtimeSortProperty;
            settings.RuntimeSortDescending = runtimeSortOrder == SortOrder.Descending;
            settings.RuntimeSegmentSortProperty = runtimeSegmentSortProperty;
            settings.RuntimeSegmentSortDescending =
                runtimeSegmentSortOrder == SortOrder.Descending;
            settings.Save();
        }

        private void ResetTableSortState()
        {
            settings.ResetTableSortStates();
            ApplySavedTableSortState();
            ApplyTableColumnLayouts(defaultTableColumnLayouts);
            UpdateSortGlyphs();
            RefreshViews(DateTimeOffset.UtcNow);
            SetStatusText(GetTableSortResetStatusText());
        }

        private void OnResetTableSortMenuItemClick(object? sender, EventArgs e)
        {
            ResetTableSortState();
        }

        private string GetResetTableSortMenuText()
        {
            return settings.UiLanguage == UiLanguage.English
                ? "Reset table sorting"
                : "화면 정렬 초기화";
        }

        private string GetTableSortResetStatusText()
        {
            return settings.UiLanguage == UiLanguage.English
                ? "Table sorting has been reset to defaults."
                : "화면 정렬을 기본값으로 되돌렸습니다.";
        }

        private void UpdateSortGlyphs()
        {
            GridSortGlyphUpdater.UpdateGlyphs(
                usageGrid,
                GridSortPropertyResolver.GetUsageSortPropertyName,
                usageSortProperty,
                usageSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(
                dailyUsageTrendGrid,
                GridSortPropertyResolver.GetDailyUsageTrendSortPropertyName,
                dailyUsageTrendSortProperty,
                dailyUsageTrendSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(
                timelineGrid,
                GridSortPropertyResolver.GetTimelineSortPropertyName,
                timelineSortProperty,
                timelineSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(
                runtimeGrid,
                GridSortPropertyResolver.GetRuntimeSortPropertyName,
                runtimeSortProperty,
                runtimeSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(
                runtimeSegmentsGrid,
                GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName,
                runtimeSegmentSortProperty,
                runtimeSegmentSortOrder);
        }
    }
}
