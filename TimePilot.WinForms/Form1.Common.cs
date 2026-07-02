using TimePilot.WinForms.Tables;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private static Icon LoadAppIcon()
        {
            var assetIconPath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "TimePilot.ico");
            if (File.Exists(assetIconPath))
                return new Icon(assetIconPath);

            return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                ?? SystemIcons.Application;
        }

        private static void SetDateStatus(Label label, bool? hasData)
        {
            label.Text = hasData switch
            {
                true => UiText.DateStatus.HasData,
                false => UiText.DateStatus.NoData,
                _ => UiText.DateStatus.NotChecked
            };
            label.ForeColor = hasData switch
            {
                true => Color.DarkGreen,
                false => SystemColors.GrayText,
                _ => SystemColors.GrayText
            };
        }

        private void OnMainTabsSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (mainTabs.SelectedTab == detailTab)
            {
                detailRuntimeFilterCoordinator.RunWithoutSelectionEvents(
                    SyncDetailRuntimeFilterComboBoxSelection);
            }

            RefreshViews(DateTimeOffset.UtcNow);
        }

        private static T? SelectGridRow<T>(
            DataGridView grid,
            int rowIndex,
            int columnIndex)
            where T : class
        {
            if (rowIndex < 0
                || rowIndex >= grid.Rows.Count
                || grid.Rows[rowIndex].DataBoundItem is not T row)
                return null;

            grid.ClearSelection();
            var targetColumnIndex = columnIndex >= 0
                ? columnIndex
                : GridViewStatePreserver.GetFirstDisplayedColumnIndex(grid);
            targetColumnIndex = Math.Clamp(
                targetColumnIndex,
                0,
                grid.Columns.Count - 1);
            grid.CurrentCell = grid.Rows[rowIndex].Cells[targetColumnIndex];
            grid.Rows[rowIndex].Selected = true;
            return row;
        }

        private static bool IsRunningInDesigner()
        {
            return System.ComponentModel.LicenseManager.UsageMode
                == System.ComponentModel.LicenseUsageMode.Designtime;
        }
    }
}
