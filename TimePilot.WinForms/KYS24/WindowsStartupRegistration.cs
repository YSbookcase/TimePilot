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
                BuildStartupCommand(Application.ExecutablePath),
                RegistryValueKind.String);
        }

        public static void Synchronize(bool isEnabled)
        {
            if (!isEnabled)
            {
                SetEnabled(false);
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var registeredCommand = key?.GetValue(ValueName) as string;
            if (IsStartupCommandForExecutable(registeredCommand, Application.ExecutablePath))
                return;

            SetEnabled(true);
        }

        internal static string BuildStartupCommand(string executablePath)
        {
            return $"\"{executablePath}\" {TrayStartupArgument}";
        }

        internal static bool IsStartupCommandForExecutable(
            string? registeredCommand,
            string executablePath)
        {
            return string.Equals(
                registeredCommand,
                BuildStartupCommand(executablePath),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
