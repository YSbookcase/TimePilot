namespace TimePilot.WinForms.KYS24
{
    internal sealed record ProcessRuntimeSegmentExportRow(
        string AppName,
        string ProcessName,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        long DurationMs,
        bool HasMainWindow,
        bool IsCurrentSessionProcess);
}
