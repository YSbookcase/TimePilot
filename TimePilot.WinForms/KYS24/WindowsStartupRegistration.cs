using Microsoft.Win32;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

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
                return Task.CompletedTask;

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

        internal static bool IsPackagedApp()
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

        internal static bool IsStartupLaunch(string[] args, ActivationKind? activationKind)
        {
            return args.Contains(TrayStartupArgument)
                || activationKind == ActivationKind.StartupTask;
        }

        private static async Task SetPackagedStartupTaskEnabledAsync(bool isEnabled)
        {
            var startupTask = await StartupTask.GetAsync(PackagedStartupTaskId).AsTask();
            if (!isEnabled)
            {
                if (startupTask.State == StartupTaskState.EnabledByPolicy)
                    throw new StartupTaskStateException(
                        startupTask.State,
                        "Windows policy requires this startup item to remain enabled.");

                if (startupTask.State != StartupTaskState.Enabled)
                    return;

                startupTask.Disable();
                var updatedTask = await StartupTask.GetAsync(PackagedStartupTaskId).AsTask();
                if (updatedTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                    throw new StartupTaskStateException(
                        updatedTask.State,
                        "Windows did not disable this startup item.");
                return;
            }

            if (startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                return;

            if (startupTask.State == StartupTaskState.Disabled)
            {
                var state = await startupTask.RequestEnableAsync().AsTask();
                ThrowIfNotEnabled(state);
                return;
            }

            ThrowIfNotEnabled(startupTask.State);
        }

        internal static async Task<StartupTaskState?> GetPackagedStateAsync()
        {
            if (!IsPackagedApp())
                return null;

            var startupTask = await StartupTask.GetAsync(PackagedStartupTaskId).AsTask();
            return startupTask.State;
        }

        internal static void ThrowIfNotEnabled(StartupTaskState state)
        {
            if (state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy)
                return;

            var reason = state switch
            {
                StartupTaskState.DisabledByUser =>
                    "Windows has disabled this startup item for the current user. Enable it from Windows Startup apps.",
                StartupTaskState.DisabledByPolicy =>
                    "Windows policy prevents this startup item from being enabled. Check the device startup policy or contact the administrator.",
                _ => $"Windows did not enable the startup task. Current state: {state}."
            };

            throw new StartupTaskStateException(state, reason);
        }
    }

    internal sealed class StartupTaskStateException : InvalidOperationException
    {
        public StartupTaskState State { get; }

        public StartupTaskStateException(StartupTaskState state, string message)
            : base(message)
        {
            State = state;
        }
    }
}
