using TimePilot.WinForms.Navigation;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DateSelectorCoordinatorTests
    {
        [Fact]
        public void NormalizeSelectableDate_ClampsFutureDateToToday()
        {
            var today = new DateTime(2026, 6, 28);

            var result = DateSelectorCoordinator.NormalizeSelectableDate(today.AddDays(1), today);

            Assert.Equal(today, result);
        }

        [Fact]
        public void GetRollover_MovesTodayViewsWhenWindowIsNotActivelyViewed()
        {
            var previousToday = new DateTime(2026, 6, 27);
            var observedToday = previousToday.AddDays(1);

            var result = DateSelectorCoordinator.GetRollover(
                previousToday,
                observedToday,
                previousToday,
                previousToday,
                true);

            Assert.True(result.DateChanged);
            Assert.Equal(observedToday, result.DetailDate);
            Assert.Equal(observedToday, result.TimelineDate);
            Assert.True(result.ResetRuntimeSelection);
        }

        [Fact]
        public void GetRollover_PreservesDatesWhileWindowIsActivelyViewed()
        {
            var previousToday = new DateTime(2026, 6, 27);
            var observedToday = previousToday.AddDays(1);

            var result = DateSelectorCoordinator.GetRollover(
                previousToday,
                observedToday,
                previousToday,
                previousToday.AddDays(-2),
                false);

            Assert.True(result.DateChanged);
            Assert.Equal(previousToday, result.DetailDate);
            Assert.Equal(previousToday.AddDays(-2), result.TimelineDate);
            Assert.False(result.ResetRuntimeSelection);
        }

        [Fact]
        public void GetRollover_DoesNothingWhenCalendarDateHasNotChanged()
        {
            var today = new DateTime(2026, 6, 28);

            var result = DateSelectorCoordinator.GetRollover(
                today,
                today,
                today,
                today,
                true);

            Assert.False(result.DateChanged);
        }
    }
}
