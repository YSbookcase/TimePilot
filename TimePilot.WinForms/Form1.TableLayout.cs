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
    }
}
