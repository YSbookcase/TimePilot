namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void OnSampleTick(object? sender, EventArgs e)
        {
            var observedAt = DateTimeOffset.UtcNow;
            if (lastSampleTickAt is { } lastTickAt)
            {
                var tickGapMs = (long)(observedAt - lastTickAt).TotalMilliseconds;
                if (tickGapMs >= SampleIntervalMs + 500)
                    ReportPerformanceEvents($"tick-gap {tickGapMs}ms");
            }

            lastSampleTickAt = observedAt;
            var idleThresholdMs = settings.IdleThresholdMs;
            var isIdle = UserIdleChecker.IsIdle(idleThresholdMs);
            var foregroundApp = ForegroundWindowReader.TryGetForegroundApp();
            UpdateTimelineDataVersion(foregroundApp, isIdle);

            storage?.UpdateRuntimeHeartbeat(observedAt);
            idleSessionTracker?.Track(isIdle, foregroundApp, idleThresholdMs, observedAt);
            foregroundSessionTracker?.Track(foregroundApp, isIdle, observedAt);
            _ = TrackProcessRuntimeSessionsAsync(observedAt);

            var idleText = isIdle ? UiText.Main.Idle : UiText.Main.Active;
            statusLabel.Text = foregroundApp is null
                ? $"{UiText.Main.ForegroundPrefix}{UiText.Main.NoForegroundApp} · {idleText}"
                : $"{UiText.Main.ForegroundPrefix}{foregroundApp.DisplayName} · {idleText}";
            SetStatusText(statusLabel.Text);
            RefreshViews(observedAt);
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            var endedAt = DateTimeOffset.UtcNow;
            isClosing = true;
            UnregisterWindowsSystemEventHandlers();
            SaveWindowPlacement();
            sampleTimer.Stop();
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            lock (processRuntimeTrackingLock)
            {
                processRuntimeSessionTracker?.EndCurrentSessions(endedAt);
            }

            RecordWindowsSystemEvent("timepilot-exit", "ApplicationClosed");
            storage?.EndRuntimeSession(endedAt, "normal");
            storage?.Dispose();
            appIconCache.Dispose();
            CloseRecordedDatePickerDropDown();
            headerToolTipForm.Dispose();
            timelineHighlightedRowFont?.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            sampleTimer.Dispose();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (isExplicitExitRequested || e.CloseReason != CloseReason.UserClosing)
                return;

            SaveWindowPlacement();
            e.Cancel = true;
            HideToTray();
        }

        private void ApplySavedWindowPlacement()
        {
            if (settings.WindowLeft is not { } left
                || settings.WindowTop is not { } top
                || settings.WindowWidth is not { } width
                || settings.WindowHeight is not { } height)
                return;

            var bounds = new Rectangle(
                left,
                top,
                Math.Max(width, MinimumSize.Width),
                Math.Max(height, MinimumSize.Height));

            if (!IsWindowBoundsVisible(bounds))
                return;

            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;

            if (settings.WindowMaximized)
                WindowState = FormWindowState.Maximized;
        }

        private void SaveWindowPlacement()
        {
            if (WindowState == FormWindowState.Minimized)
                return;

            var normalBounds = WindowState == FormWindowState.Maximized ? RestoreBounds : Bounds;
            if (normalBounds.Width <= 0 || normalBounds.Height <= 0)
                return;

            settings.SetWindowPlacement(normalBounds, WindowState == FormWindowState.Maximized);
        }

        private void ConfigureTrayIcon()
        {
            var openMenuItem = new ToolStripMenuItem(UiText.Main.OpenWindow);
            openMenuItem.Click += (_, _) => ShowMainWindow();

            var exitTrayMenuItem = new ToolStripMenuItem(UiText.Main.Exit);
            exitTrayMenuItem.Click += (_, _) => ExitApplication();

            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                openMenuItem,
                new ToolStripSeparator(),
                exitTrayMenuItem
            });

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Icon = LoadAppIcon();
            trayIcon.Text = UiText.AppName;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowMainWindow()
        {
            Show();
            ShowInTaskbar = true;
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

            Activate();
        }

        private void ExitApplication()
        {
            isExplicitExitRequested = true;
            Close();
        }
    }
}
