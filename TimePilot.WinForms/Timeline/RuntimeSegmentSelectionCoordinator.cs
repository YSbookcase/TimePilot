using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed class RuntimeSegmentSelectionCoordinator
    {
        private readonly DataGridView grid;
        private readonly RuntimeSegmentTimelineControl timelineControl;
        private bool isRestoringSelection;

        public RuntimeSegmentSelectionCoordinator(
            DataGridView grid,
            RuntimeSegmentTimelineControl timelineControl)
        {
            this.grid = grid;
            this.timelineControl = timelineControl;
        }

        public RuntimeSegmentSelectionKey? CurrentKey { get; private set; }

        public void Initialize()
        {
            grid.SelectionChanged += OnSelectionChanged;
        }

        public void RunWithoutSelectionEvents(Action action)
        {
            isRestoringSelection = true;
            try
            {
                action();
            }
            finally
            {
                isRestoringSelection = false;
            }
        }

        public void Clear()
        {
            CurrentKey = null;
            timelineControl.SetHighlightedSegment(null);
        }

        public RuntimeSegmentSelectionKey? GetCurrentOrStoredKey()
        {
            return (grid.CurrentRow?.DataBoundItem as ProcessRuntimeSegmentRow) is { } selectedSegment
                ? RuntimeSegmentSelectionKey.From(selectedSegment)
                : CurrentKey;
        }

        public bool RestoreSelection(RuntimeSegmentSelectionKey? key, bool selectFirstWhenMissing)
        {
            if (grid.Rows.Count == 0)
            {
                Clear();
                return false;
            }

            if (key is null)
                return SelectFirstIfNeeded(selectFirstWhenMissing);

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is not ProcessRuntimeSegmentRow segment || !key.Value.Matches(segment))
                    continue;

                RunWithoutSelectionEvents(() =>
                {
                    grid.ClearSelection();
                    row.Selected = true;
                    grid.CurrentCell = null;
                    CurrentKey = key;
                    timelineControl.SetHighlightedSegment(segment);
                });

                return true;
            }

            Clear();
            return SelectFirstIfNeeded(selectFirstWhenMissing);
        }

        private bool SelectFirstIfNeeded(bool shouldSelect)
        {
            if (!shouldSelect || grid.CurrentRow is not null || grid.Rows.Count == 0)
                return false;

            RunWithoutSelectionEvents(() =>
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = null;
                if (grid.Rows[0].DataBoundItem is ProcessRuntimeSegmentRow segment)
                {
                    CurrentKey = RuntimeSegmentSelectionKey.From(segment);
                    timelineControl.SetHighlightedSegment(segment);
                }
            });

            return true;
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            if (isRestoringSelection)
                return;

            if (grid.CurrentRow is null && grid.Rows.Count > 0)
                return;

            var selectedSegment = grid.CurrentRow?.DataBoundItem as ProcessRuntimeSegmentRow;
            CurrentKey = selectedSegment is null ? null : RuntimeSegmentSelectionKey.From(selectedSegment);
            timelineControl.SetHighlightedSegment(selectedSegment);
        }
    }
}
