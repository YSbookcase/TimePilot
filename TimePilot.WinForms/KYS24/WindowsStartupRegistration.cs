using Microsoft.Win32;
using Windows.ApplicationModel;

namespace TimePilot.WinForms.KYS24
{
    internal static class WindowsStartupRegistration
    {
        public const string TrayStartupArgument = "--tray";

        private const string PackagedStartupTaskId = "ActiveLogbookStartupV2";

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ActiveLogbook";
        private const string LegacyValueName = "TimePilot";

        public static Task SetEnabledAsync(bool isEnabled)
        {
            if (IsPackagedApp())
                return SetPackagedStartupTaskEnabledAsync(isEnabled);

            SetUnpackagedEnabled(isEnabled);
            return Task.CompletedTask;
        }

        public static Task SynchronizeAsync(bool isEnabled)
        {
            if (IsPackagedApp())
                return SetPackagedStartupTaskEnabledAsync(isEnabled);

            SynchronizeUnpackaged(isEnabled);
            return Task.CompletedTask;
        }

        private static void SetUnpackagedEnabled(bool isEnabled)
        {
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

        private static void SynchronizeUnpackaged(bool isEnabled)
        {
            if (!isEnabled)
            {
                SetUnpackagedEnabled(false);
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var registeredCommand = key?.GetValue(ValueName) as string;
            if (IsStartupCommandForExecutable(registeredCommand, Application.ExecutablePath))
                return;

            var legacyRegisteredCommand = key?.GetValue(LegacyValueName) as string;
            if (IsStartupCommandForExecutable(legacyRegisteredCommand, Application.ExecutablePath))
            {
                SetUnpackagedEnabled(true);
                return;
            }

            SetUnpackagedEnabled(true);
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
            var startupTask = await StartupTask.GetAsync(PackagedStartupTaskId).AsTask();
            if (!isEnabled)
            {
                startupTask.Disable();
                return;
            }

            if (startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                return;

            // Windows keeps a Task Manager user opt-out authoritative.
            if (startupTask.State == StartupTaskState.Disabled)
            {
                var state = await startupTask.RequestEnableAsync().AsTask();
                if (state is not StartupTaskState.Enabled and not StartupTaskState.EnabledByPolicy)
                {
                    throw new InvalidOperationException(
                        $"Windows did not enable the startup task. Current state: {state}.");
                }
            }
        }
    }
}
