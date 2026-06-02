using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class RestoreModeChoiceForm : Form
    {
        private readonly bool isEnglish;
        private readonly DataBackupRestorePlan plan;
        private readonly Button continueButton = new();
        private readonly Button cancelButton = new();

        public RestoreModeChoiceForm(UiLanguage language, DataBackupRestorePlan plan)
        {
            isEnglish = language == UiLanguage.English;
            this.plan = plan;
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
            ClientSize = new Size(680, 500);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 238));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font(Font, FontStyle.Bold),
                Text = isEnglish ? "Select how to apply this backup." : "이 백업을 어떻게 적용할지 선택하세요."
            };

            var planLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 116,
                Text = BuildPlanText()
            };

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
                    ? "Replace the current database and settings with this backup. Records collected after the backup can disappear from the current data."
                    : "현재 사용 기록 데이터베이스와 설정을 백업 파일의 내용으로 대체합니다. 백업 이후 현재 PC에서 쌓인 기록은 현재 데이터에서 사라질 수 있습니다.",
                enabled: true,
                selected: true), 0, 0);
            modesPanel.Controls.Add(CreateModePanel(
                isEnglish ? "Return to backup state and keep later records" : "백업 상태로 되돌리되 이후 기록 유지",
                isEnglish
                    ? "Planned later. This needs a merge policy for app IDs, categories, settings, and overlapping periods."
                    : "향후 지원 예정입니다. 앱 ID, 분류, 설정, 겹치는 기간을 어떻게 병합할지 정책이 먼저 필요합니다.",
                enabled: false,
                selected: false), 0, 1);
            modesPanel.Controls.Add(CreateModePanel(
                isEnglish ? "Import backup records into current data" : "현재 데이터에 백업 기록 가져오기",
                isEnglish
                    ? "Planned later. This keeps current settings and imports records using the current database as the base."
                    : "향후 지원 예정입니다. 현재 설정을 유지하고 현재 데이터베이스를 기준으로 백업 기록을 가져오는 방식입니다.",
                enabled: false,
                selected: false), 0, 2);

            var warningLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = UiText.Main.DataRestoreWarning
            };

            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            continueButton.Text = isEnglish ? "Continue" : "계속";
            continueButton.AutoSize = true;
            continueButton.DialogResult = DialogResult.OK;
            continueButton.Click += (_, _) => Choice = RestoreModeChoice.FullReplace;

            cancelButton.Text = isEnglish ? "Cancel" : "취소";
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Click += (_, _) => Choice = RestoreModeChoice.Cancel;

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(continueButton);

            AcceptButton = continueButton;
            CancelButton = cancelButton;

            root.Controls.Add(titleLabel, 0, 0);
            root.Controls.Add(planLabel, 0, 1);
            root.Controls.Add(modesPanel, 0, 2);
            root.Controls.Add(warningLabel, 0, 3);
            root.Controls.Add(buttonPanel, 0, 4);
            Controls.Add(root);

            ResumeLayout(false);
        }

        private Panel CreateModePanel(string title, string description, bool enabled, bool selected)
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

            var descriptionLabel = new Label
            {
                AutoSize = false,
                Enabled = enabled,
                Location = new Point(30, 34),
                Size = new Size(600, 38),
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
                    + "\nDetailed comparison with current data will be handled in a later step.";
            }
            else
            {
                var createdAt = plan.CreatedAt is { } value
                    ? $"\n백업 생성 시각: {value.ToLocalTime():yyyy-MM-dd HH:mm:ss}"
                    : "";

                return $"이 전체 복원 백업에는 사용 기록 데이터베이스{(plan.HasSettings ? ", 설정" : "")}{(plan.LogCount > 0 ? $", 로그 {plan.LogCount}개" : "")}가 포함되어 있습니다."
                    + createdAt
                    + $"\n백업 기록: 사용 기록 {backupCounts.TotalUsageRecords:N0}개, 앱 {backupCounts.Apps:N0}개"
                    + "\n현재 데이터와의 상세 비교는 후속 단계에서 처리합니다.";
            }
        }
    }

    internal enum RestoreModeChoice
    {
        Cancel,
        FullReplace
    }
}
