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
                new(SummaryPeriod.Today, $"오늘 ({FormatDate(today)})"),
                new(SummaryPeriod.Yesterday, $"어제 ({FormatDate(today.AddDays(-1))})"),
                new(SummaryPeriod.SpecificDate, "특정 날짜"),
                new(SummaryPeriod.ThisWeek, $"이번 주({FormatDate(weekStart)}~오늘)"),
                new(SummaryPeriod.LastWeek, $"지난 주({FormatDate(lastWeekStart)}~{FormatDate(lastWeekEnd)})"),
                new(SummaryPeriod.ThisMonth, $"이번 달(1일~오늘)"),
                new(SummaryPeriod.LastMonth, $"지난 달({FormatMonthDay(lastMonthStart)}~{FormatMonthDay(lastMonthEnd)})"),
                new(SummaryPeriod.ThisYear, $"올해({today.Year}.01~오늘)"),
                new(SummaryPeriod.LastYear, $"작년({today.Year - 1})")
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
