using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimePilot.WinForms.KYS24;

// Explicit DTOs are the privacy boundary: never serialize storage/import models directly.
internal static class CategoryTransferFile
{
    internal const string Format = "activelogbook-categories";
    internal const int Version = 1;
    internal const int MaximumBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 32
    };

    public static void Export(string databasePath, string destination, DateTimeOffset now)
    {
        if (!Path.IsPathFullyQualified(destination) || !string.Equals(Path.GetExtension(destination), ".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose an absolute path for a new JSON file.");
        if (Path.Exists(destination))
            throw new IOException("Choose a new file name. Existing files will not be overwritten.");
        var state = CategoryImportService.ReadCurrentState(databasePath);
        // File-local IDs avoid exporting database row identities.
        var categories = state.Categories.Select((c, index) => new TransferCategory(index + 1, c.Name, c.Color, c.SortOrder, c.Builtin)).ToArray();
        var ids = state.Categories.Select((c, index) => (c.Id, LocalId: (long)index + 1)).ToDictionary(c => c.Id, c => c.LocalId);
        var assignments = state.Apps.Where(a => a.CategoryId is not null).OrderBy(a => a.Process, StringComparer.OrdinalIgnoreCase)
            .Select(a => new TransferAssignment(a.Process, ids[a.CategoryId!.Value])).ToArray();
        var document = new TransferDocument(Format, Version, now.ToUniversalTime(), categories, assignments);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document, Options);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("The category file exceeds the 8 MiB limit.");
        // Verify the exact outbound format before publishing it.
        _ = ToState(document);
        var parent = Path.GetDirectoryName(Path.GetFullPath(destination))!;
        Directory.CreateDirectory(parent);
        var temporary = Path.Combine(parent, $"ActiveLogbook-categories-{Guid.NewGuid():N}.tmp");
        var ownsTemporary = false;
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ownsTemporary = true;
                output.Write(bytes);
                output.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: false);
        }
        finally
        {
            try { if (ownsTemporary) File.Delete(temporary); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    public static CategoryImportState Read(string path)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length > MaximumBytes) throw new InvalidDataException("The category file exceeds the 8 MiB limit.");
        var document = JsonSerializer.Deserialize<TransferDocument>(input, Options)
            ?? throw new InvalidDataException("The category file is empty.");
        return ToState(document);
    }

    private static CategoryImportState ToState(TransferDocument document)
    {
        if (document.Format != Format || document.Version != Version)
            throw new InvalidDataException("Unsupported category file format or version.");
        if (document.Categories is null || document.Assignments is null
            || document.Categories.Any(c => c is null) || document.Assignments.Any(a => a is null))
            throw new InvalidDataException("The category file contains missing data.");
        return CategoryImportService.ValidateState(new(
            document.Categories.Select(c => new ImportCategory(c.Id, c.Name, c.Color, c.SortOrder, c.IsBuiltin)).ToArray(),
            document.Assignments.Select((a, index) => new ImportApp(index + 1, a.ProcessName, a.ProcessName, null,
                a.CategoryId, AppCategorySource.Import, null)).ToArray()));
    }

    private sealed record TransferDocument(
        [property: JsonRequired] string Format,
        [property: JsonRequired] int Version,
        [property: JsonRequired] DateTimeOffset ExportedAtUtc,
        [property: JsonRequired] TransferCategory[] Categories,
        [property: JsonRequired] TransferAssignment[] Assignments);
    private sealed record TransferCategory(
        [property: JsonRequired] long Id,
        [property: JsonRequired] string Name,
        string? Color,
        [property: JsonRequired] int SortOrder,
        [property: JsonRequired] bool IsBuiltin);
    private sealed record TransferAssignment(
        [property: JsonRequired] string ProcessName,
        [property: JsonRequired] long CategoryId);
}
