using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Details
{
    internal sealed record DetailRuntimeFilterOption(string Label, DetailRuntimeFilter Value)
    {
        public override string ToString() => Label;

        public static IReadOnlyList<DetailRuntimeFilterOption> GetOptions()
        {
            return
            [
                new(UiText.Main.DetailFilterSummaryApps, DetailRuntimeFilter.SummaryApps),
                new(UiText.Main.DetailFilterCurrentScope, DetailRuntimeFilter.CurrentTrackingScope),
                new(UiText.Main.DetailFilterVisibleApps, DetailRuntimeFilter.VisibleApps),
                new(UiText.Main.DetailFilterUserProcesses, DetailRuntimeFilter.UserProcesses),
                new(UiText.Main.DetailFilterAllRecords, DetailRuntimeFilter.AllRecords)
            ];
        }
    }
}
