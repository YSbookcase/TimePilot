namespace TimePilot.WinForms.KYS24
{
    internal sealed record AppRuntimeSessionDiagnostic(
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        DateTimeOffset? LastHeartbeatAt,
        long? DurationMs,
        string? ShutdownReason,
        DateTimeOffset? SystemBootedAt,
        string? AppVersion);
}
