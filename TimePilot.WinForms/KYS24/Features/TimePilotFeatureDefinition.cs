namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotFeatureDefinition(
        string Id,
        string Name,
        TimePilotEdition Edition,
        string Description);
}
