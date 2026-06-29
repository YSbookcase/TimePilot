using System.Diagnostics;
using Microsoft.Win32;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void OnShown(object? sender, EventArgs e)
        {
            if (startMinimizedToTray)
            {
                BeginInvoke(() =>
                {
                    HideToTray();
                    ShowProcessRuntimeSafeModeNoticeIfNeeded();
                });
                return;
            }

            BeginInvoke(ShowStartupNotices);
        }

        private void ShowStartupNotices()
        {
            ShowProcessRuntimeSafeModeNoticeIfNeeded();
            ShowStartupPromptIfNeeded();
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

        private void ShowStartupPromptIfNeeded()
        {
            if (settings.StartupPromptShown || startMinimizedToTray || isClosing)
                return;

            var result = CenteredMessageDialog.Show(
                this,
                UiText.Main.StartupPromptMessage,
                UiText.Main.StartupPromptTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            settings.SetStartupPromptResult(result == DialogResult.Yes);
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
