namespace TimePilot.WinForms.KYS24
{
    internal sealed record SummaryPeriodOption(SummaryPeriod Period, string DisplayName)
    {
        public static IReadOnlyList<SummaryPeriodOption> GetOptions(DateTime today)
        {
            var weekStart = today.AddDays(-GetDaysSinceMonday(today.DayOfWeek));
            var lastWeekStart = weekStart.AddDays(-7);
            var lastWeekEnd = weekStart.AddDays(-1);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);
            var lastMonthEnd = monthStart.AddDays(-1);

            return
            [
                new(SummaryPeriod.Today, UiText.SummaryPeriod.Today(FormatDate(today))),
                new(SummaryPeriod.Yesterday, UiText.SummaryPeriod.Yesterday(FormatDate(today.AddDays(-1)))),
                new(SummaryPeriod.SpecificDate, UiText.SummaryPeriod.SpecificDate),
                new(SummaryPeriod.ThisWeek, UiText.SummaryPeriod.ThisWeek(FormatDate(weekStart))),
                new(SummaryPeriod.LastWeek, UiText.SummaryPeriod.LastWeek(
                    FormatDate(lastWeekStart),
                    FormatDate(lastWeekEnd))),
                new(SummaryPeriod.ThisMonth, UiText.SummaryPeriod.ThisMonth()),
                new(SummaryPeriod.LastMonth, UiText.SummaryPeriod.LastMonth(
                    FormatMonthDay(lastMonthStart),
                    FormatMonthDay(lastMonthEnd))),
                new(SummaryPeriod.ThisYear, UiText.SummaryPeriod.ThisYear(today.Year)),
                new(SummaryPeriod.LastYear, UiText.SummaryPeriod.LastYear(today.Year - 1))
            ];
        }

        private static int GetDaysSinceMonday(DayOfWeek dayOfWeek)
        {
            return ((int)dayOfWeek + 6) % 7;
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("M/d");
        }

        private static string FormatMonthDay(DateTime date)
        {
            return date.ToString("M/d");
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
