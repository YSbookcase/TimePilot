using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void InitializeSummaryPeriodSelector()
        {
            selectedSummaryPeriod = SummaryPeriod.Today;
            selectedSummarySpecificDate = DateTime.Today;
            selectedSummaryCustomStartDate = DateTime.Today;
            selectedSummaryCustomEndDate = DateTime.Today;
            RefreshDetailRuntimeFilterOptions();
            RefreshSummaryPeriodOptions(DateTime.Today);
        }

        private void RefreshSummaryPeriodOptions(DateTime today)
        {
            var options = SummaryPeriodOption.GetOptions(today);
            var selectedIndex = Array.FindIndex(
                options.ToArray(),
                option => option.Period == selectedSummaryPeriod);

            isInitializingSummaryPeriodSelector = true;
            summaryPeriodComboBox.BeginUpdate();
            try
            {
                summaryPeriodComboBox.Items.Clear();
                summaryPeriodComboBox.Items.AddRange(options.Cast<object>().ToArray());
                summaryPeriodComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                summaryPeriodOptionsDate = today;
                selectedSummaryPeriod =
                    ((SummaryPeriodOption)summaryPeriodComboBox.SelectedItem!).Period;

                if (selectedSummarySpecificDate > today)
                    selectedSummarySpecificDate = today;
                if (selectedSummaryCustomStartDate > today)
                    selectedSummaryCustomStartDate = today;
                if (selectedSummaryCustomEndDate > today)
                    selectedSummaryCustomEndDate = today;
                if (selectedSummaryCustomEndDate < selectedSummaryCustomStartDate)
                    selectedSummaryCustomEndDate = selectedSummaryCustomStartDate;
                if (summarySpecificDatePicker.Value.Date > today)
                    summarySpecificDatePicker.Value = today;

                summarySpecificDatePicker.MaxDate = today;
                summarySpecificDatePicker.Value = selectedSummarySpecificDate;
                UpdateSummaryPeriodControlsVisibility();
            }
            finally
            {
                summaryPeriodComboBox.EndUpdate();
                isInitializingSummaryPeriodSelector = false;
            }
        }

        private void RefreshSummaryPeriodOptionsIfDateChanged(DateTimeOffset observedAt)
        {
            var today = observedAt.ToLocalTime().Date;
            if (today != summaryPeriodOptionsDate)
                RefreshSummaryPeriodOptions(today);
        }

        private void InitializeSummaryUsageBars()
        {
            summaryUsageBarsModePanel.Dock = DockStyle.Top;
            summaryUsageBarsModePanel.Height = 30;
            summaryUsageBarsModePanel.Padding = new Padding(0, 0, 0, 2);
            summaryUsageBarsModePanel.WrapContents = false;
            summaryUsageBarsModeLabel.AutoSize = true;
            summaryUsageBarsModeLabel.Margin = new Padding(0, 6, 8, 0);
            summaryUsageBarsModePanel.Controls.Add(summaryUsageBarsModeLabel);
            summaryUsageBarsModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            summaryUsageBarsModeComboBox.Width = 110;
            summaryUsageBarsModeComboBox.SelectedIndexChanged +=
                OnSummaryUsageBarsModeComboBoxSelectedIndexChanged;
            summaryUsageBarsModePanel.Controls.Add(summaryUsageBarsModeComboBox);
            summaryUsageBarsControl.Dock = DockStyle.Fill;
            summaryUsageBarsControl.Font = usageGrid.Font;
            summaryUsageBarsPanel.Controls.Add(summaryUsageBarsControl);
            summaryUsageBarsPanel.Controls.Add(summaryUsageBarsModePanel);
            summaryUsageBarsModePanel.BringToFront();
            RefreshSummaryUsageBarsModeOptions();
        }

        private void SetSummaryUsageBars(IReadOnlyList<UsageSummaryRow> rows)
        {
            summaryUsageBarsControl.SetRows(
                rows,
                GetSelectedUsageSummaryRow(),
                selectedSummaryUsageBarMode);
            summaryUsageBarsPanel.Visible = rows.Count > 0;
        }

        private void RefreshSummaryUsageBarsModeOptions()
        {
            summaryUsageBarsModeLabel.Text =
                UiText.CurrentLanguage == UiLanguage.English ? "Bar" : "막대";
            summaryUsageBarsModeComboBox.BeginUpdate();
            try
            {
                summaryUsageBarsModeComboBox.Items.Clear();
                summaryUsageBarsModeComboBox.Items.Add(
                    UiText.CurrentLanguage == UiLanguage.English ? "App" : "앱");
                summaryUsageBarsModeComboBox.Items.Add(
                    UiText.CurrentLanguage == UiLanguage.English ? "Category" : "분류");
                summaryUsageBarsModeComboBox.SelectedIndex =
                    selectedSummaryUsageBarMode == SummaryUsageBarMode.Category ? 1 : 0;
            }
            finally
            {
                summaryUsageBarsModeComboBox.EndUpdate();
            }
        }

        private void OnSummaryUsageBarsModeComboBoxSelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            selectedSummaryUsageBarMode = summaryUsageBarsModeComboBox.SelectedIndex == 1
                ? SummaryUsageBarMode.Category
                : SummaryUsageBarMode.App;

            if (usageGrid.DataSource is IReadOnlyList<UsageSummaryRow> rows)
                SetSummaryUsageBars(rows);
        }

        private void RestoreUsageGridSelection(UsageSummaryRow? previousSelection)
        {
            if (previousSelection is null || usageGrid.Rows.Count == 0)
                return;

            for (var index = 0; index < usageGrid.Rows.Count; index++)
            {
                if (usageGrid.Rows[index].DataBoundItem is not UsageSummaryRow row
                    || !IsSameUsageSummaryApp(row, previousSelection))
                    continue;

                var columnIndex = Math.Max(
                    GridViewStatePreserver.GetFirstDisplayedColumnIndex(usageGrid),
                    0);
                columnIndex = Math.Min(columnIndex, usageGrid.Columns.Count - 1);
                usageGrid.ClearSelection();
                usageGrid.Rows[index].Selected = true;
                usageGrid.CurrentCell = usageGrid.Rows[index].Cells[columnIndex];
                return;
            }
        }

        private static bool IsSameUsageSummaryApp(UsageSummaryRow left, UsageSummaryRow right)
        {
            if (left.AppId is { } leftAppId && right.AppId is { } rightAppId)
                return leftAppId == rightAppId;

            return string.Equals(
                left.ProcessName,
                right.ProcessName,
                StringComparison.OrdinalIgnoreCase);
        }

        private void OnUsageGridSelectionChanged(object? sender, EventArgs e)
        {
            if (usageGrid.DataSource is IReadOnlyList<UsageSummaryRow> rows)
            {
                summaryUsageBarsControl.SetRows(
                    rows,
                    GetSelectedUsageSummaryRow(),
                    selectedSummaryUsageBarMode);
            }
        }

        private UsageSummaryRow? GetSelectedUsageSummaryRow()
        {
            return usageGrid.CurrentRow?.DataBoundItem as UsageSummaryRow;
        }

        private IReadOnlyList<UsageSummaryRow> SortUsageSummaryRows(
            IReadOnlyList<UsageSummaryRow> rows)
        {
            IOrderedEnumerable<UsageSummaryRow> sortedRows = usageSortProperty switch
            {
                nameof(UsageSummaryRow.AppName) => OrderUsageRows(rows, row => row.AppName),
                nameof(UsageSummaryRow.CategoryText) => OrderUsageRows(rows, row => row.CategoryText),
                nameof(UsageSummaryRow.FirstStartedAt) => OrderUsageRows(rows, row => row.FirstStartedAt),
                nameof(UsageSummaryRow.LastObservedAt) => OrderUsageRows(rows, row => row.LastObservedAt),
                nameof(UsageSummaryRow.UsageRatio) => OrderUsageRows(rows, row => row.UsageRatio),
                nameof(UsageSummaryRow.IdleRecordedMs) => OrderUsageRows(rows, row => row.IdleRecordedMs),
                nameof(UsageSummaryRow.SwitchCount) => OrderUsageRows(rows, row => row.SwitchCount),
                _ => OrderUsageRows(rows, row => row.ActiveUsageMs)
            };

            return sortedRows.ThenBy(row => row.AppName).ToList();
        }

        private IReadOnlyList<DailyUsageTrendRow> SortDailyUsageTrendRows(
            IReadOnlyList<DailyUsageTrendRow> rows)
        {
            IOrderedEnumerable<DailyUsageTrendRow> sortedRows =
                dailyUsageTrendSortProperty switch
                {
                    nameof(DailyUsageTrendRow.ActiveUsageMs) =>
                        OrderDailyUsageTrendRows(rows, row => row.ActiveUsageMs),
                    nameof(DailyUsageTrendRow.TopAppName) =>
                        OrderDailyUsageTrendRows(rows, row => row.TopAppName),
                    nameof(DailyUsageTrendRow.TopAppUsageMs) =>
                        OrderDailyUsageTrendRows(rows, row => row.TopAppUsageMs),
                    _ => OrderDailyUsageTrendRows(rows, row => row.Date)
                };

            return sortedRows.ThenByDescending(row => row.Date).ToList();
        }

        private IOrderedEnumerable<UsageSummaryRow> OrderUsageRows<TKey>(
            IReadOnlyList<UsageSummaryRow> rows,
            Func<UsageSummaryRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, usageSortOrder);
        }

        private IOrderedEnumerable<DailyUsageTrendRow> OrderDailyUsageTrendRows<TKey>(
            IReadOnlyList<DailyUsageTrendRow> rows,
            Func<DailyUsageTrendRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, dailyUsageTrendSortOrder);
        }

        private void OnUsageGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetUsageSortPropertyName(
                usageGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            usageSortOrder = string.Equals(
                usageSortProperty,
                propertyName,
                StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(usageSortOrder)
                : SortOrder.Descending;
            usageSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDailyUsageTrendGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetDailyUsageTrendSortPropertyName(
                dailyUsageTrendGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            dailyUsageTrendSortOrder = string.Equals(
                dailyUsageTrendSortProperty,
                propertyName,
                StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(dailyUsageTrendSortOrder)
                : SortOrder.Descending;
            dailyUsageTrendSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnSummaryPeriodComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isInitializingSummaryPeriodSelector
                || summaryPeriodComboBox.SelectedItem is not SummaryPeriodOption option)
                return;

            selectedSummaryPeriod = option.Period;
            UpdateSummaryPeriodControlsVisibility();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnSummarySpecificDatePickerValueChanged(object? sender, EventArgs e)
        {
            ApplySummarySpecificDate(summarySpecificDatePicker.Value.Date);
        }

        private void OnSummarySpecificDateCalendarButtonClick(object? sender, EventArgs e)
        {
            ShowRecordedDateCalendar(
                summarySpecificDateCalendarButton,
                selectedSummarySpecificDate,
                ApplySummarySpecificDate);
        }

        private void OnSummaryCustomRangeButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new SummaryPeriodRangeForm(
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate,
                DateTime.Today,
                GetRecordedDates);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            selectedSummaryCustomStartDate = dialog.StartDate;
            selectedSummaryCustomEndDate = dialog.EndDate;
            UpdateSummaryCustomRangeLabel();
            if (selectedSummaryPeriod == SummaryPeriod.CustomRange)
                RefreshViews(DateTimeOffset.UtcNow);
        }

        private void ApplySummarySpecificDate(DateTime date)
        {
            selectedSummarySpecificDate = NormalizeSelectableDate(date);
            if (summarySpecificDatePicker.Value.Date != selectedSummarySpecificDate)
            {
                isInitializingSummaryPeriodSelector = true;
                try
                {
                    EnsureDatePickerRangeIncludes(
                        summarySpecificDatePicker,
                        selectedSummarySpecificDate);
                    summarySpecificDatePicker.Value = selectedSummarySpecificDate;
                }
                finally
                {
                    isInitializingSummaryPeriodSelector = false;
                }
            }

            if (!isInitializingSummaryPeriodSelector
                && selectedSummaryPeriod == SummaryPeriod.SpecificDate)
            {
                RefreshViews(DateTimeOffset.UtcNow);
            }
        }

        private void UpdateSummaryPeriodControlsVisibility()
        {
            var isSpecificDate = selectedSummaryPeriod == SummaryPeriod.SpecificDate;
            var isCustomRange = selectedSummaryPeriod == SummaryPeriod.CustomRange;
            summarySpecificDatePicker.Visible = isSpecificDate;
            summarySpecificDateCalendarButton.Visible = isSpecificDate;
            summaryCustomRangeButton.Visible = isCustomRange;
            summaryCustomRangeLabel.Visible = isCustomRange;
            UpdateSummaryCustomRangeLabel();
        }

        private void UpdateSummaryCustomRangeLabel()
        {
            summaryCustomRangeLabel.Text = SummaryCustomRangeLabelFormatter.Format(
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate);
        }
    }
}
