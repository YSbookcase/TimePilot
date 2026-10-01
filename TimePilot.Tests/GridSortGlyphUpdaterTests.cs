using TimePilot.WinForms.Tables;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class GridSortGlyphUpdaterTests
    {
        [Fact]
        public void UpdateGlyphs_SetsSelectedColumnGlyph()
        {
            using var grid = CreateGrid();

            GridSortGlyphUpdater.UpdateGlyphs(
                grid,
                ResolveSortPropertyName,
                "Name",
                SortOrder.Descending);

            Assert.Equal(SortOrder.None, grid.Columns["idColumn"]?.HeaderCell.SortGlyphDirection);
            Assert.Equal(SortOrder.Descending, grid.Columns["nameColumn"]?.HeaderCell.SortGlyphDirection);
        }

        [Fact]
        public void UpdateGlyphs_ClearsGlyphsForUnknownSortProperty()
        {
            using var grid = CreateGrid();
            grid.Columns["nameColumn"]!.HeaderCell.SortGlyphDirection = SortOrder.Ascending;

            GridSortGlyphUpdater.UpdateGlyphs(
                grid,
                ResolveSortPropertyName,
                "Missing",
                SortOrder.Descending);

            Assert.Equal(SortOrder.None, grid.Columns["idColumn"]?.HeaderCell.SortGlyphDirection);
            Assert.Equal(SortOrder.None, grid.Columns["nameColumn"]?.HeaderCell.SortGlyphDirection);
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView();
            grid.Columns.Add("idColumn", "Id");
            grid.Columns.Add("nameColumn", "Name");
            return grid;
        }

        private static string? ResolveSortPropertyName(string columnName)
        {
            return columnName switch
            {
                "idColumn" => "Id",
                "nameColumn" => "Name",
                _ => null
            };
        }
    }
}
