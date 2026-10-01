using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Tables
{
    internal static class GridSortPropertyResolver
    {
        public static string? GetUsageSortPropertyName(string columnName)
        {
            return columnName switch
            {
                "appNameColumn" => nameof(UsageSummaryRow.AppName),
                "appCategoryColumn" => nameof(UsageSummaryRow.CategoryText),
                "firstStartedAtColumn" => nameof(UsageSummaryRow.FirstStartedAt),
                "lastObservedAtColumn" => nameof(UsageSummaryRow.LastObservedAt),
                "activeUsageTimeColumn" => nameof(UsageSummaryRow.ActiveUsageMs),
                "idleRecordedTimeColumn" => nameof(UsageSummaryRow.IdleRecordedMs),
                "usageRatioColumn" => nameof(UsageSummaryRow.UsageRatio),
                "switchCountColumn" => nameof(UsageSummaryRow.SwitchCount),
                _ => null
            };
        }

        public static string? GetDailyUsageTrendSortPropertyName(string columnName)
        {
            return columnName switch
            {
                "dailyUsageDateColumn" => nameof(DailyUsageTrendRow.Date),
                "dailyUsageActiveTimeColumn" => nameof(DailyUsageTrendRow.ActiveUsageMs),
                "dailyUsageTopAppColumn" => nameof(DailyUsageTrendRow.TopAppName),
                "dailyUsageTopAppTimeColumn" => nameof(DailyUsageTrendRow.TopAppUsageMs),
                _ => null
            };
        }

        public static string? GetTimelineSortPropertyName(string columnName)
        {
            return columnName switch
            {
                "timelineTypeColumn" => nameof(ActivityTimelineRow.ActivityType),
                "timelineStartedAtColumn" => nameof(ActivityTimelineRow.StartedAt),
                "timelineEndedAtColumn" => nameof(ActivityTimelineRow.EndedAt),
                "timelineDurationColumn" => nameof(ActivityTimelineRow.DurationMs),
                "timelineDisplayNameColumn" => nameof(ActivityTimelineRow.DisplayName),
                "timelineCategoryColumn" => nameof(ActivityTimelineRow.CategoryText),
                _ => null
            };
        }

        public static string? GetRuntimeSortPropertyName(string columnName)
        {
            return columnName switch
            {
                "runtimeAppNameColumn" => nameof(ProcessRuntimeSummaryRow.AppName),
                "runtimeCategoryColumn" => nameof(ProcessRuntimeSummaryRow.CategoryText),
                "runtimeTrackingTypeColumn" => nameof(ProcessRuntimeSummaryRow.TrackingTypeText),
                "runtimeFirstObservedAtColumn" => nameof(ProcessRuntimeSummaryRow.FirstObservedAt),
                "runtimeLastObservedAtColumn" => nameof(ProcessRuntimeSummaryRow.LastObservedAt),
                "runtimeDurationColumn" => nameof(ProcessRuntimeSummaryRow.RuntimeMs),
                "runtimeActiveUsageColumn" => nameof(ProcessRuntimeSummaryRow.ActiveUsageMs),
                "runtimeIdleRecordedColumn" => nameof(ProcessRuntimeSummaryRow.IdleRecordedMs),
                "runtimeActualUsageRatioColumn" => nameof(ProcessRuntimeSummaryRow.ActualUsageRatio),
                "runtimeSessionCountColumn" => nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount),
                "runtimeStatusColumn" => nameof(ProcessRuntimeSummaryRow.StatusText),
                _ => null
            };
        }

        public static string? GetRuntimeSegmentSortPropertyName(string columnName)
        {
            return columnName switch
            {
                "runtimeSegmentStartedAtColumn" => nameof(ProcessRuntimeSegmentRow.StartedAt),
                "runtimeSegmentEndedAtColumn" => nameof(ProcessRuntimeSegmentRow.EndedAt),
                "runtimeSegmentDurationColumn" => nameof(ProcessRuntimeSegmentRow.DurationMs),
                "runtimeSegmentStatusColumn" => nameof(ProcessRuntimeSegmentRow.IsRunning),
                "runtimeSegmentObservationTypeColumn" => nameof(ProcessRuntimeSegmentRow.ObservationTypeText),
                "runtimeSegmentProcessIdColumn" => nameof(ProcessRuntimeSegmentRow.ProcessId),
                _ => null
            };
        }

        public static string NormalizeUsageSortProperty(string? value)
        {
            return value switch
            {
                nameof(UsageSummaryRow.AppName) => value,
                nameof(UsageSummaryRow.CategoryText) => value,
                nameof(UsageSummaryRow.FirstStartedAt) => value,
                nameof(UsageSummaryRow.LastObservedAt) => value,
                nameof(UsageSummaryRow.IdleRecordedMs) => value,
                nameof(UsageSummaryRow.UsageRatio) => value,
                nameof(UsageSummaryRow.SwitchCount) => value,
                _ => nameof(UsageSummaryRow.ActiveUsageMs)
            };
        }

        public static string NormalizeDailyUsageTrendSortProperty(string? value)
        {
            return value switch
            {
                nameof(DailyUsageTrendRow.ActiveUsageMs) => value,
                nameof(DailyUsageTrendRow.TopAppName) => value,
                nameof(DailyUsageTrendRow.TopAppUsageMs) => value,
                _ => nameof(DailyUsageTrendRow.Date)
            };
        }

        public static string NormalizeTimelineSortProperty(string? value)
        {
            return value switch
            {
                nameof(ActivityTimelineRow.ActivityType) => value,
                nameof(ActivityTimelineRow.EndedAt) => value,
                nameof(ActivityTimelineRow.DurationMs) => value,
                nameof(ActivityTimelineRow.DisplayName) => value,
                nameof(ActivityTimelineRow.CategoryText) => value,
                _ => nameof(ActivityTimelineRow.StartedAt)
            };
        }

        public static string NormalizeRuntimeSortProperty(string? value)
        {
            return value switch
            {
                nameof(ProcessRuntimeSummaryRow.AppName) => value,
                nameof(ProcessRuntimeSummaryRow.CategoryText) => value,
                nameof(ProcessRuntimeSummaryRow.TrackingTypeText) => value,
                nameof(ProcessRuntimeSummaryRow.FirstObservedAt) => value,
                nameof(ProcessRuntimeSummaryRow.LastObservedAt) => value,
                nameof(ProcessRuntimeSummaryRow.ActiveUsageMs) => value,
                nameof(ProcessRuntimeSummaryRow.IdleRecordedMs) => value,
                nameof(ProcessRuntimeSummaryRow.ActualUsageRatio) => value,
                nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount) => value,
                nameof(ProcessRuntimeSummaryRow.StatusText) => value,
                _ => nameof(ProcessRuntimeSummaryRow.RuntimeMs)
            };
        }

        public static string NormalizeRuntimeSegmentSortProperty(string? value)
        {
            return value switch
            {
                nameof(ProcessRuntimeSegmentRow.EndedAt) => value,
                nameof(ProcessRuntimeSegmentRow.DurationMs) => value,
                nameof(ProcessRuntimeSegmentRow.IsRunning) => value,
                nameof(ProcessRuntimeSegmentRow.ObservationTypeText) => value,
                nameof(ProcessRuntimeSegmentRow.ProcessId) => value,
                _ => nameof(ProcessRuntimeSegmentRow.StartedAt)
            };
        }
    }
}
