using System.Text.Json.Nodes;
using TimePilot.Analytics.Contracts;
using TimePilot.Analytics.Sqlite;
using Xunit;

namespace TimePilot.Analytics.ConsumerTests;

public sealed class OfflineSnapshotTests
{
    [Fact]
    public void CreateThenPublicReadPreservesSourceAndTransfersCaptureTime()
    {
        WithDestination((fixture, destination) =>
        {
            var start = new DateTimeOffset(2024, 2, 28, 9, 0, 0, TimeSpan.Zero);
            fixture.Foreground(1, start, start.AddHours(2));
            fixture.Runtime(start, start.AddHours(3));
            fixture.Idle(start.AddHours(2), null);
            var sourceFiles = fixture.Files();
            var logicalSource = fixture.LogicalContents();
            var before = DateTimeOffset.UtcNow;
            var prepared = OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination);
            Assert.InRange(prepared.SourceCapturedAt, before, DateTimeOffset.UtcNow);
            Assert.Equal(destination, prepared.DirectoryPath);
            Assert.Equal(2, Directory.GetFiles(destination).Length);
            var metadata = File.ReadAllText(Path.Combine(destination, OfflineAnalyticsSnapshot.MetadataFileName));
            Assert.DoesNotContain(fixture.Root, metadata);
            using (var reader = OfflineAnalyticsSnapshot.Open(destination))
            {
                Assert.Equal(prepared.SourceCapturedAt, reader.SourceCapturedAt);
                var snapshot = reader.Read(new(new(2024, 2, 28), new(2024, 2, 29), TimeZoneInfo.Utc, start.AddHours(4)));
                Assert.Equal(prepared.SourceCapturedAt, snapshot.SourceCapturedAt);
                var day = Assert.Single(snapshot.Days);
                Assert.Equal(7_200_000, day.ActiveMilliseconds);
                Assert.Equal(7_200_000, day.IdleMilliseconds);
                Assert.Equal(3 * 3_600_000, day.RecorderCalendarCoverage.RecorderRunningMilliseconds);
                Assert.Equal(13 * 3_600_000, day.RecorderCalendarCoverage.ElapsedCalendarMilliseconds);
                Assert.Equal(3.0 / 13, day.RecorderCalendarCoverage.RecorderToCalendarRatio);
            }
            Assert.Equal(sourceFiles.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
            Assert.Equal(logicalSource, fixture.LogicalContents());
            Assert.Equal(prepared.DatabaseSha256,
                fixture.Files()[Path.GetFileName(fixture.DatabasePath)]);
        });
    }

