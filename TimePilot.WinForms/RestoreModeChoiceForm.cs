using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class RestoreModeChoiceForm : Form
    {
        private readonly bool isEnglish;
        private readonly UiLanguage language;
        private readonly Func<IProgress<RestoreAnalysisProgress>, DataBackupDetailedComparison?>? detailedComparisonLoader;
        private readonly Func<bool> confirmReanalysis;
        private DataBackupRestorePlan plan;
        private readonly Label planLabel = new();
        private Label? keepLaterDescriptionLabel;
        private Label? importBackupDescriptionLabel;
        private readonly Button detailedAnalysisButton = new();
        private readonly Button keepLaterPreviewButton = new();
        private readonly Button continueButton = new();
        private readonly Button cancelButton = new();
        private readonly Label analysisStatusLabel = new();
        private readonly ProgressBar analysisProgressBar = new();
        private bool detailedAnalysisAttempted;
        private bool isAnalyzing;
        private bool isConfirmingReanalysis;
        private int analysisGeneration;

        public RestoreModeChoiceForm(
            UiLanguage language,
            DataBackupRestorePlan plan,
            Func<IProgress<RestoreAnalysisProgress>, DataBackupDetailedComparison?>? detailedComparisonLoader = null,
            Func<bool>? confirmReanalysis = null)
        {
            isEnglish = language == UiLanguage.English;
            this.language = language;
            this.plan = plan;
            this.detailedComparisonLoader = detailedComparisonLoader;
            this.confirmReanalysis = confirmReanalysis ?? (() => CenteredMessageDialog.Show(this,
                isEnglish
                    ? "Analysis results already exist. Run the analysis again? Results will be refreshed and this may take time. No data will be restored."
                    : "이미 분석한 결과가 있습니다. 다시 분석할까요? 결과를 새로 계산하며 시간이 걸릴 수 있습니다. 데이터 복원은 실행하지 않습니다.",
                isEnglish ? "Confirm reanalysis" : "재분석 확인", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK);
            InitializeComponent();
        }

        public RestoreModeChoice Choice { get; private set; } = RestoreModeChoice.Cancel;

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = isEnglish ? "Choose restore method" : "복원 방식 선택";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 670);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font(Font, FontStyle.Bold),
                Text = isEnglish ? "Select how to apply this backup." : "이 백업을 어떻게 적용할지 선택하세요."
            };

            planLabel.AutoSize = false;
            planLabel.Dock = DockStyle.Top;
            planLabel.Height = 120;
            planLabel.Text = BuildPlanText();

            var modesPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            modesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            modesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            modesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            modesPanel.Controls.Add(CreateModePanel(
                isEnglish ? "Return completely to the backup state" : "백업 상태로 완전히 되돌리기",
                isEnglish
                    ? "The fastest restore method. Replace the current database and settings with this backup. Records, apps, categories, and settings changed after the backup can disappear from the current data."
                    : "가장 빠른 복원 방식입니다. 현재 사용 기록 데이터베이스와 설정을 백업 파일의 내용으로 대체합니다. 백업 이후 쌓인 기록, 앱 정보, 분류, 설정 변경은 현재 데이터에서 사라질 수 있습니다.",
                enabled: true,
                selected: true,
                descriptionLabel: out _), 0, 0);
            modesPanel.Controls.Add(CreateModePanel(
                isEnglish ? "Return to backup state and keep later records" : "백업 상태로 되돌리되 이후 기록 유지",
                BuildKeepLaterRecordsDescription(),
                enabled: false,
                selected: false,
                descriptionLabel: out var keepDescription), 0, 1);
            keepLaterDescriptionLabel = keepDescription;
            modesPanel.Controls.Add(CreateModePanel(
                isEnglish ? "Import backup records into current data" : "현재 데이터에 백업 기록 가져오기",
                BuildImportBackupRecordsDescription(),
                enabled: false,
                selected: false,
                descriptionLabel: out var importDescription), 0, 2);
            importBackupDescriptionLabel = importDescription;

            var warningLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Name = "fullRestoreWarning",
                Text = UiText.Main.DataRestoreWarningFor(language)
            };

            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            continueButton.Name = "continueFullRestore";
            continueButton.Text = isEnglish ? "Continue full restore" : "전체 복원 계속";
            continueButton.AutoSize = true;
            continueButton.DialogResult = DialogResult.OK;
            continueButton.Click += (_, _) => Choice = RestoreModeChoice.FullReplace;

            cancelButton.Text = isEnglish ? "Cancel" : "취소";
            cancelButton.Name = "cancelRestore";
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Click += (_, _) => Choice = RestoreModeChoice.Cancel;

            detailedAnalysisButton.Text = AnalysisButtonText();
            detailedAnalysisButton.Name = "analyzeRestore";
            detailedAnalysisButton.AutoSize = true;
            detailedAnalysisButton.Enabled = detailedComparisonLoader is not null;
            detailedAnalysisButton.Click += OnDetailedAnalysisButtonClick;
            keepLaterPreviewButton.Name = "keepLaterPreview";
            keepLaterPreviewButton.Text = isEnglish ? "Keep-later preview" : "이후 기록 유지 미리보기";
            keepLaterPreviewButton.AutoSize = true;
            keepLaterPreviewButton.Enabled = plan.DetailedComparison?.KeepLaterPreview is not null;
            keepLaterPreviewButton.Click += (_, _) =>
            {
                if (plan.DetailedComparison?.KeepLaterPreview is not { } preview) return;
                using var form = new KeepLaterRestorePreviewForm(preview, language, plan.DetailedComparison.Preparation);
                form.ShowDialog(this);
            };

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(continueButton);
            buttonPanel.Controls.Add(detailedAnalysisButton);
            buttonPanel.Controls.Add(keepLaterPreviewButton);

            AcceptButton = continueButton;
            CancelButton = cancelButton;

            root.Controls.Add(titleLabel, 0, 0);
            root.Controls.Add(planLabel, 0, 1);
            root.Controls.Add(modesPanel, 0, 2);
            root.Controls.Add(warningLabel, 0, 3);
            var analysisPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            analysisPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            analysisPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            analysisPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            analysisStatusLabel.Name = "restoreAnalysisStatus";
            analysisStatusLabel.Dock = DockStyle.Fill;
            analysisStatusLabel.Text = isEnglish ? "Select Detailed analysis to compare records. No data will be changed."
                : "상세 분석을 누르면 기록을 비교합니다. 데이터는 변경하지 않습니다.";
            analysisProgressBar.Name = "restoreAnalysisProgress";
            analysisProgressBar.Dock = DockStyle.Fill;
            analysisProgressBar.Style = ProgressBarStyle.Marquee;
            analysisProgressBar.MarqueeAnimationSpeed = 30;
            analysisProgressBar.Visible = false;
            analysisPanel.Controls.Add(analysisStatusLabel, 0, 0);
            analysisPanel.Controls.Add(analysisProgressBar, 0, 1);
            root.Controls.Add(analysisPanel, 0, 4);
            root.Controls.Add(buttonPanel, 0, 5);
            Controls.Add(root);

            ResumeLayout(false);
        }

        private Panel CreateModePanel(
            string title,
            string description,
            bool enabled,
            bool selected,
            out Label descriptionLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                Enabled = enabled
            };

            var radioButton = new RadioButton
            {
                AutoSize = true,
                Checked = selected,
                Enabled = enabled,
                Text = title,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(8, 8)
            };

            descriptionLabel = new Label
            {
                AutoSize = false,
                Enabled = enabled,
                Location = new Point(30, 34),
                Size = new Size(640, 44),
                Text = description
            };

            panel.Controls.Add(radioButton);
            panel.Controls.Add(descriptionLabel);
            return panel;
        }

        private string BuildPlanText()
        {
            var backupCounts = plan.BackupCounts;
            if (isEnglish)
            {
                var createdAt = plan.CreatedAt is { } value
                    ? $"\nBackup created at: {value.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                    : "";

                return $"This full-restore backup includes the usage database{(plan.HasSettings ? ", settings" : "")}{(plan.LogCount > 0 ? $", and {plan.LogCount} log file(s)" : "")}."
                    + createdAt
                    + $"\nBackup records: usage records {backupCounts.TotalUsageRecords:N0}, apps {backupCounts.Apps:N0}"
                    + BuildDetailedComparisonText();
            }
            else
            {
                var createdAt = plan.CreatedAt is { } value
                    ? $"\n백업 생성 시각: {value.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                    : "";

                return $"이 전체 복원 백업에는 사용 기록 데이터베이스{(plan.HasSettings ? ", 설정" : "")}{(plan.LogCount > 0 ? $", 로그 {plan.LogCount}개" : "")}가 포함되어 있습니다."
                    + createdAt
                    + $"\n백업 기록: 사용 기록 {backupCounts.TotalUsageRecords:N0}개, 앱 {backupCounts.Apps:N0}개"
                    + BuildDetailedComparisonText();
            }
        }

        private string BuildDetailedComparisonText()
        {
            if (plan.DetailedComparison is not { } comparison)
            {
                if (detailedAnalysisAttempted)
                {
                    return isEnglish
                        ? "\nCurrent-data comparison is unavailable. The backup file itself was validated."
                        : "\n현재 데이터와의 비교는 사용할 수 없습니다. 백업 파일 자체 검증은 완료했습니다.";
                }

                return isEnglish
                    ? "\nDetailed analysis is for checking future merge/import restore options."
                    : "\n상세 분석은 향후 병합/가져오기 복원 판단용 참고 정보입니다.";
            }

            return isEnglish
                ? $"\nOverlap comparison only (not a cutoff restore plan): current nonoverlap {comparison.CurrentRecordsWithoutBackupOverlap:N0}, backup nonoverlap {comparison.BackupRecordsWithoutCurrentOverlap:N0}."
                : $"\n겹침 비교 참고(기준 시각 복원 계획 아님): 현재 비겹침 {comparison.CurrentRecordsWithoutBackupOverlap:N0}개, 백업 비겹침 {comparison.BackupRecordsWithoutCurrentOverlap:N0}개.";
        }

        private string BuildKeepLaterRecordsDescription()
        {
            if (plan.DetailedComparison?.KeepLaterPreview is { } preview)
            {
                if (preview.ReferenceAt is null)
                    return isEnglish ? "Preview only. No reference time could be determined; review the preview. Applying is not supported."
                        : "기준 시각을 결정할 수 없습니다. 미리보기를 검토하세요. 적용은 아직 지원하지 않습니다.";
                return isEnglish
                    ? $"Preview only; applying is not supported. Current before/after-reference candidates: {preview.CurrentBefore}/{preview.CurrentAfter}; crossing/open records require review."
                    : $"미리보기만 제공하며 적용은 아직 지원하지 않습니다. 현재 기준 이전/이후 후보 {preview.CurrentBefore}/{preview.CurrentAfter}개. 경계·진행 중 기록은 검토가 필요합니다.";
            }
            if (plan.DetailedComparison is not { } comparison)
            {
                return isEnglish
                    ? "Planned later. This needs merge policy decisions before it can be enabled."
                    : "향후 지원 예정입니다. 병합 정책 결정이 먼저 필요합니다.";
            }

            return isEnglish
                ? $"Planned later. Current records without backup overlap: {comparison.CurrentRecordsWithoutBackupOverlap:N0}; overlapping current records: {comparison.CurrentRecordsOverlappingBackup:N0}."
                : $"향후 지원 예정입니다. 백업과 겹치지 않는 현재 기록 {comparison.CurrentRecordsWithoutBackupOverlap:N0}개, 겹치는 현재 기록 {comparison.CurrentRecordsOverlappingBackup:N0}개.";
        }

        private string BuildImportBackupRecordsDescription()
        {
            if (plan.DetailedComparison is not { } comparison)
            {
                return isEnglish
                    ? "Planned later. This keeps current settings and imports records using current data as the base."
                    : "향후 지원 예정입니다. 현재 설정을 유지하고 현재 데이터를 기준으로 백업 기록을 가져오는 방식입니다.";
            }

            return isEnglish
                ? $"Planned later. Backup records without current overlap: {comparison.BackupRecordsWithoutCurrentOverlap:N0}; overlapping backup records: {comparison.BackupRecordsOverlappingCurrent:N0}."
                : $"향후 지원 예정입니다. 현재 데이터와 겹치지 않는 백업 기록 {comparison.BackupRecordsWithoutCurrentOverlap:N0}개, 겹치는 백업 기록 {comparison.BackupRecordsOverlappingCurrent:N0}개.";
        }

        private async void OnDetailedAnalysisButtonClick(object? sender, EventArgs e)
            => await AnalyzeAsync();

        internal async Task AnalyzeAsync()
        {
            if (detailedComparisonLoader is null || isAnalyzing || isConfirmingReanalysis)
                return;

            if (plan.DetailedComparison is not null)
            {
                isConfirmingReanalysis = true;
                try
                {
                    if (!confirmReanalysis()) return;
                }
                finally { isConfirmingReanalysis = false; }
                if (IsDisposed) return;
            }

            isAnalyzing = true;
            var generation = ++analysisGeneration;
            continueButton.Enabled = false;
            cancelButton.Enabled = false;
            keepLaterPreviewButton.Enabled = false;
            detailedAnalysisButton.Enabled = false;
            detailedAnalysisButton.Text = isEnglish ? "Analyzing..." : "분석 중...";
            analysisProgressBar.Visible = true;
            analysisProgressBar.Style = ProgressBarStyle.Marquee;
            analysisProgressBar.Value = 0;
            analysisStatusLabel.Text = isEnglish
                ? "Analyzing backup and current records... Large databases may take time. No data is being restored."
                : "백업과 현재 기록을 분석 중입니다... 기록이 많으면 시간이 걸릴 수 있습니다. 복원은 실행하지 않습니다.";
            try
            {
                var progress = new Progress<RestoreAnalysisProgress>(value =>
                {
                    if (!IsDisposed && isAnalyzing && generation == analysisGeneration) ShowAnalysisProgress(value);
                });
                var comparison = await Task.Run(() => detailedComparisonLoader(progress));
                if (IsDisposed) return;
                detailedAnalysisAttempted = true;
                plan = plan with { DetailedComparison = comparison };
                RefreshPlanText();
                analysisStatusLabel.Text = comparison is null
                    ? (isEnglish ? "Analysis unavailable. No data was changed. You can retry."
                        : "분석 결과를 만들지 못했습니다. 데이터는 변경하지 않았습니다. 다시 시도할 수 있습니다.")
                    : comparison.KeepLaterPreview is null
                        ? (isEnglish ? "Overlap analysis completed; keep-later preview unavailable. No data was changed.\nResults reflect the last analysis. Reanalyze to refresh them."
                            : "겹침 분석 완료. 이후 기록 유지 미리보기는 사용할 수 없습니다. 데이터는 변경하지 않았습니다.\n결과는 마지막 분석 당시 기준입니다. 갱신하려면 재분석을 누르세요.")
                        : (isEnglish ? "Analysis completed. Open Keep-later preview. No data was changed.\nResults reflect the last analysis. Reanalyze to refresh them."
                            : "분석 완료. 이후 기록 유지 미리보기에서 결과를 확인하세요. 데이터는 변경하지 않았습니다.\n결과는 마지막 분석 당시 기준입니다. 갱신하려면 재분석을 누르세요.");
            }
            catch
            {
                if (IsDisposed) return;
                detailedAnalysisAttempted = true;
                plan = plan with { DetailedComparison = null };
                RefreshPlanText();
                analysisStatusLabel.Text = isEnglish ? "Analysis failed. No data was changed. You can retry."
                    : "분석 실패. 데이터는 변경하지 않았습니다. 다시 시도할 수 있습니다.";
            }
            finally
            {
                isAnalyzing = false;
                if (!IsDisposed)
                {
                    analysisProgressBar.Visible = false;
                    detailedAnalysisButton.Text = AnalysisButtonText();
                    detailedAnalysisButton.Enabled = true;
                    continueButton.Enabled = true;
                    cancelButton.Enabled = true;
                    keepLaterPreviewButton.Enabled = plan.DetailedComparison?.KeepLaterPreview is not null;
                }
            }
        }

        private string AnalysisButtonText() => plan.DetailedComparison is not null
            ? (isEnglish ? "Reanalyze" : "재분석")
            : detailedAnalysisAttempted
                ? (isEnglish ? "Retry analysis" : "분석 다시 시도")
                : (isEnglish ? "Detailed analysis" : "상세 분석");

        private void ShowAnalysisProgress(RestoreAnalysisProgress progress)
        {
            var stage = progress.Stage switch
            {
                RestoreAnalysisStage.ExtractBackup => isEnglish ? "Extracting backup" : "백업 추출 중",
                RestoreAnalysisStage.SnapshotCurrent => isEnglish ? "Creating current DB snapshot" : "현재 DB 스냅샷 생성 중",
                RestoreAnalysisStage.ReadBackup => isEnglish ? "Reading backup records" : "백업 기록 조회 중",
                RestoreAnalysisStage.ReadCurrent => isEnglish ? "Reading current records" : "현재 기록 조회 중",
                RestoreAnalysisStage.Compare => isEnglish ? "Comparing record intervals" : "기록 구간 비교 중",
                RestoreAnalysisStage.PrepareMapping => isEnglish ? "Checking identity mapping and snapshot hashes" : "앱·분류 연결과 스냅샷 해시 점검 중",
                _ => isEnglish ? "Classifying records by reference time" : "기준 시각별 기록 집계 중"
            };
            var step = (int)progress.Stage + 1;
            var stages = Enum.GetValues<RestoreAnalysisStage>().Length;
            var unitText = "";
            if (progress.Total is > 0)
            {
                var completed = Math.Clamp(progress.Completed, 0, progress.Total.Value);
                analysisProgressBar.Style = ProgressBarStyle.Continuous;
                analysisProgressBar.Maximum = progress.Total.Value;
                analysisProgressBar.Value = completed;
                unitText = isEnglish ? $" | Table passes {completed}/{progress.Total}" : $" | 테이블 처리 {completed}/{progress.Total}";
            }
            else analysisProgressBar.Style = ProgressBarStyle.Marquee;
            analysisStatusLabel.Text = isEnglish
                ? $"Step {step}/{stages}: {stage}{unitText}\nThe bar shows this stage, not overall time remaining. No data is being restored."
                : $"{step}/{stages}단계: {stage}{unitText}\n막대는 현재 단계의 처리량이며 남은 시간 비율이 아닙니다. 복원은 실행하지 않습니다.";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isAnalyzing && e.CloseReason is CloseReason.UserClosing or CloseReason.FormOwnerClosing) e.Cancel = true;
            base.OnFormClosing(e);
        }

        private void RefreshPlanText()
        {
            planLabel.Text = BuildPlanText();
            if (keepLaterDescriptionLabel is not null)
                keepLaterDescriptionLabel.Text = BuildKeepLaterRecordsDescription();
            if (importBackupDescriptionLabel is not null)
                importBackupDescriptionLabel.Text = BuildImportBackupRecordsDescription();
        }
    }

    internal enum RestoreModeChoice
    {
        Cancel,
        FullReplace
    }
}
