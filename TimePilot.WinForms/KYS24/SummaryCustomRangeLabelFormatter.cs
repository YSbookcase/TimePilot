namespace TimePilot.WinForms.KYS24
{
    internal static class SummaryCustomRangeLabelFormatter
    {
        public static string Format(DateTime startDate, DateTime endDate)
        {
            var dateText = startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd (ddd)")
                : $"{startDate:yyyy-MM-dd (ddd)} ~ {endDate:yyyy-MM-dd (ddd)}";
            var durationText = CalendarRangeDurationFormatter.Format(startDate, endDate, includePrefix: false);
            return $"{dateText} · {durationText}";
        }
    }
}
