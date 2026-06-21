using TimePilot.WinForms.KYS24.Features;

namespace TimePilot.WinForms.Menus
{
    internal sealed class TimePilotFeatureMenuRequestedEventArgs : EventArgs
    {
        public TimePilotFeatureMenuRequestedEventArgs(TimePilotMenuRegistration registration)
        {
            Registration = registration;
        }

        public TimePilotMenuRegistration Registration { get; }
    }
}
