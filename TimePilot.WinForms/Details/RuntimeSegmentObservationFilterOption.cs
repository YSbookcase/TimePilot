using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Details
{
    internal sealed record RuntimeSegmentObservationFilterOption(string Label, RuntimeSegmentObservationFilter Value)
    {
        public override string ToString() => Label;

        public static IReadOnlyList<RuntimeSegmentObservationFilterOption> GetOptions()
        {
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return
            [
                new(isEnglish ? "All basis" : "전체 기준", RuntimeSegmentObservationFilter.All),
                new(UiText.Main.WindowedApp, RuntimeSegmentObservationFilter.VisibleApps),
                new(UiText.Main.UserProcess, RuntimeSegmentObservationFilter.UserProcesses),
                new(UiText.Main.AllProcesses, RuntimeSegmentObservationFilter.AllProcesses)
            ];
        }
    }
}
