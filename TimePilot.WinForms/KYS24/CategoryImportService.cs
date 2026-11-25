using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TimePilot.WinForms.KYS24;

internal sealed record ImportCategory(long Id, string Name, string? Color, int SortOrder, bool Builtin);
internal sealed record ImportApp(long Id, string Process, string Display, string? Path, long? CategoryId, string Source, string? UpdatedAt);
internal sealed record CategoryImportState(ImportCategory[] Categories, ImportApp[] Apps);
internal enum CategoryImportAction { AddCategory, Assign, KeepCurrent, KeepUser, PathConflict, MissingApp, UnsupportedCategory, MatchCategory, Replace, ReplaceUser, UpdateColor }
internal sealed record CategoryImportOptions(bool PreferFile = false, bool ReplaceColors = false);
internal sealed record CategoryImportRow(string App, string? Current, string? Backup, CategoryImportAction Action,
    long? AppId = null, long? BackupCategoryId = null, string? CurrentColor = null, string? FileColor = null);
internal sealed record CategoryImportPlan(string DatabasePath, string CurrentFingerprint, CategoryImportState Backup,
    IReadOnlyList<CategoryImportRow> Rows, bool ProcessNameOnly = false,
    CategoryImportOptions? Options = null, CategoryImportState? CurrentSnapshot = null)
{
    public int AddedCategories => Rows.Count(r => r.Action == CategoryImportAction.AddCategory);
    public int AssignedApps => Rows.Count(r => r.Action == CategoryImportAction.Assign);
    public int ReplacedApps => Rows.Count(r => r.Action is CategoryImportAction.Replace or CategoryImportAction.ReplaceUser);
    public int ReplacedUserApps => Rows.Count(r => r.Action == CategoryImportAction.ReplaceUser);
    public int ChangedColors => Rows.Count(r => r.Action == CategoryImportAction.UpdateColor);
    public bool RequiresOverwriteApproval => ReplacedApps + ChangedColors > 0;
    public bool HasChanges => AddedCategories + AssignedApps + ReplacedApps + ChangedColors > 0;
}

// Only category metadata is writable here. Session tables and settings are never restored.
internal sealed class CategoryImportService(string databasePath)
{
    private readonly string databasePath = Path.GetFullPath(databasePath);
    private const long MaximumDatabaseBytes = 1024L * 1024 * 1024;

    public void ApplyWithSafetyBackup(CategoryImportPlan approved, DataBackupService backup, string safetyPath, DateTimeOffset now,
        bool overwriteApproved = false)
    {
        RequireApproval(approved, overwriteApproved);
        backup.CreateBackup(safetyPath, now);
        if (!backup.InspectBackup(safetyPath).HasSettings)
            throw new InvalidDataException("The safety backup is missing settings. Categories were not imported.");
        Apply(approved, now, overwriteApproved);
    }

