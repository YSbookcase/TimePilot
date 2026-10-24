namespace TimePilot.Analytics.Contracts;

public static class AnalyticsContract
{
    public const string Version = "1.0.0";
    public const string SchemaProfile = "community-daily-v1";
    public const int MaximumDays = 366;
}

// EndDateExclusive is a calendar date in TimeZone, not a fixed 24-hour offset.
public sealed record DailyQuery(
    DateOnly StartDate,
    DateOnly EndDateExclusive,
    TimeZoneInfo TimeZone,
    DateTimeOffset ObservedAt);

public interface IDailyAnalyticsReader : IDisposable
{
    DailySnapshot Read(DailyQuery query, CancellationToken cancellationToken = default);
}

public sealed record DailySnapshot(
    string ContractVersion,
    string SchemaProfile,
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
    RecordingCoverage Coverage);

public sealed record AppActivity(long AppId, string DisplayName, long? CategoryId, long ActiveMilliseconds);
public sealed record CategoryActivity(long? CategoryId, string? Name, long ActiveMilliseconds);

// Calendar coverage only: gaps are unknown, not necessarily OS downtime or inactivity.
public sealed record RecordingCoverage(long ObservedMilliseconds, long RecordedMilliseconds)
{
    public long UnobservedMilliseconds => Math.Max(0, ObservedMilliseconds - RecordedMilliseconds);
    public double Ratio => ObservedMilliseconds == 0 ? 0 : (double)RecordedMilliseconds / ObservedMilliseconds;
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
