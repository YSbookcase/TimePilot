namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotAnalyticsPanelRegistration(
        string FeatureId,
        string PanelKey,
        string Title,
        int SortOrder = 0);
}
