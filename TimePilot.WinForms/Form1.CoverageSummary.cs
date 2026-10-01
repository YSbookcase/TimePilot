using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void SetRuntimeCoverageSummary(RuntimeCoverageSummary? summary)
        {
            if (summary is null)
            {
                SetRuntimeCoverageSummaryParts(UiText.RuntimeCoverage.NotChecked);
                return;
            }

            SetRuntimeCoverageSummaryParts(summary.SummaryParts);
        }

        private void SetSummaryIdleAnalysis(
            IReadOnlyList<ForegroundUsageSummary> foregroundUsage,
            IdleUsageSummary? idleUsage)
        {
            var activeMs = foregroundUsage.Sum(x => x.ActiveUsageMs);
            var idleMs = idleUsage?.IdleMs ?? 0;
            var totalMs = activeMs + idleMs;
            if (totalMs <= 0)
            {
                summaryIdleAnalysisPanel.Visible = false;
                summaryIdleAnalysisLabel.Text = "";
                return;
            }

            var inputActivityRatio = (double)activeMs / totalMs;
            summaryIdleAnalysisLabel.Text = UiText.Main.SummaryIdleAnalysis(
                RuntimeDiagnosticsMessageBuilder.FormatDuration(activeMs),
                RuntimeDiagnosticsMessageBuilder.FormatDuration(idleMs),
                inputActivityRatio,
                settings.IdleThresholdMinutes);
            summaryIdleAnalysisPanel.Visible = true;
            UpdateSummaryIdleAnalysisPanelHeight();
        }

        private void SetRuntimeCoverageSummaryParts(params string[] parts)
        {
            SetRuntimeCoverageSummaryParts((IEnumerable<string>)parts);
        }

        private void SetRuntimeCoverageSummaryParts(IEnumerable<string> parts)
        {
            var partList = parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();
            runtimeCoverageSummaryPanel.Visible = partList.Count > 0;
            var toolTipText =
                runtimeCoverageSummaryToolTip.GetToolTip(runtimeCoverageSummaryPanel);

            runtimeCoverageSummaryPanel.SuspendLayout();
            try
            {
                while (runtimeCoverageSummaryLabels.Count < partList.Count)
                {
                    var label = new Label
                    {
                        AutoSize = true,
                        Margin = new Padding(0, 4, 14, 0),
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    runtimeCoverageSummaryToolTip.SetToolTip(label, toolTipText);
                    runtimeCoverageSummaryLabels.Add(label);
                    runtimeCoverageSummaryPanel.Controls.Add(label);
                }

                for (var i = 0; i < partList.Count; i++)
                {
                    if (runtimeCoverageSummaryLabels[i].Text != partList[i])
                        runtimeCoverageSummaryLabels[i].Text = partList[i];
                }

                while (runtimeCoverageSummaryLabels.Count > partList.Count)
                {
                    var lastIndex = runtimeCoverageSummaryLabels.Count - 1;
                    var label = runtimeCoverageSummaryLabels[lastIndex];
                    runtimeCoverageSummaryLabels.RemoveAt(lastIndex);
                    runtimeCoverageSummaryPanel.Controls.Remove(label);
                    label.Dispose();
                }

                UpdateRuntimeCoverageSummaryPanelHeight();
            }
            finally
            {
                runtimeCoverageSummaryPanel.ResumeLayout();
            }
        }

        private void UpdateRuntimeCoverageSummaryPanelHeight()
        {
            var preferredHeight = runtimeCoverageSummaryPanel.GetPreferredSize(
                new Size(runtimeCoverageSummaryPanel.Width, 0)).Height;
            runtimeCoverageSummaryPanel.Height =
                Math.Clamp(preferredHeight, 24, 48);
        }

        private void UpdateSummaryIdleAnalysisPanelHeight()
        {
            var preferredHeight = summaryIdleAnalysisPanel.GetPreferredSize(
                new Size(summaryIdleAnalysisPanel.Width, 0)).Height;
            summaryIdleAnalysisPanel.Height = Math.Clamp(preferredHeight, 24, 48);
        }
    }
}
