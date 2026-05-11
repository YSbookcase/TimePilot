using System.Diagnostics;

namespace TimePilot.WinForms.KYS24
{
    internal static class RunningProcessReader
    {
        public static IReadOnlyList<RunningProcessSnapshot> GetWindowedApps()
        {
            var apps = new List<RunningProcessSnapshot>();

            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    if (process.MainWindowHandle == IntPtr.Zero)
                        continue;

                    try
                    {
                        var executablePath = TryGetExecutablePath(process);
                        var processName = process.ProcessName;
                        apps.Add(new RunningProcessSnapshot(
                            process.Id,
                            new AppMetadata(
                                processName,
                                GetDisplayName(processName, executablePath),
                                executablePath)));
                    }
                    catch
                    {
                    }
                }
            }

            return apps;
        }

        private static string? TryGetExecutablePath(Process process)
        {
            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static string GetDisplayName(string processName, string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return processName;

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                return FirstNonEmpty(
                    versionInfo.FileDescription,
                    versionInfo.ProductName,
                    Path.GetFileNameWithoutExtension(executablePath),
                    processName);
            }
            catch
            {
                return processName;
            }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }
    }
}
