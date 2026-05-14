using System.Diagnostics;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class PreferencesForm : Form
    {
        private static readonly IReadOnlyList<IdleThresholdOption> IdleThresholdOptions =
        [
            new("1분", 1),
            new("2분", 2),
            new("5분", 5),
            new("10분", 10),
            new("15분", 15),
            new("사용자 지정", null)
        ];

        private static readonly IReadOnlyList<ProcessRuntimeIntervalOption> ProcessRuntimeIntervalOptions =
        [
            new("10초", 10),
            new("30초", 30),
            new("60초", 60),
            new("5분", 300),
            new("사용자 지정", null)
        ];

        private readonly ComboBox idleThresholdComboBox = new();
        private readonly NumericUpDown customIdleThresholdNumeric = new();
        private readonly Label customIdleThresholdUnitLabel = new();
        private readonly CheckBox startWithWindowsCheckBox = new();
        private readonly CheckBox performanceDiagnosticsCheckBox = new();
        private readonly CheckBox processRuntimeTrackingCheckBox = new();
        private readonly ComboBox processRuntimeScopeComboBox = new();
        private readonly ComboBox processRuntimeIntervalComboBox = new();
        private readonly NumericUpDown customProcessRuntimeIntervalNumeric = new();
        private readonly Label customProcessRuntimeIntervalUnitLabel = new();
        private readonly Label processRuntimeWarningLabel = new();
        private readonly Button openDataFolderButton = new();
        private readonly Button clearUsageDataButton = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();

        public PreferencesForm(AppSettings settings)
        {
            IdleThresholdMinutes = settings.IdleThresholdMinutes;
            StartWithWindows = settings.StartWithWindows;
            PerformanceDiagnosticsEnabled = settings.PerformanceDiagnosticsEnabled;
            ProcessRuntimeTrackingEnabled = settings.ProcessRuntimeTrackingEnabled;
            ProcessRuntimeTrackingScope = settings.ProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = settings.ProcessRuntimeSampleIntervalSeconds;

            InitializeComponent();
            ConfigureIdleThresholdControls();
            ConfigureStartupControls();
            ConfigureProcessRuntimeControls();
        }

        public int IdleThresholdMinutes { get; private set; }

        public bool StartWithWindows { get; private set; }

        public bool PerformanceDiagnosticsEnabled { get; private set; }

        public bool ProcessRuntimeTrackingEnabled { get; private set; }

        public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; private set; }

        public int ProcessRuntimeSampleIntervalSeconds { get; private set; }

        public bool ProcessRuntimeRiskAccepted { get; private set; }

        public bool ClearUsageDataRequested { get; private set; }

        private void InitializeComponent()
        {
            var idleThresholdLabel = new Label();
            var processRuntimeGroupBox = new GroupBox();
            var processRuntimeScopeLabel = new Label();
            var processRuntimeIntervalLabel = new Label();
            var dataManagementGroupBox = new GroupBox();
            var dataManagementLabel = new Label();

            SuspendLayout();
            processRuntimeGroupBox.SuspendLayout();
            dataManagementGroupBox.SuspendLayout();

            idleThresholdLabel.AutoSize = true;
            idleThresholdLabel.Location = new Point(20, 22);
            idleThresholdLabel.Name = "idleThresholdLabel";
            idleThresholdLabel.Size = new Size(126, 15);
            idleThresholdLabel.Text = "유휴 판단 대기시간";

            idleThresholdComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            idleThresholdComboBox.FormattingEnabled = true;
            idleThresholdComboBox.Location = new Point(20, 48);
            idleThresholdComboBox.Name = "idleThresholdComboBox";
            idleThresholdComboBox.Size = new Size(160, 23);
            idleThresholdComboBox.SelectedIndexChanged += OnIdleThresholdSelectionChanged;

            customIdleThresholdNumeric.Location = new Point(196, 48);
            customIdleThresholdNumeric.Maximum = AppSettings.MaxIdleThresholdMinutes;
            customIdleThresholdNumeric.Minimum = AppSettings.MinIdleThresholdMinutes;
            customIdleThresholdNumeric.Name = "customIdleThresholdNumeric";
            customIdleThresholdNumeric.Size = new Size(72, 23);

            customIdleThresholdUnitLabel.AutoSize = true;
            customIdleThresholdUnitLabel.Location = new Point(274, 52);
            customIdleThresholdUnitLabel.Name = "customIdleThresholdUnitLabel";
            customIdleThresholdUnitLabel.Size = new Size(19, 15);
            customIdleThresholdUnitLabel.Text = "분";

            startWithWindowsCheckBox.AutoSize = true;
            startWithWindowsCheckBox.Location = new Point(20, 88);
            startWithWindowsCheckBox.Name = "startWithWindowsCheckBox";
            startWithWindowsCheckBox.Size = new Size(178, 19);
            startWithWindowsCheckBox.Text = "Windows 시작 시 자동 실행";

            performanceDiagnosticsCheckBox.AutoSize = true;
            performanceDiagnosticsCheckBox.Location = new Point(20, 116);
            performanceDiagnosticsCheckBox.Name = "performanceDiagnosticsCheckBox";
            performanceDiagnosticsCheckBox.Size = new Size(112, 19);
            performanceDiagnosticsCheckBox.Text = "성능 진단 표시";

            processRuntimeGroupBox.Controls.Add(processRuntimeTrackingCheckBox);
            processRuntimeGroupBox.Controls.Add(processRuntimeScopeLabel);
            processRuntimeGroupBox.Controls.Add(processRuntimeScopeComboBox);
            processRuntimeGroupBox.Controls.Add(processRuntimeIntervalLabel);
            processRuntimeGroupBox.Controls.Add(processRuntimeIntervalComboBox);
            processRuntimeGroupBox.Controls.Add(customProcessRuntimeIntervalNumeric);
            processRuntimeGroupBox.Controls.Add(customProcessRuntimeIntervalUnitLabel);
            processRuntimeGroupBox.Controls.Add(processRuntimeWarningLabel);
            processRuntimeGroupBox.Location = new Point(20, 150);
            processRuntimeGroupBox.Name = "processRuntimeGroupBox";
            processRuntimeGroupBox.Size = new Size(430, 190);
            processRuntimeGroupBox.TabIndex = 4;
            processRuntimeGroupBox.TabStop = false;
            processRuntimeGroupBox.Text = "백그라운드 앱 추적";

            processRuntimeTrackingCheckBox.AutoSize = true;
            processRuntimeTrackingCheckBox.Location = new Point(16, 28);
            processRuntimeTrackingCheckBox.Name = "processRuntimeTrackingCheckBox";
            processRuntimeTrackingCheckBox.Size = new Size(150, 19);
            processRuntimeTrackingCheckBox.Text = "실행 중 앱 세션 추적";
            processRuntimeTrackingCheckBox.CheckedChanged += OnProcessRuntimeSettingsChanged;

            processRuntimeScopeLabel.AutoSize = true;
            processRuntimeScopeLabel.Location = new Point(16, 62);
            processRuntimeScopeLabel.Name = "processRuntimeScopeLabel";
            processRuntimeScopeLabel.Size = new Size(59, 15);
            processRuntimeScopeLabel.Text = "추적 범위";

            processRuntimeScopeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            processRuntimeScopeComboBox.FormattingEnabled = true;
            processRuntimeScopeComboBox.Location = new Point(120, 58);
            processRuntimeScopeComboBox.Name = "processRuntimeScopeComboBox";
            processRuntimeScopeComboBox.Size = new Size(180, 23);
            processRuntimeScopeComboBox.SelectedIndexChanged += OnProcessRuntimeSettingsChanged;

            processRuntimeIntervalLabel.AutoSize = true;
            processRuntimeIntervalLabel.Location = new Point(16, 100);
            processRuntimeIntervalLabel.Name = "processRuntimeIntervalLabel";
            processRuntimeIntervalLabel.Size = new Size(59, 15);
            processRuntimeIntervalLabel.Text = "확인 주기";

            processRuntimeIntervalComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            processRuntimeIntervalComboBox.FormattingEnabled = true;
            processRuntimeIntervalComboBox.Location = new Point(120, 96);
            processRuntimeIntervalComboBox.Name = "processRuntimeIntervalComboBox";
            processRuntimeIntervalComboBox.Size = new Size(120, 23);
            processRuntimeIntervalComboBox.SelectedIndexChanged += OnProcessRuntimeSettingsChanged;

            customProcessRuntimeIntervalNumeric.Location = new Point(256, 96);
            customProcessRuntimeIntervalNumeric.Maximum = AppSettings.MaxProcessRuntimeSampleIntervalSeconds;
            customProcessRuntimeIntervalNumeric.Minimum = AppSettings.MinProcessRuntimeSampleIntervalSeconds;
            customProcessRuntimeIntervalNumeric.Name = "customProcessRuntimeIntervalNumeric";
            customProcessRuntimeIntervalNumeric.Size = new Size(72, 23);
            customProcessRuntimeIntervalNumeric.ValueChanged += OnProcessRuntimeSettingsChanged;

            customProcessRuntimeIntervalUnitLabel.AutoSize = true;
            customProcessRuntimeIntervalUnitLabel.Location = new Point(334, 100);
            customProcessRuntimeIntervalUnitLabel.Name = "customProcessRuntimeIntervalUnitLabel";
            customProcessRuntimeIntervalUnitLabel.Size = new Size(19, 15);
            customProcessRuntimeIntervalUnitLabel.Text = "초";

            processRuntimeWarningLabel.ForeColor = Color.Firebrick;
            processRuntimeWarningLabel.Location = new Point(16, 132);
            processRuntimeWarningLabel.Name = "processRuntimeWarningLabel";
            processRuntimeWarningLabel.Size = new Size(396, 42);
            processRuntimeWarningLabel.Text = "짧은 확인 주기는 CPU 사용량, 배터리 소모, 저장 데이터 증가를 유발할 수 있습니다.";

            dataManagementGroupBox.Controls.Add(dataManagementLabel);
            dataManagementGroupBox.Controls.Add(openDataFolderButton);
            dataManagementGroupBox.Controls.Add(clearUsageDataButton);
            dataManagementGroupBox.Location = new Point(20, 350);
            dataManagementGroupBox.Name = "dataManagementGroupBox";
            dataManagementGroupBox.Size = new Size(430, 72);
            dataManagementGroupBox.TabIndex = 5;
            dataManagementGroupBox.TabStop = false;
            dataManagementGroupBox.Text = "데이터 관리";

            dataManagementLabel.AutoSize = false;
            dataManagementLabel.Location = new Point(16, 32);
            dataManagementLabel.Name = "dataManagementLabel";
            dataManagementLabel.Size = new Size(170, 30);
            dataManagementLabel.Text = "기록과 설정 저장 위치를 관리합니다.";

            openDataFolderButton.Location = new Point(206, 27);
            openDataFolderButton.Name = "openDataFolderButton";
            openDataFolderButton.Size = new Size(92, 27);
            openDataFolderButton.Text = "폴더 열기";
            openDataFolderButton.Click += OnOpenDataFolderButtonClick;

            clearUsageDataButton.Location = new Point(304, 27);
            clearUsageDataButton.Name = "clearUsageDataButton";
            clearUsageDataButton.Size = new Size(108, 27);
            clearUsageDataButton.Text = "기록 삭제";
            clearUsageDataButton.Click += OnClearUsageDataButtonClick;

            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(294, 446);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 27);
            okButton.Text = "저장";
            okButton.Click += OnOkButtonClick;

            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(375, 446);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 27);
            cancelButton.Text = "취소";

            AcceptButton = okButton;
            CancelButton = cancelButton;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(470, 493);
            Controls.Add(idleThresholdLabel);
            Controls.Add(idleThresholdComboBox);
            Controls.Add(customIdleThresholdNumeric);
            Controls.Add(customIdleThresholdUnitLabel);
            Controls.Add(startWithWindowsCheckBox);
            Controls.Add(performanceDiagnosticsCheckBox);
            Controls.Add(processRuntimeGroupBox);
            Controls.Add(dataManagementGroupBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PreferencesForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "환경 설정";

            processRuntimeGroupBox.ResumeLayout(false);
            processRuntimeGroupBox.PerformLayout();
            dataManagementGroupBox.ResumeLayout(false);
            dataManagementGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void ConfigureIdleThresholdControls()
        {
            idleThresholdComboBox.DataSource = IdleThresholdOptions.ToList();
            idleThresholdComboBox.DisplayMember = nameof(IdleThresholdOption.Label);
            idleThresholdComboBox.ValueMember = nameof(IdleThresholdOption.Minutes);
            customIdleThresholdNumeric.Value = IdleThresholdMinutes;

            var selectedOption = IdleThresholdOptions.FirstOrDefault(option => option.Minutes == IdleThresholdMinutes)
                ?? IdleThresholdOptions.First(option => option.Minutes is null);
            idleThresholdComboBox.SelectedItem = selectedOption;
            UpdateCustomIdleThresholdVisibility();
        }

        private void ConfigureProcessRuntimeControls()
        {
            processRuntimeTrackingCheckBox.Checked = ProcessRuntimeTrackingEnabled;

            processRuntimeScopeComboBox.DataSource = new List<ProcessRuntimeScopeOption>
            {
                new("창이 있는 앱만", ProcessRuntimeTrackingScope.WindowedApps),
                new("모든 사용자 프로세스", ProcessRuntimeTrackingScope.UserProcesses),
                new("모든 프로세스", ProcessRuntimeTrackingScope.AllProcesses)
            };
            processRuntimeScopeComboBox.DisplayMember = nameof(ProcessRuntimeScopeOption.Label);
            processRuntimeScopeComboBox.ValueMember = nameof(ProcessRuntimeScopeOption.Scope);
            processRuntimeScopeComboBox.SelectedItem = processRuntimeScopeComboBox.Items
                .Cast<ProcessRuntimeScopeOption>()
                .First(option => option.Scope == ProcessRuntimeTrackingScope);

            processRuntimeIntervalComboBox.DataSource = ProcessRuntimeIntervalOptions.ToList();
            processRuntimeIntervalComboBox.DisplayMember = nameof(ProcessRuntimeIntervalOption.Label);
            processRuntimeIntervalComboBox.ValueMember = nameof(ProcessRuntimeIntervalOption.Seconds);
            customProcessRuntimeIntervalNumeric.Value = Math.Clamp(
                ProcessRuntimeSampleIntervalSeconds,
                AppSettings.MinProcessRuntimeSampleIntervalSeconds,
                AppSettings.MaxProcessRuntimeSampleIntervalSeconds);

            var selectedOption = ProcessRuntimeIntervalOptions
                .FirstOrDefault(option => option.Seconds == ProcessRuntimeSampleIntervalSeconds)
                ?? ProcessRuntimeIntervalOptions.First(option => option.Seconds is null);
            processRuntimeIntervalComboBox.SelectedItem = selectedOption;
            UpdateProcessRuntimeControls();
        }

        private void ConfigureStartupControls()
        {
            startWithWindowsCheckBox.Checked = StartWithWindows;
            performanceDiagnosticsCheckBox.Checked = PerformanceDiagnosticsEnabled;
        }

        private void OnIdleThresholdSelectionChanged(object? sender, EventArgs e)
        {
            UpdateCustomIdleThresholdVisibility();
        }

        private void OnProcessRuntimeSettingsChanged(object? sender, EventArgs e)
        {
            UpdateProcessRuntimeControls();
        }

        private void OnOpenDataFolderButtonClick(object? sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(AppDataPaths.DataDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppDataPaths.DataDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    $"데이터 폴더를 열 수 없습니다.\n\n{ex.Message}",
                    "데이터 폴더 열기",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnClearUsageDataButtonClick(object? sender, EventArgs e)
        {
            var result = CenteredMessageDialog.Show(
                this,
                "저장을 누르면 앱 사용 기록과 타임라인 기록이 삭제됩니다.\n\n환경 설정과 Windows 시작 시 자동 실행 설정은 유지됩니다.",
                "사용 기록 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            ClearUsageDataRequested = true;
            clearUsageDataButton.Text = "삭제 예정";
        }

        private void OnOkButtonClick(object? sender, EventArgs e)
        {
            if (idleThresholdComboBox.SelectedItem is IdleThresholdOption { Minutes: { } minutes })
            {
                IdleThresholdMinutes = minutes;
            }
            else
            {
                IdleThresholdMinutes = (int)customIdleThresholdNumeric.Value;
            }

            StartWithWindows = startWithWindowsCheckBox.Checked;
            PerformanceDiagnosticsEnabled = performanceDiagnosticsCheckBox.Checked;
            ProcessRuntimeTrackingEnabled = processRuntimeTrackingCheckBox.Checked;
            ProcessRuntimeTrackingScope = processRuntimeScopeComboBox.SelectedItem is ProcessRuntimeScopeOption scopeOption
                ? scopeOption.Scope
                : AppSettings.DefaultProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = GetSelectedProcessRuntimeIntervalSeconds();

            if (!ConfirmAdvancedProcessRuntimeSettings())
            {
                DialogResult = DialogResult.None;
                ProcessRuntimeRiskAccepted = false;
            }
        }

        private void UpdateCustomIdleThresholdVisibility()
        {
            var isCustom = idleThresholdComboBox.SelectedItem is IdleThresholdOption { Minutes: null };
            customIdleThresholdNumeric.Visible = isCustom;
            customIdleThresholdUnitLabel.Visible = isCustom;
        }

        private void UpdateProcessRuntimeControls()
        {
            var isEnabled = processRuntimeTrackingCheckBox.Checked;
            var isCustomInterval =
                processRuntimeIntervalComboBox.SelectedItem is ProcessRuntimeIntervalOption { Seconds: null };
            var intervalSeconds = GetSelectedProcessRuntimeIntervalSeconds();
            var isDangerousSetting = AppSettings.IsDangerousProcessRuntimeTracking(
                isEnabled,
                processRuntimeScopeComboBox.SelectedItem is ProcessRuntimeScopeOption scopeOption
                    ? scopeOption.Scope
                    : AppSettings.DefaultProcessRuntimeTrackingScope,
                intervalSeconds);

            processRuntimeScopeComboBox.Enabled = isEnabled;
            processRuntimeIntervalComboBox.Enabled = isEnabled;
            customProcessRuntimeIntervalNumeric.Enabled = isEnabled;
            customProcessRuntimeIntervalNumeric.Visible = isCustomInterval;
            customProcessRuntimeIntervalUnitLabel.Visible = isCustomInterval;
            processRuntimeWarningLabel.Text = isDangerousSetting
                ? "위험 설정입니다. 반복 비정상 종료가 감지되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼질 수 있습니다."
                : "짧은 확인 주기는 CPU 사용량, 배터리 소모, 저장 데이터 증가를 유발할 수 있습니다.";
            processRuntimeWarningLabel.Visible = isEnabled
                && (isDangerousSetting || intervalSeconds < AppSettings.WarningProcessRuntimeSampleIntervalSeconds);
        }

        private int GetSelectedProcessRuntimeIntervalSeconds()
        {
            if (processRuntimeIntervalComboBox.SelectedItem is ProcessRuntimeIntervalOption { Seconds: { } seconds })
                return seconds;

            return (int)customProcessRuntimeIntervalNumeric.Value;
        }

        private bool ConfirmAdvancedProcessRuntimeSettings()
        {
            ProcessRuntimeRiskAccepted = false;
            if (!AppSettings.IsDangerousProcessRuntimeTracking(
                    ProcessRuntimeTrackingEnabled,
                    ProcessRuntimeTrackingScope,
                    ProcessRuntimeSampleIntervalSeconds))
                return true;

            var message = ProcessRuntimeTrackingScope switch
            {
                ProcessRuntimeTrackingScope.AllProcesses =>
                    "모든 프로세스를 10초 이하 주기로 추적하면 환경에 따라 TimePilot이 멈추거나 비정상 종료될 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?",
                ProcessRuntimeTrackingScope.UserProcesses =>
                    "모든 사용자 프로세스를 5초 이하 주기로 추적하면 CPU 사용량과 저장 데이터가 크게 증가할 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?",
                _ =>
                    "3초 이하 확인 주기는 추적 범위와 관계없이 시스템 부하와 저장 데이터 증가를 유발할 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?"
            };

            ProcessRuntimeRiskAccepted = ShowCenteredWarning(message) == DialogResult.Yes;
            return ProcessRuntimeRiskAccepted;
        }

        private DialogResult ShowCenteredWarning(string message)
        {
            using var dialog = new Form();
            var messageLabel = new Label();
            var yesButton = new Button();
            var noButton = new Button();

            dialog.SuspendLayout();

            messageLabel.Location = new Point(18, 18);
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new Size(424, 128);
            messageLabel.Text = message;

            yesButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            yesButton.DialogResult = DialogResult.Yes;
            yesButton.Location = new Point(286, 160);
            yesButton.Name = "yesButton";
            yesButton.Size = new Size(75, 27);
            yesButton.Text = "저장";

            noButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            noButton.DialogResult = DialogResult.No;
            noButton.Location = new Point(367, 160);
            noButton.Name = "noButton";
            noButton.Size = new Size(75, 27);
            noButton.Text = "취소";

            dialog.AcceptButton = yesButton;
            dialog.CancelButton = noButton;
            dialog.AutoScaleMode = AutoScaleMode.Font;
            dialog.ClientSize = new Size(460, 205);
            dialog.Controls.Add(messageLabel);
            dialog.Controls.Add(yesButton);
            dialog.Controls.Add(noButton);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.Name = "advancedTrackingWarningDialog";
            dialog.ShowIcon = false;
            dialog.ShowInTaskbar = false;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.Text = "고급 추적 설정";

            dialog.ResumeLayout(false);

            return dialog.ShowDialog(this);
        }

        private sealed record IdleThresholdOption(string Label, int? Minutes);

        private sealed record ProcessRuntimeScopeOption(string Label, ProcessRuntimeTrackingScope Scope);

        private sealed record ProcessRuntimeIntervalOption(string Label, int? Seconds);
    }
}
