using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal static class DateNavigationStatusFormatter
    {
        public static string BuildAutoAdvancedStatus(DateTime today, UiLanguage language)
        {
            var dateText = today.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
            return language == UiLanguage.English
                ? $"Moved today's views to {dateText}"
                : $"오늘 보기 날짜를 {dateText}(으)로 갱신했습니다.";
        }
    }
}
