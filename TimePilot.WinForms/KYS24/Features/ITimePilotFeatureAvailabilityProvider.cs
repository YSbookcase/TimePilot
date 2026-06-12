namespace TimePilot.WinForms.KYS24.Features
{
    internal interface ITimePilotFeatureAvailabilityProvider
    {
        TimePilotFeatureAvailability GetAvailability(string featureId);
    }
}
