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
            var today = now.ToLocalTime().Date;
            return ExportRange(baseFilePath, today, today, now);
        }

        public IReadOnlyList<string> ExportRange(string baseFilePath, DateTime startDate, DateTime endDate, DateTimeOffset now)
        {
            var directory = Path.GetDirectoryName(baseFilePath);
            if (string.IsNullOrWhiteSpace(directory))
                directory = Environment.CurrentDirectory;

            Directory.CreateDirectory(directory);

            var baseName = Path.GetFileNameWithoutExtension(baseFilePath);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = $"DeskTrace-usage-{FormatDateRangeForFileName(startDate, endDate)}";

            var summaryPath = Path.Combine(directory, $"{baseName}-summary.csv");
            var timelinePath = Path.Combine(directory, $"{baseName}-timeline.csv");
            var runtimeSegmentsPath = Path.Combine(directory, $"{baseName}-runtime-segments.csv");

            ExportSummary(summaryPath, startDate, endDate);
            ExportTimeline(timelinePath, startDate, endDate, now);
            ExportRuntimeSegments(runtimeSegmentsPath, startDate, endDate, now);

            return [summaryPath, timelinePath, runtimeSegmentsPath];
        }

        private void ExportSummary(string path, DateTime startDate, DateTime endDate)
        {
            var dateText = FormatDateRange(startDate, endDate);
            var periodStart = CreateLocalDateTimeOffset(startDate);
            var periodEnd = CreateLocalDateTimeOffset(endDate.AddDays(1));
            var rows = UsageSummaryRowBuilder.FromForegroundUsage(storage.GetForegroundUsageForPeriod(periodStart, periodEnd));
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

        private void ExportTimeline(string path, DateTime startDate, DateTime endDate, DateTimeOffset now)
        {
            var rows = EnumerateDates(startDate, endDate)
                .SelectMany(date => storage.GetActivityTimelineForDate(date, now))
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
                    FormatDate(row.StartedAt),
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

        private void ExportRuntimeSegments(string path, DateTime startDate, DateTime endDate, DateTimeOffset now)
        {
            var rows = EnumerateDates(startDate, endDate)
                .SelectMany(date => storage.GetProcessRuntimeSegmentExportsForDate(date, now))
                .OrderByDescending(row => row.StartedAt)
                .ToList();
            WriteCsv(
                path,
                [
                    UiText.Csv.Date,
                    UiText.Csv.AppName,
                    UiText.Csv.Category,
                    UiText.Main.Type,
                    UiText.Csv.Status,
                    UiText.Csv.Runtime,
                    UiText.Csv.StartedAt,
                    UiText.Csv.EndedAt,
                    UiText.Csv.StartTime,
                    UiText.Csv.EndTime,
                    UiText.Csv.ProcessName,
                ],
                rows.Select(row => new[]
                {
                    FormatDate(row.StartedAt),
                    row.AppName,
                    string.IsNullOrWhiteSpace(row.CategoryName)
                        ? UiText.Main.Uncategorized
                        : AppCategoryDisplay.GetDisplayName(row.CategoryName),
                    GetRuntimeTrackingType(row),
                    row.EndedAt is null ? UiText.Csv.Running : UiText.Csv.Ended,
                    FormatDuration(row.DurationMs),
                    FormatDateTime(row.StartedAt),
                    FormatDateTime(row.EndedAt),
                    FormatTime(row.StartedAt),
                    FormatTime(row.EndedAt),
                    row.ProcessName,
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

        private static string GetRuntimeTrackingType(ProcessRuntimeSegmentExportRow row)
        {
            if (row.HasMainWindow)
                return UiText.Main.WindowedApp;

            return row.IsCurrentSessionProcess ? UiText.Main.UserProcess : UiText.Main.AllProcesses;
        }

        private static string FormatDateRange(DateTime startDate, DateTime endDate)
        {
            return startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture)
                : $"{startDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd}";
        }

        private static string FormatDateRangeForFileName(DateTime startDate, DateTime endDate)
        {
            return startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : $"{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";
        }

        private static DateTimeOffset CreateLocalDateTimeOffset(DateTime localDate)
        {
            return new DateTimeOffset(localDate.Date, TimeZoneInfo.Local.GetUtcOffset(localDate.Date));
        }

        private static IEnumerable<DateTime> EnumerateDates(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                yield return date;
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
