namespace TimePilot.WinForms.Tables
{
    internal static class GridSortGlyphUpdater
    {
        public static void UpdateGlyphs(
            DataGridView grid,
            Func<string, string?> resolveSortPropertyName,
            string selectedSortProperty,
            SortOrder selectedSortOrder)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = resolveSortPropertyName(column.Name) == selectedSortProperty
                    ? selectedSortOrder
                    : SortOrder.None;
            }
        }
    }
}
