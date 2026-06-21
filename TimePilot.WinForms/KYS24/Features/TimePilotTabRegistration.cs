namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotTabRegistration(
        string FeatureId,
        string TabKey,
        string Title,
        int SortOrder = 0);
}
