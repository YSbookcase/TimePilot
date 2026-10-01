namespace TimePilot.WinForms.KYS24
{
    internal sealed record SystemEventDiagnostic(
        DateTimeOffset OccurredAt,
        string EventType,
        string? Details);
}
