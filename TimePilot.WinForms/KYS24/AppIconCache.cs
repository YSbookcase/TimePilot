namespace TimePilot.WinForms.KYS24
{
    internal sealed class AppIconCache : IDisposable
    {
        private readonly Dictionary<string, Image> icons = new(StringComparer.OrdinalIgnoreCase);
        private readonly Image defaultIcon = SystemIcons.Application.ToBitmap();
        private bool disposed;

        public Image GetIcon(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return defaultIcon;

            if (icons.TryGetValue(executablePath, out var cachedIcon))
                return cachedIcon;

            var icon = TryLoadIcon(executablePath) ?? defaultIcon;
            icons[executablePath] = icon;
            return icon;
        }

        public void Dispose()
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

        private static Image? TryLoadIcon(string executablePath)
        {
            try
            {
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
