using System.Globalization;
using System.Text;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class UsageCsvExporter
    {
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly TimePilotStorage storage;

        public UsageCsvExporter(TimePilotStorage storage)
        {
            this.storage = storage;
        }

        public IReadOnlyList<string> ExportToday(string baseFilePath, DateTimeOffset now)
        {
            var directory = Path.GetDirectoryName(baseFilePath);
            if (string.IsNullOrWhiteSpace(directory))
                directory = Environment.CurrentDirectory;

            Directory.CreateDirectory(directory);

            var baseName = Path.GetFileNameWithoutExtension(baseFilePath);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = $"TimePilot-usage-{now.ToLocalTime():yyyy-MM-dd}";

            var summaryPath = Path.Combine(directory, $"{baseName}-summary.csv");
            var timelinePath = Path.Combine(directory, $"{baseName}-timeline.csv");
            var runtimeSegmentsPath = Path.Combine(directory, $"{baseName}-runtime-segments.csv");

            ExportSummary(summaryPath, now);
            ExportTimeline(timelinePath, now);
            ExportRuntimeSegments(runtimeSegmentsPath, now);

            return [summaryPath, timelinePath, runtimeSegmentsPath];
        }

        private void ExportSummary(string path, DateTimeOffset now)
        {
            var dateText = FormatDate(now);
            var rows = UsageSummaryRowBuilder.FromForegroundUsage(storage.GetForegroundUsageForDay(now));
            WriteCsv(
                path,
                [
                    UiText.Csv.Date,
                    UiText.Csv.AppName,
                    UiText.Csv.Category,
                    UiText.Csv.ProcessName,
                    UiText.Csv.ActiveUsageTime,
                    UiText.Csv.OverallRatio,
                    UiText.Csv.SwitchCount,
                    UiText.Csv.FirstStartedAt,
                    UiText.Csv.LastObservedAt
                ],
                rows.Select(row => new[]
                {
                    dateText,
                    row.AppName,
                    row.CategoryText,
                    row.ProcessName,
                    row.ActiveUsageTimeText,
                    row.UsageRatioText,
                    row.SwitchCountText,
                    row.FirstStartedAtText,
                    row.LastObservedAtText
                }));
        }

        private void ExportTimeline(string path, DateTimeOffset now)
        {
            var dateText = FormatDate(now);
            var rows = storage.GetActivityTimelineForDay(now)
                .OrderByDescending(row => row.StartedAt)
                .ToList();
            WriteCsv(
                path,
                [
                    UiText.Csv.Date,
                    UiText.Csv.StartedAt,
                    UiText.Csv.EndedAt,
                    UiText.Csv.StartTime,
                    UiText.Csv.EndTime,
                    UiText.Csv.Duration,
                    UiText.Csv.Status,
                    UiText.Csv.AppName,
                    UiText.Csv.ProcessName
                ],
                rows.Select(row => new[]
                {
                    dateText,
                    FormatDateTime(row.StartedAt),
                    FormatDateTime(row.EndedAt),
                    FormatTime(row.StartedAt),
                    FormatTime(row.EndedAt),
                    FormatDuration(row.DurationMs),
                    row.ActivityType,
                    row.DisplayName,
                    FirstNonEmpty(row.ProcessName, GetProcessNameFromPath(row.ExecutablePath))
                }));
        }

        private void ExportRuntimeSegments(string path, DateTimeOffset now)
        {
            var dateText = FormatDate(now);
            var rows = storage.GetProcessRuntimeSegmentExportsForDay(now)
                .OrderByDescending(row => row.StartedAt)
                .ToList();
            WriteCsv(
                path,
                [
                    UiText.Csv.Date,
                    UiText.Csv.AppName,
                    UiText.Csv.ProcessName,
                    UiText.Csv.StartedAt,
                    UiText.Csv.EndedAt,
                    UiText.Csv.StartTime,
                    UiText.Csv.EndTime,
                    UiText.Csv.Runtime,
                    UiText.Csv.Status
                ],
                rows.Select(row => new[]
                {
                    dateText,
                    row.AppName,
                    row.ProcessName,
                    FormatDateTime(row.StartedAt),
                    FormatDateTime(row.EndedAt),
                    FormatTime(row.StartedAt),
                    FormatTime(row.EndedAt),
                    FormatDuration(row.DurationMs),
                    row.EndedAt is null ? UiText.Csv.Running : UiText.Csv.Ended
                }));
        }

        private static void WriteCsv(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
            using var writer = new StreamWriter(path, append: false, Utf8WithBom);
            writer.WriteLine(string.Join(",", headers.Select(Escape)));

            foreach (var row in rows)
            {
                writer.WriteLine(string.Join(",", row.Select(Escape)));
            }
        }

        private static string Escape(string? value)
        {
            value ??= "";
            if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\r') && !value.Contains('\n'))
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string FormatDate(DateTimeOffset timestamp)
        {
            return timestamp.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
        }

        private static string FormatTime(DateTimeOffset? timestamp)
        {
            return timestamp?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture) ?? "";
        }

        private static string FormatDateTime(DateTimeOffset? timestamp)
        {
            return timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture) ?? "";
        }

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(durationMs);
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                (int)span.TotalHours,
                span.Minutes,
                span.Seconds);
        }

        private static string GetProcessNameFromPath(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return "";

            try
            {
                return Path.GetFileNameWithoutExtension(executablePath);
            }
            catch (ArgumentException)
            {
                return "";
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
        }
    }
}
