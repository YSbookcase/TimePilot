namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void SetStatusText(string text)
        {
            statusText = text;
            RefreshStatusLabel();
        }

        private void SetViewRefreshRunning(bool isRunning, string? message)
        {
            isViewRefreshWaitCursorActive = isRunning;
            viewRefreshStatusText = message;
            UpdateWaitCursor();
            RefreshStatusLabel();
        }

        private static string BuildViewRefreshInProgressStatus()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Loading summary..."
                : "요약 불러오는 중...";
        }

        private void UpdateWaitCursor()
        {
            UseWaitCursor = isExportRunning || isViewRefreshWaitCursorActive;
        }

        private void ReportPerformanceTimings(
            params (string Name, long ElapsedMs)[] timings)
        {
            if (!settings.PerformanceDiagnosticsEnabled)
                return;

            var slowTimings = timings
                .Where(x => x.ElapsedMs >= SlowOperationThresholdMs)
                .Select(x => $"{x.Name} {x.ElapsedMs}ms")
                .ToList();

            if (slowTimings.Count == 0)
                return;

            performanceStatusText =
                UiText.Main.PerformancePrefix + string.Join(", ", slowTimings);
            performanceStatusExpiresAt =
                DateTimeOffset.UtcNow.Add(PerformanceStatusDuration);
            RefreshStatusLabel();
        }

        private void ReportPerformanceEvents(params string[] events)
        {
            if (!settings.PerformanceDiagnosticsEnabled || events.Length == 0)
                return;

            performanceStatusText =
                UiText.Main.PerformancePrefix + string.Join(", ", events);
            performanceStatusExpiresAt =
                DateTimeOffset.UtcNow.Add(PerformanceStatusDuration);
            RefreshStatusLabel();
        }

        private void RefreshStatusLabel()
        {
            if (performanceStatusExpiresAt <= DateTimeOffset.UtcNow)
            {
                performanceStatusText = null;
                performanceStatusExpiresAt = null;
            }

            var parts = new List<string> { statusText };
            if (!string.IsNullOrWhiteSpace(exportStatusText))
                parts.Add(exportStatusText);
            if (!string.IsNullOrWhiteSpace(viewRefreshStatusText))
                parts.Add(viewRefreshStatusText);
            if (!string.IsNullOrWhiteSpace(performanceStatusText))
                parts.Add(performanceStatusText);

            statusLabel.Text = string.Join(
                " | ",
                parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
