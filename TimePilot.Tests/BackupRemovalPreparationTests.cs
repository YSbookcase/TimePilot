using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class BackupRemovalPreparationTests
{
    [Fact]
    public void FullBackupEnablesPreparationWithoutChangingSourceRecordsOrSettings()
    {
        using var fixture = new RemovalFixture();
        var before = fixture.SourceHashes();
        var preparation = new BackupRemovalPreparation(fixture.Data);
        Assert.False(preparation.CanOpenInstalledApps);
        Assert.Throws<InvalidOperationException>(() => preparation.VerifyBeforeOpeningSettings());
        var receipt = preparation.CreateBackup(fixture.Backup, DateTimeOffset.UtcNow);
        Assert.True(preparation.CanOpenInstalledApps);
        Assert.True(new DataBackupService(fixture.Data).InspectBackup(receipt.Path).HasSettings);
        Assert.Equal(receipt, preparation.VerifyBeforeOpeningSettings());
        Assert.Equal(before, fixture.SourceHashes());
        Assert.Equal("ms-settings:appsfeatures", BackupRemovalPreparation.InstalledAppsUri);
    }

    [Theory]
    [InlineData("missing-db")]
    [InlineData("missing-settings")]
    [InlineData("malformed-settings")]
    [InlineData("non-object-settings")]
    [InlineData("broken-db")]
    public void InvalidFullBackupNeverEnablesOrPublishesFinalZip(string scenario)
    {
        using var fixture = new RemovalFixture();
        if (scenario == "missing-db") File.Delete(Path.Combine(fixture.Data, "timepilot.db"));
        if (scenario == "missing-settings") File.Delete(Path.Combine(fixture.Data, "settings.json"));
        if (scenario == "malformed-settings") File.WriteAllText(Path.Combine(fixture.Data, "settings.json"), "broken JSON");
        if (scenario == "non-object-settings") File.WriteAllText(Path.Combine(fixture.Data, "settings.json"), "[]");
        if (scenario == "broken-db") File.WriteAllText(Path.Combine(fixture.Data, "timepilot.db"), "broken DB");
        var preparation = new BackupRemovalPreparation(fixture.Data);
        Assert.ThrowsAny<Exception>(() => preparation.CreateBackup(fixture.Backup, DateTimeOffset.UtcNow));
        Assert.False(preparation.CanOpenInstalledApps);
        Assert.False(File.Exists(fixture.Backup));
        Assert.Empty(Directory.GetFiles(fixture.External));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingOrChangedBackupRevokesReadiness(bool tamper)
    {
        using var fixture = new RemovalFixture();
        var preparation = new BackupRemovalPreparation(fixture.Data);
        preparation.CreateBackup(fixture.Backup, DateTimeOffset.UtcNow);
        if (tamper) File.AppendAllText(fixture.Backup, "changed");
        else File.Delete(fixture.Backup);
        Assert.ThrowsAny<Exception>(() => preparation.VerifyBeforeOpeningSettings());
        Assert.False(preparation.CanOpenInstalledApps);
    }

    [Fact]
    public void FailedRetryCannotReusePreviousSuccessOrOverwriteIt()
    {
        using var fixture = new RemovalFixture();
        var preparation = new BackupRemovalPreparation(fixture.Data);
        preparation.CreateBackup(fixture.Backup, DateTimeOffset.UtcNow);
        var before = File.ReadAllBytes(fixture.Backup);
        Assert.Throws<IOException>(() => preparation.CreateBackup(fixture.Backup, DateTimeOffset.UtcNow));
        Assert.False(preparation.CanOpenInstalledApps);
        Assert.Equal(before, File.ReadAllBytes(fixture.Backup));
        preparation.CreateBackup(Path.Combine(fixture.External, "new.zip"), DateTimeOffset.UtcNow);
        Assert.True(preparation.CanOpenInstalledApps);
    }

    [Fact]
    public void DataInstallAndWholePackageFoldersAreRejectedBeforeWriting()
    {
        using var fixture = new RemovalFixture();
        var install = Path.Combine(fixture.Root, "install");
        var preparation = new BackupRemovalPreparation(fixture.Data, install);
        Assert.Throws<ArgumentException>(() => preparation.CreateBackup(Path.Combine(fixture.Data, "nested", "unsafe.zip"), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => preparation.ValidateDestination(Path.Combine(install, "unsafe.zip")));
        Assert.False(Directory.Exists(Path.Combine(fixture.Data, "nested")));
        var packageRoot = Path.Combine(fixture.Root, "Packages", "YSBookcase.ActiveLogbook_fixture");
        var packaged = new BackupRemovalPreparation(Path.Combine(packageRoot, "LocalState", "TimePilot"));
        Assert.Throws<ArgumentException>(() => packaged.ValidateDestination(Path.Combine(packageRoot, "LocalCache", "unsafe.zip")));
        Assert.Throws<ArgumentException>(() => packaged.ValidateDestination(Path.Combine(packageRoot, "LocalState", "unsafe.zip")));
        Assert.Equal(Path.GetFullPath(fixture.Backup), packaged.ValidateDestination(fixture.Backup));
    }

    [Fact]
    public void ExternalHistoryRoundTripsAndKeepsOtherPreferences()
    {
        using var fixture = new RemovalFixture();
        var path = Path.Combine(fixture.Root, "history-settings.json");
        var settings = AppSettings.Load(path);
        settings.SetKoreanWebSearchSuffix("뜻");
        var at = new DateTimeOffset(2026, 11, 21, 1, 2, 3, TimeSpan.Zero);
        settings.RecordExternalBackupSuccess(at, fixture.Backup);
        var loaded = AppSettings.Load(path);
        Assert.Equal(at, loaded.LastExternalBackupAt);
        Assert.Equal(fixture.Backup, loaded.LastExternalBackupPath);
        Assert.Equal("뜻", loaded.KoreanWebSearchSuffix);
        Assert.False(loaded.AutomaticBackupEnabled);
    }
}

internal sealed class RemovalFixture : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"TimePilotRemoval-{Guid.NewGuid():N}");
    public string Data => Path.Combine(Root, "data");
    public string External => Path.Combine(Root, "external");
    public string Backup => Path.Combine(External, "before-removal.zip");

    public RemovalFixture()
    {
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(External);
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(Data, "timepilot.db"), Pooling = false }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE apps (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
                CREATE TABLE foreground_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE idle_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE app_runtime_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE process_runtime_sessions (id INTEGER PRIMARY KEY);
                CREATE TABLE system_events (id INTEGER PRIMARY KEY);
                INSERT INTO apps VALUES(1, 'Synthetic app');
                INSERT INTO foreground_sessions VALUES(1);
                """;
            command.ExecuteNonQuery();
        }
        File.WriteAllText(Path.Combine(Data, "settings.json"), "{\"KoreanWebSearchSuffix\":\"이란\"}");
    }

    public string[] SourceHashes() => Directory.GetFiles(Data).OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => Path.GetFileName(path) + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))).ToArray();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(Root, recursive: true);
    }
}
