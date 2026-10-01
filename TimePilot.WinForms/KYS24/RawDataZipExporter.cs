using System.IO.Compression;
using System.Text;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class RawDataZipExporter
    {
        private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

        private readonly TimePilotStorage storage;

        public RawDataZipExporter(TimePilotStorage storage)
        {
            this.storage = storage;
        }

        public IReadOnlyList<string> Export(string zipFilePath)
        {
            var directory = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var tables = storage.GetRawDataExportTables();
            var tempFilePath = Path.Combine(
                string.IsNullOrWhiteSpace(directory) ? Path.GetTempPath() : directory,
                $"TimePilot-raw-data-{Guid.NewGuid():N}.tmp");

            try
            {
                using (var archive = ZipFile.Open(tempFilePath, ZipArchiveMode.Create))
                {
                    WriteTextEntry(archive, "README.txt", BuildReadme(tables));
                    foreach (var table in tables)
                    {
                        WriteCsvEntry(archive, table);
                    }
                }

                File.Move(tempFilePath, zipFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }

            return tables.Select(table => table.FileName)
                .Prepend("README.txt")
                .ToList();
        }

        private static string BuildReadme(IReadOnlyList<RawDataExportTable> tables)
        {
            var builder = new StringBuilder();
            builder.AppendLine(UiText.RawDataExport.ReadmeTitle);
            builder.AppendLine();
            builder.AppendLine(UiText.RawDataExport.ReadmePrivacyNotice);
            builder.AppendLine();
            builder.AppendLine(UiText.RawDataExport.ReadmeTableList);

            foreach (var table in tables)
            {
                builder.AppendLine($"- {table.FileName}: {string.Join(", ", table.Columns)}");
            }

            return builder.ToString();
        }

        private static void WriteCsvEntry(ZipArchive archive, RawDataExportTable table)
        {
            var entry = archive.CreateEntry(table.FileName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Utf8WithBom);
            writer.WriteLine(string.Join(",", table.Columns.Select(Escape)));

            foreach (var row in table.Rows)
            {
                writer.WriteLine(string.Join(",", row.Select(Escape)));
            }
        }

        private static void WriteTextEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Utf8WithBom);
            writer.Write(content);
        }

        private static string Escape(string? value)
        {
            value ??= "";
            if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\r') && !value.Contains('\n'))
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
