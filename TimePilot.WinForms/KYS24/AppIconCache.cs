namespace TimePilot.WinForms.KYS24
{
    internal sealed class AppIconCache : IDisposable
    {
        private readonly object syncRoot = new();
        private readonly Dictionary<string, Image> icons = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> missingIconPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly Image defaultIcon = SystemIcons.Application.ToBitmap();
        private bool disposed;

        public Image GetIcon(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return defaultIcon;

            lock (syncRoot)
            {
                if (icons.TryGetValue(executablePath, out var cachedIcon))
                    return cachedIcon;

                if (missingIconPaths.Contains(executablePath))
                    return defaultIcon;
            }

            var icon = TryLoadIcon(executablePath) ?? defaultIcon;
            lock (syncRoot)
            {
                if (disposed)
                {
                    if (!ReferenceEquals(icon, defaultIcon))
                        icon.Dispose();
                    return defaultIcon;
                }

                if (ReferenceEquals(icon, defaultIcon))
                {
                    missingIconPaths.Add(executablePath);
                }
                else if (icons.TryGetValue(executablePath, out var cachedIcon))
                {
                    icon.Dispose();
                    icon = cachedIcon;
                }
                else
                {
                    icons[executablePath] = icon;
                }
            }

            return icon;
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;

                disposed = true;
                foreach (var icon in icons.Values.Where(icon => !ReferenceEquals(icon, defaultIcon)))
                {
                    icon.Dispose();
                }

                defaultIcon.Dispose();
            }
        }

        private static Image? TryLoadIcon(string executablePath)
        {
            try
            {
                if (!File.Exists(executablePath))
                    return null;

                using var icon = Icon.ExtractAssociatedIcon(executablePath);
                return icon?.ToBitmap();
            }
            catch
            {
                return null;
            }
        }
    }
}
