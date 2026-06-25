using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal static class DataOperationStatusFormatter
    {
        public static string FormatCsvExportRangeForFileName(DateTime startDate, DateTime endDate)
        {
            return startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : $"{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";
        }

        public static string BuildInProgressStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} in progress..."
                : $"{title} 진행 중...";
        }

        public static string BuildCompletedStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} completed."
                : $"{title} 완료.";
        }

        public static string BuildFailedStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} failed."
                : $"{title} 실패.";
        }
    }
}
