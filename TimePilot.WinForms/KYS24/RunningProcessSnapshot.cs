namespace TimePilot.WinForms.KYS24
{
    internal sealed record RunningProcessSnapshot(
        int ProcessId,
        AppMetadata App,
        bool HasMainWindow,
        bool IsCurrentSessionProcess);
}
