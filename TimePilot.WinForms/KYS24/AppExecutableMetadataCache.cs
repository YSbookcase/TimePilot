using System.Diagnostics;

namespace TimePilot.WinForms.KYS24
{
    internal sealed class AppExecutableMetadataCache
    {
        private readonly object syncRoot = new();
        private readonly Dictionary<string, AppExecutableMetadata> metadataByPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly AppExecutableMetadata missingMetadata = new(null, null, null, false);

        public AppExecutableMetadata GetMetadata(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return missingMetadata;

            lock (syncRoot)
            {
                if (metadataByPath.TryGetValue(executablePath, out var cachedMetadata))
                    return cachedMetadata;
            }

            var metadata = LoadMetadata(executablePath);
            lock (syncRoot)
            {
                if (metadataByPath.TryGetValue(executablePath, out var cachedMetadata))
                    return cachedMetadata;

                metadataByPath[executablePath] = metadata;
                return metadata;
            }
        }

        private static AppExecutableMetadata LoadMetadata(string executablePath)
        {
            if (!File.Exists(executablePath))
                return new AppExecutableMetadata(null, null, null, false);

            var fileDescription = default(string?);
            var productName = default(string?);
            var companyName = default(string?);
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                fileDescription = NormalizeMetadata(versionInfo.FileDescription);
                productName = NormalizeMetadata(versionInfo.ProductName);
                companyName = NormalizeMetadata(versionInfo.CompanyName);
            }
            catch
            {
            }

            return new AppExecutableMetadata(
                fileDescription,
                productName,
                companyName,
                HasDistinctAssociatedIcon(executablePath));
        }

        private static bool HasDistinctAssociatedIcon(string executablePath)
        {
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(executablePath);
                if (icon is null)
                    return false;

                using var extractedBitmap = icon.ToBitmap();
                using var defaultBitmap = SystemIcons.Application.ToBitmap();
                return !AreBitmapsEqual(extractedBitmap, defaultBitmap);
            }
            catch
            {
                return false;
            }
        }

        private static bool AreBitmapsEqual(Bitmap left, Bitmap right)
        {
            if (left.Size != right.Size)
                return false;

            for (var y = 0; y < left.Height; y++)
            {
                for (var x = 0; x < left.Width; x++)
                {
                    if (left.GetPixel(x, y) != right.GetPixel(x, y))
                        return false;
                }
            }

            return true;
        }

        private static string? NormalizeMetadata(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
