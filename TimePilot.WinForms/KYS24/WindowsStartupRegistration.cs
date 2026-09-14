using Microsoft.Win32;
using Windows.ApplicationModel;

namespace TimePilot.WinForms.KYS24
{
    internal static class WindowsStartupRegistration
    {
        public const string TrayStartupArgument = "--tray";

        private const string PackagedStartupTaskId = "ActiveLogbookStartup";

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ActiveLogbook";
        private const string LegacyValueName = "TimePilot";

        public static void SetEnabled(bool isEnabled)
        {
            if (IsPackagedApp())
            {
                SetPackagedStartupTaskEnabledAsync(isEnabled).GetAwaiter().GetResult();
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (!isEnabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
                return;
            }

            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            key.SetValue(
                ValueName,
                BuildStartupCommand(Application.ExecutablePath),
                RegistryValueKind.String);
        }

        public static void Synchronize(bool isEnabled)
        {
            if (IsPackagedApp())
            {
                SetPackagedStartupTaskEnabledAsync(isEnabled).GetAwaiter().GetResult();
                return;
            }

            if (!isEnabled)
            {
                SetEnabled(false);
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var registeredCommand = key?.GetValue(ValueName) as string;
            if (IsStartupCommandForExecutable(registeredCommand, Application.ExecutablePath))
                return;

            var legacyRegisteredCommand = key?.GetValue(LegacyValueName) as string;
            if (IsStartupCommandForExecutable(legacyRegisteredCommand, Application.ExecutablePath))
            {
                SetEnabled(true);
                return;
            }

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

        private static bool IsPackagedApp()
        {
            try
            {
                _ = Package.Current;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static async Task SetPackagedStartupTaskEnabledAsync(bool isEnabled)
        {
            var startupTask = await StartupTask.GetAsync(PackagedStartupTaskId).AsTask().ConfigureAwait(false);
            if (!isEnabled)
            {
                startupTask.Disable();
                return;
            }

            if (startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                return;

            // Windows keeps a Task Manager user opt-out authoritative.
            if (startupTask.State == StartupTaskState.Disabled)
                await startupTask.RequestEnableAsync().AsTask().ConfigureAwait(false);
        }
    }
}
