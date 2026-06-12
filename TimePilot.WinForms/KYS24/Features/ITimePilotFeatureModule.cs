namespace TimePilot.WinForms.KYS24.Features
{
    internal interface ITimePilotFeatureModule
    {
        string Id { get; }

        void Register(TimePilotFeatureRegistry registry);
    }
}
