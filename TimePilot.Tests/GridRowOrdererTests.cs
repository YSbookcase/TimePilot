using TimePilot.WinForms.Tables;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class GridRowOrdererTests
    {
        [Fact]
        public void OrderRows_SortsAscending()
        {
            var rows = CreateRows();

            var ordered = GridRowOrderer.OrderRows(rows, x => x.Value, SortOrder.Ascending).ToList();

            Assert.Equal(new[] { 1, 2, 3 }, ordered.Select(x => x.Value));
        }

        [Fact]
        public void OrderRows_SortsDescending()
        {
            var rows = CreateRows();

            var ordered = GridRowOrderer.OrderRows(rows, x => x.Value, SortOrder.Descending).ToList();

            Assert.Equal(new[] { 3, 2, 1 }, ordered.Select(x => x.Value));
        }

        private static TestRow[] CreateRows()
        {
            return new[]
            {
                new TestRow(2),
                new TestRow(1),
                new TestRow(3)
            };
        }

        private sealed record TestRow(int Value);
    }
}
