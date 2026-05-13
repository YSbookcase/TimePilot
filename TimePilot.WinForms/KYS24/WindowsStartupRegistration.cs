using Microsoft.Win32;

namespace TimePilot.WinForms.KYS24
{
    internal static class WindowsStartupRegistration
    {
        public const string TrayStartupArgument = "--tray";

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "TimePilot";

        public static void SetEnabled(bool isEnabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (!isEnabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return;
            }

            key.SetValue(
                ValueName,
                $"\"{Application.ExecutablePath}\" {TrayStartupArgument}",
                RegistryValueKind.String);
        }
    }
}
