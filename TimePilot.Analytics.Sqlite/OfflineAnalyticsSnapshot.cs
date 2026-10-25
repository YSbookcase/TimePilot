using System.Security.Cryptography;
using System.Text.Json;
using TimePilot.Analytics.Contracts;

namespace TimePilot.Analytics.Sqlite;

public sealed record PreparedAnalyticsSnapshot(string DirectoryPath, DateTimeOffset SourceCapturedAt, string DatabaseSha256);

/// <summary>Explicit snapshot preparation. Never called implicitly by the daily reader.</summary>
public static class OfflineAnalyticsSnapshot
{
    public const string DatabaseFileName = "analytics.db";
    public const string MetadataFileName = "snapshot.json";
    public const int FormatVersion = 1;

    /// <summary>
    /// Copies a quiescent rollback-journal source under a Windows write-exclusion lease.
    /// Destination must not exist, must be outside the source folder, and its parent must already exist.
    /// The source DB, settings and sidecars are never modified or checkpointed.
    /// </summary>
    public static PreparedAnalyticsSnapshot Create(string sourceDatabasePath, string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceDatabasePath) || !Path.IsPathFullyQualified(sourceDatabasePath))
            throw new ArgumentException("An explicit absolute source database path is required.", nameof(sourceDatabasePath));
        if (string.IsNullOrWhiteSpace(destinationDirectory) || !Path.IsPathFullyQualified(destinationDirectory))
            throw new ArgumentException("An explicit absolute new snapshot directory is required.", nameof(destinationDirectory));

        var source = Path.GetFullPath(sourceDatabasePath);
        var destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationDirectory));
        var parent = Path.GetDirectoryName(destination);
        var sourceDirectory = Path.GetDirectoryName(source)!;
        var sourcePrefix = Path.EndsInDirectorySeparator(sourceDirectory)
            ? sourceDirectory : sourceDirectory + Path.DirectorySeparatorChar;
        if (parent is null || !Directory.Exists(parent))
            throw new ArgumentException("The destination parent directory must already exist.", nameof(destinationDirectory));
        if (destination.Equals(sourceDirectory, StringComparison.OrdinalIgnoreCase)
            || destination.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The snapshot must be outside the source data folder.", nameof(destinationDirectory));
        RejectReparsePoints(source);
        RejectReparsePoints(parent);
        if (Path.Exists(destination))
            throw new IOException("The destination already exists. Snapshots are never overwritten.");
        cancellationToken.ThrowIfCancellationRequested();

        using var sourceGuard = SqliteDailyAnalyticsReader.OpenForPreparation(source);
        // The source cannot change after the lease is acquired; this is not the copy-completion time.
        var capturedAt = DateTimeOffset.UtcNow;
        var staging = Path.Combine(parent, $".analytics-preparing-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(staging);
            var copiedPath = Path.Combine(staging, DatabaseFileName);
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(copiedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, read);
                }
                output.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            string hash;
            using (var copy = File.OpenRead(copiedPath))
                hash = Convert.ToHexString(SHA256.HashData(copy));
            var metadata = new SnapshotMetadata(FormatVersion, AnalyticsContract.Version,
                AnalyticsContract.SchemaProfile, capturedAt, hash);
            File.WriteAllText(Path.Combine(staging, MetadataFileName), JsonSerializer.Serialize(metadata));
            using (var verified = Open(staging))
            {
                if (verified.SourceCapturedAt != capturedAt)
                    throw new AnalyticsReadException(AnalyticsError.InvalidData, "Snapshot capture metadata verification failed.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            // Publish a complete directory in one move. A racing existing destination is not overwritten.
            Directory.Move(staging, destination);
            return new PreparedAnalyticsSnapshot(destination, capturedAt, hash);
        }
        finally
        {
            // Only remove the two known files in our own staging folder, never recurse or touch the source.
            if (Path.GetDirectoryName(Path.GetFullPath(staging)) != parent)
                throw new InvalidOperationException("Invalid snapshot staging path.");
            try
            {
                if (Directory.Exists(staging))
                {
                    File.Delete(Path.Combine(staging, MetadataFileName));
                    File.Delete(Path.Combine(staging, DatabaseFileName));
                    Directory.Delete(staging);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A failed cleanup may leave an unpublished staging directory; never delete other files.
            }
        }
    }

    /// <summary>Open a prepared artifact, verifying its metadata and DB hash before returning a reader.</summary>
    public static IDailyAnalyticsReader Open(string snapshotDirectory)
    {
        if (string.IsNullOrWhiteSpace(snapshotDirectory) || !Path.IsPathFullyQualified(snapshotDirectory))
            throw new ArgumentException("An explicit absolute snapshot directory is required.", nameof(snapshotDirectory));
        var directory = Path.GetFullPath(snapshotDirectory);
        SnapshotMetadata metadata;
        try
        {
            var metadataPath = Path.Combine(directory, MetadataFileName);
            if (new FileInfo(metadataPath).Length > 16_384)
                throw new AnalyticsReadException(AnalyticsError.InvalidData, "Snapshot metadata exceeds the size limit.");
            metadata = JsonSerializer.Deserialize<SnapshotMetadata>(File.ReadAllText(metadataPath))
                ?? throw new AnalyticsReadException(AnalyticsError.InvalidData, "Snapshot metadata is missing.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new AnalyticsReadException(AnalyticsError.InvalidData, "Snapshot metadata cannot be read. No fallback is used.", ex);
        }
        if (metadata.FormatVersion != FormatVersion || metadata.ContractVersion != AnalyticsContract.Version
            || metadata.SchemaProfile != AnalyticsContract.SchemaProfile)
            throw new AnalyticsReadException(AnalyticsError.UnsupportedSchema, "The snapshot metadata version is not supported.");
        if (metadata.SourceCapturedAt == default || metadata.SourceCapturedAt == DateTimeOffset.MaxValue
            || metadata.DatabaseSha256 is null || metadata.DatabaseSha256.Length != 64
            || !metadata.DatabaseSha256.All(Uri.IsHexDigit))
            throw new AnalyticsReadException(AnalyticsError.InvalidData, "Snapshot capture time or hash is invalid.");

        var path = Path.Combine(directory, DatabaseFileName);
        var reader = SqliteDailyAnalyticsReader.Open(path, metadata.SourceCapturedAt);
        try
        {
            using var database = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hash = Convert.ToHexString(SHA256.HashData(database));
            if (!hash.Equals(metadata.DatabaseSha256, StringComparison.OrdinalIgnoreCase))
                throw new AnalyticsReadException(AnalyticsError.InvalidData, "Snapshot DB hash does not match its metadata.");
            return reader;
        }
        catch
        {
            reader.Dispose();
            throw;
        }
    }

    private static void RejectReparsePoints(string path)
    {
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if (Path.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Snapshot paths must not contain symbolic links or junctions.");
        }
    }

    private sealed record SnapshotMetadata(int FormatVersion, string ContractVersion, string SchemaProfile,
        DateTimeOffset SourceCapturedAt, string DatabaseSha256);
}
