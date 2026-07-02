using TimePilot.WinForms.Details;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void ApplyUiText()
        {
            Text = UiText.AppName;
            mainMenuController.ApplyText(CreateMainMenuText());

            summaryTab.Text = UiText.Main.SummaryTab;
            detailTab.Text = UiText.Main.DetailTab;
            timelineTab.Text = UiText.Main.TimelineTab;
            summaryPeriodLabel.Text = UiText.Main.Period;
            summarySpecificDateCalendarButton.Text = UiText.Main.Calendar;
            summaryCustomRangeButton.Text = UiText.SummaryPeriod.CustomRangeButton;
            UpdateSummaryCustomRangeLabel();
            RefreshSummaryUsageBarsModeOptions();
            detailDateLabel.Text = UiText.Main.Date;
            detailCalendarButton.Text = UiText.Main.Calendar;
            detailTodayButton.Text = UiText.Main.Today;
            detailRuntimeFilterLabel.Text = UiText.Main.DetailRuntimeFilter;
            runningRuntimeOnlyCheckBox.Text = UiText.Main.RunningOnly;
            detailHelpButton.Text = UiText.Main.DetailHelp;
            detailDescriptionLabel.Text = UiText.Main.DetailDescription;
            detailTrackingDisabledLabel.Text =
                UiText.Main.DetailTrackingDisabledMessage;
            detailTrackingDisabledPreferencesButton.Text =
                UiText.Main.DetailTrackingDisabledOpenPreferences;
            runtimeSegmentTimelineControl.Invalidate();
            runtimeSegmentObservationFilterLabel.Text =
                RuntimeSegmentHelpContentBuilder.GetObservationFilterLabelText();
            runtimeSegmentHelpButton.Text = UiText.Main.DetailHelp;
            runtimeSegmentZoomCoordinator.ApplyText();
            UpdateRuntimeSegmentZoomControls();
            timelineDateLabel.Text = UiText.Main.Date;
            timelineCalendarButton.Text = UiText.Main.Calendar;
            timelineTodayButton.Text = UiText.Main.Today;
            timelineHighlightClearButton.Text = UiText.Main.ClearTimelineHighlight;
            timelineHighlightHintLabel.Text = UiText.Main.TimelineHighlightHint;
            timelineZoomCoordinator.ApplyText();
            timelineHelpButton.Text = UiText.Main.TimelineHelp;
            timelineSelectorCoordinator.ApplyText(settings.UiLanguage);
            timelineOverviewControl.Invalidate();
            UpdateTimelineZoomControls();
            UpdateTimelineHighlightUi();
            RefreshTimelineCategoryBucketOptions();
            RefreshTimelineTypeHighlightOptions();
            RefreshTimelineSystemEventFilterOptions();
            RefreshRuntimeSegmentObservationFilterOptions();

            dailyUsageDateColumn.HeaderText = UiText.Main.Date;
            dailyUsageActiveTimeColumn.HeaderText = UiText.Main.TotalActiveUsageTime;
            dailyUsageTopAppColumn.HeaderText = UiText.Main.TopApp;
            dailyUsageTopAppTimeColumn.HeaderText = UiText.Main.TopAppTime;
            appNameColumn.HeaderText = UiText.Main.App;
            appCategoryColumn.HeaderText = UiText.Main.Category;
            firstStartedAtColumn.HeaderText = UiText.Main.FirstStartedAt;
            lastObservedAtColumn.HeaderText = UiText.Main.LastObservedAt;
            activeUsageTimeColumn.HeaderText = UiText.Main.ActiveUsageTime;
            idleRecordedTimeColumn.HeaderText = UiText.Main.IdleRecordedTime;
            idleRecordedTimeColumn.ToolTipText = UiText.Main.IdleRecordedTimeTooltip;
            usageRatioColumn.HeaderText = UiText.Main.ActiveRatio;
            usageRatioColumn.ToolTipText = UiText.Main.UsageRatioTooltip;
            switchCountColumn.HeaderText = UiText.Main.SwitchCount;
            summaryHighlightHintLabel.Text =
                UiText.Main.SummaryTimelineHighlightHint;

            runtimeAppNameColumn.HeaderText = UiText.Main.App;
            runtimeCategoryColumn.HeaderText = UiText.Main.Category;
            runtimeTrackingTypeColumn.HeaderText = UiText.Main.Type;
            runtimeTrackingTypeColumn.ToolTipText =
                UiText.Main.RuntimeTrackingTypeTooltip;
            runtimeFirstObservedAtColumn.HeaderText = UiText.Main.FirstObservedAt;
            runtimeLastObservedAtColumn.HeaderText = UiText.Main.LastObservedAt;
            runtimeLastObservedAtColumn.ToolTipText =
                UiText.Main.RuntimeLastObservedTooltip;
            runtimeDurationColumn.HeaderText = UiText.Main.Runtime;
            runtimeDurationColumn.ToolTipText = UiText.Main.RuntimeDurationTooltip;
            runtimeActiveUsageColumn.HeaderText = UiText.Main.ActiveUsageTime;
            runtimeIdleRecordedColumn.HeaderText = UiText.Main.IdleRecordedTime;
            runtimeIdleRecordedColumn.ToolTipText =
                UiText.Main.IdleRecordedTimeTooltip;
            runtimeActualUsageRatioColumn.HeaderText = UiText.Main.ActualUsageRatio;
            runtimeActualUsageRatioColumn.ToolTipText =
                UiText.Main.RuntimeActualUsageRatioTooltip;
            runtimeSessionCountColumn.HeaderText = UiText.Main.RuntimeSegmentCount;
            runtimeSessionCountColumn.ToolTipText =
                UiText.Main.RuntimeSegmentCountTooltip;
            runtimeStatusColumn.HeaderText = UiText.Main.Status;
            runtimeStatusColumn.ToolTipText = UiText.Main.RuntimeStatusTooltip;
            runtimeSegmentStartedAtColumn.HeaderText = UiText.Main.Start;
            runtimeSegmentEndedAtColumn.HeaderText = UiText.Main.End;
            runtimeSegmentDurationColumn.HeaderText = UiText.Main.Duration;
            runtimeSegmentStatusColumn.HeaderText = UiText.Main.Status;
            runtimeSegmentObservationTypeColumn.HeaderText =
                UiText.Main.ObservationBasis;
            runtimeSegmentObservationTypeColumn.ToolTipText =
                UiText.Main.RuntimeSegmentObservationTooltip;
            runtimeSegmentProcessIdColumn.HeaderText = UiText.Main.Pid;

            timelineTypeColumn.HeaderText = UiText.Main.Type;
            timelineStartedAtColumn.HeaderText = UiText.Main.Start;
            timelineEndedAtColumn.HeaderText = UiText.Main.End;
            timelineDurationColumn.HeaderText = UiText.Main.Duration;
            timelineDisplayNameColumn.HeaderText = UiText.Main.App;
            timelineCategoryColumn.HeaderText = UiText.Main.Category;

            if (trayMenu.Items.Count > 0)
            {
                if (trayMenu.Items[0] is ToolStripMenuItem openItem)
                    openItem.Text = UiText.Main.OpenWindow;
                if (trayMenu.Items[^1] is ToolStripMenuItem exitItem)
                    exitItem.Text = UiText.Main.Exit;
            }

            trayIcon.Text = UiText.AppName;
            ApplyLocalizedToolTips();
            RefreshDetailRuntimeFilterOptions();
            RefreshSummaryPeriodOptions(DateTime.Today);
            UpdateDetailTrackingDisabledBanner();
            SetDateStatus(detailDateStatusLabel, null);
            SetDateStatus(timelineDateStatusLabel, null);
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void ApplyLocalizedToolTips()
        {
            runtimeCoverageSummaryToolTip.SetToolTip(
                runtimeCoverageSummaryPanel,
                UiText.RuntimeCoverage.Tooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                summarySpecificDateCalendarButton,
                UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                summaryCustomRangeButton,
                UiText.SummaryPeriod.CustomRangeTitle);
            runtimeCoverageSummaryToolTip.SetToolTip(
                detailCalendarButton,
                UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                detailRuntimeFilterComboBox,
                UiText.Main.RuntimeTrackingTypeTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                detailHelpButton,
                UiText.Main.DetailHelpTitle);
            runtimeCoverageSummaryToolTip.SetToolTip(
                runtimeSegmentResetButton,
                RuntimeSegmentHelpContentBuilder.GetResetTooltip());
            runtimeCoverageSummaryToolTip.SetToolTip(
                runtimeSegmentHelpButton,
                RuntimeSegmentHelpContentBuilder.GetHelpTitle());
            runtimeCoverageSummaryToolTip.SetToolTip(
                detailDescriptionLabel,
                UiText.Main.DetailDescription);
            runtimeCoverageSummaryToolTip.SetToolTip(
                timelineCalendarButton,
                UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                timelineHelpButton,
                UiText.Main.TimelineHelpTitle);
            runtimeCoverageSummaryToolTip.SetToolTip(
                timelineHighlightSummaryPanel,
                UiText.Main.TimelineHighlightSummaryTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                timelineHighlightSummaryLabel,
                UiText.Main.TimelineHighlightSummaryTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                summaryIdleAnalysisPanel,
                UiText.Main.SummaryIdleAnalysisTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                summaryIdleAnalysisLabel,
                UiText.Main.SummaryIdleAnalysisTooltip);
        }
    }
}
