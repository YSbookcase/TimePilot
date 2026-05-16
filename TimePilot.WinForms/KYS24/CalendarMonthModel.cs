namespace TimePilot.WinForms.KYS24
{
    internal sealed record CalendarMonthModel(
        int Year,
        int Month,
        IReadOnlyList<CalendarDayCell> Days)
    {
        private const int VisibleDayCount = 42;

        public DateTime FirstDayOfMonth => new(Year, Month, 1);

        public static CalendarMonthModel Create(
            DateTime visibleMonth,
            DateTime selectedDate,
            DateTime today,
            IReadOnlySet<DateTime> recordedDates)
        {
            var monthStart = new DateTime(visibleMonth.Year, visibleMonth.Month, 1);
            var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
            var days = new List<CalendarDayCell>(VisibleDayCount);

            for (var index = 0; index < VisibleDayCount; index++)
            {
                var date = gridStart.AddDays(index).Date;
                days.Add(new CalendarDayCell(
                    date,
                    date.Month == monthStart.Month,
                    date == today.Date,
                    date == selectedDate.Date,
                    recordedDates.Contains(date)));
            }

            return new CalendarMonthModel(monthStart.Year, monthStart.Month, days);
        }
    }
}
