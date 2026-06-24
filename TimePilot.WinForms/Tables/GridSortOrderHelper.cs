namespace TimePilot.WinForms.Tables
{
    internal static class GridSortOrderHelper
    {
        public static SortOrder Toggle(SortOrder current)
        {
            return current == SortOrder.Descending
                ? SortOrder.Ascending
                : SortOrder.Descending;
        }

        public static SortOrder FromSavedDescending(bool? descending, SortOrder defaultSortOrder)
        {
            return descending is null
                ? defaultSortOrder
                : descending.Value ? SortOrder.Descending : SortOrder.Ascending;
        }
    }
}
