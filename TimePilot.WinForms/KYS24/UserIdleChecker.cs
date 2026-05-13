using System.Runtime.InteropServices;

namespace TimePilot.WinForms.KYS24
{
    internal static class UserIdleChecker
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        public static bool IsIdle(int idleThresholdMs)
        {
            var lastInputInfo = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
            if (!GetLastInputInfo(ref lastInputInfo))
                return false;
            var idleMs = GetTickCount() - lastInputInfo.dwTime;
            return idleMs >= idleThresholdMs;
        }
    }
}
