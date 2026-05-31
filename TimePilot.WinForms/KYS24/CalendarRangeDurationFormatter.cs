namespace TimePilot.WinForms.KYS24
{
    internal static class CalendarRangeDurationFormatter
    {
        public static string Format(DateTime startDate, DateTime endDate, bool includePrefix)
        {
            if (endDate.Date < startDate.Date)
                return UiText.SummaryPeriod.InvalidCustomRange;

            var totalDays = (endDate.Date - startDate.Date).Days + 1;
            var endExclusive = endDate.Date.AddDays(1);
            var cursor = startDate.Date;

            var years = endExclusive.Year - cursor.Year;
            if (cursor.AddYears(years) > endExclusive)
                years--;
            cursor = cursor.AddYears(years);

            var months = ((endExclusive.Year - cursor.Year) * 12) + endExclusive.Month - cursor.Month;
            if (cursor.AddMonths(months) > endExclusive)
                months--;
            cursor = cursor.AddMonths(months);

            var days = (endExclusive - cursor).Days;
            var text = UiText.CurrentLanguage == UiLanguage.English
                ? FormatEnglishDuration(years, months, days)
                : FormatKoreanDuration(years, months, days);

            var prefix = includePrefix
                ? UiText.CurrentLanguage == UiLanguage.English ? "Selected period: " : "선택 기간: "
                : string.Empty;

            if (years == 0 && months == 0)
                return $"{prefix}{text}";

            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{prefix}{text} ({totalDays:N0} days total)"
                : $"{prefix}{text} (총 {totalDays:N0}일)";
        }

        private static string FormatKoreanDuration(int years, int months, int days)
        {
            var parts = new List<string>();
            if (years > 0)
                parts.Add($"{years:N0}년");
            if (months > 0)
                parts.Add($"{months:N0}개월");
            if (days > 0 || parts.Count == 0)
                parts.Add($"{days:N0}일");

            return string.Join(" ", parts);
        }

        private static string FormatEnglishDuration(int years, int months, int days)
        {
            var parts = new List<string>();
            AddEnglishPart(parts, years, "year");
            AddEnglishPart(parts, months, "month");
            if (days > 0 || parts.Count == 0)
                AddEnglishPart(parts, days, "day");

            return string.Join(" ", parts);
        }

        private static void AddEnglishPart(List<string> parts, int value, string unit)
        {
            if (value <= 0)
                return;

            parts.Add($"{value:N0} {unit}{(value == 1 ? string.Empty : "s")}");
        }
    }
}
