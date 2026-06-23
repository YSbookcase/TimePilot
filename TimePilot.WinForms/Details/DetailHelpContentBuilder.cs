using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Details
{
    internal static class DetailHelpContentBuilder
    {
        public static string BuildMessage(DetailRuntimeFilter selectedFilter)
        {
            return UiText.Main.DetailHelpCurrentSelection(
                    GetFilterText(selectedFilter),
                    GetFilterDescription(selectedFilter))
                + Environment.NewLine
                + Environment.NewLine
                + UiText.Main.DetailHelpMessage;
        }

        public static string GetFilterText(DetailRuntimeFilter filter)
        {
            return filter switch
            {
                DetailRuntimeFilter.CurrentTrackingScope => UiText.Main.DetailFilterCurrentScope,
                DetailRuntimeFilter.VisibleApps => UiText.Main.DetailFilterVisibleApps,
                DetailRuntimeFilter.UserProcesses => UiText.Main.DetailFilterUserProcesses,
                DetailRuntimeFilter.AllRecords => UiText.Main.DetailFilterAllRecords,
                _ => UiText.Main.DetailFilterSummaryApps
            };
        }

        public static string GetFilterDescription(DetailRuntimeFilter filter)
        {
            return filter switch
            {
                DetailRuntimeFilter.CurrentTrackingScope => UiText.Main.DetailFilterCurrentScopeDescription,
                DetailRuntimeFilter.VisibleApps => UiText.Main.DetailFilterVisibleAppsDescription,
                DetailRuntimeFilter.UserProcesses => UiText.Main.DetailFilterUserProcessesDescription,
                DetailRuntimeFilter.AllRecords => UiText.Main.DetailFilterAllRecordsDescription,
                _ => UiText.Main.DetailFilterSummaryAppsDescription
            };
        }
    }
}
