using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TimePilot.WinForms.KYS24
{
    internal static class ForegroundWindowReader
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public static AppMetadata? TryGetForegroundApp()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;
            _ = GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0)
                return null;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                var executablePath = TryGetExecutablePath(process);
                return new AppMetadata(
                    process.ProcessName,
                    GetDisplayName(process.ProcessName, executablePath),
                    executablePath);
            }
            catch
            {
                return null;
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
