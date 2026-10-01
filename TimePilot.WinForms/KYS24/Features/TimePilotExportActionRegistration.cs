namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotExportActionRegistration(
        string FeatureId,
        string ActionKey,
        string Label,
        int SortOrder = 0);
}
