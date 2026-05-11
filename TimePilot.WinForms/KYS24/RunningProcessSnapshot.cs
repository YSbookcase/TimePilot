namespace TimePilot.WinForms.KYS24
{
    internal sealed record RunningProcessSnapshot(
        int ProcessId,
        AppMetadata App);
}
