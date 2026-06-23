using TimePilot.WinForms.Tables;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class GridViewStatePreserverTests
    {
        [Fact]
        public void SetDataSourcePreservingView_AllowsEmptyRows()
        {
            using var grid = CreateGrid();
            grid.DataSource = new[] { new TestRow(1, "First") };

            GridViewStatePreserver.SetDataSourcePreservingView(grid, Array.Empty<TestRow>());

            Assert.Empty(grid.Rows);
        }

        [Fact]
        public void SetDataSourcePreservingView_RestoresSelectedRowByIndex()
        {
            using var grid = CreateGrid();
            grid.DataSource = CreateRows("One", "Two", "Three");
            grid.CurrentCell = grid.Rows[1].Cells[0];
            grid.Rows[1].Selected = true;

            GridViewStatePreserver.SetDataSourcePreservingView(grid, CreateRows("Alpha", "Beta", "Gamma"));

            Assert.Equal(1, grid.CurrentRow?.Index);
            Assert.True(grid.Rows[1].Selected);
        }

        [Fact]
        public void SetDataSourcePreservingView_CanSkipSelectionRestore()
        {
            using var grid = CreateGrid();
            grid.DataSource = CreateRows("One", "Two", "Three");
            grid.CurrentCell = grid.Rows[2].Cells[0];
            grid.Rows[2].Selected = true;

            GridViewStatePreserver.SetDataSourcePreservingView(
                grid,
                CreateRows("Alpha", "Beta", "Gamma"),
                preserveSelection: false);

            Assert.NotEqual(2, grid.CurrentRow?.Index);
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

        private static TestRow[] CreateRows(params string[] names)
        {
            return names.Select((name, index) => new TestRow(index + 1, name)).ToArray();
        }

        private sealed record TestRow(int Id, string Name);
    }
}
