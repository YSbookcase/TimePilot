using System.Diagnostics;
using Microsoft.Data.Sqlite;
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;
using Xunit;

namespace TimePilot.Analytics.ConsumerTests;

public sealed class DailyReaderTests
{
    private static readonly DateTimeOffset CaptureTime = new(2031, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static DateTimeOffset At(int day, int hour = 0) => new(2024, 2, day, hour, 0, 0, TimeSpan.Zero);
    private static DailyQuery Query(int start = 28, int end = 29) =>
        new(new DateOnly(2024, 2, start), new DateOnly(2024, 2, end), TimeZoneInfo.Utc, At(29, 23));

    [Fact]
    public void PublicConsumerReadsAppAndCategoryTotalsWithoutInferringCategoryFromTopApp()
    {
        using var fixture = new DailyFixture();
        fixture.Foreground(1, At(28, 9), At(28, 11));
        fixture.Foreground(2, At(28, 11), At(28, 12));
        fixture.Foreground(3, At(28, 12), At(28, 13));
        fixture.Idle(At(28, 13), At(28, 14));
        fixture.Idle(At(28, 13), At(28, 15));
        fixture.Runtime(At(28, 8), At(28, 16));
        fixture.Runtime(At(28, 9), At(28, 10));
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        var snapshot = reader.Read(Query());
        Assert.Equal("2.0.0", snapshot.ContractVersion);
        var day = Assert.Single(snapshot.Days);
        Assert.Equal(4 * 3_600_000, day.ActiveMilliseconds);
        Assert.Equal(2 * 3_600_000, day.IdleMilliseconds);
        Assert.Equal("My editor", day.TopApp!.DisplayName);
        Assert.Equal(3, day.Categories.Count);
        Assert.Equal(3_600_000, day.Categories.Single(c => c.CategoryId == 20).ActiveMilliseconds);
        Assert.Equal(3_600_000, day.Categories.Single(c => c.CategoryId is null).ActiveMilliseconds);
        Assert.Equal(8 * 3_600_000, day.RecorderCalendarCoverage.RecorderRunningMilliseconds);
        Assert.Equal(16 * 3_600_000, day.RecorderCalendarCoverage.CalendarGapMilliseconds);
    }

    [Fact]
    public void RepeatedReadAndDisposeLeaveDatabaseSettingsSchemaAndFileListUnchanged()
    {
        using var fixture = new DailyFixture();
        fixture.Foreground(1, At(28, 9), At(28, 10));
        var logical = fixture.LogicalContents();
        var before = fixture.Files();
        var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        reader.Read(Query());
        reader.Read(Query());
        Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
        reader.Dispose();
        reader.Dispose();
        Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
        Assert.Equal(logical, fixture.LogicalContents());
        Assert.Throws<ObjectDisposedException>(() => reader.Read(Query()));
    }

    [Fact]
    public void SourceLeaseRejectsWriterAndReleasesAfterDispose()
    {
        using var fixture = new DailyFixture();
        using (var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime))
        {
            Assert.Throws<IOException>(() => new FileStream(fixture.DatabasePath, FileMode.Open, FileAccess.Write).Dispose());
            reader.Read(Query());
        }
        fixture.Execute("INSERT INTO app_categories VALUES (30, 'After reader');");
    }

