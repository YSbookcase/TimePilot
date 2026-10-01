using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal static class SummaryOverviewFormatter
    {
        public static IReadOnlyList<SummaryOverviewMetric> Build(
            IReadOnlyList<UsageSummaryRow> rows)
        {
            var activeRows = rows
                .Where(row => row.ActiveUsageMs > 0)
                .ToList();
            var totalActiveMs = activeRows.Sum(row => row.ActiveUsageMs);
            var topApp = activeRows
                .OrderByDescending(row => row.ActiveUsageMs)
                .ThenBy(row => row.AppName, StringComparer.CurrentCulture)
                .FirstOrDefault();
            var switchCount = activeRows.Sum(row => row.SwitchCount);
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;

            return
            [
                new SummaryOverviewMetric(
                    UiText.Main.TotalActiveUsageTime,
                    RuntimeDiagnosticsMessageBuilder.FormatDuration(totalActiveMs),
                    isEnglish ? "Foreground active time" : "전경 활성 사용 시간"),
                new SummaryOverviewMetric(
                    isEnglish ? "Apps" : "앱 수",
                    activeRows.Count.ToString("N0", CultureInfo.CurrentCulture),
                    isEnglish ? "Apps with active usage" : "활성 사용 기록이 있는 앱"),
                new SummaryOverviewMetric(
                    UiText.Main.TopApp,
                    topApp?.AppName ?? UiText.Main.NoData,
                    topApp?.ActiveUsageTimeText ?? "-"),
                new SummaryOverviewMetric(
                    UiText.Main.SwitchCount,
                    switchCount.ToString("N0", CultureInfo.CurrentCulture),
                    isEnglish ? "Foreground app changes" : "전경 앱 전환")
            ];
        }
    }

    internal sealed record SummaryOverviewMetric(
        string Label,
        string Value,
        string Detail);
}
