namespace TimePilot.WinForms.KYS24
{
    internal static class AppWebSearchQueryFormatter
    {
        public static string Format(string appName, string processName, UiLanguage language, string? koreanSuffix)
        {
            var query = Format(appName, processName);
            if (query.Length == 0 || language == UiLanguage.English)
                return query;

            var suffix = koreanSuffix?.Trim() ?? AppSettings.DefaultKoreanWebSearchSuffix;
            return suffix.Length == 0 ? query : $"{query} {suffix}";
        }

        public static string Format(string appName, string processName)
        {
            var parts = new[] { appName, processName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Take(2);
            return string.Join(" ", parts);
        }
    }
}
