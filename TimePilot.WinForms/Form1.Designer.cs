namespace TimePilot.WinForms
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mainMenuStrip = new MenuStrip();
            fileMenuItem = new ToolStripMenuItem();
            exportCsvMenuItem = new ToolStripMenuItem();
            exportRawDataMenuItem = new ToolStripMenuItem();
            exitMenuItem = new ToolStripMenuItem();
            settingsMenuItem = new ToolStripMenuItem();
            preferencesMenuItem = new ToolStripMenuItem();
            helpMenuItem = new ToolStripMenuItem();
            runtimeDiagnosticsMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            statusLabel = new Label();
            mainTabs = new TabControl();
            summaryTab = new TabPage();
            summaryPeriodPanel = new FlowLayoutPanel();
            summaryPeriodLabel = new Label();
            summaryPeriodComboBox = new ComboBox();
            summarySpecificDatePicker = new DateTimePicker();
            summarySpecificDateCalendarButton = new Button();
            summaryHighlightHintLabel = new Label();
            runtimeCoverageSummaryPanel = new FlowLayoutPanel();
            summaryIdleAnalysisPanel = new FlowLayoutPanel();
            summaryIdleAnalysisLabel = new Label();
            runtimeCoverageSummaryToolTip = new ToolTip(components);
            usageGrid = new BufferedDataGridView();
            dailyUsageTrendGrid = new BufferedDataGridView();
            dailyUsageDateColumn = new DataGridViewTextBoxColumn();
            dailyUsageActiveTimeColumn = new DataGridViewTextBoxColumn();
            dailyUsageTopAppColumn = new DataGridViewTextBoxColumn();
            dailyUsageTopAppTimeColumn = new DataGridViewTextBoxColumn();
            appIconColumn = new DataGridViewImageColumn();
            appNameColumn = new DataGridViewTextBoxColumn();
            appCategoryColumn = new DataGridViewTextBoxColumn();
            firstStartedAtColumn = new DataGridViewTextBoxColumn();
            lastObservedAtColumn = new DataGridViewTextBoxColumn();
            activeUsageTimeColumn = new DataGridViewTextBoxColumn();
            usageRatioColumn = new DataGridViewTextBoxColumn();
            switchCountColumn = new DataGridViewTextBoxColumn();
            detailTab = new TabPage();
            detailTrackingDisabledPanel = new Panel();
            detailTrackingDisabledLabel = new Label();
            detailTrackingDisabledPreferencesButton = new Button();
            detailFilterPanel = new Panel();
            detailDateLabel = new Label();
            detailDatePicker = new DateTimePicker();
            detailCalendarButton = new Button();
            detailPreviousDateButton = new Button();
            detailNextDateButton = new Button();
            detailTodayButton = new Button();
            detailDateStatusLabel = new Label();
            detailRuntimeFilterLabel = new Label();
            detailRuntimeFilterComboBox = new ComboBox();
            runningRuntimeOnlyCheckBox = new CheckBox();
            detailHelpButton = new Button();
            detailDescriptionLabel = new Label();
            detailSplitContainer = new SplitContainer();
            runtimeGrid = new BufferedDataGridView();
            runtimeAppIconColumn = new DataGridViewImageColumn();
            runtimeAppNameColumn = new DataGridViewTextBoxColumn();
            runtimeCategoryColumn = new DataGridViewTextBoxColumn();
            runtimeTrackingTypeColumn = new DataGridViewTextBoxColumn();
            runtimeFirstObservedAtColumn = new DataGridViewTextBoxColumn();
            runtimeLastObservedAtColumn = new DataGridViewTextBoxColumn();
            runtimeDurationColumn = new DataGridViewTextBoxColumn();
            runtimeActiveUsageColumn = new DataGridViewTextBoxColumn();
            runtimeActualUsageRatioColumn = new DataGridViewTextBoxColumn();
            runtimeSessionCountColumn = new DataGridViewTextBoxColumn();
            runtimeStatusColumn = new DataGridViewTextBoxColumn();
            runtimeSegmentsGrid = new BufferedDataGridView();
            runtimeSegmentStartedAtColumn = new DataGridViewTextBoxColumn();
            runtimeSegmentEndedAtColumn = new DataGridViewTextBoxColumn();
            runtimeSegmentDurationColumn = new DataGridViewTextBoxColumn();
            runtimeSegmentStatusColumn = new DataGridViewTextBoxColumn();
            runtimeSegmentObservationTypeColumn = new DataGridViewTextBoxColumn();
            runtimeSegmentProcessIdColumn = new DataGridViewTextBoxColumn();
            timelineTab = new TabPage();
            timelineDatePanel = new FlowLayoutPanel();
            timelineDateLabel = new Label();
            timelineDatePicker = new DateTimePicker();
            timelineCalendarButton = new Button();
            timelinePreviousDateButton = new Button();
            timelineNextDateButton = new Button();
            timelineTodayButton = new Button();
            timelineDateStatusLabel = new Label();
            timelineHighlightHintLabel = new Label();
            timelineHighlightLabel = new Label();
            timelineHighlightClearButton = new Button();
            timelineZoomPanel = new FlowLayoutPanel();
            timelineZoomRangeLabel = new Label();
            timelineZoomOutButton = new Button();
            timelineZoomInButton = new Button();
            timelineZoomPreviousButton = new Button();
            timelineZoomNextButton = new Button();
            timelineZoomResetButton = new Button();
            timelineHelpButton = new Button();
            timelineCategoryBucketLabel = new Label();
            timelineCategoryBucketComboBox = new ComboBox();
            timelineHighlightSummaryPanel = new FlowLayoutPanel();
            timelineHighlightSummaryLabel = new Label();
            timelineZoomScrollBar = new HScrollBar();
            timelineOverviewControl = new TimelineOverviewControl();
            timelineGrid = new BufferedDataGridView();
            timelineTypeColumn = new DataGridViewTextBoxColumn();
            timelineStartedAtColumn = new DataGridViewTextBoxColumn();
            timelineEndedAtColumn = new DataGridViewTextBoxColumn();
            timelineDurationColumn = new DataGridViewTextBoxColumn();
            timelineAppIconColumn = new DataGridViewImageColumn();
            timelineDisplayNameColumn = new DataGridViewTextBoxColumn();
            mainMenuStrip.SuspendLayout();
            mainTabs.SuspendLayout();
            summaryTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)usageGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dailyUsageTrendGrid).BeginInit();
            detailTab.SuspendLayout();
            detailTrackingDisabledPanel.SuspendLayout();
            detailFilterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)detailSplitContainer).BeginInit();
            detailSplitContainer.Panel1.SuspendLayout();
            detailSplitContainer.Panel2.SuspendLayout();
            detailSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)runtimeGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)runtimeSegmentsGrid).BeginInit();
            timelineTab.SuspendLayout();
            timelineDatePanel.SuspendLayout();
            timelineZoomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)timelineGrid).BeginInit();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenuItem, settingsMenuItem, helpMenuItem });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(720, 24);
            mainMenuStrip.TabIndex = 0;
            // 
            // fileMenuItem
            // 
            fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exportCsvMenuItem, exportRawDataMenuItem, exitMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new Size(43, 20);
            fileMenuItem.Text = UiText.Main.FileMenu;
            // 
            // exportCsvMenuItem
            // 
            exportCsvMenuItem.Name = "exportCsvMenuItem";
            exportCsvMenuItem.Size = new Size(180, 22);
            exportCsvMenuItem.Text = UiText.Main.ExportCsv;
            exportCsvMenuItem.Click += OnExportCsvMenuItemClick;
            //
            // exportRawDataMenuItem
            //
            exportRawDataMenuItem.Name = "exportRawDataMenuItem";
            exportRawDataMenuItem.Size = new Size(180, 22);
            exportRawDataMenuItem.Text = UiText.Main.ExportRawData;
            exportRawDataMenuItem.Click += OnExportRawDataMenuItemClick;
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new Size(180, 22);
            exitMenuItem.Text = UiText.Main.Exit;
            exitMenuItem.Click += OnExitMenuItemClick;
            // 
            // settingsMenuItem
            // 
            settingsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { preferencesMenuItem });
            settingsMenuItem.Name = "settingsMenuItem";
            settingsMenuItem.Size = new Size(43, 20);
            settingsMenuItem.Text = UiText.Main.SettingsMenu;
            // 
            // preferencesMenuItem
            // 
            preferencesMenuItem.Name = "preferencesMenuItem";
            preferencesMenuItem.Size = new Size(146, 22);
            preferencesMenuItem.Text = UiText.Main.Preferences;
            preferencesMenuItem.Click += OnPreferencesMenuItemClick;
            // 
            // helpMenuItem
            // 
            helpMenuItem.DropDownItems.AddRange(new ToolStripItem[] { runtimeDiagnosticsMenuItem, aboutMenuItem });
            helpMenuItem.Name = "helpMenuItem";
            helpMenuItem.Size = new Size(55, 20);
            helpMenuItem.Text = UiText.Main.HelpMenu;
            //
            // runtimeDiagnosticsMenuItem
            //
            runtimeDiagnosticsMenuItem.Name = "runtimeDiagnosticsMenuItem";
            runtimeDiagnosticsMenuItem.Size = new Size(180, 22);
            runtimeDiagnosticsMenuItem.Text = UiText.Main.RuntimeDiagnostics;
            runtimeDiagnosticsMenuItem.Click += OnRuntimeDiagnosticsMenuItemClick;
            // 
            // aboutMenuItem
            // 
            aboutMenuItem.Name = "aboutMenuItem";
            aboutMenuItem.Size = new Size(180, 22);
            aboutMenuItem.Text = UiText.Main.About;
            aboutMenuItem.Click += OnAboutMenuItemClick;
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Location = new Point(0, 24);
            statusLabel.Name = "statusLabel";
            statusLabel.Padding = new Padding(10, 0, 0, 0);
            statusLabel.Size = new Size(720, 32);
            statusLabel.TabIndex = 1;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // mainTabs
            // 
            mainTabs.Controls.Add(summaryTab);
            mainTabs.Controls.Add(detailTab);
            mainTabs.Controls.Add(timelineTab);
            mainTabs.Dock = DockStyle.Fill;
            mainTabs.Location = new Point(0, 56);
            mainTabs.Name = "mainTabs";
            mainTabs.SelectedIndex = 0;
            mainTabs.Size = new Size(720, 424);
            mainTabs.TabIndex = 2;
            // 
            // summaryTab
            // 
            summaryTab.Controls.Add(usageGrid);
            summaryTab.Controls.Add(dailyUsageTrendGrid);
            summaryTab.Controls.Add(summaryIdleAnalysisPanel);
            summaryTab.Controls.Add(runtimeCoverageSummaryPanel);
            summaryTab.Controls.Add(summaryPeriodPanel);
            summaryTab.Location = new Point(4, 24);
            summaryTab.Name = "summaryTab";
            summaryTab.Padding = new Padding(3);
            summaryTab.Size = new Size(712, 420);
            summaryTab.TabIndex = 0;
            summaryTab.Text = UiText.Main.SummaryTab;
            summaryTab.UseVisualStyleBackColor = true;
            // 
            // summaryPeriodPanel
            // 
            summaryPeriodPanel.Controls.Add(summaryPeriodLabel);
            summaryPeriodPanel.Controls.Add(summaryPeriodComboBox);
            summaryPeriodPanel.Controls.Add(summarySpecificDatePicker);
            summaryPeriodPanel.Controls.Add(summarySpecificDateCalendarButton);
            summaryPeriodPanel.Controls.Add(summaryHighlightHintLabel);
            summaryPeriodPanel.Dock = DockStyle.Top;
            summaryPeriodPanel.Location = new Point(3, 3);
            summaryPeriodPanel.Name = "summaryPeriodPanel";
            summaryPeriodPanel.Padding = new Padding(8, 4, 8, 2);
            summaryPeriodPanel.Size = new Size(706, 36);
            summaryPeriodPanel.TabIndex = 3;
            summaryPeriodPanel.WrapContents = false;
            // 
            // summaryPeriodLabel
            // 
            summaryPeriodLabel.AutoSize = true;
            summaryPeriodLabel.Location = new Point(11, 10);
            summaryPeriodLabel.Margin = new Padding(0, 6, 8, 0);
            summaryPeriodLabel.Name = "summaryPeriodLabel";
            summaryPeriodLabel.Size = new Size(31, 15);
            summaryPeriodLabel.TabIndex = 0;
            summaryPeriodLabel.Text = UiText.Main.Period;
            // 
            // summaryPeriodComboBox
            // 
            summaryPeriodComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            summaryPeriodComboBox.FormattingEnabled = true;
            summaryPeriodComboBox.Location = new Point(50, 7);
            summaryPeriodComboBox.Name = "summaryPeriodComboBox";
            summaryPeriodComboBox.Size = new Size(220, 23);
            summaryPeriodComboBox.TabIndex = 1;
            summaryPeriodComboBox.SelectedIndexChanged += OnSummaryPeriodComboBoxSelectedIndexChanged;
            // 
            // summarySpecificDatePicker
            // 
            summarySpecificDatePicker.Format = DateTimePickerFormat.Short;
            summarySpecificDatePicker.Location = new Point(282, 7);
            summarySpecificDatePicker.Name = "summarySpecificDatePicker";
            summarySpecificDatePicker.ShowUpDown = true;
            summarySpecificDatePicker.Size = new Size(112, 23);
            summarySpecificDatePicker.TabIndex = 2;
            summarySpecificDatePicker.Visible = false;
            summarySpecificDatePicker.ValueChanged += OnSummarySpecificDatePickerValueChanged;
            //
            // summarySpecificDateCalendarButton
            //
            summarySpecificDateCalendarButton.Location = new Point(400, 7);
            summarySpecificDateCalendarButton.Name = "summarySpecificDateCalendarButton";
            summarySpecificDateCalendarButton.Size = new Size(52, 23);
            summarySpecificDateCalendarButton.TabIndex = 3;
            summarySpecificDateCalendarButton.Text = UiText.Main.Calendar;
            summarySpecificDateCalendarButton.UseVisualStyleBackColor = true;
            summarySpecificDateCalendarButton.Visible = false;
            summarySpecificDateCalendarButton.Click += OnSummarySpecificDateCalendarButtonClick;
            //
            // summaryHighlightHintLabel
            //
            summaryHighlightHintLabel.AutoSize = true;
            summaryHighlightHintLabel.ForeColor = SystemColors.GrayText;
            summaryHighlightHintLabel.Location = new Point(458, 10);
            summaryHighlightHintLabel.Margin = new Padding(8, 6, 4, 0);
            summaryHighlightHintLabel.Name = "summaryHighlightHintLabel";
            summaryHighlightHintLabel.Size = new Size(0, 15);
            summaryHighlightHintLabel.TabIndex = 4;
            summaryHighlightHintLabel.Text = UiText.Main.SummaryTimelineHighlightHint;
            //
            // runtimeCoverageSummaryPanel
            // 
            runtimeCoverageSummaryPanel.Dock = DockStyle.Top;
            runtimeCoverageSummaryPanel.Location = new Point(3, 39);
            runtimeCoverageSummaryPanel.Name = "runtimeCoverageSummaryPanel";
            runtimeCoverageSummaryPanel.Padding = new Padding(8, 4, 8, 4);
            runtimeCoverageSummaryPanel.Size = new Size(706, 48);
            runtimeCoverageSummaryPanel.TabIndex = 2;
            runtimeCoverageSummaryPanel.Visible = false;
            runtimeCoverageSummaryPanel.WrapContents = true;
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeCoverageSummaryPanel, "");
            // 
            // summaryIdleAnalysisPanel
            //
            summaryIdleAnalysisPanel.Controls.Add(summaryIdleAnalysisLabel);
            summaryIdleAnalysisPanel.Dock = DockStyle.Top;
            summaryIdleAnalysisPanel.Location = new Point(3, 87);
            summaryIdleAnalysisPanel.Name = "summaryIdleAnalysisPanel";
            summaryIdleAnalysisPanel.Padding = new Padding(8, 4, 8, 4);
            summaryIdleAnalysisPanel.Size = new Size(706, 32);
            summaryIdleAnalysisPanel.TabIndex = 4;
            summaryIdleAnalysisPanel.Visible = false;
            summaryIdleAnalysisPanel.WrapContents = true;
            //
            // summaryIdleAnalysisLabel
            //
            summaryIdleAnalysisLabel.AutoSize = true;
            summaryIdleAnalysisLabel.ForeColor = SystemColors.ControlText;
            summaryIdleAnalysisLabel.Location = new Point(11, 10);
            summaryIdleAnalysisLabel.Margin = new Padding(0, 6, 4, 0);
            summaryIdleAnalysisLabel.Name = "summaryIdleAnalysisLabel";
            summaryIdleAnalysisLabel.Size = new Size(0, 15);
            summaryIdleAnalysisLabel.TabIndex = 0;
            //
            // usageGrid
            // 
            usageGrid.AllowUserToAddRows = false;
            usageGrid.AllowUserToDeleteRows = false;
            usageGrid.AllowUserToOrderColumns = true;
            usageGrid.AllowUserToResizeRows = false;
            usageGrid.AutoGenerateColumns = false;
            usageGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            usageGrid.BackgroundColor = SystemColors.Window;
            usageGrid.BorderStyle = BorderStyle.None;
            usageGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            usageGrid.Columns.AddRange(new DataGridViewColumn[] { appIconColumn, appNameColumn, appCategoryColumn, firstStartedAtColumn, lastObservedAtColumn, activeUsageTimeColumn, usageRatioColumn, switchCountColumn });
            usageGrid.Dock = DockStyle.Fill;
            usageGrid.Location = new Point(3, 39);
            usageGrid.MultiSelect = false;
            usageGrid.Name = "usageGrid";
            usageGrid.ReadOnly = true;
            usageGrid.RowHeadersVisible = false;
            usageGrid.ScrollBars = ScrollBars.Both;
            usageGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            usageGrid.Size = new Size(706, 378);
            usageGrid.TabIndex = 1;
            usageGrid.ColumnHeaderMouseClick += OnUsageGridColumnHeaderMouseClick;
            // 
            // dailyUsageTrendGrid
            // 
            dailyUsageTrendGrid.AllowUserToAddRows = false;
            dailyUsageTrendGrid.AllowUserToDeleteRows = false;
            dailyUsageTrendGrid.AllowUserToResizeRows = false;
            dailyUsageTrendGrid.AutoGenerateColumns = false;
            dailyUsageTrendGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dailyUsageTrendGrid.BackgroundColor = SystemColors.Window;
            dailyUsageTrendGrid.BorderStyle = BorderStyle.None;
            dailyUsageTrendGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dailyUsageTrendGrid.Columns.AddRange(new DataGridViewColumn[] { dailyUsageDateColumn, dailyUsageActiveTimeColumn, dailyUsageTopAppColumn, dailyUsageTopAppTimeColumn });
            dailyUsageTrendGrid.Dock = DockStyle.Bottom;
            dailyUsageTrendGrid.Location = new Point(3, 289);
            dailyUsageTrendGrid.MultiSelect = false;
            dailyUsageTrendGrid.Name = "dailyUsageTrendGrid";
            dailyUsageTrendGrid.ReadOnly = true;
            dailyUsageTrendGrid.RowHeadersVisible = false;
            dailyUsageTrendGrid.ScrollBars = ScrollBars.Both;
            dailyUsageTrendGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dailyUsageTrendGrid.Size = new Size(706, 128);
            dailyUsageTrendGrid.TabIndex = 4;
            // 
            // dailyUsageDateColumn
            // 
            dailyUsageDateColumn.DataPropertyName = "DateText";
            dailyUsageDateColumn.HeaderText = UiText.Main.Date;
            dailyUsageDateColumn.MinimumWidth = 110;
            dailyUsageDateColumn.Name = "dailyUsageDateColumn";
            dailyUsageDateColumn.ReadOnly = true;
            dailyUsageDateColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            dailyUsageDateColumn.Width = 120;
            // 
            // dailyUsageActiveTimeColumn
            // 
            dailyUsageActiveTimeColumn.DataPropertyName = "ActiveUsageTimeText";
            dailyUsageActiveTimeColumn.HeaderText = UiText.Main.TotalActiveUsageTime;
            dailyUsageActiveTimeColumn.MinimumWidth = 130;
            dailyUsageActiveTimeColumn.Name = "dailyUsageActiveTimeColumn";
            dailyUsageActiveTimeColumn.ReadOnly = true;
            dailyUsageActiveTimeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            dailyUsageActiveTimeColumn.Width = 150;
            // 
            // dailyUsageTopAppColumn
            // 
            dailyUsageTopAppColumn.DataPropertyName = "TopAppName";
            dailyUsageTopAppColumn.HeaderText = UiText.Main.TopApp;
            dailyUsageTopAppColumn.MinimumWidth = 180;
            dailyUsageTopAppColumn.Name = "dailyUsageTopAppColumn";
            dailyUsageTopAppColumn.ReadOnly = true;
            dailyUsageTopAppColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            dailyUsageTopAppColumn.Width = 220;
            // 
            // dailyUsageTopAppTimeColumn
            // 
            dailyUsageTopAppTimeColumn.DataPropertyName = "TopAppUsageTimeText";
            dailyUsageTopAppTimeColumn.HeaderText = UiText.Main.TopAppTime;
            dailyUsageTopAppTimeColumn.MinimumWidth = 120;
            dailyUsageTopAppTimeColumn.Name = "dailyUsageTopAppTimeColumn";
            dailyUsageTopAppTimeColumn.ReadOnly = true;
            dailyUsageTopAppTimeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            dailyUsageTopAppTimeColumn.Width = 130;
            // 
            // appIconColumn
            // 
            appIconColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            appIconColumn.DataPropertyName = "AppIcon";
            appIconColumn.HeaderText = "";
            appIconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            appIconColumn.MinimumWidth = 36;
            appIconColumn.Name = "appIconColumn";
            appIconColumn.ReadOnly = true;
            appIconColumn.Width = 36;
            // 
            // appNameColumn
            // 
            appNameColumn.DataPropertyName = "AppName";
            appNameColumn.HeaderText = UiText.Main.App;
            appNameColumn.MinimumWidth = 180;
            appNameColumn.Name = "appNameColumn";
            appNameColumn.ReadOnly = true;
            appNameColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            appNameColumn.Width = 220;
            //
            // appCategoryColumn
            //
            appCategoryColumn.DataPropertyName = "CategoryText";
            appCategoryColumn.HeaderText = UiText.Main.Category;
            appCategoryColumn.MinimumWidth = 100;
            appCategoryColumn.Name = "appCategoryColumn";
            appCategoryColumn.ReadOnly = true;
            appCategoryColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            appCategoryColumn.Width = 120;
            // 
            // firstStartedAtColumn
            // 
            firstStartedAtColumn.DataPropertyName = "FirstStartedAtText";
            firstStartedAtColumn.HeaderText = UiText.Main.FirstStartedAt;
            firstStartedAtColumn.MinimumWidth = 120;
            firstStartedAtColumn.Name = "firstStartedAtColumn";
            firstStartedAtColumn.ReadOnly = true;
            firstStartedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            firstStartedAtColumn.Width = 145;
            // 
            // lastObservedAtColumn
            // 
            lastObservedAtColumn.DataPropertyName = "LastObservedAtText";
            lastObservedAtColumn.HeaderText = UiText.Main.LastObservedAt;
            lastObservedAtColumn.MinimumWidth = 120;
            lastObservedAtColumn.Name = "lastObservedAtColumn";
            lastObservedAtColumn.ReadOnly = true;
            lastObservedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            lastObservedAtColumn.Width = 145;
            // 
            // activeUsageTimeColumn
            // 
            activeUsageTimeColumn.DataPropertyName = "ActiveUsageTimeText";
            activeUsageTimeColumn.HeaderText = UiText.Main.ActiveUsageTime;
            activeUsageTimeColumn.MinimumWidth = 120;
            activeUsageTimeColumn.Name = "activeUsageTimeColumn";
            activeUsageTimeColumn.ReadOnly = true;
            activeUsageTimeColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            activeUsageTimeColumn.Width = 130;
            // 
            // usageRatioColumn
            // 
            usageRatioColumn.DataPropertyName = "UsageRatioText";
            usageRatioColumn.HeaderText = UiText.Main.ActiveRatio;
            usageRatioColumn.MinimumWidth = 80;
            usageRatioColumn.Name = "usageRatioColumn";
            usageRatioColumn.ReadOnly = true;
            usageRatioColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            usageRatioColumn.ToolTipText = UiText.Main.UsageRatioTooltip;
            usageRatioColumn.Width = 90;
            // 
            // switchCountColumn
            // 
            switchCountColumn.DataPropertyName = "SwitchCountText";
            switchCountColumn.HeaderText = UiText.Main.SwitchCount;
            switchCountColumn.MinimumWidth = 90;
            switchCountColumn.Name = "switchCountColumn";
            switchCountColumn.ReadOnly = true;
            switchCountColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            switchCountColumn.Width = 100;
            //
            // detailTab
            //
            detailTab.Controls.Add(detailSplitContainer);
            detailTab.Controls.Add(detailTrackingDisabledPanel);
            detailTab.Controls.Add(detailFilterPanel);
            detailTab.Location = new Point(4, 24);
            detailTab.Name = "detailTab";
            detailTab.Padding = new Padding(3);
            detailTab.Size = new Size(712, 420);
            detailTab.TabIndex = 1;
            detailTab.Text = UiText.Main.DetailTab;
            detailTab.UseVisualStyleBackColor = true;
            //
            // detailTrackingDisabledPanel
            //
            detailTrackingDisabledPanel.BackColor = Color.FromArgb(255, 248, 220);
            detailTrackingDisabledPanel.Controls.Add(detailTrackingDisabledLabel);
            detailTrackingDisabledPanel.Controls.Add(detailTrackingDisabledPreferencesButton);
            detailTrackingDisabledPanel.Dock = DockStyle.Top;
            detailTrackingDisabledPanel.Location = new Point(3, 85);
            detailTrackingDisabledPanel.Name = "detailTrackingDisabledPanel";
            detailTrackingDisabledPanel.Padding = new Padding(8, 6, 8, 6);
            detailTrackingDisabledPanel.Size = new Size(706, 54);
            detailTrackingDisabledPanel.TabIndex = 2;
            detailTrackingDisabledPanel.Visible = false;
            //
            // detailTrackingDisabledLabel
            //
            detailTrackingDisabledLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            detailTrackingDisabledLabel.Location = new Point(11, 7);
            detailTrackingDisabledLabel.Name = "detailTrackingDisabledLabel";
            detailTrackingDisabledLabel.Size = new Size(560, 40);
            detailTrackingDisabledLabel.TabIndex = 0;
            detailTrackingDisabledLabel.Text = UiText.Main.DetailTrackingDisabledMessage;
            detailTrackingDisabledLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // detailTrackingDisabledPreferencesButton
            //
            detailTrackingDisabledPreferencesButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            detailTrackingDisabledPreferencesButton.Location = new Point(577, 15);
            detailTrackingDisabledPreferencesButton.Name = "detailTrackingDisabledPreferencesButton";
            detailTrackingDisabledPreferencesButton.Size = new Size(118, 25);
            detailTrackingDisabledPreferencesButton.TabIndex = 1;
            detailTrackingDisabledPreferencesButton.Text = UiText.Main.DetailTrackingDisabledOpenPreferences;
            detailTrackingDisabledPreferencesButton.UseVisualStyleBackColor = true;
            detailTrackingDisabledPreferencesButton.Click += OnDetailTrackingDisabledPreferencesButtonClick;
            //
            // detailFilterPanel
            //
            detailFilterPanel.Controls.Add(detailDateLabel);
            detailFilterPanel.Controls.Add(detailDatePicker);
            detailFilterPanel.Controls.Add(detailCalendarButton);
            detailFilterPanel.Controls.Add(detailPreviousDateButton);
            detailFilterPanel.Controls.Add(detailNextDateButton);
            detailFilterPanel.Controls.Add(detailTodayButton);
            detailFilterPanel.Controls.Add(detailDateStatusLabel);
            detailFilterPanel.Controls.Add(detailRuntimeFilterLabel);
            detailFilterPanel.Controls.Add(detailRuntimeFilterComboBox);
            detailFilterPanel.Controls.Add(runningRuntimeOnlyCheckBox);
            detailFilterPanel.Controls.Add(detailHelpButton);
            detailFilterPanel.Controls.Add(detailDescriptionLabel);
            detailFilterPanel.Dock = DockStyle.Top;
            detailFilterPanel.Location = new Point(3, 3);
            detailFilterPanel.Name = "detailFilterPanel";
            detailFilterPanel.Size = new Size(706, 82);
            detailFilterPanel.TabIndex = 1;
            //
            // detailDateLabel
            //
            detailDateLabel.AutoSize = true;
            detailDateLabel.Location = new Point(8, 8);
            detailDateLabel.Name = "detailDateLabel";
            detailDateLabel.Size = new Size(31, 15);
            detailDateLabel.TabIndex = 0;
            detailDateLabel.Text = UiText.Main.Date;
            //
            // detailDatePicker
            //
            detailDatePicker.Format = DateTimePickerFormat.Short;
            detailDatePicker.Location = new Point(47, 4);
            detailDatePicker.Name = "detailDatePicker";
            detailDatePicker.ShowUpDown = true;
            detailDatePicker.Size = new Size(112, 23);
            detailDatePicker.TabIndex = 1;
            detailDatePicker.ValueChanged += OnDetailDatePickerValueChanged;
            //
            // detailCalendarButton
            //
            detailCalendarButton.Location = new Point(165, 4);
            detailCalendarButton.Name = "detailCalendarButton";
            detailCalendarButton.Size = new Size(52, 23);
            detailCalendarButton.TabIndex = 2;
            detailCalendarButton.Text = UiText.Main.Calendar;
            detailCalendarButton.UseVisualStyleBackColor = true;
            detailCalendarButton.Click += OnDetailCalendarButtonClick;
            //
            // detailPreviousDateButton
            //
            detailPreviousDateButton.Location = new Point(223, 4);
            detailPreviousDateButton.Name = "detailPreviousDateButton";
            detailPreviousDateButton.Size = new Size(32, 23);
            detailPreviousDateButton.TabIndex = 3;
            detailPreviousDateButton.Text = "<";
            detailPreviousDateButton.UseVisualStyleBackColor = true;
            detailPreviousDateButton.Click += OnDetailPreviousDateButtonClick;
            //
            // detailNextDateButton
            //
            detailNextDateButton.Location = new Point(259, 4);
            detailNextDateButton.Name = "detailNextDateButton";
            detailNextDateButton.Size = new Size(32, 23);
            detailNextDateButton.TabIndex = 4;
            detailNextDateButton.Text = ">";
            detailNextDateButton.UseVisualStyleBackColor = true;
            detailNextDateButton.Click += OnDetailNextDateButtonClick;
            //
            // detailTodayButton
            //
            detailTodayButton.Location = new Point(297, 4);
            detailTodayButton.Name = "detailTodayButton";
            detailTodayButton.Size = new Size(48, 23);
            detailTodayButton.TabIndex = 5;
            detailTodayButton.Text = UiText.Main.Today;
            detailTodayButton.UseVisualStyleBackColor = true;
            detailTodayButton.Click += OnDetailTodayButtonClick;
            //
            // detailDateStatusLabel
            //
            detailDateStatusLabel.AutoSize = true;
            detailDateStatusLabel.ForeColor = SystemColors.GrayText;
            detailDateStatusLabel.Location = new Point(354, 8);
            detailDateStatusLabel.Name = "detailDateStatusLabel";
            detailDateStatusLabel.Size = new Size(55, 15);
            detailDateStatusLabel.TabIndex = 6;
            detailDateStatusLabel.Text = UiText.Main.NotChecked;
            //
            // detailRuntimeFilterLabel
            //
            detailRuntimeFilterLabel.AutoSize = true;
            detailRuntimeFilterLabel.Location = new Point(8, 37);
            detailRuntimeFilterLabel.Name = "detailRuntimeFilterLabel";
            detailRuntimeFilterLabel.Size = new Size(31, 15);
            detailRuntimeFilterLabel.TabIndex = 7;
            detailRuntimeFilterLabel.Text = UiText.Main.DetailRuntimeFilter;
            //
            // detailRuntimeFilterComboBox
            //
            detailRuntimeFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            detailRuntimeFilterComboBox.FormattingEnabled = true;
            detailRuntimeFilterComboBox.Location = new Point(47, 33);
            detailRuntimeFilterComboBox.Name = "detailRuntimeFilterComboBox";
            detailRuntimeFilterComboBox.Size = new Size(174, 23);
            detailRuntimeFilterComboBox.TabIndex = 8;
            detailRuntimeFilterComboBox.SelectedIndexChanged += OnDetailRuntimeFilterComboBoxSelectedIndexChanged;
            //
            // runningRuntimeOnlyCheckBox
            //
            runningRuntimeOnlyCheckBox.AutoSize = true;
            runningRuntimeOnlyCheckBox.Location = new Point(235, 35);
            runningRuntimeOnlyCheckBox.Name = "runningRuntimeOnlyCheckBox";
            runningRuntimeOnlyCheckBox.Size = new Size(82, 19);
            runningRuntimeOnlyCheckBox.TabIndex = 9;
            runningRuntimeOnlyCheckBox.Text = UiText.Main.RunningOnly;
            runningRuntimeOnlyCheckBox.UseVisualStyleBackColor = true;
            runningRuntimeOnlyCheckBox.CheckedChanged += OnRunningRuntimeOnlyCheckBoxCheckedChanged;
            //
            // detailHelpButton
            //
            detailHelpButton.Location = new Point(329, 33);
            detailHelpButton.Name = "detailHelpButton";
            detailHelpButton.Size = new Size(28, 23);
            detailHelpButton.TabIndex = 10;
            detailHelpButton.Text = UiText.Main.DetailHelp;
            detailHelpButton.UseVisualStyleBackColor = true;
            detailHelpButton.Click += OnDetailHelpButtonClick;
            //
            // detailDescriptionLabel
            //
            detailDescriptionLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            detailDescriptionLabel.AutoEllipsis = true;
            detailDescriptionLabel.ForeColor = SystemColors.GrayText;
            detailDescriptionLabel.Location = new Point(8, 61);
            detailDescriptionLabel.Name = "detailDescriptionLabel";
            detailDescriptionLabel.Size = new Size(690, 15);
            detailDescriptionLabel.TabIndex = 11;
            detailDescriptionLabel.Text = UiText.Main.DetailDescription;
            //
            // detailSplitContainer
            //
            detailSplitContainer.Dock = DockStyle.Fill;
            detailSplitContainer.Location = new Point(3, 85);
            detailSplitContainer.Name = "detailSplitContainer";
            detailSplitContainer.Orientation = Orientation.Horizontal;
            //
            // detailSplitContainer.Panel1
            //
            detailSplitContainer.Panel1.Controls.Add(runtimeGrid);
            //
            // detailSplitContainer.Panel2
            //
            detailSplitContainer.Panel2.Controls.Add(runtimeSegmentsGrid);
            detailSplitContainer.Size = new Size(706, 332);
            detailSplitContainer.SplitterDistance = 212;
            detailSplitContainer.TabIndex = 0;
            //
            // runtimeGrid
            //
            runtimeGrid.AllowUserToAddRows = false;
            runtimeGrid.AllowUserToDeleteRows = false;
            runtimeGrid.AllowUserToOrderColumns = true;
            runtimeGrid.AllowUserToResizeRows = false;
            runtimeGrid.AutoGenerateColumns = false;
            runtimeGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            runtimeGrid.BackgroundColor = SystemColors.Window;
            runtimeGrid.BorderStyle = BorderStyle.None;
            runtimeGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            runtimeGrid.Columns.AddRange(new DataGridViewColumn[] { runtimeAppIconColumn, runtimeAppNameColumn, runtimeCategoryColumn, runtimeTrackingTypeColumn, runtimeFirstObservedAtColumn, runtimeLastObservedAtColumn, runtimeDurationColumn, runtimeActiveUsageColumn, runtimeActualUsageRatioColumn, runtimeSessionCountColumn, runtimeStatusColumn });
            runtimeGrid.Dock = DockStyle.Fill;
            runtimeGrid.Location = new Point(0, 0);
            runtimeGrid.MultiSelect = false;
            runtimeGrid.Name = "runtimeGrid";
            runtimeGrid.ReadOnly = true;
            runtimeGrid.RowHeadersVisible = false;
            runtimeGrid.ScrollBars = ScrollBars.Both;
            runtimeGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            runtimeGrid.Size = new Size(706, 226);
            runtimeGrid.TabIndex = 0;
            runtimeGrid.ColumnHeaderMouseClick += OnRuntimeGridColumnHeaderMouseClick;
            runtimeGrid.SelectionChanged += OnRuntimeGridSelectionChanged;
            //
            // runtimeAppIconColumn
            //
            runtimeAppIconColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            runtimeAppIconColumn.DataPropertyName = "AppIcon";
            runtimeAppIconColumn.HeaderText = "";
            runtimeAppIconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            runtimeAppIconColumn.MinimumWidth = 36;
            runtimeAppIconColumn.Name = "runtimeAppIconColumn";
            runtimeAppIconColumn.ReadOnly = true;
            runtimeAppIconColumn.Width = 36;
            //
            // runtimeAppNameColumn
            //
            runtimeAppNameColumn.DataPropertyName = "AppName";
            runtimeAppNameColumn.HeaderText = UiText.Main.App;
            runtimeAppNameColumn.MinimumWidth = 180;
            runtimeAppNameColumn.Name = "runtimeAppNameColumn";
            runtimeAppNameColumn.ReadOnly = true;
            runtimeAppNameColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeAppNameColumn.Width = 220;
            //
            // runtimeCategoryColumn
            //
            runtimeCategoryColumn.DataPropertyName = "CategoryText";
            runtimeCategoryColumn.HeaderText = UiText.Main.Category;
            runtimeCategoryColumn.MinimumWidth = 100;
            runtimeCategoryColumn.Name = "runtimeCategoryColumn";
            runtimeCategoryColumn.ReadOnly = true;
            runtimeCategoryColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeCategoryColumn.Width = 120;
            //
            // runtimeTrackingTypeColumn
            //
            runtimeTrackingTypeColumn.DataPropertyName = "TrackingTypeText";
            runtimeTrackingTypeColumn.HeaderText = UiText.Main.Type;
            runtimeTrackingTypeColumn.MinimumWidth = 90;
            runtimeTrackingTypeColumn.Name = "runtimeTrackingTypeColumn";
            runtimeTrackingTypeColumn.ReadOnly = true;
            runtimeTrackingTypeColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeTrackingTypeColumn.ToolTipText = UiText.Main.RuntimeTrackingTypeTooltip;
            runtimeTrackingTypeColumn.Width = 110;
            //
            // runtimeFirstObservedAtColumn
            //
            runtimeFirstObservedAtColumn.DataPropertyName = "FirstObservedAtText";
            runtimeFirstObservedAtColumn.HeaderText = UiText.Main.FirstObservedAt;
            runtimeFirstObservedAtColumn.MinimumWidth = 90;
            runtimeFirstObservedAtColumn.Name = "runtimeFirstObservedAtColumn";
            runtimeFirstObservedAtColumn.ReadOnly = true;
            runtimeFirstObservedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeFirstObservedAtColumn.Width = 100;
            //
            // runtimeLastObservedAtColumn
            //
            runtimeLastObservedAtColumn.DataPropertyName = "LastObservedAtText";
            runtimeLastObservedAtColumn.HeaderText = UiText.Main.LastObservedAt;
            runtimeLastObservedAtColumn.MinimumWidth = 100;
            runtimeLastObservedAtColumn.Name = "runtimeLastObservedAtColumn";
            runtimeLastObservedAtColumn.ReadOnly = true;
            runtimeLastObservedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeLastObservedAtColumn.ToolTipText = UiText.Main.RuntimeLastObservedTooltip;
            runtimeLastObservedAtColumn.Width = 110;
            //
            // runtimeDurationColumn
            //
            runtimeDurationColumn.DataPropertyName = "RuntimeText";
            runtimeDurationColumn.HeaderText = UiText.Main.Runtime;
            runtimeDurationColumn.MinimumWidth = 110;
            runtimeDurationColumn.Name = "runtimeDurationColumn";
            runtimeDurationColumn.ReadOnly = true;
            runtimeDurationColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeDurationColumn.ToolTipText = UiText.Main.RuntimeDurationTooltip;
            runtimeDurationColumn.Width = 120;
            //
            // runtimeActiveUsageColumn
            //
            runtimeActiveUsageColumn.DataPropertyName = "ActiveUsageTimeText";
            runtimeActiveUsageColumn.HeaderText = UiText.Main.ActiveUsageTime;
            runtimeActiveUsageColumn.MinimumWidth = 120;
            runtimeActiveUsageColumn.Name = "runtimeActiveUsageColumn";
            runtimeActiveUsageColumn.ReadOnly = true;
            runtimeActiveUsageColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeActiveUsageColumn.Width = 130;
            //
            // runtimeActualUsageRatioColumn
            //
            runtimeActualUsageRatioColumn.DataPropertyName = "ActualUsageRatioText";
            runtimeActualUsageRatioColumn.HeaderText = UiText.Main.ActualUsageRatio;
            runtimeActualUsageRatioColumn.MinimumWidth = 100;
            runtimeActualUsageRatioColumn.Name = "runtimeActualUsageRatioColumn";
            runtimeActualUsageRatioColumn.ReadOnly = true;
            runtimeActualUsageRatioColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeActualUsageRatioColumn.ToolTipText = UiText.Main.RuntimeActualUsageRatioTooltip;
            runtimeActualUsageRatioColumn.Width = 110;
            //
            // runtimeSessionCountColumn
            //
            runtimeSessionCountColumn.DataPropertyName = "RuntimeSegmentCountText";
            runtimeSessionCountColumn.HeaderText = UiText.Main.RuntimeSegmentCount;
            runtimeSessionCountColumn.MinimumWidth = 80;
            runtimeSessionCountColumn.Name = "runtimeSessionCountColumn";
            runtimeSessionCountColumn.ReadOnly = true;
            runtimeSessionCountColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSessionCountColumn.ToolTipText = UiText.Main.RuntimeSegmentCountTooltip;
            runtimeSessionCountColumn.Width = 90;
            //
            // runtimeStatusColumn
            //
            runtimeStatusColumn.DataPropertyName = "StatusText";
            runtimeStatusColumn.HeaderText = UiText.Main.Status;
            runtimeStatusColumn.MinimumWidth = 80;
            runtimeStatusColumn.Name = "runtimeStatusColumn";
            runtimeStatusColumn.ReadOnly = true;
            runtimeStatusColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeStatusColumn.ToolTipText = UiText.Main.RuntimeStatusTooltip;
            runtimeStatusColumn.Width = 90;
            //
            // runtimeSegmentsGrid
            //
            runtimeSegmentsGrid.AllowUserToAddRows = false;
            runtimeSegmentsGrid.AllowUserToDeleteRows = false;
            runtimeSegmentsGrid.AllowUserToResizeRows = false;
            runtimeSegmentsGrid.AutoGenerateColumns = false;
            runtimeSegmentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            runtimeSegmentsGrid.BackgroundColor = SystemColors.Window;
            runtimeSegmentsGrid.BorderStyle = BorderStyle.None;
            runtimeSegmentsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            runtimeSegmentsGrid.Columns.AddRange(new DataGridViewColumn[] { runtimeSegmentStartedAtColumn, runtimeSegmentEndedAtColumn, runtimeSegmentDurationColumn, runtimeSegmentStatusColumn, runtimeSegmentObservationTypeColumn, runtimeSegmentProcessIdColumn });
            runtimeSegmentsGrid.Dock = DockStyle.Fill;
            runtimeSegmentsGrid.Location = new Point(0, 0);
            runtimeSegmentsGrid.MultiSelect = false;
            runtimeSegmentsGrid.Name = "runtimeSegmentsGrid";
            runtimeSegmentsGrid.ReadOnly = true;
            runtimeSegmentsGrid.RowHeadersVisible = false;
            runtimeSegmentsGrid.ScrollBars = ScrollBars.Both;
            runtimeSegmentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            runtimeSegmentsGrid.Size = new Size(706, 124);
            runtimeSegmentsGrid.TabIndex = 0;
            runtimeSegmentsGrid.ColumnHeaderMouseClick += OnRuntimeSegmentsGridColumnHeaderMouseClick;
            //
            // runtimeSegmentStartedAtColumn
            //
            runtimeSegmentStartedAtColumn.DataPropertyName = "StartedAtText";
            runtimeSegmentStartedAtColumn.HeaderText = UiText.Main.Start;
            runtimeSegmentStartedAtColumn.MinimumWidth = 90;
            runtimeSegmentStartedAtColumn.Name = "runtimeSegmentStartedAtColumn";
            runtimeSegmentStartedAtColumn.ReadOnly = true;
            runtimeSegmentStartedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentStartedAtColumn.Width = 100;
            //
            // runtimeSegmentEndedAtColumn
            //
            runtimeSegmentEndedAtColumn.DataPropertyName = "EndedAtText";
            runtimeSegmentEndedAtColumn.HeaderText = UiText.Main.End;
            runtimeSegmentEndedAtColumn.MinimumWidth = 90;
            runtimeSegmentEndedAtColumn.Name = "runtimeSegmentEndedAtColumn";
            runtimeSegmentEndedAtColumn.ReadOnly = true;
            runtimeSegmentEndedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentEndedAtColumn.Width = 100;
            //
            // runtimeSegmentDurationColumn
            //
            runtimeSegmentDurationColumn.DataPropertyName = "DurationText";
            runtimeSegmentDurationColumn.HeaderText = UiText.Main.Duration;
            runtimeSegmentDurationColumn.MinimumWidth = 100;
            runtimeSegmentDurationColumn.Name = "runtimeSegmentDurationColumn";
            runtimeSegmentDurationColumn.ReadOnly = true;
            runtimeSegmentDurationColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentDurationColumn.Width = 110;
            //
            // runtimeSegmentStatusColumn
            //
            runtimeSegmentStatusColumn.DataPropertyName = "StatusText";
            runtimeSegmentStatusColumn.HeaderText = UiText.Main.Status;
            runtimeSegmentStatusColumn.MinimumWidth = 80;
            runtimeSegmentStatusColumn.Name = "runtimeSegmentStatusColumn";
            runtimeSegmentStatusColumn.ReadOnly = true;
            runtimeSegmentStatusColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentStatusColumn.Width = 90;
            //
            // runtimeSegmentObservationTypeColumn
            //
            runtimeSegmentObservationTypeColumn.DataPropertyName = "ObservationTypeText";
            runtimeSegmentObservationTypeColumn.HeaderText = UiText.Main.ObservationBasis;
            runtimeSegmentObservationTypeColumn.MinimumWidth = 110;
            runtimeSegmentObservationTypeColumn.Name = "runtimeSegmentObservationTypeColumn";
            runtimeSegmentObservationTypeColumn.ReadOnly = true;
            runtimeSegmentObservationTypeColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentObservationTypeColumn.Width = 130;
            //
            // runtimeSegmentProcessIdColumn
            //
            runtimeSegmentProcessIdColumn.DataPropertyName = "ProcessId";
            runtimeSegmentProcessIdColumn.HeaderText = UiText.Main.Pid;
            runtimeSegmentProcessIdColumn.MinimumWidth = 70;
            runtimeSegmentProcessIdColumn.Name = "runtimeSegmentProcessIdColumn";
            runtimeSegmentProcessIdColumn.ReadOnly = true;
            runtimeSegmentProcessIdColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentProcessIdColumn.Width = 80;
            // 
            // timelineTab
            //
            timelineTab.Controls.Add(timelineGrid);
            timelineTab.Controls.Add(timelineOverviewControl);
            timelineTab.Controls.Add(timelineZoomScrollBar);
            timelineTab.Controls.Add(timelineHighlightSummaryPanel);
            timelineTab.Controls.Add(timelineZoomPanel);
            timelineTab.Controls.Add(timelineDatePanel);
            timelineTab.Location = new Point(4, 24);
            timelineTab.Name = "timelineTab";
            timelineTab.Padding = new Padding(3);
            timelineTab.Size = new Size(712, 420);
            timelineTab.TabIndex = 2;
            timelineTab.Text = UiText.Main.TimelineTab;
            timelineTab.UseVisualStyleBackColor = true;
            // 
            // timelineDatePanel
            // 
            timelineDatePanel.Controls.Add(timelineDateLabel);
            timelineDatePanel.Controls.Add(timelineDatePicker);
            timelineDatePanel.Controls.Add(timelineCalendarButton);
            timelineDatePanel.Controls.Add(timelinePreviousDateButton);
            timelineDatePanel.Controls.Add(timelineNextDateButton);
            timelineDatePanel.Controls.Add(timelineTodayButton);
            timelineDatePanel.Controls.Add(timelineDateStatusLabel);
            timelineDatePanel.Controls.Add(timelineHighlightHintLabel);
            timelineDatePanel.Controls.Add(timelineHighlightLabel);
            timelineDatePanel.Controls.Add(timelineHighlightClearButton);
            timelineDatePanel.Dock = DockStyle.Top;
            timelineDatePanel.Location = new Point(3, 3);
            timelineDatePanel.Name = "timelineDatePanel";
            timelineDatePanel.Padding = new Padding(8, 4, 8, 2);
            timelineDatePanel.Size = new Size(706, 36);
            timelineDatePanel.TabIndex = 1;
            timelineDatePanel.WrapContents = false;
            // 
            // timelineDateLabel
            // 
            timelineDateLabel.AutoSize = true;
            timelineDateLabel.Location = new Point(11, 10);
            timelineDateLabel.Margin = new Padding(0, 6, 8, 0);
            timelineDateLabel.Name = "timelineDateLabel";
            timelineDateLabel.Size = new Size(31, 15);
            timelineDateLabel.TabIndex = 0;
            timelineDateLabel.Text = UiText.Main.Date;
            // 
            // timelineDatePicker
            // 
            timelineDatePicker.Format = DateTimePickerFormat.Short;
            timelineDatePicker.Location = new Point(50, 7);
            timelineDatePicker.Name = "timelineDatePicker";
            timelineDatePicker.ShowUpDown = true;
            timelineDatePicker.Size = new Size(112, 23);
            timelineDatePicker.TabIndex = 1;
            timelineDatePicker.ValueChanged += OnTimelineDatePickerValueChanged;
            // 
            // timelineCalendarButton
            // 
            timelineCalendarButton.Location = new Point(168, 7);
            timelineCalendarButton.Name = "timelineCalendarButton";
            timelineCalendarButton.Size = new Size(52, 23);
            timelineCalendarButton.TabIndex = 2;
            timelineCalendarButton.Text = UiText.Main.Calendar;
            timelineCalendarButton.UseVisualStyleBackColor = true;
            timelineCalendarButton.Click += OnTimelineCalendarButtonClick;
            // 
            // timelinePreviousDateButton
            // 
            timelinePreviousDateButton.Location = new Point(226, 7);
            timelinePreviousDateButton.Name = "timelinePreviousDateButton";
            timelinePreviousDateButton.Size = new Size(32, 23);
            timelinePreviousDateButton.TabIndex = 3;
            timelinePreviousDateButton.Text = "<";
            timelinePreviousDateButton.UseVisualStyleBackColor = true;
            timelinePreviousDateButton.Click += OnTimelinePreviousDateButtonClick;
            // 
            // timelineNextDateButton
            // 
            timelineNextDateButton.Location = new Point(264, 7);
            timelineNextDateButton.Name = "timelineNextDateButton";
            timelineNextDateButton.Size = new Size(32, 23);
            timelineNextDateButton.TabIndex = 4;
            timelineNextDateButton.Text = ">";
            timelineNextDateButton.UseVisualStyleBackColor = true;
            timelineNextDateButton.Click += OnTimelineNextDateButtonClick;
            // 
            // timelineTodayButton
            // 
            timelineTodayButton.Location = new Point(302, 7);
            timelineTodayButton.Name = "timelineTodayButton";
            timelineTodayButton.Size = new Size(48, 23);
            timelineTodayButton.TabIndex = 5;
            timelineTodayButton.Text = UiText.Main.Today;
            timelineTodayButton.UseVisualStyleBackColor = true;
            timelineTodayButton.Click += OnTimelineTodayButtonClick;
            // 
            // timelineDateStatusLabel
            // 
            timelineDateStatusLabel.AutoSize = true;
            timelineDateStatusLabel.ForeColor = SystemColors.GrayText;
            timelineDateStatusLabel.Location = new Point(364, 10);
            timelineDateStatusLabel.Margin = new Padding(8, 6, 8, 0);
            timelineDateStatusLabel.Name = "timelineDateStatusLabel";
            timelineDateStatusLabel.Size = new Size(55, 15);
            timelineDateStatusLabel.TabIndex = 6;
            timelineDateStatusLabel.Text = UiText.Main.NotChecked;
            //
            // timelineHighlightHintLabel
            //
            timelineHighlightHintLabel.AutoSize = true;
            timelineHighlightHintLabel.ForeColor = SystemColors.GrayText;
            timelineHighlightHintLabel.Location = new Point(427, 10);
            timelineHighlightHintLabel.Margin = new Padding(8, 6, 4, 0);
            timelineHighlightHintLabel.Name = "timelineHighlightHintLabel";
            timelineHighlightHintLabel.Size = new Size(0, 15);
            timelineHighlightHintLabel.TabIndex = 7;
            timelineHighlightHintLabel.Text = UiText.Main.TimelineHighlightHint;
            //
            // timelineHighlightLabel
            //
            timelineHighlightLabel.AutoSize = true;
            timelineHighlightLabel.ForeColor = SystemColors.Highlight;
            timelineHighlightLabel.Location = new Point(435, 10);
            timelineHighlightLabel.Margin = new Padding(8, 6, 4, 0);
            timelineHighlightLabel.Name = "timelineHighlightLabel";
            timelineHighlightLabel.Size = new Size(0, 15);
            timelineHighlightLabel.TabIndex = 8;
            timelineHighlightLabel.Visible = false;
            //
            // timelineHighlightClearButton
            //
            timelineHighlightClearButton.Location = new Point(435, 7);
            timelineHighlightClearButton.Name = "timelineHighlightClearButton";
            timelineHighlightClearButton.Size = new Size(52, 23);
            timelineHighlightClearButton.TabIndex = 9;
            timelineHighlightClearButton.Text = UiText.Main.ClearTimelineHighlight;
            timelineHighlightClearButton.UseVisualStyleBackColor = true;
            timelineHighlightClearButton.Visible = false;
            timelineHighlightClearButton.Click += OnTimelineHighlightClearButtonClick;
            //
            // timelineZoomPanel
            //
            timelineZoomPanel.Controls.Add(timelineZoomRangeLabel);
            timelineZoomPanel.Controls.Add(timelineZoomOutButton);
            timelineZoomPanel.Controls.Add(timelineZoomInButton);
            timelineZoomPanel.Controls.Add(timelineZoomPreviousButton);
            timelineZoomPanel.Controls.Add(timelineZoomNextButton);
            timelineZoomPanel.Controls.Add(timelineZoomResetButton);
            timelineZoomPanel.Controls.Add(timelineHelpButton);
            timelineZoomPanel.Controls.Add(timelineCategoryBucketLabel);
            timelineZoomPanel.Controls.Add(timelineCategoryBucketComboBox);
            timelineZoomPanel.Dock = DockStyle.Top;
            timelineZoomPanel.Location = new Point(3, 39);
            timelineZoomPanel.Name = "timelineZoomPanel";
            timelineZoomPanel.Padding = new Padding(8, 4, 8, 2);
            timelineZoomPanel.Size = new Size(706, 32);
            timelineZoomPanel.TabIndex = 2;
            timelineZoomPanel.WrapContents = false;
            //
            // timelineZoomRangeLabel
            //
            timelineZoomRangeLabel.AutoSize = true;
            timelineZoomRangeLabel.ForeColor = SystemColors.GrayText;
            timelineZoomRangeLabel.Location = new Point(11, 10);
            timelineZoomRangeLabel.Margin = new Padding(0, 6, 12, 0);
            timelineZoomRangeLabel.Name = "timelineZoomRangeLabel";
            timelineZoomRangeLabel.Size = new Size(91, 15);
            timelineZoomRangeLabel.TabIndex = 0;
            timelineZoomRangeLabel.Text = UiText.Main.TimelineViewRange(UiText.Main.TimelineFullDay);
            //
            // timelineZoomOutButton
            //
            timelineZoomOutButton.Enabled = false;
            timelineZoomOutButton.Location = new Point(117, 7);
            timelineZoomOutButton.Name = "timelineZoomOutButton";
            timelineZoomOutButton.Size = new Size(32, 23);
            timelineZoomOutButton.TabIndex = 1;
            timelineZoomOutButton.Text = UiText.Main.TimelineZoomOut;
            timelineZoomOutButton.UseVisualStyleBackColor = true;
            timelineZoomOutButton.Click += OnTimelineZoomOutButtonClick;
            //
            // timelineZoomInButton
            //
            timelineZoomInButton.Location = new Point(155, 7);
            timelineZoomInButton.Name = "timelineZoomInButton";
            timelineZoomInButton.Size = new Size(32, 23);
            timelineZoomInButton.TabIndex = 2;
            timelineZoomInButton.Text = UiText.Main.TimelineZoomIn;
            timelineZoomInButton.UseVisualStyleBackColor = true;
            timelineZoomInButton.Click += OnTimelineZoomInButtonClick;
            //
            // timelineZoomPreviousButton
            //
            timelineZoomPreviousButton.Enabled = false;
            timelineZoomPreviousButton.Location = new Point(193, 7);
            timelineZoomPreviousButton.Name = "timelineZoomPreviousButton";
            timelineZoomPreviousButton.Size = new Size(32, 23);
            timelineZoomPreviousButton.TabIndex = 3;
            timelineZoomPreviousButton.Text = UiText.Main.TimelinePanPrevious;
            timelineZoomPreviousButton.UseVisualStyleBackColor = true;
            timelineZoomPreviousButton.Click += OnTimelineZoomPreviousButtonClick;
            //
            // timelineZoomNextButton
            //
            timelineZoomNextButton.Enabled = false;
            timelineZoomNextButton.Location = new Point(231, 7);
            timelineZoomNextButton.Name = "timelineZoomNextButton";
            timelineZoomNextButton.Size = new Size(32, 23);
            timelineZoomNextButton.TabIndex = 4;
            timelineZoomNextButton.Text = UiText.Main.TimelinePanNext;
            timelineZoomNextButton.UseVisualStyleBackColor = true;
            timelineZoomNextButton.Click += OnTimelineZoomNextButtonClick;
            //
            // timelineZoomResetButton
            //
            timelineZoomResetButton.Enabled = false;
            timelineZoomResetButton.Location = new Point(269, 7);
            timelineZoomResetButton.Name = "timelineZoomResetButton";
            timelineZoomResetButton.Size = new Size(52, 23);
            timelineZoomResetButton.TabIndex = 5;
            timelineZoomResetButton.Text = UiText.Main.TimelineResetView;
            timelineZoomResetButton.UseVisualStyleBackColor = true;
            timelineZoomResetButton.Click += OnTimelineZoomResetButtonClick;
            //
            // timelineHelpButton
            //
            timelineHelpButton.Location = new Point(327, 7);
            timelineHelpButton.Name = "timelineHelpButton";
            timelineHelpButton.Size = new Size(28, 23);
            timelineHelpButton.TabIndex = 6;
            timelineHelpButton.Text = UiText.Main.TimelineHelp;
            timelineHelpButton.UseVisualStyleBackColor = true;
            timelineHelpButton.Click += OnTimelineHelpButtonClick;
            //
            // timelineCategoryBucketLabel
            //
            timelineCategoryBucketLabel.AutoSize = true;
            timelineCategoryBucketLabel.Location = new Point(363, 10);
            timelineCategoryBucketLabel.Margin = new Padding(8, 6, 4, 0);
            timelineCategoryBucketLabel.Name = "timelineCategoryBucketLabel";
            timelineCategoryBucketLabel.Size = new Size(59, 15);
            timelineCategoryBucketLabel.TabIndex = 7;
            timelineCategoryBucketLabel.Text = UiText.Main.TimelineCategoryBucket;
            //
            // timelineCategoryBucketComboBox
            //
            timelineCategoryBucketComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            timelineCategoryBucketComboBox.FormattingEnabled = true;
            timelineCategoryBucketComboBox.Location = new Point(426, 7);
            timelineCategoryBucketComboBox.Name = "timelineCategoryBucketComboBox";
            timelineCategoryBucketComboBox.Size = new Size(86, 23);
            timelineCategoryBucketComboBox.TabIndex = 8;
            timelineCategoryBucketComboBox.SelectedIndexChanged += OnTimelineCategoryBucketComboBoxSelectedIndexChanged;
            //
            // timelineHighlightSummaryPanel
            //
            timelineHighlightSummaryPanel.Controls.Add(timelineHighlightSummaryLabel);
            timelineHighlightSummaryPanel.Dock = DockStyle.Top;
            timelineHighlightSummaryPanel.Location = new Point(3, 71);
            timelineHighlightSummaryPanel.Name = "timelineHighlightSummaryPanel";
            timelineHighlightSummaryPanel.Padding = new Padding(8, 4, 8, 2);
            timelineHighlightSummaryPanel.Size = new Size(706, 30);
            timelineHighlightSummaryPanel.TabIndex = 3;
            timelineHighlightSummaryPanel.Visible = false;
            timelineHighlightSummaryPanel.WrapContents = false;
            //
            // timelineHighlightSummaryLabel
            //
            timelineHighlightSummaryLabel.AutoSize = true;
            timelineHighlightSummaryLabel.ForeColor = SystemColors.ControlText;
            timelineHighlightSummaryLabel.Location = new Point(11, 10);
            timelineHighlightSummaryLabel.Margin = new Padding(0, 6, 4, 0);
            timelineHighlightSummaryLabel.Name = "timelineHighlightSummaryLabel";
            timelineHighlightSummaryLabel.Size = new Size(0, 15);
            timelineHighlightSummaryLabel.TabIndex = 0;
            //
            // timelineZoomScrollBar
            //
            timelineZoomScrollBar.Dock = DockStyle.Top;
            timelineZoomScrollBar.Enabled = false;
            timelineZoomScrollBar.LargeChange = 1000;
            timelineZoomScrollBar.Location = new Point(3, 71);
            timelineZoomScrollBar.Maximum = 1000;
            timelineZoomScrollBar.Name = "timelineZoomScrollBar";
            timelineZoomScrollBar.Size = new Size(706, 17);
            timelineZoomScrollBar.TabIndex = 4;
            timelineZoomScrollBar.Visible = false;
            //
            // timelineOverviewControl
            //
            timelineOverviewControl.Dock = DockStyle.Top;
            timelineOverviewControl.Location = new Point(3, 88);
            timelineOverviewControl.Name = "timelineOverviewControl";
            timelineOverviewControl.Size = new Size(706, 156);
            timelineOverviewControl.TabIndex = 5;
            timelineOverviewControl.ViewRangeChanged += OnTimelineOverviewViewRangeChanged;
            //
            // timelineGrid
            // 
            timelineGrid.AllowUserToAddRows = false;
            timelineGrid.AllowUserToDeleteRows = false;
            timelineGrid.AllowUserToResizeRows = false;
            timelineGrid.AutoGenerateColumns = false;
            timelineGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            timelineGrid.BackgroundColor = SystemColors.Window;
            timelineGrid.BorderStyle = BorderStyle.None;
            timelineGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            timelineGrid.Columns.AddRange(new DataGridViewColumn[] { timelineTypeColumn, timelineStartedAtColumn, timelineEndedAtColumn, timelineDurationColumn, timelineAppIconColumn, timelineDisplayNameColumn });
            timelineGrid.Dock = DockStyle.Fill;
            timelineGrid.Location = new Point(3, 244);
            timelineGrid.MultiSelect = false;
            timelineGrid.Name = "timelineGrid";
            timelineGrid.ReadOnly = true;
            timelineGrid.RowHeadersVisible = false;
            timelineGrid.ScrollBars = ScrollBars.Both;
            timelineGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            timelineGrid.Size = new Size(706, 173);
            timelineGrid.TabIndex = 0;
            timelineGrid.ColumnHeaderMouseClick += OnTimelineGridColumnHeaderMouseClick;
            // 
            // timelineTypeColumn
            // 
            timelineTypeColumn.DataPropertyName = "ActivityType";
            timelineTypeColumn.HeaderText = UiText.Main.Type;
            timelineTypeColumn.MinimumWidth = 80;
            timelineTypeColumn.Name = "timelineTypeColumn";
            timelineTypeColumn.ReadOnly = true;
            timelineTypeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineTypeColumn.Width = 90;
            // 
            // timelineStartedAtColumn
            // 
            timelineStartedAtColumn.DataPropertyName = "StartedAtText";
            timelineStartedAtColumn.HeaderText = UiText.Main.Start;
            timelineStartedAtColumn.MinimumWidth = 90;
            timelineStartedAtColumn.Name = "timelineStartedAtColumn";
            timelineStartedAtColumn.ReadOnly = true;
            timelineStartedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            timelineStartedAtColumn.Width = 100;
            // 
            // timelineEndedAtColumn
            // 
            timelineEndedAtColumn.DataPropertyName = "EndedAtText";
            timelineEndedAtColumn.HeaderText = UiText.Main.End;
            timelineEndedAtColumn.MinimumWidth = 90;
            timelineEndedAtColumn.Name = "timelineEndedAtColumn";
            timelineEndedAtColumn.ReadOnly = true;
            timelineEndedAtColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineEndedAtColumn.Width = 100;
            // 
            // timelineDurationColumn
            // 
            timelineDurationColumn.DataPropertyName = "DurationText";
            timelineDurationColumn.HeaderText = UiText.Main.Duration;
            timelineDurationColumn.MinimumWidth = 100;
            timelineDurationColumn.Name = "timelineDurationColumn";
            timelineDurationColumn.ReadOnly = true;
            timelineDurationColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineDurationColumn.Width = 110;
            // 
            // timelineAppIconColumn
            // 
            timelineAppIconColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            timelineAppIconColumn.DataPropertyName = "AppIcon";
            timelineAppIconColumn.HeaderText = "";
            timelineAppIconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            timelineAppIconColumn.MinimumWidth = 36;
            timelineAppIconColumn.Name = "timelineAppIconColumn";
            timelineAppIconColumn.ReadOnly = true;
            timelineAppIconColumn.Width = 36;
            // 
            // timelineDisplayNameColumn
            // 
            timelineDisplayNameColumn.DataPropertyName = "DisplayName";
            timelineDisplayNameColumn.HeaderText = UiText.Main.App;
            timelineDisplayNameColumn.MinimumWidth = 220;
            timelineDisplayNameColumn.Name = "timelineDisplayNameColumn";
            timelineDisplayNameColumn.ReadOnly = true;
            timelineDisplayNameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineDisplayNameColumn.Width = 260;
            // 
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1040, 640);
            Controls.Add(mainTabs);
            Controls.Add(statusLabel);
            Controls.Add(mainMenuStrip);
            MainMenuStrip = mainMenuStrip;
            MinimumSize = new Size(760, 480);
            Name = "Form1";
            Text = UiText.AppName;
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            mainTabs.ResumeLayout(false);
            summaryTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)usageGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)dailyUsageTrendGrid).EndInit();
            detailTab.ResumeLayout(false);
            detailTrackingDisabledPanel.ResumeLayout(false);
            detailFilterPanel.ResumeLayout(false);
            detailFilterPanel.PerformLayout();
            detailSplitContainer.Panel1.ResumeLayout(false);
            detailSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)detailSplitContainer).EndInit();
            detailSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)runtimeGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)runtimeSegmentsGrid).EndInit();
            timelineTab.ResumeLayout(false);
            timelineDatePanel.ResumeLayout(false);
            timelineDatePanel.PerformLayout();
            timelineZoomPanel.ResumeLayout(false);
            timelineZoomPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)timelineGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip mainMenuStrip;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem exportCsvMenuItem;
        private ToolStripMenuItem exportRawDataMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem preferencesMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem runtimeDiagnosticsMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private Label statusLabel;
        private TabControl mainTabs;
        private TabPage summaryTab;
        private FlowLayoutPanel summaryPeriodPanel;
        private Label summaryPeriodLabel;
        private ComboBox summaryPeriodComboBox;
        private DateTimePicker summarySpecificDatePicker;
        private Button summarySpecificDateCalendarButton;
        private Label summaryHighlightHintLabel;
        private FlowLayoutPanel runtimeCoverageSummaryPanel;
        private FlowLayoutPanel summaryIdleAnalysisPanel;
        private Label summaryIdleAnalysisLabel;
        private ToolTip runtimeCoverageSummaryToolTip;
        private DataGridView usageGrid;
        private DataGridView dailyUsageTrendGrid;
        private DataGridViewTextBoxColumn dailyUsageDateColumn;
        private DataGridViewTextBoxColumn dailyUsageActiveTimeColumn;
        private DataGridViewTextBoxColumn dailyUsageTopAppColumn;
        private DataGridViewTextBoxColumn dailyUsageTopAppTimeColumn;
        private DataGridViewImageColumn appIconColumn;
        private DataGridViewTextBoxColumn appNameColumn;
        private DataGridViewTextBoxColumn appCategoryColumn;
        private DataGridViewTextBoxColumn firstStartedAtColumn;
        private DataGridViewTextBoxColumn lastObservedAtColumn;
        private DataGridViewTextBoxColumn activeUsageTimeColumn;
        private DataGridViewTextBoxColumn usageRatioColumn;
        private DataGridViewTextBoxColumn switchCountColumn;
        private TabPage detailTab;
        private Panel detailTrackingDisabledPanel;
        private Label detailTrackingDisabledLabel;
        private Button detailTrackingDisabledPreferencesButton;
        private Panel detailFilterPanel;
        private Label detailDateLabel;
        private DateTimePicker detailDatePicker;
        private Button detailCalendarButton;
        private Button detailPreviousDateButton;
        private Button detailNextDateButton;
        private Button detailTodayButton;
        private Label detailDateStatusLabel;
        private Label detailRuntimeFilterLabel;
        private ComboBox detailRuntimeFilterComboBox;
        private CheckBox runningRuntimeOnlyCheckBox;
        private Button detailHelpButton;
        private Label detailDescriptionLabel;
        private SplitContainer detailSplitContainer;
        private DataGridView runtimeGrid;
        private DataGridViewImageColumn runtimeAppIconColumn;
        private DataGridViewTextBoxColumn runtimeAppNameColumn;
        private DataGridViewTextBoxColumn runtimeCategoryColumn;
        private DataGridViewTextBoxColumn runtimeTrackingTypeColumn;
        private DataGridViewTextBoxColumn runtimeFirstObservedAtColumn;
        private DataGridViewTextBoxColumn runtimeLastObservedAtColumn;
        private DataGridViewTextBoxColumn runtimeDurationColumn;
        private DataGridViewTextBoxColumn runtimeActiveUsageColumn;
        private DataGridViewTextBoxColumn runtimeActualUsageRatioColumn;
        private DataGridViewTextBoxColumn runtimeSessionCountColumn;
        private DataGridViewTextBoxColumn runtimeStatusColumn;
        private DataGridView runtimeSegmentsGrid;
        private DataGridViewTextBoxColumn runtimeSegmentStartedAtColumn;
        private DataGridViewTextBoxColumn runtimeSegmentEndedAtColumn;
        private DataGridViewTextBoxColumn runtimeSegmentDurationColumn;
        private DataGridViewTextBoxColumn runtimeSegmentStatusColumn;
        private DataGridViewTextBoxColumn runtimeSegmentObservationTypeColumn;
        private DataGridViewTextBoxColumn runtimeSegmentProcessIdColumn;
        private TabPage timelineTab;
        private FlowLayoutPanel timelineDatePanel;
        private Label timelineDateLabel;
        private DateTimePicker timelineDatePicker;
        private Button timelineCalendarButton;
        private Button timelinePreviousDateButton;
        private Button timelineNextDateButton;
        private Button timelineTodayButton;
        private Label timelineDateStatusLabel;
        private Label timelineHighlightHintLabel;
        private Label timelineHighlightLabel;
        private Button timelineHighlightClearButton;
        private FlowLayoutPanel timelineZoomPanel;
        private Label timelineZoomRangeLabel;
        private Button timelineZoomOutButton;
        private Button timelineZoomInButton;
        private Button timelineZoomPreviousButton;
        private Button timelineZoomNextButton;
        private Button timelineZoomResetButton;
        private Button timelineHelpButton;
        private Label timelineCategoryBucketLabel;
        private ComboBox timelineCategoryBucketComboBox;
        private FlowLayoutPanel timelineHighlightSummaryPanel;
        private Label timelineHighlightSummaryLabel;
        private HScrollBar timelineZoomScrollBar;
        private TimelineOverviewControl timelineOverviewControl;
        private DataGridView timelineGrid;
        private DataGridViewTextBoxColumn timelineTypeColumn;
        private DataGridViewTextBoxColumn timelineStartedAtColumn;
        private DataGridViewTextBoxColumn timelineEndedAtColumn;
        private DataGridViewTextBoxColumn timelineDurationColumn;
        private DataGridViewImageColumn timelineAppIconColumn;
        private DataGridViewTextBoxColumn timelineDisplayNameColumn;

        #endregion
    }
}
