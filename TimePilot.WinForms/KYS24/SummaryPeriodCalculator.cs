namespace TimePilot.WinForms.KYS24
{
    internal static class SummaryPeriodCalculator
    {
        public static SummaryPeriodRange GetRange(
            DateTimeOffset observedAt,
            SummaryPeriod period,
            DateTime specificDate)
        {
            var localNow = observedAt.ToLocalTime();
            var localNowDateTime = localNow.DateTime;
            var today = localNowDateTime.Date;
            var periodStart = period switch
            {
                SummaryPeriod.Yesterday => today.AddDays(-1),
                SummaryPeriod.SpecificDate => specificDate.Date,
                SummaryPeriod.ThisWeek => today.AddDays(-GetDaysSinceMonday(today.DayOfWeek)),
                SummaryPeriod.LastWeek => today.AddDays(-GetDaysSinceMonday(today.DayOfWeek) - 7),
                SummaryPeriod.ThisMonth => new DateTime(today.Year, today.Month, 1),
                SummaryPeriod.LastMonth => new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                SummaryPeriod.ThisYear => new DateTime(today.Year, 1, 1),
                SummaryPeriod.LastYear => new DateTime(today.Year - 1, 1, 1),
                _ => today
            };
            var periodEnd = period switch
            {
                SummaryPeriod.Yesterday => today,
                SummaryPeriod.SpecificDate => periodStart.AddDays(1),
                SummaryPeriod.ThisWeek => localNowDateTime,
                SummaryPeriod.LastWeek => periodStart.AddDays(7),
                SummaryPeriod.ThisMonth => localNowDateTime,
                SummaryPeriod.LastMonth => periodStart.AddMonths(1),
                SummaryPeriod.ThisYear => localNowDateTime,
                SummaryPeriod.LastYear => periodStart.AddYears(1),
                _ => localNowDateTime
            };

            var start = new DateTimeOffset(periodStart, TimeZoneInfo.Local.GetUtcOffset(periodStart));
            var end = periodEnd == localNowDateTime
                ? observedAt
                : new DateTimeOffset(periodEnd, TimeZoneInfo.Local.GetUtcOffset(periodEnd));
            return new SummaryPeriodRange(start, end, period != SummaryPeriod.Today);
        }

        private static int GetDaysSinceMonday(DayOfWeek dayOfWeek)
        {
            return ((int)dayOfWeek + 6) % 7;
        }
    }
}
