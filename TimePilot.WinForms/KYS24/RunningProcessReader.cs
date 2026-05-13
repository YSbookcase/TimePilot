using System.Diagnostics;

namespace TimePilot.WinForms.KYS24
{
    internal static class RunningProcessReader
    {
        private static readonly object ExecutablePathCacheLock = new();
        private static readonly object DisplayNameCacheLock = new();
        private static readonly Dictionary<string, string?> ExecutablePathCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> DisplayNameCache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<RunningProcessSnapshot> GetWindowedApps()
        {
            return GetProcesses(ProcessRuntimeTrackingScope.WindowedApps);
        }

        public static IReadOnlyList<RunningProcessSnapshot> GetProcesses(ProcessRuntimeTrackingScope scope)
        {
            var apps = new List<RunningProcessSnapshot>();
            var currentSessionId = Process.GetCurrentProcess().SessionId;

            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    try
                    {
                        var hasMainWindow = process.MainWindowHandle != IntPtr.Zero;
                        var isCurrentSessionProcess = IsCurrentSessionProcess(process, currentSessionId);
                        if (!ShouldTrack(scope, hasMainWindow, isCurrentSessionProcess))
                            continue;

                        var processName = process.ProcessName;
                        var executablePath = TryGetExecutablePath(process, process.Id, processName);
                        apps.Add(new RunningProcessSnapshot(
                            process.Id,
                            new AppMetadata(
                                processName,
                                GetDisplayName(processName, executablePath),
                                executablePath),
                            hasMainWindow,
                            isCurrentSessionProcess));
                    }
                    catch
                    {
                    }
                }
            }

            return apps;
        }

        private static bool ShouldTrack(ProcessRuntimeTrackingScope scope, bool hasMainWindow, bool isCurrentSessionProcess)
        {
            return scope switch
            {
                ProcessRuntimeTrackingScope.AllProcesses => true,
                ProcessRuntimeTrackingScope.UserProcesses => isCurrentSessionProcess,
                _ => hasMainWindow
            };
        }

        private static bool IsCurrentSessionProcess(Process process, int currentSessionId)
        {
            try
            {
                return process.SessionId == currentSessionId;
            }
            catch
            {
                return false;
            }
        }

        private static string? TryGetExecutablePath(Process process, int processId, string processName)
        {
            var cacheKey = $"{processId}|{processName}";
            lock (ExecutablePathCacheLock)
            {
                if (ExecutablePathCache.TryGetValue(cacheKey, out var cachedExecutablePath))
                    return cachedExecutablePath;
            }

            try
            {
                var executablePath = process.MainModule?.FileName;
                lock (ExecutablePathCacheLock)
                {
                    ExecutablePathCache[cacheKey] = executablePath;
                }

                return executablePath;
            }
            catch
            {
                lock (ExecutablePathCacheLock)
                {
                    ExecutablePathCache[cacheKey] = null;
                }

                return null;
            }
        }

        private static string GetDisplayName(string processName, string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return processName;

            var cacheKey = $"{processName}|{executablePath}";
            lock (DisplayNameCacheLock)
            {
                if (DisplayNameCache.TryGetValue(cacheKey, out var cachedDisplayName))
                    return cachedDisplayName;
            }

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                var displayName = FirstNonEmpty(
                    versionInfo.FileDescription,
                    versionInfo.ProductName,
                    Path.GetFileNameWithoutExtension(executablePath),
                    processName);
                lock (DisplayNameCacheLock)
                {
                    DisplayNameCache[cacheKey] = displayName;
                }

                return displayName;
            }
            catch
            {
                lock (DisplayNameCacheLock)
                {
                    DisplayNameCache[cacheKey] = processName;
                }

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
