using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private DetailRuntimeFilterCoordinator CreateDetailRuntimeFilterCoordinator()
        {
            return new DetailRuntimeFilterCoordinator(detailRuntimeFilterComboBox);
        }

        private RuntimeSegmentObservationFilterCoordinator CreateRuntimeSegmentObservationFilterCoordinator()
        {
            return new RuntimeSegmentObservationFilterCoordinator(runtimeSegmentObservationFilterComboBox);
        }

        private RuntimeSegmentZoomCoordinator CreateRuntimeSegmentZoomCoordinator()
        {
            return new RuntimeSegmentZoomCoordinator(
                new TimelineZoomControls(
                    runtimeSegmentZoomRangeLabel,
                    runtimeSegmentZoomOutButton,
                    runtimeSegmentZoomInButton,
                    runtimeSegmentPreviousButton,
                    runtimeSegmentNextButton,
                    runtimeSegmentResetButton,
                    runtimeSegmentZoomScrollBar),
                new TimelineZoomActions(
                    runtimeSegmentTimelineControl.ZoomOut,
                    runtimeSegmentTimelineControl.ZoomIn,
                    runtimeSegmentTimelineControl.PanPrevious,
                    runtimeSegmentTimelineControl.PanNext,
                    runtimeSegmentTimelineControl.ResetView,
                    runtimeSegmentTimelineControl.SetViewStartRatio),
                () => new TimelineZoomState(
                    runtimeSegmentTimelineControl.ViewRangeText,
                    runtimeSegmentTimelineControl.IsZoomed,
                    runtimeSegmentTimelineControl.CanPanPrevious,
                    runtimeSegmentTimelineControl.CanPanNext,
                    runtimeSegmentTimelineControl.ViewWidthRatio,
                    runtimeSegmentTimelineControl.ViewStartRatio));
        }

        private RuntimeSegmentSelectionCoordinator CreateRuntimeSegmentSelectionCoordinator()
        {
            return new RuntimeSegmentSelectionCoordinator(
                runtimeSegmentsGrid,
                runtimeSegmentTimelineControl);
        }

        private void InitializeRuntimeSegmentTimeline()
        {
            detailSplitContainer.Panel2.Controls.Remove(runtimeSegmentsGrid);

            runtimeSegmentPanel.SuspendLayout();
            runtimeSegmentPanel.Dock = DockStyle.Fill;
            runtimeSegmentPanel.ColumnCount = 1;
            runtimeSegmentPanel.RowCount = 2;
            runtimeSegmentPanel.ColumnStyles.Clear();
            runtimeSegmentPanel.RowStyles.Clear();
            runtimeSegmentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            runtimeSegmentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
            runtimeSegmentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            runtimeSegmentTimelinePanel.Dock = DockStyle.Fill;
            runtimeSegmentTimelinePanel.ColumnCount = 1;
            runtimeSegmentTimelinePanel.RowCount = 3;
            runtimeSegmentTimelinePanel.ColumnStyles.Clear();
            runtimeSegmentTimelinePanel.RowStyles.Clear();
            runtimeSegmentTimelinePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            runtimeSegmentTimelinePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            runtimeSegmentTimelinePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            runtimeSegmentTimelinePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 17));

            runtimeSegmentZoomPanel.Dock = DockStyle.Fill;
            runtimeSegmentZoomPanel.Height = 36;
            runtimeSegmentZoomPanel.Padding = new Padding(8, 3, 8, 3);
            runtimeSegmentZoomPanel.WrapContents = false;
            runtimeSegmentZoomRangeLabel.AutoSize = true;
            runtimeSegmentZoomRangeLabel.ForeColor = SystemColors.GrayText;
            runtimeSegmentZoomRangeLabel.Margin = new Padding(0, 7, 12, 0);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentZoomOutButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentZoomInButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentPreviousButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentNextButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentResetButton, width: 52);

            runtimeSegmentTimelineControl.Dock = DockStyle.Fill;
            runtimeSegmentTimelineControl.ViewRangeChanged += OnRuntimeSegmentTimelineViewRangeChanged;
            runtimeSegmentObservationFilterLabel.AutoSize = true;
            runtimeSegmentObservationFilterLabel.Margin = new Padding(12, 8, 3, 0);
            runtimeSegmentObservationFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            runtimeSegmentObservationFilterComboBox.Width = 132;
            runtimeSegmentObservationFilterComboBox.SelectedIndexChanged +=
                OnRuntimeSegmentObservationFilterComboBoxSelectedIndexChanged;
            runtimeSegmentHelpButton.Size = new Size(28, 23);
            runtimeSegmentHelpButton.Margin = new Padding(3, 2, 3, 0);
            runtimeSegmentHelpButton.UseVisualStyleBackColor = true;
            runtimeSegmentHelpButton.Click += OnRuntimeSegmentHelpButtonClick;
            runtimeSegmentZoomScrollBar.Dock = DockStyle.Fill;
            runtimeSegmentZoomScrollBar.Enabled = false;
            runtimeSegmentZoomScrollBar.Visible = false;
            runtimeSegmentZoomScrollBar.Minimum = 0;
            runtimeSegmentZoomScrollBar.Maximum = 1000;
            runtimeSegmentZoomScrollBar.LargeChange = 1000;
            runtimeSegmentsGrid.Dock = DockStyle.Fill;
            runtimeSegmentTimelineControl.SetSegments(
                selectedDetailDate,
                null,
                Array.Empty<ProcessRuntimeSegmentRow>());
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentZoomRangeLabel);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentZoomOutButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentZoomInButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentPreviousButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentNextButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentResetButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentHelpButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentObservationFilterLabel);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentObservationFilterComboBox);
            runtimeSegmentTimelinePanel.Controls.Add(runtimeSegmentZoomPanel, 0, 0);
            runtimeSegmentTimelinePanel.Controls.Add(runtimeSegmentTimelineControl, 0, 1);
            runtimeSegmentTimelinePanel.Controls.Add(runtimeSegmentZoomScrollBar, 0, 2);
            runtimeSegmentPanel.Controls.Add(runtimeSegmentTimelinePanel, 0, 0);
            runtimeSegmentPanel.Controls.Add(runtimeSegmentsGrid, 0, 1);
            runtimeSegmentPanel.ResumeLayout();

            detailSplitContainer.Panel2.Controls.Add(runtimeSegmentPanel);
            RefreshRuntimeSegmentObservationFilterOptions();
            UpdateRuntimeSegmentZoomControls();
        }

        private void RefreshRuntimeSegmentObservationFilterOptions()
        {
            runtimeSegmentObservationFilterCoordinator.RefreshOptions(
                selectedRuntimeSegmentObservationFilter);
            if (runtimeSegmentObservationFilterCoordinator.TryGetSelectedFilter(
                out var selectedFilter))
            {
                selectedRuntimeSegmentObservationFilter = selectedFilter;
            }
        }

        private void OnRuntimeSegmentTimelineViewRangeChanged(object? sender, EventArgs e)
        {
            UpdateRuntimeSegmentZoomControls();
        }

        private void OnRuntimeSegmentHelpButtonClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                RuntimeSegmentHelpContentBuilder.GetHelpMessage(),
                RuntimeSegmentHelpContentBuilder.GetHelpTitle(),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateRuntimeSegmentZoomControls()
        {
            runtimeSegmentZoomCoordinator.Update();
        }

        private static void ConfigureRuntimeSegmentZoomButton(
            Button button,
            int width = 32)
        {
            button.Size = new Size(width, 24);
            button.Margin = new Padding(3, 2, 3, 0);
            button.UseVisualStyleBackColor = true;
        }

        private void RefreshRuntimeSegments(
            DateTimeOffset observedAt,
            RuntimeSegmentSelectionKey? selectionKeyToRestore = null,
            bool selectFirstWhenMissing = true)
        {
            if (storage is null)
                return;

            var selectedRow = GetRuntimeRowForSelectedApp();
            if (selectedRow is null)
            {
                runtimeSegmentSelectionCoordinator.Clear();
                GridViewStatePreserver.SetDataSourcePreservingView(
                    runtimeSegmentsGrid,
                    Array.Empty<ProcessRuntimeSegmentRow>());
                UpdateRuntimeSegmentTimeline(null, Array.Empty<ProcessRuntimeSegmentRow>());
                return;
            }

            var segmentRows = SortRuntimeSegmentRows(FilterRuntimeSegmentRows(
                storage.GetProcessRuntimeSegmentsForDate(
                    selectedRow.AppId,
                    selectedDetailDate,
                    observedAt)));
            var keyToRestore =
                selectionKeyToRestore ?? runtimeSegmentSelectionCoordinator.CurrentKey;
            SetRuntimeSegmentsDataSource(segmentRows);
            runtimeSegmentSelectionCoordinator.RestoreSelection(
                keyToRestore,
                selectFirstWhenMissing: selectFirstWhenMissing && keyToRestore is null);
            UpdateRuntimeSegmentTimeline(selectedRow, segmentRows);
        }

        private void UpdateRuntimeSegmentTimeline(
            ProcessRuntimeSummaryRow? selectedRow,
            IReadOnlyList<ProcessRuntimeSegmentRow> segmentRows)
        {
            runtimeSegmentTimelineControl.SetSegments(
                selectedDetailDate,
                selectedRow,
                segmentRows);
            UpdateRuntimeSegmentZoomControls();
        }

        private void SetRuntimeSegmentsDataSource(
            IReadOnlyList<ProcessRuntimeSegmentRow> segmentRows)
        {
            runtimeSegmentSelectionCoordinator.RunWithoutSelectionEvents(() =>
            {
                GridViewStatePreserver.SetDataSourcePreservingView(
                    runtimeSegmentsGrid,
                    segmentRows,
                    preserveSelection: false);
                runtimeSegmentsGrid.ClearSelection();
                runtimeSegmentsGrid.CurrentCell = null;
            });
        }

        private long? GetSelectedRuntimeAppId()
        {
            return (runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow)?.AppId;
        }

        private ProcessRuntimeSummaryRow? GetSelectedRuntimeSummaryRow()
        {
            return runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow;
        }

        private ProcessRuntimeSummaryRow? GetRuntimeRowForSelectedApp()
        {
            if (selectedRuntimeAppId is { } appId)
            {
                foreach (DataGridViewRow row in runtimeGrid.Rows)
                {
                    if (row.DataBoundItem is ProcessRuntimeSummaryRow runtimeRow
                        && runtimeRow.AppId == appId)
                    {
                        return runtimeRow;
                    }
                }
            }

            return runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow;
        }

        private ProcessRuntimeSummaryRow? SelectRuntimeGridRow(
            int rowIndex,
            int columnIndex,
            bool refreshSegments = true)
        {
            if (rowIndex < 0
                || rowIndex >= runtimeGrid.Rows.Count
                || runtimeGrid.Rows[rowIndex].DataBoundItem is not ProcessRuntimeSummaryRow row)
            {
                return null;
            }

            isSelectingRuntimeGridRow = true;
            try
            {
                runtimeGrid.ClearSelection();
                var targetColumnIndex = columnIndex >= 0
                    ? columnIndex
                    : GridViewStatePreserver.GetFirstDisplayedColumnIndex(runtimeGrid);
                targetColumnIndex = Math.Clamp(
                    targetColumnIndex,
                    0,
                    runtimeGrid.Columns.Count - 1);
                runtimeGrid.CurrentCell = runtimeGrid.Rows[rowIndex].Cells[targetColumnIndex];
                runtimeGrid.Rows[rowIndex].Selected = true;
                selectedRuntimeAppId = row.AppId;
            }
            finally
            {
                isSelectingRuntimeGridRow = false;
            }

            if (refreshSegments)
                RefreshRuntimeSegments(DateTimeOffset.UtcNow);

            return row;
        }

        private void RestoreRuntimeSelection(
            long? appId,
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
        {
            if (appId is null)
                return;

            foreach (DataGridViewRow row in runtimeGrid.Rows)
            {
                if (row.DataBoundItem is not ProcessRuntimeSummaryRow runtimeRow
                    || runtimeRow.AppId != appId.Value)
                {
                    continue;
                }

                runtimeGrid.ClearSelection();
                row.Selected = true;
                var currentCellIndex = Math.Min(
                    Math.Max(firstDisplayedColumnIndex, 0),
                    runtimeGrid.Columns.Count - 1);
                runtimeGrid.CurrentCell = row.Cells[currentCellIndex];
                GridViewStatePreserver.TrySetFirstDisplayedRowIndex(
                    runtimeGrid,
                    firstDisplayedRowIndex);
                GridViewStatePreserver.TrySetFirstDisplayedColumnIndex(
                    runtimeGrid,
                    firstDisplayedColumnIndex);
                GridViewStatePreserver.TrySetHorizontalScrollingOffset(
                    runtimeGrid,
                    horizontalScrollingOffset);
                return;
            }
        }

        private void RestoreRuntimeGridView(
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
        {
            GridViewStatePreserver.TrySetFirstDisplayedRowIndex(
                runtimeGrid,
                firstDisplayedRowIndex);
            GridViewStatePreserver.TrySetFirstDisplayedColumnIndex(
                runtimeGrid,
                firstDisplayedColumnIndex);
            GridViewStatePreserver.TrySetHorizontalScrollingOffset(
                runtimeGrid,
                horizontalScrollingOffset);
        }

        private void ScheduleRuntimeGridViewRestore(
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
        {
            if (runtimeGrid.IsDisposed || !runtimeGrid.IsHandleCreated)
                return;

            runtimeGrid.BeginInvoke(new Action(() =>
            {
                if (runtimeGrid.IsDisposed)
                    return;

                RestoreRuntimeGridView(
                    firstDisplayedRowIndex,
                    firstDisplayedColumnIndex,
                    horizontalScrollingOffset);
            }));
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> FilterRuntimeSummaryRows(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows,
            IReadOnlySet<long>? summaryAppIds)
        {
            IEnumerable<ProcessRuntimeSummaryRow> filteredRows = rows;
            filteredRows = selectedDetailRuntimeFilter switch
            {
                DetailRuntimeFilter.SummaryApps =>
                    filteredRows.Where(row => summaryAppIds?.Contains(row.AppId) == true),
                DetailRuntimeFilter.CurrentTrackingScope =>
                    filteredRows.Where(row => row.IsInCurrentTrackingScope),
                DetailRuntimeFilter.VisibleApps =>
                    filteredRows.Where(row => row.HasMainWindow),
                DetailRuntimeFilter.UserProcesses =>
                    filteredRows.Where(row => row.IsCurrentSessionProcess),
                _ => filteredRows
            };

            if (showRunningRuntimeOnly)
                filteredRows = filteredRows.Where(row => row.HasRunningSession);

            return filteredRows.ToList();
        }

        private IReadOnlyList<ProcessRuntimeSegmentRow> FilterRuntimeSegmentRows(
            IReadOnlyList<ProcessRuntimeSegmentRow> rows)
        {
            IEnumerable<ProcessRuntimeSegmentRow> filteredRows = rows;
            filteredRows = selectedRuntimeSegmentObservationFilter switch
            {
                RuntimeSegmentObservationFilter.VisibleApps =>
                    filteredRows.Where(row => row.HasMainWindow),
                RuntimeSegmentObservationFilter.UserProcesses =>
                    filteredRows.Where(row => !row.HasMainWindow && row.IsCurrentSessionProcess),
                RuntimeSegmentObservationFilter.AllProcesses =>
                    filteredRows.Where(row => !row.HasMainWindow && !row.IsCurrentSessionProcess),
                _ => filteredRows
            };
            return filteredRows.ToList();
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> SortRuntimeSummaryRows(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows)
        {
            IOrderedEnumerable<ProcessRuntimeSummaryRow> sortedRows = runtimeSortProperty switch
            {
                nameof(ProcessRuntimeSummaryRow.AppName) => OrderRuntimeRows(rows, row => row.AppName),
                nameof(ProcessRuntimeSummaryRow.CategoryText) => OrderRuntimeRows(rows, row => row.CategoryText),
                nameof(ProcessRuntimeSummaryRow.FirstObservedAt) => OrderRuntimeRows(rows, row => row.FirstObservedAt),
                nameof(ProcessRuntimeSummaryRow.LastObservedAt) => OrderRuntimeRows(rows, row => row.LastObservedAt),
                nameof(ProcessRuntimeSummaryRow.ActiveUsageMs) => OrderRuntimeRows(rows, row => row.ActiveUsageMs),
                nameof(ProcessRuntimeSummaryRow.IdleRecordedMs) => OrderRuntimeRows(rows, row => row.IdleRecordedMs),
                nameof(ProcessRuntimeSummaryRow.ActualUsageRatio) => OrderRuntimeRows(rows, row => row.ActualUsageRatio ?? -1),
                nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount) => OrderRuntimeRows(rows, row => row.RuntimeSegmentCount),
                nameof(ProcessRuntimeSummaryRow.TrackingTypeText) => OrderRuntimeRows(rows, row => row.TrackingTypeText),
                nameof(ProcessRuntimeSummaryRow.StatusText) => OrderRuntimeRows(rows, row => row.StatusText),
                _ => OrderRuntimeRows(rows, row => row.RuntimeMs)
            };

            return sortedRows.ThenBy(row => row.AppName).ToList();
        }

        private IReadOnlyList<ProcessRuntimeSegmentRow> SortRuntimeSegmentRows(
            IReadOnlyList<ProcessRuntimeSegmentRow> rows)
        {
            IOrderedEnumerable<ProcessRuntimeSegmentRow> sortedRows =
                runtimeSegmentSortProperty switch
                {
                    nameof(ProcessRuntimeSegmentRow.EndedAt) =>
                        OrderRuntimeSegmentRows(rows, row => row.EndedAt),
                    nameof(ProcessRuntimeSegmentRow.DurationMs) =>
                        OrderRuntimeSegmentRows(rows, row => row.DurationMs),
                    nameof(ProcessRuntimeSegmentRow.IsRunning) =>
                        OrderRuntimeSegmentRows(rows, row => row.IsRunning),
                    nameof(ProcessRuntimeSegmentRow.ObservationTypeText) =>
                        OrderRuntimeSegmentRows(rows, row => row.ObservationTypeText),
                    nameof(ProcessRuntimeSegmentRow.ProcessId) =>
                        OrderRuntimeSegmentRows(rows, row => row.ProcessId),
                    _ => OrderRuntimeSegmentRows(rows, row => row.StartedAt)
                };

            return sortedRows.ThenByDescending(row => row.StartedAt).ToList();
        }

        private IOrderedEnumerable<ProcessRuntimeSummaryRow> OrderRuntimeRows<TKey>(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows,
            Func<ProcessRuntimeSummaryRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, runtimeSortOrder);
        }

        private IOrderedEnumerable<ProcessRuntimeSegmentRow> OrderRuntimeSegmentRows<TKey>(
            IReadOnlyList<ProcessRuntimeSegmentRow> rows,
            Func<ProcessRuntimeSegmentRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, runtimeSegmentSortOrder);
        }

        private void OnRuntimeSegmentObservationFilterComboBoxSelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (!runtimeSegmentObservationFilterCoordinator.TryGetSelectedFilter(out var selectedFilter)
                || selectedFilter == selectedRuntimeSegmentObservationFilter)
                return;

            selectedRuntimeSegmentObservationFilter = selectedFilter;
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetRuntimeSortPropertyName(
                runtimeGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            runtimeSortOrder = string.Equals(
                runtimeSortProperty,
                propertyName,
                StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(runtimeSortOrder)
                : SortOrder.Descending;
            runtimeSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeGridSelectionChanged(object? sender, EventArgs e)
        {
            if (isRefreshingRuntimeGrid || isSelectingRuntimeGridRow)
                return;

            selectedRuntimeAppId = GetSelectedRuntimeAppId();
            runtimeSegmentSelectionCoordinator.Clear();
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeSegmentsGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var selectionKey = runtimeSegmentSelectionCoordinator.GetCurrentOrStoredKey();
            var propertyName = GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName(
                runtimeSegmentsGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            runtimeSegmentSortOrder = string.Equals(
                runtimeSegmentSortProperty,
                propertyName,
                StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(runtimeSegmentSortOrder)
                : SortOrder.Descending;
            runtimeSegmentSortProperty = propertyName;
            SaveTableSortState();
            RefreshRuntimeSegments(
                DateTimeOffset.UtcNow,
                selectionKeyToRestore: selectionKey,
                selectFirstWhenMissing: false);
            UpdateSortGlyphs();
        }

        private void OnRunningRuntimeOnlyCheckBoxCheckedChanged(object? sender, EventArgs e)
        {
            showRunningRuntimeOnly = runningRuntimeOnlyCheckBox.Checked;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDetailRuntimeFilterComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (detailRuntimeFilterCoordinator.IsUpdating
                || !detailRuntimeFilterCoordinator.TryGetSelectedFilter(out var selectedFilter))
                return;

            if (mainTabs.SelectedTab != detailTab)
            {
                detailRuntimeFilterCoordinator.RunWithoutSelectionEvents(
                    SyncDetailRuntimeFilterComboBoxSelection);
                return;
            }

            selectedDetailRuntimeFilter = selectedFilter;
            RefreshViews(DateTimeOffset.UtcNow);
        }
    }
}