    [Fact]
    public void PersistedCaptureTimePreventsOngoingIdleFromGrowingWhenSnapshotIsReopened()
    {
        WithDestination((fixture, destination) =>
        {
            var start = DateTimeOffset.UtcNow.AddMinutes(-10);
            fixture.Idle(start, null);
            var prepared = OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination);
            var query = new DailyQuery(DateOnly.FromDateTime(start.UtcDateTime),
                DateOnly.FromDateTime(prepared.SourceCapturedAt.UtcDateTime).AddDays(1), TimeZoneInfo.Utc,
                prepared.SourceCapturedAt);
            long idle;
            using (var reader = OfflineAnalyticsSnapshot.Open(destination))
            {
                idle = reader.Read(query).Days.Sum(day => day.IdleMilliseconds);
                Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(query with { ObservedAt = prepared.SourceCapturedAt.AddTicks(1) }));
                Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(query with { ObservedAt = prepared.SourceCapturedAt.AddDays(1) }));
                Assert.True(reader.Read(query with { ObservedAt = start }).Days.All(day => day.IdleMilliseconds == 0));
            }
            using var reopened = OfflineAnalyticsSnapshot.Open(destination);
            Assert.Equal(prepared.SourceCapturedAt, reopened.SourceCapturedAt);
            Assert.Equal(idle, reopened.Read(query).Days.Sum(day => day.IdleMilliseconds));
            Assert.InRange((long)(prepared.SourceCapturedAt - start).TotalMilliseconds - idle, 0, 1);
        });
    }

    [Fact]
    public void SourceCanChangeAfterPreparationWithoutChangingTheSnapshot()
    {
        WithDestination((fixture, destination) =>
        {
            OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination);
            var start = new DateTimeOffset(2024, 2, 28, 9, 0, 0, TimeSpan.Zero);
            fixture.Foreground(1, start, start.AddHours(1));
            using var reader = OfflineAnalyticsSnapshot.Open(destination);
            Assert.Equal(0, Assert.Single(reader.Read(new(new(2024, 2, 28), new(2024, 2, 29), TimeZoneInfo.Utc, start.AddDays(1))).Days).ActiveMilliseconds);
        });
    }

    [Fact]
    public void ExistingDestinationAndSourceFolderAreNeverOverwritten()
    {
        WithDestination((fixture, destination) =>
        {
            Directory.CreateDirectory(destination);
            var sentinel = Path.Combine(destination, "keep.txt");
            File.WriteAllText(sentinel, "keep");
            Assert.Throws<IOException>(() => OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination));
            Assert.Equal("keep", File.ReadAllText(sentinel));
            Assert.Throws<ArgumentException>(() => OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, Path.Combine(fixture.Root, "snapshot")));
            Assert.False(Directory.Exists(Path.Combine(fixture.Root, "snapshot")));
        });
    }

    [Theory]
    [InlineData("wal")]
    [InlineData("writer")]
    [InlineData("canceled")]
    [InlineData("schema")]
    public void FailedPreparationPublishesNothingAndLeavesSourceUnchanged(string scenario)
    {
        WithDestination((fixture, destination) =>
        {
            if (scenario == "wal")
                fixture.Execute("PRAGMA journal_mode=WAL;");
            if (scenario == "schema")
                fixture.Execute("PRAGMA user_version=999;");
            using var writer = scenario == "writer" ? fixture.Connect() : null;
            var before = fixture.Files();
            var token = scenario == "canceled" ? new CancellationToken(canceled: true) : default;
            if (scenario == "canceled")
                Assert.Throws<OperationCanceledException>(() => OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination, token));
            else
                Assert.Throws<AnalyticsReadException>(() => OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination, token));
            Assert.False(Directory.Exists(destination));
            Assert.Empty(Directory.GetFileSystemEntries(Path.GetDirectoryName(destination)!));
            Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
        });
    }

    [Theory]
    [InlineData("hash")]
    [InlineData("time")]
    [InlineData("version")]
    [InlineData("missing")]
    [InlineData("database")]
    [InlineData("json")]
    [InlineData("max-time")]
    public void InvalidSnapshotMetadataFailsWithoutFallbackOrSourceChanges(string change)
    {
        WithDestination((fixture, destination) =>
        {
            OfflineAnalyticsSnapshot.Create(fixture.DatabasePath, destination);
            var path = Path.Combine(destination, OfflineAnalyticsSnapshot.MetadataFileName);
            var metadata = JsonNode.Parse(File.ReadAllText(path))!;
            if (change == "missing")
                File.Delete(path);
            else if (change == "json")
                File.WriteAllText(path, "not JSON");
            else
            {
                if (change == "hash") metadata["DatabaseSha256"] = new string('0', 64);
                if (change == "time") metadata["SourceCapturedAt"] = default(DateTimeOffset);
                if (change == "max-time") metadata["SourceCapturedAt"] = DateTimeOffset.MaxValue;
                if (change == "version") metadata["ContractVersion"] = "999.0.0";
                File.WriteAllText(path, metadata.ToJsonString());
                if (change == "database")
                    File.AppendAllText(Path.Combine(destination, OfflineAnalyticsSnapshot.DatabaseFileName), "mismatched snapshot bytes");
            }
            var before = fixture.Files();
            Assert.Throws<AnalyticsReadException>(() => OfflineAnalyticsSnapshot.Open(destination));
            Assert.Equal(before.OrderBy(p => p.Key), fixture.Files().OrderBy(p => p.Key));
            // Failed validation must release the snapshot lease too.
            using var writable = new FileStream(Path.Combine(destination, OfflineAnalyticsSnapshot.DatabaseFileName), FileMode.Open, FileAccess.Write);
        });
    }

    private static void WithDestination(Action<DailyFixture, string> test)
    {
        using var fixture = new DailyFixture();
        var destinationParent = Path.Combine(Path.GetTempPath(), $"TimePilotSnapshotOutput-{Guid.NewGuid():N}");
        Directory.CreateDirectory(destinationParent);
        try
        {
            test(fixture, Path.Combine(destinationParent, "snapshot"));
        }
        finally
        {
            Directory.Delete(destinationParent, recursive: true);
        }
    }
}
