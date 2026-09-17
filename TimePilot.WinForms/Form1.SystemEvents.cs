using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private async void OnShown(object? sender, EventArgs e)
        {
            StartupLaunchDiagnostics.Record("form-shown", new
            {
                StartMinimizedToTray = startMinimizedToTray,
                Visible,
                ShowInTaskbar,
                WindowState = WindowState.ToString()
            });

            try
            {
                var startupState = await WindowsStartupRegistration.GetPackagedStateAsync();
                if (startupState.HasValue)
                {
                    Directory.CreateDirectory(AppDataPaths.DataDirectory);
                    File.WriteAllText(
                        Path.Combine(AppDataPaths.DataDirectory, "startup-task-diagnostic.json"),
                        JsonSerializer.Serialize(new
                        {
                            Timestamp = DateTimeOffset.Now,
                            State = startupState.Value.ToString(),
                            StateValue = (int)startupState.Value,
                            StartWithWindows = settings.StartWithWindows,
                            StartMinimizedToTray = startMinimizedToTray,
                            Executable = Application.ExecutablePath
                        }));
                }
            }
            catch
            {
                // A failed diagnostic must not block the app from opening.
            }

            try
            {
                await WindowsStartupRegistration.SynchronizeAsync(settings.StartWithWindows);
            }
            catch
            {
                // Startup registration should never prevent the app from opening.
            }

            if (startMinimizedToTray)
            {
                HideToTray();
                ShowProcessRuntimeSafeModeNoticeIfNeeded();
                return;
            }

            await ShowStartupNoticesAsync();
        }

        private async Task ShowStartupNoticesAsync()
        {
            ShowProcessRuntimeSafeModeNoticeIfNeeded();
            await ShowStartupPromptIfNeededAsync();
        }

        private void ShowProcessRuntimeSafeModeNoticeIfNeeded()
        {
            if (!processRuntimeSafeModeActivated)
                return;

            if (startMinimizedToTray)
            {
                trayIcon.ShowBalloonTip(
                    8000,
                    UiText.Main.SafeModeTitle,
                    UiText.Main.SafeModeBalloonMessage,
                    ToolTipIcon.Warning);
                return;
            }

            CenteredMessageDialog.Show(
                this,
                UiText.Main.SafeModeMessage,
                UiText.Main.SafeModeTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private async Task ShowStartupPromptIfNeededAsync()
        {
            if (settings.StartupPromptShown || startMinimizedToTray || isClosing)
                return;

            var result = CenteredMessageDialog.Show(
                this,
                UiText.Main.StartupPromptMessage,
                UiText.Main.StartupPromptTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            try
            {
                await settings.SetStartupPromptResultAsync(result == DialogResult.Yes);
            }
            catch (Exception ex)
            {
                settings.MarkStartupPromptShown();
                var reason = settings.UiLanguage == UiLanguage.Korean && ex is StartupTaskStateException blocked
                    ? blocked.State switch
                    {
                        Windows.ApplicationModel.StartupTaskState.DisabledByPolicy =>
                            "Windows 정책이 이 앱의 자동 시작을 차단하고 있습니다. 이 PC의 시작 앱 정책을 확인하세요.",
                        Windows.ApplicationModel.StartupTaskState.DisabledByUser =>
                            "Windows 시작 앱에서 이 앱이 꺼져 있습니다. Windows 시작 앱 설정에서 다시 켜세요.",
                        Windows.ApplicationModel.StartupTaskState.EnabledByPolicy =>
                            "Windows 정책이 이 앱의 자동 시작을 요구하고 있어 앱에서 끌 수 없습니다.",
                        _ => ex.Message
                    }
                    : ex.Message;
                var message = settings.UiLanguage == UiLanguage.English
                    ? $"Windows could not update the startup setting.\n\n{reason}"
                    : $"Windows 시작 앱 설정을 변경하지 못했습니다.\n\n{reason}";
                CenteredMessageDialog.Show(
                    this,
                    message,
                    UiText.Main.StartupPromptTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ApplyProcessRuntimeSafeModeIfNeeded()
        {
            if (storage is null
                || !AppSettings.IsDangerousProcessRuntimeTracking(
                    settings.ProcessRuntimeTrackingEnabled,
                    settings.ProcessRuntimeTrackingScope,
                    settings.ProcessRuntimeSampleIntervalSeconds))
                return;

            if (!storage.HasRecentRepeatedShortUnexpectedRuntimeSessions(
                    SafeModeUnexpectedExitCount,
                    SafeModeShortRuntimeThreshold))
                return;

            settings.DisableProcessRuntimeTrackingForSafeMode();
            processRuntimeSafeModeActivated = true;
        }

        private static DateTimeOffset GetCurrentSystemBootedAt(DateTimeOffset now)
        {
            return now - TimeSpan.FromMilliseconds(Environment.TickCount64);
        }

        private void RegisterWindowsSystemEventHandlers()
        {
            if (systemEventHandlersRegistered)
                return;

            SystemEvents.SessionSwitch += OnSystemSessionSwitch;
            SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;
            SystemEvents.SessionEnding += OnSystemSessionEnding;
            systemEventHandlersRegistered = true;
        }

        private void UnregisterWindowsSystemEventHandlers()
        {
            if (!systemEventHandlersRegistered)
                return;

            SystemEvents.SessionSwitch -= OnSystemSessionSwitch;
            SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;
            SystemEvents.SessionEnding -= OnSystemSessionEnding;
            systemEventHandlersRegistered = false;
        }

        private void OnSystemSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            var eventType = e.Reason switch
            {
                SessionSwitchReason.SessionLock => "lock",
                SessionSwitchReason.SessionUnlock => "unlock",
                SessionSwitchReason.SessionLogon => "logon",
                SessionSwitchReason.SessionLogoff => "logoff",
                SessionSwitchReason.ConsoleConnect => "console-connect",
                SessionSwitchReason.ConsoleDisconnect => "console-disconnect",
                SessionSwitchReason.RemoteConnect => "remote-connect",
                SessionSwitchReason.RemoteDisconnect => "remote-disconnect",
                SessionSwitchReason.SessionRemoteControl => "remote-control",
                _ => "session-switch"
            };

            RecordWindowsSystemEvent(eventType, e.Reason.ToString());
        }

        private void OnSystemPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            var eventType = e.Mode switch
            {
                PowerModes.Suspend => "suspend",
                PowerModes.Resume => "resume",
                PowerModes.StatusChange => "power-status-change",
                _ => "power-mode"
            };

            RecordWindowsSystemEvent(eventType, e.Mode.ToString());
        }

        private void OnSystemSessionEnding(object sender, SessionEndingEventArgs e)
        {
            var eventType = e.Reason == SessionEndReasons.Logoff
                ? "logoff"
                : "system-shutdown";

            RecordWindowsSystemEvent(eventType, $"SessionEnding:{e.Reason}");
        }

        private void RecordWindowsSystemEvent(string eventType, string details)
        {
            if (storage is null)
                return;

            try
            {
                var observedAt = DateTimeOffset.UtcNow;
                storage.RecordSystemEvent(
                    eventType,
                    observedAt,
                    GetCurrentSystemBootedAt(observedAt),
                    details);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to record Windows system event: {ex}");
            }
        }
    }
}
