using System.Diagnostics;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class PreferencesForm : Form
    {
        private readonly ComboBox languageComboBox = new();
        private readonly ComboBox idleThresholdComboBox = new();
        private readonly NumericUpDown customIdleThresholdNumeric = new();
        private readonly Label customIdleThresholdUnitLabel = new();
        private readonly CheckBox startWithWindowsCheckBox = new();
        private readonly CheckBox performanceDiagnosticsCheckBox = new();
        private readonly CheckBox processRuntimeTrackingCheckBox = new();
        private readonly ComboBox processRuntimeScopeComboBox = new();
        private readonly Button processRuntimeScopeHelpButton = new();
        private readonly ComboBox processRuntimeIntervalComboBox = new();
        private readonly NumericUpDown customProcessRuntimeIntervalNumeric = new();
        private readonly Label customProcessRuntimeIntervalUnitLabel = new();
        private readonly Label processRuntimeWarningLabel = new();
        private readonly CheckBox automaticBackupCheckBox = new();
        private readonly TextBox automaticBackupDirectoryTextBox = new();
        private readonly Button chooseAutomaticBackupFolderButton = new();
        private readonly NumericUpDown automaticBackupRetentionNumeric = new();
        private readonly Label automaticBackupCopiesLabel = new();
        private readonly Button openDataFolderButton = new();
        private readonly Button deploymentDiagnosticsButton = new();
        private readonly Button clearUsageDataButton = new();
        private readonly LinkLabel sponsorLinkLabel = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();

        public PreferencesForm(AppSettings settings, bool? effectiveStartWithWindows = null)
        {
            IdleThresholdMinutes = settings.IdleThresholdMinutes;
            UiLanguage = settings.UiLanguage;
            StartWithWindows = effectiveStartWithWindows ?? settings.StartWithWindows;
            PerformanceDiagnosticsEnabled = settings.PerformanceDiagnosticsEnabled;
            ProcessRuntimeTrackingEnabled = settings.ProcessRuntimeTrackingEnabled;
            ProcessRuntimeTrackingScope = settings.ProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = settings.ProcessRuntimeSampleIntervalSeconds;
            AutomaticBackupEnabled = settings.AutomaticBackupEnabled;
            AutomaticBackupDirectory = settings.AutomaticBackupDirectory;
            AutomaticBackupRetentionCount = settings.AutomaticBackupRetentionCount;
            AutomaticBackupLastSuccessAt = settings.AutomaticBackupLastSuccessAt;
            AutomaticBackupLastPath = settings.AutomaticBackupLastPath;
            AutomaticBackupLastFailureAt = settings.AutomaticBackupLastFailureAt;
            AutomaticBackupLastError = settings.AutomaticBackupLastError;

            InitializeComponent();
            ConfigureLanguageControls();
            ConfigureIdleThresholdControls();
            ConfigureStartupControls();
            ConfigureProcessRuntimeControls();
            ConfigureAutomaticBackupControls();
        }

        public int IdleThresholdMinutes { get; private set; }

        public UiLanguage UiLanguage { get; private set; }

        public bool StartWithWindows { get; private set; }

        public bool PerformanceDiagnosticsEnabled { get; private set; }

        public bool ProcessRuntimeTrackingEnabled { get; private set; }

        public ProcessRuntimeTrackingScope ProcessRuntimeTrackingScope { get; private set; }

        public int ProcessRuntimeSampleIntervalSeconds { get; private set; }

        public bool ProcessRuntimeRiskAccepted { get; private set; }

        public bool ClearUsageDataRequested { get; private set; }

        public bool AutomaticBackupEnabled { get; private set; }

        public string? AutomaticBackupDirectory { get; private set; }

        public int AutomaticBackupRetentionCount { get; private set; }

        private DateTimeOffset? AutomaticBackupLastSuccessAt { get; }

        private string? AutomaticBackupLastPath { get; }

        private DateTimeOffset? AutomaticBackupLastFailureAt { get; }

        private string? AutomaticBackupLastError { get; }

        private void InitializeComponent()
        {
            var idleThresholdLabel = new Label();
            var languageLabel = new Label();
            var tabControl = new TabControl();
            var generalTabPage = new TabPage();
            var dataTabPage = new TabPage();
            var processRuntimeGroupBox = new GroupBox();
            var processRuntimeScopeLabel = new Label();
            var processRuntimeIntervalLabel = new Label();
            var automaticBackupGroupBox = new GroupBox();
            var automaticBackupFolderLabel = new Label();
            var automaticBackupRetentionLabel = new Label();
            var automaticBackupPrivacyLabel = new Label();
            var automaticBackupStatusLabel = new Label();
            var dataManagementGroupBox = new GroupBox();
            var dataManagementLabel = new Label();

            SuspendLayout();
            generalTabPage.SuspendLayout();
            dataTabPage.SuspendLayout();
            processRuntimeGroupBox.SuspendLayout();
            automaticBackupGroupBox.SuspendLayout();
            dataManagementGroupBox.SuspendLayout();

            tabControl.Location = new Point(10, 10);
            tabControl.Name = "preferencesTabControl";
            tabControl.Size = new Size(450, 490);
            tabControl.TabPages.Add(generalTabPage);
            tabControl.TabPages.Add(dataTabPage);

            generalTabPage.Text = UiText.Preferences.GeneralTab;
            generalTabPage.UseVisualStyleBackColor = true;
            dataTabPage.Text = UiText.Preferences.DataTab;
            dataTabPage.UseVisualStyleBackColor = true;

            languageLabel.AutoSize = true;
            languageLabel.Location = new Point(20, 22);
            languageLabel.Name = "languageLabel";
            languageLabel.Size = new Size(59, 15);
            languageLabel.Text = UiText.Preferences.LanguageLabel;

            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.FormattingEnabled = true;
            languageComboBox.Location = new Point(120, 18);
            languageComboBox.Name = "languageComboBox";
            languageComboBox.Size = new Size(160, 23);

            idleThresholdLabel.AutoSize = true;
            idleThresholdLabel.Location = new Point(20, 58);
            idleThresholdLabel.Name = "idleThresholdLabel";
            idleThresholdLabel.Size = new Size(126, 15);
            idleThresholdLabel.Text = UiText.Preferences.IdleThresholdLabel;

            idleThresholdComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            idleThresholdComboBox.FormattingEnabled = true;
            idleThresholdComboBox.Location = new Point(20, 84);
            idleThresholdComboBox.Name = "idleThresholdComboBox";
            idleThresholdComboBox.Size = new Size(160, 23);
            idleThresholdComboBox.SelectedIndexChanged += OnIdleThresholdSelectionChanged;

            customIdleThresholdNumeric.Location = new Point(196, 84);
            customIdleThresholdNumeric.Maximum = AppSettings.MaxIdleThresholdMinutes;
            customIdleThresholdNumeric.Minimum = AppSettings.MinIdleThresholdMinutes;
            customIdleThresholdNumeric.Name = "customIdleThresholdNumeric";
            customIdleThresholdNumeric.Size = new Size(72, 23);

            customIdleThresholdUnitLabel.AutoSize = true;
            customIdleThresholdUnitLabel.Location = new Point(274, 88);
            customIdleThresholdUnitLabel.Name = "customIdleThresholdUnitLabel";
            customIdleThresholdUnitLabel.Size = new Size(19, 15);
            customIdleThresholdUnitLabel.Text = UiText.Preferences.MinuteUnit;

            startWithWindowsCheckBox.AutoSize = true;
            startWithWindowsCheckBox.Location = new Point(20, 124);
            startWithWindowsCheckBox.Name = "startWithWindowsCheckBox";
            startWithWindowsCheckBox.Size = new Size(178, 19);
            startWithWindowsCheckBox.Text = UiText.Preferences.StartWithWindows;

            performanceDiagnosticsCheckBox.AutoSize = true;
            performanceDiagnosticsCheckBox.Location = new Point(20, 152);
            performanceDiagnosticsCheckBox.Name = "performanceDiagnosticsCheckBox";
            performanceDiagnosticsCheckBox.Size = new Size(112, 19);
            performanceDiagnosticsCheckBox.Text = UiText.Preferences.PerformanceDiagnostics;

            processRuntimeGroupBox.Controls.Add(processRuntimeTrackingCheckBox);
            processRuntimeGroupBox.Controls.Add(processRuntimeScopeLabel);
            processRuntimeGroupBox.Controls.Add(processRuntimeScopeComboBox);
            processRuntimeGroupBox.Controls.Add(processRuntimeScopeHelpButton);
            processRuntimeGroupBox.Controls.Add(processRuntimeIntervalLabel);
            processRuntimeGroupBox.Controls.Add(processRuntimeIntervalComboBox);
            processRuntimeGroupBox.Controls.Add(customProcessRuntimeIntervalNumeric);
            processRuntimeGroupBox.Controls.Add(customProcessRuntimeIntervalUnitLabel);
            processRuntimeGroupBox.Controls.Add(processRuntimeWarningLabel);
            processRuntimeGroupBox.Location = new Point(10, 186);
            processRuntimeGroupBox.Name = "processRuntimeGroupBox";
            processRuntimeGroupBox.Size = new Size(410, 190);
            processRuntimeGroupBox.TabIndex = 4;
            processRuntimeGroupBox.TabStop = false;
            processRuntimeGroupBox.Text = UiText.Preferences.ProcessRuntimeGroup;

            processRuntimeTrackingCheckBox.AutoSize = true;
            processRuntimeTrackingCheckBox.Location = new Point(16, 28);
            processRuntimeTrackingCheckBox.Name = "processRuntimeTrackingCheckBox";
            processRuntimeTrackingCheckBox.Size = new Size(150, 19);
            processRuntimeTrackingCheckBox.Text = UiText.Preferences.ProcessRuntimeTracking;
            processRuntimeTrackingCheckBox.CheckedChanged += OnProcessRuntimeSettingsChanged;

            processRuntimeScopeLabel.AutoSize = true;
            processRuntimeScopeLabel.Location = new Point(16, 62);
            processRuntimeScopeLabel.Name = "processRuntimeScopeLabel";
            processRuntimeScopeLabel.Size = new Size(59, 15);
            processRuntimeScopeLabel.Text = UiText.Preferences.ProcessRuntimeScope;

            processRuntimeScopeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            processRuntimeScopeComboBox.FormattingEnabled = true;
            processRuntimeScopeComboBox.Location = new Point(120, 58);
            processRuntimeScopeComboBox.Name = "processRuntimeScopeComboBox";
            processRuntimeScopeComboBox.Size = new Size(180, 23);
            processRuntimeScopeComboBox.SelectedIndexChanged += OnProcessRuntimeSettingsChanged;

            processRuntimeScopeHelpButton.Location = new Point(306, 58);
            processRuntimeScopeHelpButton.Name = "processRuntimeScopeHelpButton";
            processRuntimeScopeHelpButton.Size = new Size(28, 23);
            processRuntimeScopeHelpButton.Text = UiText.Preferences.ProcessRuntimeScopeHelp;
            processRuntimeScopeHelpButton.UseVisualStyleBackColor = true;
            processRuntimeScopeHelpButton.Click += OnProcessRuntimeScopeHelpButtonClick;

            processRuntimeIntervalLabel.AutoSize = true;
            processRuntimeIntervalLabel.Location = new Point(16, 100);
            processRuntimeIntervalLabel.Name = "processRuntimeIntervalLabel";
            processRuntimeIntervalLabel.Size = new Size(59, 15);
            processRuntimeIntervalLabel.Text = UiText.Preferences.ProcessRuntimeInterval;

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
            customProcessRuntimeIntervalUnitLabel.Text = UiText.Preferences.SecondUnit;

            processRuntimeWarningLabel.ForeColor = Color.Firebrick;
            processRuntimeWarningLabel.Location = new Point(16, 132);
            processRuntimeWarningLabel.Name = "processRuntimeWarningLabel";
            processRuntimeWarningLabel.Size = new Size(376, 42);
            processRuntimeWarningLabel.Text = UiText.Preferences.ProcessRuntimeWarning;

            automaticBackupGroupBox.Controls.Add(automaticBackupCheckBox);
            automaticBackupGroupBox.Controls.Add(automaticBackupFolderLabel);
            automaticBackupGroupBox.Controls.Add(automaticBackupDirectoryTextBox);
            automaticBackupGroupBox.Controls.Add(chooseAutomaticBackupFolderButton);
            automaticBackupGroupBox.Controls.Add(automaticBackupRetentionLabel);
            automaticBackupGroupBox.Controls.Add(automaticBackupRetentionNumeric);
            automaticBackupGroupBox.Controls.Add(automaticBackupCopiesLabel);
            automaticBackupGroupBox.Controls.Add(automaticBackupPrivacyLabel);
            automaticBackupGroupBox.Controls.Add(automaticBackupStatusLabel);
            automaticBackupGroupBox.Location = new Point(10, 12);
            automaticBackupGroupBox.Name = "automaticBackupGroupBox";
            automaticBackupGroupBox.Size = new Size(410, 225);
            automaticBackupGroupBox.TabIndex = 5;
            automaticBackupGroupBox.TabStop = false;
            automaticBackupGroupBox.Text = UiText.Preferences.AutomaticBackupGroup;

            automaticBackupCheckBox.AutoSize = true;
            automaticBackupCheckBox.Location = new Point(16, 27);
            automaticBackupCheckBox.Name = "automaticBackupCheckBox";
            automaticBackupCheckBox.Text = UiText.Preferences.AutomaticBackupEnabled;
            automaticBackupCheckBox.CheckedChanged += OnAutomaticBackupSettingChanged;

            automaticBackupFolderLabel.AutoSize = true;
            automaticBackupFolderLabel.Location = new Point(16, 58);
            automaticBackupFolderLabel.Text = UiText.Preferences.AutomaticBackupFolder;

            automaticBackupDirectoryTextBox.Location = new Point(16, 78);
            automaticBackupDirectoryTextBox.Name = "automaticBackupDirectoryTextBox";
            automaticBackupDirectoryTextBox.ReadOnly = true;
            automaticBackupDirectoryTextBox.Size = new Size(280, 23);

            chooseAutomaticBackupFolderButton.Location = new Point(302, 77);
            chooseAutomaticBackupFolderButton.Name = "chooseAutomaticBackupFolderButton";
            chooseAutomaticBackupFolderButton.Size = new Size(90, 25);
            chooseAutomaticBackupFolderButton.Text = UiText.Preferences.ChooseAutomaticBackupFolder;
            chooseAutomaticBackupFolderButton.Click += OnChooseAutomaticBackupFolderButtonClick;

            automaticBackupRetentionLabel.AutoSize = true;
            automaticBackupRetentionLabel.Location = new Point(16, 116);
            automaticBackupRetentionLabel.Text = UiText.Preferences.AutomaticBackupRetention;

            automaticBackupRetentionNumeric.Location = new Point(120, 112);
            automaticBackupRetentionNumeric.Minimum = AppSettings.MinAutomaticBackupRetentionCount;
            automaticBackupRetentionNumeric.Maximum = AppSettings.MaxAutomaticBackupRetentionCount;
            automaticBackupRetentionNumeric.Size = new Size(64, 23);
            automaticBackupRetentionNumeric.ValueChanged += (_, _) =>
                automaticBackupCopiesLabel.Text = UiText.Preferences.AutomaticBackupCopies(
                    (int)automaticBackupRetentionNumeric.Value);

            automaticBackupCopiesLabel.AutoSize = true;
            automaticBackupCopiesLabel.Location = new Point(194, 116);

            automaticBackupPrivacyLabel.Location = new Point(16, 143);
            automaticBackupPrivacyLabel.Size = new Size(376, 34);
            automaticBackupPrivacyLabel.Text = UiText.Preferences.AutomaticBackupPrivacy;

            automaticBackupStatusLabel.AutoEllipsis = true;
            automaticBackupStatusLabel.ForeColor = SystemColors.GrayText;
            automaticBackupStatusLabel.Location = new Point(16, 181);
            automaticBackupStatusLabel.Size = new Size(376, 32);
            automaticBackupStatusLabel.Text = BuildAutomaticBackupStatusText();

            dataManagementGroupBox.Controls.Add(dataManagementLabel);
            dataManagementGroupBox.Controls.Add(openDataFolderButton);
            dataManagementGroupBox.Controls.Add(deploymentDiagnosticsButton);
            dataManagementGroupBox.Controls.Add(clearUsageDataButton);
            dataManagementGroupBox.Location = new Point(10, 247);
            dataManagementGroupBox.Name = "dataManagementGroupBox";
            dataManagementGroupBox.Size = new Size(410, 108);
            dataManagementGroupBox.TabIndex = 6;
            dataManagementGroupBox.TabStop = false;
            dataManagementGroupBox.Text = UiText.Preferences.DataManagementGroup;

            dataManagementLabel.AutoSize = false;
            dataManagementLabel.Location = new Point(16, 32);
            dataManagementLabel.Name = "dataManagementLabel";
            dataManagementLabel.Size = new Size(170, 30);
            dataManagementLabel.Text = UiText.Preferences.DataManagementDescription;

            openDataFolderButton.Location = new Point(186, 27);
            openDataFolderButton.Name = "openDataFolderButton";
            openDataFolderButton.Size = new Size(92, 27);
            openDataFolderButton.Text = UiText.Preferences.OpenDataFolder;
            openDataFolderButton.Click += OnOpenDataFolderButtonClick;

            deploymentDiagnosticsButton.Location = new Point(186, 64);
            deploymentDiagnosticsButton.Name = "deploymentDiagnosticsButton";
            deploymentDiagnosticsButton.Size = new Size(206, 27);
            deploymentDiagnosticsButton.Text = UiText.Preferences.InstallationInfo;
            deploymentDiagnosticsButton.Click += OnDeploymentDiagnosticsButtonClick;

            clearUsageDataButton.Location = new Point(284, 27);
            clearUsageDataButton.Name = "clearUsageDataButton";
            clearUsageDataButton.Size = new Size(108, 27);
            clearUsageDataButton.Text = UiText.Preferences.ClearUsageData;
            clearUsageDataButton.Click += OnClearUsageDataButtonClick;

            sponsorLinkLabel.AutoSize = true;
            sponsorLinkLabel.Location = new Point(10, 385);
            sponsorLinkLabel.Name = "sponsorLinkLabel";
            sponsorLinkLabel.Size = new Size(120, 15);
            sponsorLinkLabel.TabStop = true;
            sponsorLinkLabel.Text = UiText.Preferences.SponsorLink;
            sponsorLinkLabel.LinkClicked += OnSponsorLinkLabelLinkClicked;

            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(294, 518);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 27);
            okButton.Text = UiText.Common.Save;
            okButton.Click += OnOkButtonClick;

            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(375, 518);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 27);
            cancelButton.Text = UiText.Common.Cancel;

            AcceptButton = okButton;
            CancelButton = cancelButton;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(470, 565);
            generalTabPage.Controls.Add(languageLabel);
            generalTabPage.Controls.Add(languageComboBox);
            generalTabPage.Controls.Add(idleThresholdLabel);
            generalTabPage.Controls.Add(idleThresholdComboBox);
            generalTabPage.Controls.Add(customIdleThresholdNumeric);
            generalTabPage.Controls.Add(customIdleThresholdUnitLabel);
            generalTabPage.Controls.Add(startWithWindowsCheckBox);
            generalTabPage.Controls.Add(performanceDiagnosticsCheckBox);
            generalTabPage.Controls.Add(processRuntimeGroupBox);
            dataTabPage.Controls.Add(automaticBackupGroupBox);
            dataTabPage.Controls.Add(dataManagementGroupBox);
            dataTabPage.Controls.Add(sponsorLinkLabel);
            Controls.Add(tabControl);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PreferencesForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = UiText.Preferences.Title;

            generalTabPage.ResumeLayout(false);
            generalTabPage.PerformLayout();
            dataTabPage.ResumeLayout(false);
            dataTabPage.PerformLayout();
            processRuntimeGroupBox.ResumeLayout(false);
            processRuntimeGroupBox.PerformLayout();
            automaticBackupGroupBox.ResumeLayout(false);
            automaticBackupGroupBox.PerformLayout();
            dataManagementGroupBox.ResumeLayout(false);
            dataManagementGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void ConfigureIdleThresholdControls()
        {
            var idleThresholdOptions = GetIdleThresholdOptions();
            idleThresholdComboBox.DataSource = idleThresholdOptions.ToList();
            idleThresholdComboBox.DisplayMember = nameof(IdleThresholdOption.Label);
            idleThresholdComboBox.ValueMember = nameof(IdleThresholdOption.Minutes);
            customIdleThresholdNumeric.Value = IdleThresholdMinutes;

            var selectedOption = idleThresholdOptions.FirstOrDefault(option => option.Minutes == IdleThresholdMinutes)
                ?? idleThresholdOptions.First(option => option.Minutes is null);
            idleThresholdComboBox.SelectedItem = selectedOption;
            UpdateCustomIdleThresholdVisibility();
        }

        private void ConfigureLanguageControls()
        {
            var options = GetLanguageOptions();
            languageComboBox.DataSource = options.ToList();
            languageComboBox.DisplayMember = nameof(LanguageOption.Label);
            languageComboBox.ValueMember = nameof(LanguageOption.Language);
            languageComboBox.SelectedItem = options.First(option => option.Language == UiLanguage);
        }

        private void ConfigureProcessRuntimeControls()
        {
            processRuntimeTrackingCheckBox.Checked = ProcessRuntimeTrackingEnabled;

            var processRuntimeScopeOptions = new List<ProcessRuntimeScopeOption>
            {
                new(UiText.Preferences.WindowedAppsScope, ProcessRuntimeTrackingScope.WindowedApps),
                new(UiText.Preferences.UserProcessesScope, ProcessRuntimeTrackingScope.UserProcesses),
                new(UiText.Preferences.AllProcessesScope, ProcessRuntimeTrackingScope.AllProcesses)
            };
            processRuntimeScopeComboBox.DataSource = processRuntimeScopeOptions;
            processRuntimeScopeComboBox.DisplayMember = nameof(ProcessRuntimeScopeOption.Label);
            processRuntimeScopeComboBox.ValueMember = nameof(ProcessRuntimeScopeOption.Scope);
            processRuntimeScopeComboBox.SelectedItem =
                processRuntimeScopeOptions.FirstOrDefault(option => option.Scope == ProcessRuntimeTrackingScope)
                ?? processRuntimeScopeOptions.First(option => option.Scope == AppSettings.DefaultProcessRuntimeTrackingScope);

            var processRuntimeIntervalOptions = GetProcessRuntimeIntervalOptions();
            processRuntimeIntervalComboBox.DataSource = processRuntimeIntervalOptions.ToList();
            processRuntimeIntervalComboBox.DisplayMember = nameof(ProcessRuntimeIntervalOption.Label);
            processRuntimeIntervalComboBox.ValueMember = nameof(ProcessRuntimeIntervalOption.Seconds);
            customProcessRuntimeIntervalNumeric.Value = Math.Clamp(
                ProcessRuntimeSampleIntervalSeconds,
                AppSettings.MinProcessRuntimeSampleIntervalSeconds,
                AppSettings.MaxProcessRuntimeSampleIntervalSeconds);

            var selectedOption = processRuntimeIntervalOptions
                .FirstOrDefault(option => option.Seconds == ProcessRuntimeSampleIntervalSeconds)
                ?? processRuntimeIntervalOptions.First(option => option.Seconds is null);
            processRuntimeIntervalComboBox.SelectedItem = selectedOption;
            UpdateProcessRuntimeControls();
        }

        private void ConfigureAutomaticBackupControls()
        {
            automaticBackupCheckBox.Checked = AutomaticBackupEnabled;
            automaticBackupDirectoryTextBox.Text = AutomaticBackupDirectory ?? string.Empty;
            automaticBackupRetentionNumeric.Value = Math.Clamp(
                AutomaticBackupRetentionCount,
                AppSettings.MinAutomaticBackupRetentionCount,
                AppSettings.MaxAutomaticBackupRetentionCount);
            automaticBackupCopiesLabel.Text = UiText.Preferences.AutomaticBackupCopies(
                (int)automaticBackupRetentionNumeric.Value);
            UpdateAutomaticBackupControls();
        }

        private string BuildAutomaticBackupStatusText()
        {
            if (AutomaticBackupLastFailureAt is { } failureAt
                && !string.IsNullOrWhiteSpace(AutomaticBackupLastError)
                && (AutomaticBackupLastSuccessAt is null || failureAt > AutomaticBackupLastSuccessAt))
            {
                return UiText.Preferences.AutomaticBackupLastFailure(failureAt, AutomaticBackupLastError);
            }

            if (AutomaticBackupLastSuccessAt is { } successAt
                && !string.IsNullOrWhiteSpace(AutomaticBackupLastPath))
            {
                return UiText.Preferences.AutomaticBackupLastSuccess(successAt, AutomaticBackupLastPath);
            }

            return UiText.Preferences.AutomaticBackupNeverRun;
        }

        private void OnAutomaticBackupSettingChanged(object? sender, EventArgs e)
        {
            UpdateAutomaticBackupControls();
        }

        private void UpdateAutomaticBackupControls()
        {
            var isEnabled = automaticBackupCheckBox.Checked;
            automaticBackupDirectoryTextBox.Enabled = isEnabled;
            chooseAutomaticBackupFolderButton.Enabled = isEnabled;
            automaticBackupRetentionNumeric.Enabled = isEnabled;
        }

        private void OnChooseAutomaticBackupFolderButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = UiText.Preferences.AutomaticBackupFolderDialogDescription,
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };
            if (Directory.Exists(automaticBackupDirectoryTextBox.Text))
                dialog.InitialDirectory = automaticBackupDirectoryTextBox.Text;

            if (dialog.ShowDialog(this) == DialogResult.OK)
                automaticBackupDirectoryTextBox.Text = dialog.SelectedPath;
        }

        private static IReadOnlyList<IdleThresholdOption> GetIdleThresholdOptions()
        {
            return
            [
                new(UiText.Preferences.Minutes(1), 1),
                new(UiText.Preferences.Minutes(2), 2),
                new(UiText.Preferences.Minutes(5), 5),
                new(UiText.Preferences.Minutes(10), 10),
                new(UiText.Preferences.Minutes(15), 15),
                new(UiText.Preferences.Custom, null)
            ];
        }

        private static IReadOnlyList<LanguageOption> GetLanguageOptions()
        {
            return
            [
                new("한국어", UiLanguage.Korean),
                new("English", UiLanguage.English)
            ];
        }

        private static IReadOnlyList<ProcessRuntimeIntervalOption> GetProcessRuntimeIntervalOptions()
        {
            return
            [
                new(UiText.Preferences.Seconds(10), 10),
                new(UiText.Preferences.Seconds(30), 30),
                new(UiText.Preferences.Seconds(60), 60),
                new(UiText.Preferences.Minutes(5), 300),
                new(UiText.Preferences.Custom, null)
            ];
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
                var directory = AppDataPaths.EnsureDataDirectoryForShell();
                Process.Start(new ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Preferences.DataFolderOpenFailed(ex.Message),
                    UiText.Preferences.DataFolderOpenTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnClearUsageDataButtonClick(object? sender, EventArgs e)
        {
            var result = CenteredMessageDialog.Show(
                this,
                UiText.Preferences.ClearUsageDataMessage,
                UiText.Preferences.ClearUsageDataTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            ClearUsageDataRequested = true;
            clearUsageDataButton.Text = UiText.Preferences.ClearUsageDataPending;
        }

        private void OnDeploymentDiagnosticsButtonClick(object? sender, EventArgs e)
        {
            var snapshot = DeploymentDiagnosticsService.Collect();
            using var dialog = new DeploymentDiagnosticsForm(snapshot, UiLanguage);
            dialog.ShowDialog(this);
        }

        private void OnSponsorLinkLabelLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ExternalLinks.SponsorUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Preferences.SponsorOpenFailed(ex.Message),
                    UiText.Preferences.SponsorLink,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
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

            UiLanguage = languageComboBox.SelectedItem is LanguageOption languageOption
                ? languageOption.Language
                : AppSettings.DefaultUiLanguage;
            StartWithWindows = startWithWindowsCheckBox.Checked;
            PerformanceDiagnosticsEnabled = performanceDiagnosticsCheckBox.Checked;
            ProcessRuntimeTrackingEnabled = processRuntimeTrackingCheckBox.Checked;
            ProcessRuntimeTrackingScope = processRuntimeScopeComboBox.SelectedItem is ProcessRuntimeScopeOption scopeOption
                ? scopeOption.Scope
                : AppSettings.DefaultProcessRuntimeTrackingScope;
            ProcessRuntimeSampleIntervalSeconds = GetSelectedProcessRuntimeIntervalSeconds();
            AutomaticBackupEnabled = automaticBackupCheckBox.Checked;
            AutomaticBackupDirectory = string.IsNullOrWhiteSpace(automaticBackupDirectoryTextBox.Text)
                ? null
                : automaticBackupDirectoryTextBox.Text;
            AutomaticBackupRetentionCount = (int)automaticBackupRetentionNumeric.Value;

            if (AutomaticBackupEnabled && AutomaticBackupDirectory is null)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Preferences.AutomaticBackupFolderRequired,
                    UiText.Preferences.AutomaticBackupGroup,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

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
            processRuntimeScopeHelpButton.Enabled = isEnabled;
            processRuntimeIntervalComboBox.Enabled = isEnabled;
            customProcessRuntimeIntervalNumeric.Enabled = isEnabled;
            customProcessRuntimeIntervalNumeric.Visible = isCustomInterval;
            customProcessRuntimeIntervalUnitLabel.Visible = isCustomInterval;
            processRuntimeWarningLabel.Text = isDangerousSetting
                ? UiText.Preferences.ProcessRuntimeDangerWarning
                : UiText.Preferences.ProcessRuntimeWarning;
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
                    UiText.Preferences.AllProcessesRiskMessage,
                ProcessRuntimeTrackingScope.UserProcesses =>
                    UiText.Preferences.UserProcessesRiskMessage,
                _ =>
                    UiText.Preferences.AnyScopeRiskMessage
            };

            ProcessRuntimeRiskAccepted = ShowCenteredWarning(message) == DialogResult.Yes;
            return ProcessRuntimeRiskAccepted;
        }

        private void OnProcessRuntimeScopeHelpButtonClick(object? sender, EventArgs e)
        {
            var scope = processRuntimeScopeComboBox.SelectedItem is ProcessRuntimeScopeOption scopeOption
                ? scopeOption.Scope
                : AppSettings.DefaultProcessRuntimeTrackingScope;
            var selection = GetProcessRuntimeScopeLabel(scope);
            var description = GetProcessRuntimeScopeDescription(scope);
            var message = UiText.Preferences.ProcessRuntimeScopeHelpCurrentSelection(selection, description)
                + Environment.NewLine
                + Environment.NewLine
                + UiText.Preferences.ProcessRuntimeScopeHelpMessage;

            CenteredMessageDialog.Show(
                this,
                message,
                UiText.Preferences.ProcessRuntimeScopeHelpTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static string GetProcessRuntimeScopeLabel(ProcessRuntimeTrackingScope scope)
        {
            return scope switch
            {
                ProcessRuntimeTrackingScope.UserProcesses => UiText.Preferences.UserProcessesScope,
                ProcessRuntimeTrackingScope.AllProcesses => UiText.Preferences.AllProcessesScope,
                _ => UiText.Preferences.WindowedAppsScope
            };
        }

        private static string GetProcessRuntimeScopeDescription(ProcessRuntimeTrackingScope scope)
        {
            return scope switch
            {
                ProcessRuntimeTrackingScope.UserProcesses => UiText.Preferences.UserProcessesScopeDescription,
                ProcessRuntimeTrackingScope.AllProcesses => UiText.Preferences.AllProcessesScopeDescription,
                _ => UiText.Preferences.WindowedAppsScopeDescription
            };
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
            yesButton.Text = UiText.Common.Save;

            noButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            noButton.DialogResult = DialogResult.No;
            noButton.Location = new Point(367, 160);
            noButton.Name = "noButton";
            noButton.Size = new Size(75, 27);
            noButton.Text = UiText.Common.Cancel;

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
            dialog.Text = UiText.Preferences.AdvancedTrackingTitle;

            dialog.ResumeLayout(false);

            return dialog.ShowDialog(this);
        }

        private sealed record IdleThresholdOption(string Label, int? Minutes);

        private sealed record LanguageOption(string Label, UiLanguage Language);

        private sealed record ProcessRuntimeScopeOption(string Label, ProcessRuntimeTrackingScope Scope);

        private sealed record ProcessRuntimeIntervalOption(string Label, int? Seconds);
    }
}
