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
