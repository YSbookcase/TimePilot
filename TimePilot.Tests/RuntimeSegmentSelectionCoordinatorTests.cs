using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using TimePilot.WinForms;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class RuntimeSegmentSelectionCoordinatorTests
    {
        [Fact]
        public void RestoreSelection_SelectsMatchingSegment()
        {
            using var grid = CreateGrid();
            using var timeline = new RuntimeSegmentTimelineControl();
            var rows = new[]
            {
                CreateSegment(1, DateTimeOffset.Parse("2026-06-23T01:00:00+09:00")),
                CreateSegment(2, DateTimeOffset.Parse("2026-06-23T02:00:00+09:00"))
            };
            grid.DataSource = rows;
            var coordinator = new RuntimeSegmentSelectionCoordinator(grid, timeline);

            var restored = coordinator.RestoreSelection(
                RuntimeSegmentSelectionKey.From(rows[1]),
                selectFirstWhenMissing: false);

            Assert.True(restored);
            Assert.Equal(RuntimeSegmentSelectionKey.From(rows[1]), coordinator.CurrentKey);
            Assert.True(grid.Rows[1].Selected);
        }

        [Fact]
        public void RestoreSelection_SelectsFirstWhenMissingAndRequested()
        {
            using var grid = CreateGrid();
            using var timeline = new RuntimeSegmentTimelineControl();
            var rows = new[]
            {
                CreateSegment(1, DateTimeOffset.Parse("2026-06-23T01:00:00+09:00")),
                CreateSegment(2, DateTimeOffset.Parse("2026-06-23T02:00:00+09:00"))
            };
            grid.DataSource = rows;
            var coordinator = new RuntimeSegmentSelectionCoordinator(grid, timeline);
            var missing = RuntimeSegmentSelectionKey.From(
                CreateSegment(3, DateTimeOffset.Parse("2026-06-23T03:00:00+09:00")));

            var restored = coordinator.RestoreSelection(missing, selectFirstWhenMissing: true);

            Assert.True(restored);
            Assert.Equal(RuntimeSegmentSelectionKey.From(rows[0]), coordinator.CurrentKey);
            Assert.True(grid.Rows[0].Selected);
        }

        [Fact]
        public void Clear_ResetsStoredSelection()
        {
            using var grid = CreateGrid();
            using var timeline = new RuntimeSegmentTimelineControl();
            var rows = new[]
            {
                CreateSegment(1, DateTimeOffset.Parse("2026-06-23T01:00:00+09:00"))
            };
            grid.DataSource = rows;
            var coordinator = new RuntimeSegmentSelectionCoordinator(grid, timeline);
            coordinator.RestoreSelection(RuntimeSegmentSelectionKey.From(rows[0]), selectFirstWhenMissing: false);

            coordinator.Clear();

            Assert.Null(coordinator.CurrentKey);
        }

        private static DataGridView CreateGrid()
        {
            return new DataGridView
            {
                AutoGenerateColumns = true,
                BindingContext = new BindingContext(),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
        }

        private static ProcessRuntimeSegmentRow CreateSegment(int processId, DateTimeOffset startedAt)
        {
            return new ProcessRuntimeSegmentRow(
                startedAt,
                startedAt.AddMinutes(5),
                300_000,
                processId,
                HasMainWindow: true,
                IsCurrentSessionProcess: true);
        }
    }
}
