using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Tables
{
    internal static class GridHeaderTooltipResolver
    {
        public static string? GetTooltipText(string gridName, string columnName)
        {
            return (gridName, columnName) switch
            {
                ("usageGrid", "usageRatioColumn") => UiText.Main.UsageRatioTooltip,
                ("usageGrid", "idleRecordedTimeColumn") => UiText.Main.IdleRecordedTimeTooltip,
                ("runtimeGrid", "runtimeLastObservedAtColumn") => UiText.Main.RuntimeLastObservedTooltip,
                ("runtimeGrid", "runtimeDurationColumn") => UiText.Main.RuntimeDurationTooltip,
                ("runtimeGrid", "runtimeIdleRecordedColumn") => UiText.Main.IdleRecordedTimeTooltip,
                ("runtimeGrid", "runtimeActualUsageRatioColumn") => UiText.Main.RuntimeActualUsageRatioTooltip,
                ("runtimeGrid", "runtimeSessionCountColumn") => UiText.Main.RuntimeSegmentCountTooltip,
                ("runtimeGrid", "runtimeStatusColumn") => UiText.Main.RuntimeStatusTooltip,
                _ => null
            };
        }
    }
}
