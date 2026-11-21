using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace TimePilot.WinForms.KYS24;

internal sealed record RemovalBackupReceipt(string Path, DateTimeOffset CreatedAt, string Sha256);

internal sealed class BackupRemovalPreparation
{
    public const string InstalledAppsUri = "ms-settings:appsfeatures";
    private readonly string dataDirectory;
    private readonly string[] protectedDirectories;

    public RemovalBackupReceipt? VerifiedBackup { get; private set; }
    public bool CanOpenInstalledApps => VerifiedBackup is not null;

    internal BackupRemovalPreparation(string dataDirectory, params string[] additionalProtectedDirectories)
    {
        this.dataDirectory = Path.GetFullPath(dataDirectory);
        var roots = new List<string> { this.dataDirectory };
        roots.AddRange(additionalProtectedDirectories.Select(Path.GetFullPath));
        // SharedStoreData and packaged runs must reject siblings under the whole package, not just TimePilot.
        for (var parent = new DirectoryInfo(this.dataDirectory); parent is not null; parent = parent.Parent)
        {
            if (parent.Name.StartsWith("YSBookcase.ActiveLogbook_", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parent.Parent?.Name, "Packages", StringComparison.OrdinalIgnoreCase))
                roots.Add(parent.FullName);
        }
        protectedDirectories = roots.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static BackupRemovalPreparation ForCurrentApp()
    {
        var roots = new List<string> { Application.StartupPath };
        var state = AppDataPaths.TryGetPackagedLocalStateDirectory();
        if (state is not null && Path.GetDirectoryName(state) is { } packageRoot)
            roots.Add(packageRoot);
        return new BackupRemovalPreparation(AppDataPaths.DataDirectory, roots.ToArray());
    }

    internal string ValidateDestination(string path, bool requireNew = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new ArgumentException("Choose an absolute path for an external ZIP backup.");
        var fullPath = Path.GetFullPath(path);
        if (!string.Equals(Path.GetExtension(fullPath), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The backup must be a ZIP file.");
        foreach (var root in protectedDirectories)
        {
            var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
            if (fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Save the backup outside the application, data and package folders.");
        }
        for (var current = fullPath; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if (Path.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Choose a backup location without symbolic links or junctions.");
        }
        if (requireNew && Path.Exists(fullPath))
            throw new IOException("Choose a new backup file name. Existing backups will not be overwritten.");
        return fullPath;
    }

    public RemovalBackupReceipt CreateBackup(string path, DateTimeOffset now, IProgress<DataBackupProgressStage>? progress = null)
    {
        VerifiedBackup = null;
        var destination = ValidateDestination(path);
        if (!File.Exists(Path.Combine(dataDirectory, "settings.json")))
            throw new InvalidDataException("The settings file is missing; a full removal backup cannot be created.");
        var backup = new DataBackupService(dataDirectory);
        var parent = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, $"ActiveLogbook-removal-staging-{Guid.NewGuid():N}.zip");
        try
        {
            backup.CreateBackup(staging, now, progress);
            ValidateContents(backup, staging);
            var hash = Hash(staging);
            File.Move(staging, destination, overwrite: false);
            VerifiedBackup = new RemovalBackupReceipt(destination, now, hash);
            return VerifiedBackup;
        }
        finally
        {
            if (Path.GetDirectoryName(Path.GetFullPath(staging)) != parent)
                throw new InvalidOperationException("Invalid removal backup staging path.");
            try { File.Delete(staging); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    public RemovalBackupReceipt VerifyBeforeOpeningSettings()
    {
        var candidate = VerifiedBackup;
        VerifiedBackup = null;
        if (candidate is null)
            throw new InvalidOperationException("Create and verify an external backup in this window first.");
        ValidateDestination(candidate.Path, requireNew: false);
        if (!string.Equals(Hash(candidate.Path), candidate.Sha256, StringComparison.Ordinal))
            throw new InvalidDataException("The backup changed after verification. Create a new backup before proceeding.");
        ValidateContents(new DataBackupService(dataDirectory), candidate.Path);
        VerifiedBackup = candidate;
        return candidate;
    }

    private static void ValidateContents(DataBackupService service, string path)
    {
        var plan = service.InspectBackup(path);
        if (!plan.HasDatabase || !plan.HasSettings)
            throw new InvalidDataException("The full backup must contain both the database and settings.");
        using var archive = ZipFile.OpenRead(path);
        using var settingsStream = archive.GetEntry("settings.json")!.Open();
        using var settings = JsonDocument.Parse(settingsStream);
        if (settings.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("The backup settings must be a JSON object.");
    }

    private static string Hash(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(file));
    }
}
