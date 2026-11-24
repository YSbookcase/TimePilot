using System.Diagnostics;
using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class DataBackupLockTests
{
    [Fact]
    public void TransientExclusiveWriterIsRetriedAndSnapshotContainsCommittedData()
    {
        using var fixture = new RemovalFixture();
        var path = Path.Combine(fixture.Data, "timepilot.db");
        using var source = Open(path, readOnly: true);
        using var destination = Open(Path.Combine(fixture.External, "snapshot.db"));
        using var writer = Open(path);
        Execute(writer, "BEGIN EXCLUSIVE; UPDATE apps SET name='Committed after retry' WHERE id=1;");
        var released = false;
        var retries = 0;
        try
        {
            DataBackupService.BackupDatabaseWithRetry(source, destination, TimeSpan.FromSeconds(2), () =>
            {
                retries++;
                Execute(writer, "COMMIT;");
                released = true;
            });
        }
        finally { if (!released) Execute(writer, "ROLLBACK;"); }
        Assert.Equal(1, retries);
        using var read = destination.CreateCommand();
        read.CommandText = "SELECT name FROM apps WHERE id=1;";
        Assert.Equal("Committed after retry", read.ExecuteScalar());
    }

    [Fact]
    public void PersistentLockHasBoundedWaitAndBackupCanRecoverAfterRelease()
    {
        using var fixture = new RemovalFixture();
        var path = Path.Combine(fixture.Data, "timepilot.db");
        using var source = Open(path, readOnly: true);
        using var destination = Open(Path.Combine(fixture.External, "snapshot.db"));
        using var writer = Open(path);
        Execute(writer, "BEGIN EXCLUSIVE;");
        var watch = Stopwatch.StartNew();
        try
        {
            var error = Assert.Throws<TimeoutException>(() => DataBackupService.BackupDatabaseWithRetry(source, destination, TimeSpan.FromMilliseconds(150)));
            Assert.Equal(5, Assert.IsType<SqliteException>(error.InnerException).SqliteErrorCode);
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), watch.Elapsed.ToString());
        }
        finally { Execute(writer, "ROLLBACK;"); }
        DataBackupService.BackupDatabaseWithRetry(source, destination);
        using var check = destination.CreateCommand();
        check.CommandText = "PRAGMA quick_check;";
        Assert.Equal("ok", check.ExecuteScalar());
    }

    [Fact]
    public void LockedBackupPreservesExistingZipCleansStagingAndCanBeRetried()
    {
        using var fixture = new RemovalFixture();
        var path = Path.Combine(fixture.Data, "timepilot.db");
        using var writer = Open(path);
        File.WriteAllText(fixture.Backup, "Existing output must survive");
        Execute(writer, "BEGIN EXCLUSIVE;");
        try
        {
            Assert.Throws<TimeoutException>(() => new DataBackupService(fixture.Data).CreateBackup(fixture.Backup, DateTimeOffset.UtcNow));
        }
        finally { Execute(writer, "ROLLBACK;"); }
        Assert.Equal("Existing output must survive", File.ReadAllText(fixture.Backup));
        Assert.Empty(Directory.GetFiles(fixture.External, "*.tmp"));
        var service = new DataBackupService(fixture.Data);
        service.CreateBackup(fixture.Backup, DateTimeOffset.UtcNow);
        Assert.True(service.InspectBackup(fixture.Backup).HasDatabase);
        Assert.True(service.InspectBackup(fixture.Backup).HasSettings);
    }

    [Fact]
    public void NonLockFailureIsNotRetried()
    {
        using var fixture = new RemovalFixture();
        using var source = Open(Path.Combine(fixture.Data, "timepilot.db"), readOnly: true);
        using var destination = Open(Path.Combine(fixture.Data, "timepilot.db"), readOnly: true);
        var retried = false;
        var error = Assert.Throws<SqliteException>(() => DataBackupService.BackupDatabaseWithRetry(source, destination,
            TimeSpan.FromSeconds(1), () => retried = true));
        Assert.NotEqual(5, error.SqliteErrorCode);
        Assert.NotEqual(6, error.SqliteErrorCode);
        Assert.False(retried);
    }

    private static SqliteConnection Open(string path, bool readOnly = false)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Pooling = false, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
