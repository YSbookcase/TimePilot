namespace TimePilot.WinForms.KYS24
{
    internal sealed record AppIdentityObservationRow(
        string DisplayName,
        string ProcessName,
        string? ExecutablePath,
        DateTimeOffset FirstSeenAt,
        DateTimeOffset LastSeenAt,
        int ObservedCount);
}