    [Fact]
    public void ExistingWriterIsRejectedWithoutMutatingItsSource()
    {
        using var fixture = new DailyFixture();
        using var writer = fixture.Connect();
        var before = fixture.Files();
        Assert.Equal(AnalyticsError.SourceUnavailable,
            Assert.Throws<AnalyticsReadException>(() => SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime)).Error);
        Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
    }

    [Theory]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData("-journal")]
    public void SidecarsAreRejectedBeforeSQLiteCanTouchThem(string suffix)
    {
        using var fixture = new DailyFixture();
        File.WriteAllText(fixture.DatabasePath + suffix, "synthetic sidecar");
        var before = fixture.Files();
        Assert.Equal(AnalyticsError.UnsupportedJournal,
            Assert.Throws<AnalyticsReadException>(() => SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime)).Error);
        Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
    }

    [Fact]
    public void ClosedWalDatabaseWithoutSidecarsIsStillRejected()
    {
        using var fixture = new DailyFixture();
        fixture.Execute("PRAGMA journal_mode=WAL;");
        var before = fixture.Files();
        Assert.Equal(AnalyticsError.UnsupportedJournal,
            Assert.Throws<AnalyticsReadException>(() => SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime)).Error);
        Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
    }

    [Fact]
    public void MissingPathDoesNotCreateFoldersOrDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), "TimePilotMissing-" + Guid.NewGuid(), "missing.db");
        Assert.Equal(AnalyticsError.SourceUnavailable,
            Assert.Throws<AnalyticsReadException>(() => SqliteDailyAnalyticsReader.Open(path, CaptureTime)).Error);
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)));
        Assert.Throws<ArgumentException>(() => SqliteDailyAnalyticsReader.Open("relative.db", CaptureTime));
    }

    [Theory]
    [InlineData("PRAGMA user_version=999;")]
    [InlineData("DROP TABLE idle_sessions;")]
    [InlineData("ALTER TABLE apps RENAME COLUMN user_alias TO unknown;")]
    public void UnsupportedSchemaIsNotMigrated(string change)
    {
        using var fixture = new DailyFixture();
        fixture.Execute(change);
        var before = fixture.Files();
        Assert.Equal(AnalyticsError.UnsupportedSchema,
            Assert.Throws<AnalyticsReadException>(() => SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime)).Error);
        Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
    }

    [Fact]
    public void CorruptionAndMalformedIntervalsDoNotFallBack()
    {
        using var fixture = new DailyFixture();
        fixture.Execute("INSERT INTO idle_sessions(started_at,ended_at) VALUES ('bad', 'also bad');");
        using (var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime))
            Assert.Equal(AnalyticsError.InvalidData, Assert.Throws<AnalyticsReadException>(() => reader.Read(Query())).Error);
        File.WriteAllText(fixture.DatabasePath, "corrupt database");
        Assert.Equal(AnalyticsError.InvalidData,
            Assert.Throws<AnalyticsReadException>(() => SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime)).Error);
    }

    [Fact]
    public void LeapDayMidnightAndObservationClipAreExplicit()
    {
        using var fixture = new DailyFixture();
        fixture.Foreground(1, At(28, 23), null, At(29, 4));
        fixture.Idle(At(29), null);
        fixture.Runtime(At(28, 23), null, At(29, 4));
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        var result = reader.Read(new DailyQuery(new(2024, 2, 28), new(2024, 3, 1), TimeZoneInfo.Utc, At(29, 2)));
        Assert.Equal(2, result.Days.Count);
        Assert.Equal(3_600_000, result.Days[0].ActiveMilliseconds);
        Assert.Equal(7_200_000, result.Days[1].ActiveMilliseconds);
        Assert.Equal(7_200_000, result.Days[1].IdleMilliseconds);
        Assert.Equal(7_200_000, result.Days[1].RecorderCalendarCoverage.ElapsedCalendarMilliseconds);
    }

    [Theory]
    [InlineData(2024, 3, 10, 23)]
    [InlineData(2024, 11, 3, 25)]
    public void DayLengthUsesRequestedTimezoneAcrossDst(int year, int month, int day, int hours)
    {
        using var fixture = new DailyFixture();
        var date = new DateOnly(year, month, day);
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        var result = reader.Read(new DailyQuery(date, date.AddDays(1),
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        var activity = Assert.Single(result.Days);
        Assert.Equal(hours * 3_600_000L, activity.RecorderCalendarCoverage.ElapsedCalendarMilliseconds);
        Assert.Equal(hours, (activity.EndsAt - activity.StartsAt).TotalHours);
        Assert.Null(activity.TopApp);
    }

    [Fact]
    public void EmptyFutureInvalidRangeAndCancellationArePredictable()
    {
        using var fixture = new DailyFixture();
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        var futureCoverage = reader.Read(Query() with { ObservedAt = At(27) }).Days[0].RecorderCalendarCoverage;
        Assert.Equal(0, futureCoverage.ElapsedCalendarMilliseconds);
        Assert.Equal(0, futureCoverage.RecorderRunningMilliseconds);
        Assert.Equal(0, futureCoverage.CalendarGapMilliseconds);
        Assert.Equal(0, futureCoverage.RecorderToCalendarRatio);
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(Query() with { EndDateExclusive = new(2024, 2, 27) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(Query() with { EndDateExclusive = new(2026, 2, 27) }));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => reader.Read(Query(), cancellation.Token));
    }

    [Fact]
    public void ExplicitTimezoneSplitsAcrossYearAndPreservesDistinctAppIdentities()
    {
        using var fixture = new DailyFixture();
        fixture.Execute("UPDATE apps SET user_alias='Same name' WHERE id IN (1,2);");
        var start = new DateTimeOffset(2024, 12, 31, 14, 0, 0, TimeSpan.Zero);
        fixture.Foreground(1, start, start.AddHours(2));
        fixture.Foreground(2, start, start.AddHours(1));
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        var query = new DailyQuery(new(2024, 12, 31), new(2025, 1, 2),
            TimeZoneInfo.CreateCustomTimeZone("UTC+09", TimeSpan.FromHours(9), "UTC+09", "UTC+09"), start.AddDays(2));
        var days = reader.Read(query).Days;
        Assert.Equal(7_200_000, days[0].ActiveMilliseconds);
        Assert.Equal(3_600_000, days[1].ActiveMilliseconds);
        Assert.Equal(2, days[0].Apps.Count);
        Assert.Equal(1, days[0].TopApp!.AppId);
    }

    [Fact]
    public void RowLimitFailsExplicitlyRatherThanReturningPartialResults()
    {
        using var fixture = new DailyFixture();
        fixture.Execute($"""
            WITH RECURSIVE n(x) AS (VALUES(0) UNION ALL SELECT x+1 FROM n WHERE x<{SqliteDailyAnalyticsReader.MaximumRowsPerQuery})
            INSERT INTO idle_sessions(started_at,ended_at)
            SELECT '2024-02-28T09:00:00.0000000+00:00', '2024-02-28T10:00:00.0000000+00:00' FROM n;
            """);
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        Assert.Equal(AnalyticsError.LimitExceeded,
            Assert.Throws<AnalyticsReadException>(() => reader.Read(Query())).Error);
    }

    [Fact]
    public void SyntheticYearQueryHasBoundedPerformance()
    {
        using var fixture = new DailyFixture();
        fixture.Execute("""
            WITH RECURSIVE n(x) AS (VALUES(0) UNION ALL SELECT x+1 FROM n WHERE x<9999)
            INSERT INTO foreground_sessions(app_id,started_at,ended_at)
            SELECT 1, strftime('%Y-%m-%dT%H:%M:%f', '2024-01-01', '+' || (x*30) || ' minutes') || '0000+00:00',
                      strftime('%Y-%m-%dT%H:%M:%f', '2024-01-01', '+' || (x*30+10) || ' minutes') || '0000+00:00' FROM n;
            """);
        using var reader = SqliteDailyAnalyticsReader.Open(fixture.DatabasePath, CaptureTime);
        var watch = Stopwatch.StartNew();
        var result = reader.Read(new(new(2024, 1, 1), new(2025, 1, 1), TimeZoneInfo.Utc, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal(366, result.Days.Count);
        Assert.Equal(10_000L * 600_000, result.Days.Sum(day => day.ActiveMilliseconds));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15), $"10k synthetic sessions took {watch.Elapsed}.");
    }
}
