namespace TimePilot.WinForms.KYS24.Features
{
    internal sealed record TimePilotFeatureRegistrationAvailability<TRegistration>(
        TRegistration Registration,
        TimePilotFeatureAvailability Availability);
}
