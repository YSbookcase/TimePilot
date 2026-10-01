using TimePilot.WinForms.Tables;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class GridSortOrderHelperTests
    {
        [Fact]
        public void Toggle_ChangesDescendingToAscending()
        {
            Assert.Equal(SortOrder.Ascending, GridSortOrderHelper.Toggle(SortOrder.Descending));
        }

        [Fact]
        public void Toggle_ChangesAscendingOrNoneToDescending()
        {
            Assert.Equal(SortOrder.Descending, GridSortOrderHelper.Toggle(SortOrder.Ascending));
            Assert.Equal(SortOrder.Descending, GridSortOrderHelper.Toggle(SortOrder.None));
        }

        [Fact]
        public void FromSavedDescending_UsesDefaultWhenNoSavedValue()
        {
            Assert.Equal(
                SortOrder.Ascending,
                GridSortOrderHelper.FromSavedDescending(null, SortOrder.Ascending));
        }

        [Fact]
        public void FromSavedDescending_MapsSavedValue()
        {
            Assert.Equal(SortOrder.Descending, GridSortOrderHelper.FromSavedDescending(true, SortOrder.Ascending));
            Assert.Equal(SortOrder.Ascending, GridSortOrderHelper.FromSavedDescending(false, SortOrder.Descending));
        }
    }
}
