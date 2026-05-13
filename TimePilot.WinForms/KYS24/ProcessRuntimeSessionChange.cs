namespace TimePilot.WinForms.KYS24
{
    internal sealed record ProcessRuntimeSessionStart(
        int ProcessId,
        AppMetadata App,
        ProcessRuntimeTrackingScope TrackingScope,
        bool HasMainWindow,
        bool IsCurrentSessionProcess);

    internal sealed record ProcessRuntimeSessionUpdate(
        long SessionId,
        AppMetadata App,
        ProcessRuntimeTrackingScope TrackingScope,
        bool HasMainWindow,
        bool IsCurrentSessionProcess);

    internal sealed record ProcessRuntimeSessionStartResult(
        int ProcessId,
        long SessionId,
        string ProcessName);
}
