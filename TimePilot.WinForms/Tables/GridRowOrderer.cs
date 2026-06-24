namespace TimePilot.WinForms.Tables
{
    internal static class GridRowOrderer
    {
        public static IOrderedEnumerable<T> OrderRows<T, TKey>(
            IEnumerable<T> rows,
            Func<T, TKey> keySelector,
            SortOrder sortOrder)
        {
            return sortOrder == SortOrder.Ascending
                ? rows.OrderBy(keySelector)
                : rows.OrderByDescending(keySelector);
        }
    }
}