    public CategoryImportPlan Preview(string filePath)
    {
        var portable = string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase);
        var backup = portable ? CategoryTransferFile.Read(filePath) : ReadBackup(filePath);
        return BuildPlan(ReadCurrentState(databasePath), backup, portable);
    }

    internal static CategoryImportState ReadCurrentState(string path)
    {
        using var current = Open(path, readOnly: true);
        using var transaction = current.BeginTransaction(deferred: true);
        return ReadState(current, transaction);
    }

    public static CategoryImportPlan Replan(CategoryImportPlan preview, CategoryImportOptions options)
    {
        var current = preview.CurrentSnapshot ?? throw new InvalidOperationException("Reopen the file to preview category choices.");
        return new CategoryImportService(preview.DatabasePath).BuildPlan(current, preview.Backup, preview.ProcessNameOnly, options);
    }

    private static void RequireApproval(CategoryImportPlan plan, bool approved)
    {
        if (plan.RequiresOverwriteApproval && !approved)
            throw new InvalidOperationException("Review and explicitly approve assignment or color replacements before applying.");
    }

    public void Apply(CategoryImportPlan approved, DateTimeOffset now, bool overwriteApproved = false)
    {
        if (!string.Equals(databasePath, approved.DatabasePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The category preview belongs to a different database.");
        using var connection = Open(databasePath, readOnly: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var current = ReadState(connection, transaction);
        if (Fingerprint(current) != approved.CurrentFingerprint)
            throw new InvalidOperationException("Categories or app identities changed. Open the backup again to review a new preview.");
        var plan = BuildPlan(current, approved.Backup, approved.ProcessNameOnly, approved.Options);
        RequireApproval(plan, overwriteApproved);
        var categoryIds = new Dictionary<long, long>();
        var sortOrder = current.Categories.Select(c => c.SortOrder).DefaultIfEmpty(0).Max();
        foreach (var category in approved.Backup.Categories)
        {
            var row = plan.Rows.Single(r => r.AppId is null && r.BackupCategoryId == category.Id);
            if (row.Action is CategoryImportAction.MatchCategory or CategoryImportAction.UpdateColor)
            {
                categoryIds[category.Id] = MatchCategory(current.Categories, category)!.Id;
                if (row.Action == CategoryImportAction.UpdateColor)
                {
                    using var color = connection.CreateCommand();
                    color.Transaction = transaction;
                    color.CommandText = "UPDATE app_categories SET color=$color, updated_at=$now WHERE id=$id;";
                    color.Parameters.AddWithValue("$color", (object?)category.Color ?? DBNull.Value);
                    color.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O"));
                    color.Parameters.AddWithValue("$id", categoryIds[category.Id]);
                    if (color.ExecuteNonQuery() != 1) throw new InvalidOperationException("A category disappeared. Nothing was imported.");
                }
            }
            else if (row.Action == CategoryImportAction.AddCategory)
            {
                sortOrder = checked(sortOrder + 10);
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO app_categories (name, color, sort_order, is_builtin, source, created_at, updated_at)
                    VALUES ($name, $color, $sort, 0, 'import', $now, $now) RETURNING id;
                    """;
                insert.Parameters.AddWithValue("$name", category.Name);
                insert.Parameters.AddWithValue("$color", (object?)category.Color ?? DBNull.Value);
                insert.Parameters.AddWithValue("$sort", sortOrder);
                insert.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O"));
                categoryIds[category.Id] = (long)insert.ExecuteScalar()!;
            }
        }
        foreach (var row in plan.Rows.Where(r => r.Action is CategoryImportAction.Assign or CategoryImportAction.Replace or CategoryImportAction.ReplaceUser))
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE apps SET primary_category_id = $category, primary_category_source = 'import',
                    primary_category_updated_at = $now
                WHERE id = $app;
                """;
            update.Parameters.AddWithValue("$category", categoryIds[row.BackupCategoryId!.Value]);
            update.Parameters.AddWithValue("$app", row.AppId!.Value);
            update.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O"));
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("An app category changed. Nothing was imported.");
        }
        transaction.Commit();
    }

    private CategoryImportPlan BuildPlan(CategoryImportState current, CategoryImportState backup, bool processNameOnly = false,
        CategoryImportOptions? options = null)
    {
        options ??= new CategoryImportOptions();
        var rows = new List<CategoryImportRow>();
        var supported = new HashSet<long>();
        foreach (var category in backup.Categories)
        {
            var match = MatchCategory(current.Categories, category);
            var collision = current.Categories.Any(c => SameName(c.Name, category.Name)
                || (c.Builtin && AppCategoryDisplay.BuiltinCategories.Any(definition =>
                    SameName(definition.CanonicalName, Canonical(c))
                    && (SameName(definition.CanonicalName, category.Name) || SameName(definition.KoreanName, category.Name)))));
            var action = match is not null ? options.ReplaceColors && !SameColor(match.Color, category.Color)
                    ? CategoryImportAction.UpdateColor : CategoryImportAction.MatchCategory
                : !category.Builtin && !collision ? CategoryImportAction.AddCategory
                : CategoryImportAction.UnsupportedCategory;
            if (action != CategoryImportAction.UnsupportedCategory) supported.Add(category.Id);
            rows.Add(new("", match?.Name, category.Name, action, BackupCategoryId: category.Id,
                CurrentColor: match?.Color, FileColor: category.Color));
        }
        foreach (var app in backup.Apps.Where(a => a.CategoryId is not null))
        {
            var category = backup.Categories.SingleOrDefault(c => c.Id == app.CategoryId)
                ?? throw new InvalidDataException("A backup app references a missing category.");
            var match = current.Apps.SingleOrDefault(a => SameName(a.Process, app.Process));
            var currentCategory = current.Categories.SingleOrDefault(c => c.Id == match?.CategoryId)?.Name;
            var target = MatchCategory(current.Categories, category);
            var sameAssignment = match?.CategoryId is { } id && id == target?.Id;
            var action = match is null ? CategoryImportAction.MissingApp
                : !string.IsNullOrWhiteSpace(match.Path) && !string.IsNullOrWhiteSpace(app.Path)
                    && !SamePath(match.Path, app.Path) ? CategoryImportAction.PathConflict
                : !supported.Contains(category.Id) ? CategoryImportAction.UnsupportedCategory
                : SameName(match.Source, AppCategorySource.User)
                    ? options.PreferFile && !sameAssignment ? CategoryImportAction.ReplaceUser : CategoryImportAction.KeepUser
                : match.CategoryId is not null
                    ? options.PreferFile && !sameAssignment ? CategoryImportAction.Replace : CategoryImportAction.KeepCurrent
                : CategoryImportAction.Assign;
            rows.Add(new(match?.Display ?? app.Display, currentCategory, category.Name, action,
                match?.Id ?? -app.Id, category.Id));
        }
        return new(databasePath, Fingerprint(current), backup, rows.AsReadOnly(), processNameOnly, options, current);
    }

    private static ImportCategory? MatchCategory(ImportCategory[] current, ImportCategory backup) =>
        current.SingleOrDefault(c => c.Builtin == backup.Builtin && (backup.Builtin
            ? Canonical(backup) is { } name && SameName(Canonical(c), name)
            : SameName(c.Name, backup.Name)));

    private static string? Canonical(ImportCategory category) => AppCategoryDisplay.BuiltinCategories
        .FirstOrDefault(c => SameName(c.CanonicalName, category.Name) || SameName(c.KoreanName, category.Name))?.CanonicalName;
    private static bool SameName(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static bool SamePath(string a, string b) => SameName(a.Replace('/', '\\'), b.Replace('/', '\\'));
    private static bool SameColor(string? a, string? b) => string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)
        ? string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)
        : ColorTranslator.FromHtml(a).ToArgb() == ColorTranslator.FromHtml(b).ToArgb();
    private static string Fingerprint(CategoryImportState state) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state)));

    private static CategoryImportState ReadBackup(string zipPath)
    {
        var temporary = Path.Combine(Path.GetTempPath(), $"TimePilot-category-import-{Guid.NewGuid():N}.db");
        var ownsTemporary = false;
        try
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var entries = archive.Entries.Where(e => e.FullName == "timepilot.db").ToArray();
                if (entries.Length != 1 || entries[0].Length > MaximumDatabaseBytes)
                    throw new InvalidDataException("Choose a backup containing one database of at most 1 GiB.");
                using var input = entries[0].Open();
                using var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                ownsTemporary = true;
                var buffer = new byte[81920];
                long total = 0;
                int count;
                while ((count = input.Read(buffer)) > 0)
                {
                    total += count;
                    if (total > MaximumDatabaseBytes) throw new InvalidDataException("The backup database is too large.");
                    output.Write(buffer, 0, count);
                }
            }
            using var connection = Open(temporary, readOnly: true);
            using var check = connection.CreateCommand();
            check.CommandText = "PRAGMA quick_check;";
            if (!string.Equals(check.ExecuteScalar() as string, "ok", StringComparison.Ordinal))
                throw new InvalidDataException("The backup database is damaged.");
            return ReadState(connection);
        }
        finally
        {
            // Only this operation's owned extraction is removed; archive paths are never extracted.
            try { if (ownsTemporary) File.Delete(temporary); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    private static SqliteConnection Open(string path, bool readOnly)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false, ForeignKeys = true
        }.ToString());
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = readOnly ? "PRAGMA trusted_schema=OFF; PRAGMA query_only=ON;" : "PRAGMA trusted_schema=OFF;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    internal static CategoryImportState ReadState(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        var categories = new List<ImportCategory>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT id, name, color, sort_order, is_builtin FROM app_categories ORDER BY id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                var color = reader.IsDBNull(2) ? null : reader.GetString(2);
                categories.Add(new(reader.GetInt64(0), name, color, reader.GetInt32(3), reader.GetInt64(4) != 0));
            }
        }
        var apps = new List<ImportApp>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT id, process_name, display_name, executable_path, primary_category_id, primary_category_source, primary_category_updated_at FROM apps ORDER BY id;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                apps.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    reader.IsDBNull(5) ? AppCategorySource.User : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
        }
        return ValidateState(new(categories.ToArray(), apps.ToArray()));
    }

    internal static CategoryImportState ValidateState(CategoryImportState state)
    {
        foreach (var category in state.Categories)
        {
            if (category.Id <= 0 || string.IsNullOrWhiteSpace(category.Name) || category.Name != category.Name.Trim())
                throw new InvalidDataException("A category identity or name is invalid.");
            if (!string.IsNullOrWhiteSpace(category.Color))
            {
                try { _ = ColorTranslator.FromHtml(category.Color); }
                catch (Exception ex) { throw new InvalidDataException("A category color is invalid.", ex); }
            }
        }
        if (state.Categories.Select(c => c.Id).Distinct().Count() != state.Categories.Length
            || state.Categories.Select(c => c.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.Categories.Length
            || state.Apps.Select(a => a.Id).Distinct().Count() != state.Apps.Length
            || state.Apps.Select(a => a.Process).Distinct(StringComparer.OrdinalIgnoreCase).Count() != state.Apps.Length)
            throw new InvalidDataException("The backup contains ambiguous app or category identities.");
        var ids = state.Categories.Select(c => c.Id).ToHashSet();
        if (state.Apps.Any(a => a.Id <= 0 || string.IsNullOrWhiteSpace(a.Process) || a.Process != a.Process.Trim()
            || (a.CategoryId is { } categoryId && !ids.Contains(categoryId))))
            throw new InvalidDataException("An app identity or category reference is invalid.");
        return state;
    }
}
