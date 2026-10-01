namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotSettingsSectionRegistration(
        string FeatureId,
        string SectionKey,
        string Title,
        int SortOrder = 0);
}
