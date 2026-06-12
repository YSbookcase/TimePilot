namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotFeatureAvailability(
        TimePilotFeatureDefinition Feature,
        bool IsAvailable,
        string? UnavailableReason);
}
