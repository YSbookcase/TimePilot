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
    }
}
