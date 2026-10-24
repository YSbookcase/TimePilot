namespace TimePilot.Analytics.Contracts;

public static class AnalyticsContract
{
    public const string Version = "2.0.0";
    public const string SchemaProfile = "community-daily-v1";
    public const int MaximumDays = 366;
}

// EndDateExclusive is a calendar date in TimeZone, not a fixed 24-hour offset.
// ObservedAt is an evaluation cutoff, never later than the source snapshot capture time.
public sealed record DailyQuery(
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    TimeZoneInfo TimeZone,
    DateTimeOffset ObservedAt);

public interface IDailyAnalyticsReader : IDisposable
{
    DateTimeOffset SourceCapturedAt { get; }
    DailySnapshot Read(DailyQuery query, CancellationToken cancellationToken = default);
}

public sealed record DailySnapshot(
    string ContractVersion,
    string SchemaProfile,
    DateTimeOffset SourceCapturedAt,
    DailyQuery Query,
    IReadOnlyList<DailyActivity> Days);

public sealed record DailyActivity(
    DateOnly Date,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    long ActiveMilliseconds,
    long IdleMilliseconds,
    AppActivity? TopApp,
    IReadOnlyList<AppActivity> Apps,
    IReadOnlyList<CategoryActivity> Categories,
    RecorderCalendarCoverage RecorderCalendarCoverage);

public sealed record AppActivity(long AppId, string DisplayName, long? CategoryId, long ActiveMilliseconds);
public sealed record CategoryActivity(long? CategoryId, string? Name, long ActiveMilliseconds);

/// <summary>
/// Recorder runtime divided by elapsed calendar time, NOT the Community UI's Windows-recordable coverage.
/// No sleep, lock or OS-off exclusions are applied. Calendar gaps are unknown, not evidence of inactivity.
/// </summary>
public sealed record RecorderCalendarCoverage(long ElapsedCalendarMilliseconds, long RecorderRunningMilliseconds)
{
    public long CalendarGapMilliseconds => Math.Max(0, ElapsedCalendarMilliseconds - RecorderRunningMilliseconds);
    public double RecorderToCalendarRatio => ElapsedCalendarMilliseconds == 0 ? 0 : (double)RecorderRunningMilliseconds / ElapsedCalendarMilliseconds;
}

public enum AnalyticsError
{
    SourceUnavailable,
    UnsupportedSchema,
    UnsupportedJournal,
    InvalidData,
    LimitExceeded
}

public sealed class AnalyticsReadException : Exception
{
    public AnalyticsError Error { get; }

    public AnalyticsReadException(AnalyticsError error, string message, Exception? innerException = null)
        : base(message, innerException) => Error = error;
}
