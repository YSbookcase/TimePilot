namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotMenuRegistration(
        string FeatureId,
        string MenuPath,
        string Label,
        int SortOrder = 0);
}
