namespace TimePilot.WinForms.KYS24.Analytics
{
    internal sealed record DailyAnalyticsRange(DateTime StartDate, DateTime EndDate)
    {
        public int DayCount => (EndDate.Date - StartDate.Date).Days + 1;

        public static DailyAnalyticsRange Create(DateTime startDate, DateTime endDate)
        {
            var normalizedStart = startDate.Date;
            var normalizedEnd = endDate.Date;
            if (normalizedEnd < normalizedStart)
                throw new ArgumentException("End date must be on or after start date.", nameof(endDate));

            return new DailyAnalyticsRange(normalizedStart, normalizedEnd);
        }

        public IEnumerable<DateTime> EnumerateDates()
        {
            for (var date = StartDate.Date; date <= EndDate.Date; date = date.AddDays(1))
                yield return date;
        }
    }
}
