namespace TimePilot.WinForms.Navigation
{
    internal static class DateSelectorCoordinator
    {
        public static DateTime NormalizeSelectableDate(DateTime date, DateTime today)
        {
            return date.Date > today.Date ? today.Date : date.Date;
        }

        public static bool CanMoveForward(DateTime selectedDate, DateTime today)
        {
            return selectedDate.Date < today.Date;
        }

        public static DateSelectorRolloverResult GetRollover(
            DateTime previousToday,
            DateTime observedToday,
            DateTime selectedDetailDate,
            DateTime selectedTimelineDate,
            bool autoMoveTodayViews)
        {
            var normalizedToday = observedToday.Date;
            if (normalizedToday == previousToday.Date)
            {
                return new DateSelectorRolloverResult(
                    false,
                    selectedDetailDate.Date,
                    selectedTimelineDate.Date,
                    false);
            }

            var moveDetailToToday =
                autoMoveTodayViews && selectedDetailDate.Date == previousToday.Date;
            var moveTimelineToToday =
                autoMoveTodayViews && selectedTimelineDate.Date == previousToday.Date;

            return new DateSelectorRolloverResult(
                true,
                moveDetailToToday ? normalizedToday : selectedDetailDate.Date,
                moveTimelineToToday ? normalizedToday : selectedTimelineDate.Date,
                moveDetailToToday);
        }
    }

    internal sealed record DateSelectorRolloverResult(
        bool DateChanged,
        DateTime DetailDate,
        DateTime TimelineDate,
        bool ResetRuntimeSelection);
}
