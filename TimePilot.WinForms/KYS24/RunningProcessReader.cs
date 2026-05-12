using System.Diagnostics;

namespace TimePilot.WinForms.KYS24
{
    internal static class RunningProcessReader
    {
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
                        if (!ShouldTrack(process, scope, currentSessionId))
                            continue;

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

        private static bool ShouldTrack(Process process, ProcessRuntimeTrackingScope scope, int currentSessionId)
        {
            return scope switch
            {
                ProcessRuntimeTrackingScope.AllProcesses => true,
                ProcessRuntimeTrackingScope.UserProcesses => IsCurrentSessionProcess(process, currentSessionId),
                _ => process.MainWindowHandle != IntPtr.Zero
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
