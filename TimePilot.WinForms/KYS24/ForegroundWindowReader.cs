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

        public static string? TryGetForegroundProcessName()
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
                return process.ProcessName;
            }
            catch
            {
                return null;
            }
        }
    }
}
