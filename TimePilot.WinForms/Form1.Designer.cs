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
            exitMenuItem = new ToolStripMenuItem();
            settingsMenuItem = new ToolStripMenuItem();
            preferencesMenuItem = new ToolStripMenuItem();
            helpMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            statusLabel = new Label();
            mainTabs = new TabControl();
            summaryTab = new TabPage();
            runtimeCoverageSummaryPanel = new FlowLayoutPanel();
            runtimeCoverageSummaryToolTip = new ToolTip(components);
            usageGrid = new BufferedDataGridView();
            appIconColumn = new DataGridViewImageColumn();
            appNameColumn = new DataGridViewTextBoxColumn();
            firstStartedAtColumn = new DataGridViewTextBoxColumn();
            lastObservedAtColumn = new DataGridViewTextBoxColumn();
            activeUsageTimeColumn = new DataGridViewTextBoxColumn();
            usageRatioColumn = new DataGridViewTextBoxColumn();
            switchCountColumn = new DataGridViewTextBoxColumn();
            detailTab = new TabPage();
            detailFilterPanel = new Panel();
            currentTrackingScopeOnlyCheckBox = new CheckBox();
            runningRuntimeOnlyCheckBox = new CheckBox();
            detailSplitContainer = new SplitContainer();
            runtimeGrid = new BufferedDataGridView();
            runtimeAppIconColumn = new DataGridViewImageColumn();
            runtimeAppNameColumn = new DataGridViewTextBoxColumn();
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
            detailTab.SuspendLayout();
            detailFilterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)detailSplitContainer).BeginInit();
            detailSplitContainer.Panel1.SuspendLayout();
            detailSplitContainer.Panel2.SuspendLayout();
            detailSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)runtimeGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)runtimeSegmentsGrid).BeginInit();
            timelineTab.SuspendLayout();
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
            fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exportCsvMenuItem, exitMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new Size(43, 20);
            fileMenuItem.Text = "파일";
            // 
            // exportCsvMenuItem
            // 
            exportCsvMenuItem.Name = "exportCsvMenuItem";
            exportCsvMenuItem.Size = new Size(180, 22);
            exportCsvMenuItem.Text = "CSV 내보내기...";
            exportCsvMenuItem.Click += OnExportCsvMenuItemClick;
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new Size(180, 22);
            exitMenuItem.Text = "종료";
            exitMenuItem.Click += OnExitMenuItemClick;
            // 
            // settingsMenuItem
            // 
            settingsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { preferencesMenuItem });
            settingsMenuItem.Name = "settingsMenuItem";
            settingsMenuItem.Size = new Size(43, 20);
            settingsMenuItem.Text = "설정";
            // 
            // preferencesMenuItem
            // 
            preferencesMenuItem.Name = "preferencesMenuItem";
            preferencesMenuItem.Size = new Size(146, 22);
            preferencesMenuItem.Text = "환경 설정...";
            preferencesMenuItem.Click += OnPreferencesMenuItemClick;
            // 
            // helpMenuItem
            // 
            helpMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutMenuItem });
            helpMenuItem.Name = "helpMenuItem";
            helpMenuItem.Size = new Size(55, 20);
            helpMenuItem.Text = "도움말";
            // 
            // aboutMenuItem
            // 
            aboutMenuItem.Name = "aboutMenuItem";
            aboutMenuItem.Size = new Size(98, 22);
            aboutMenuItem.Text = "정보";
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
            summaryTab.Controls.Add(runtimeCoverageSummaryPanel);
            summaryTab.Location = new Point(4, 24);
            summaryTab.Name = "summaryTab";
            summaryTab.Padding = new Padding(3);
            summaryTab.Size = new Size(712, 420);
            summaryTab.TabIndex = 0;
            summaryTab.Text = "요약";
            summaryTab.UseVisualStyleBackColor = true;
            // 
            // runtimeCoverageSummaryPanel
            // 
            runtimeCoverageSummaryPanel.Dock = DockStyle.Top;
            runtimeCoverageSummaryPanel.Location = new Point(3, 3);
            runtimeCoverageSummaryPanel.Name = "runtimeCoverageSummaryPanel";
            runtimeCoverageSummaryPanel.Padding = new Padding(8, 4, 8, 4);
            runtimeCoverageSummaryPanel.Size = new Size(706, 48);
            runtimeCoverageSummaryPanel.TabIndex = 2;
            runtimeCoverageSummaryPanel.WrapContents = true;
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeCoverageSummaryPanel, "기록 커버리지는 오늘 0시부터 현재까지의 전체 시간 중 TimePilot이 실행되어 기록할 수 있었던 시간의 비율입니다. 컴퓨터를 실제로 사용한 시간 기준이 아닙니다.\r\n\r\n미기록 시간은 TimePilot이 실행되지 않았거나 기록할 수 없었던 구간입니다. PC 종료, Windows 절전, 앱 종료, 비정상 종료 등이 원인일 수 있으며 정확한 원인은 단정하지 않습니다.\r\n\r\n부팅 후 미실행은 Windows 시스템 시작 후 TimePilot이 처음 실행되기 전까지의 추정 시간입니다. Windows 로그인 시간이 아니라 시스템 시작 시각 기준입니다.");
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
            usageGrid.Columns.AddRange(new DataGridViewColumn[] { appIconColumn, appNameColumn, firstStartedAtColumn, lastObservedAtColumn, activeUsageTimeColumn, usageRatioColumn, switchCountColumn });
            usageGrid.Dock = DockStyle.Fill;
            usageGrid.Location = new Point(3, 51);
            usageGrid.MultiSelect = false;
            usageGrid.Name = "usageGrid";
            usageGrid.ReadOnly = true;
            usageGrid.RowHeadersVisible = false;
            usageGrid.ScrollBars = ScrollBars.Both;
            usageGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            usageGrid.Size = new Size(706, 366);
            usageGrid.TabIndex = 1;
            usageGrid.ColumnHeaderMouseClick += OnUsageGridColumnHeaderMouseClick;
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
            appNameColumn.HeaderText = "앱";
            appNameColumn.MinimumWidth = 180;
            appNameColumn.Name = "appNameColumn";
            appNameColumn.ReadOnly = true;
            appNameColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            appNameColumn.Width = 220;
            // 
            // firstStartedAtColumn
            // 
            firstStartedAtColumn.DataPropertyName = "FirstStartedAtText";
            firstStartedAtColumn.HeaderText = "첫 시작";
            firstStartedAtColumn.MinimumWidth = 90;
            firstStartedAtColumn.Name = "firstStartedAtColumn";
            firstStartedAtColumn.ReadOnly = true;
            firstStartedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            firstStartedAtColumn.Width = 100;
            // 
            // lastObservedAtColumn
            // 
            lastObservedAtColumn.DataPropertyName = "LastObservedAtText";
            lastObservedAtColumn.HeaderText = "마지막 감지";
            lastObservedAtColumn.MinimumWidth = 100;
            lastObservedAtColumn.Name = "lastObservedAtColumn";
            lastObservedAtColumn.ReadOnly = true;
            lastObservedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            lastObservedAtColumn.Width = 110;
            // 
            // activeUsageTimeColumn
            // 
            activeUsageTimeColumn.DataPropertyName = "ActiveUsageTimeText";
            activeUsageTimeColumn.HeaderText = "활성 사용 시간";
            activeUsageTimeColumn.MinimumWidth = 120;
            activeUsageTimeColumn.Name = "activeUsageTimeColumn";
            activeUsageTimeColumn.ReadOnly = true;
            activeUsageTimeColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            activeUsageTimeColumn.Width = 130;
            // 
            // usageRatioColumn
            // 
            usageRatioColumn.DataPropertyName = "UsageRatioText";
            usageRatioColumn.HeaderText = "활성 비중";
            usageRatioColumn.MinimumWidth = 80;
            usageRatioColumn.Name = "usageRatioColumn";
            usageRatioColumn.ReadOnly = true;
            usageRatioColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            usageRatioColumn.ToolTipText = "오늘 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.";
            usageRatioColumn.Width = 90;
            // 
            // switchCountColumn
            // 
            switchCountColumn.DataPropertyName = "SwitchCountText";
            switchCountColumn.HeaderText = "전환 횟수";
            switchCountColumn.MinimumWidth = 90;
            switchCountColumn.Name = "switchCountColumn";
            switchCountColumn.ReadOnly = true;
            switchCountColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            switchCountColumn.Width = 100;
            //
            // detailTab
            //
            detailTab.Controls.Add(detailSplitContainer);
            detailTab.Controls.Add(detailFilterPanel);
            detailTab.Location = new Point(4, 24);
            detailTab.Name = "detailTab";
            detailTab.Padding = new Padding(3);
            detailTab.Size = new Size(712, 420);
            detailTab.TabIndex = 1;
            detailTab.Text = "상세";
            detailTab.UseVisualStyleBackColor = true;
            //
            // detailFilterPanel
            //
            detailFilterPanel.Controls.Add(currentTrackingScopeOnlyCheckBox);
            detailFilterPanel.Controls.Add(runningRuntimeOnlyCheckBox);
            detailFilterPanel.Dock = DockStyle.Top;
            detailFilterPanel.Location = new Point(3, 3);
            detailFilterPanel.Name = "detailFilterPanel";
            detailFilterPanel.Size = new Size(706, 32);
            detailFilterPanel.TabIndex = 1;
            //
            // currentTrackingScopeOnlyCheckBox
            //
            currentTrackingScopeOnlyCheckBox.AutoSize = true;
            currentTrackingScopeOnlyCheckBox.Checked = true;
            currentTrackingScopeOnlyCheckBox.CheckState = CheckState.Checked;
            currentTrackingScopeOnlyCheckBox.Location = new Point(8, 7);
            currentTrackingScopeOnlyCheckBox.Name = "currentTrackingScopeOnlyCheckBox";
            currentTrackingScopeOnlyCheckBox.Size = new Size(134, 19);
            currentTrackingScopeOnlyCheckBox.TabIndex = 0;
            currentTrackingScopeOnlyCheckBox.Text = "현재 추적 범위만";
            currentTrackingScopeOnlyCheckBox.UseVisualStyleBackColor = true;
            currentTrackingScopeOnlyCheckBox.CheckedChanged += OnCurrentTrackingScopeOnlyCheckBoxCheckedChanged;
            //
            // runningRuntimeOnlyCheckBox
            //
            runningRuntimeOnlyCheckBox.AutoSize = true;
            runningRuntimeOnlyCheckBox.Location = new Point(154, 7);
            runningRuntimeOnlyCheckBox.Name = "runningRuntimeOnlyCheckBox";
            runningRuntimeOnlyCheckBox.Size = new Size(82, 19);
            runningRuntimeOnlyCheckBox.TabIndex = 1;
            runningRuntimeOnlyCheckBox.Text = "실행 중만";
            runningRuntimeOnlyCheckBox.UseVisualStyleBackColor = true;
            runningRuntimeOnlyCheckBox.CheckedChanged += OnRunningRuntimeOnlyCheckBoxCheckedChanged;
            //
            // detailSplitContainer
            //
            detailSplitContainer.Dock = DockStyle.Fill;
            detailSplitContainer.Location = new Point(3, 35);
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
            detailSplitContainer.Size = new Size(706, 382);
            detailSplitContainer.SplitterDistance = 245;
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
            runtimeGrid.Columns.AddRange(new DataGridViewColumn[] { runtimeAppIconColumn, runtimeAppNameColumn, runtimeFirstObservedAtColumn, runtimeLastObservedAtColumn, runtimeDurationColumn, runtimeActiveUsageColumn, runtimeActualUsageRatioColumn, runtimeSessionCountColumn, runtimeStatusColumn });
            runtimeGrid.Dock = DockStyle.Fill;
            runtimeGrid.Location = new Point(0, 0);
            runtimeGrid.MultiSelect = false;
            runtimeGrid.Name = "runtimeGrid";
            runtimeGrid.ReadOnly = true;
            runtimeGrid.RowHeadersVisible = false;
            runtimeGrid.ScrollBars = ScrollBars.Both;
            runtimeGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            runtimeGrid.Size = new Size(706, 245);
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
            runtimeAppNameColumn.HeaderText = "앱";
            runtimeAppNameColumn.MinimumWidth = 180;
            runtimeAppNameColumn.Name = "runtimeAppNameColumn";
            runtimeAppNameColumn.ReadOnly = true;
            runtimeAppNameColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeAppNameColumn.Width = 220;
            //
            // runtimeFirstObservedAtColumn
            //
            runtimeFirstObservedAtColumn.DataPropertyName = "FirstObservedAtText";
            runtimeFirstObservedAtColumn.HeaderText = "첫 감지";
            runtimeFirstObservedAtColumn.MinimumWidth = 90;
            runtimeFirstObservedAtColumn.Name = "runtimeFirstObservedAtColumn";
            runtimeFirstObservedAtColumn.ReadOnly = true;
            runtimeFirstObservedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeFirstObservedAtColumn.Width = 100;
            //
            // runtimeLastObservedAtColumn
            //
            runtimeLastObservedAtColumn.DataPropertyName = "LastObservedAtText";
            runtimeLastObservedAtColumn.HeaderText = "마지막 감지";
            runtimeLastObservedAtColumn.MinimumWidth = 100;
            runtimeLastObservedAtColumn.Name = "runtimeLastObservedAtColumn";
            runtimeLastObservedAtColumn.ReadOnly = true;
            runtimeLastObservedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeLastObservedAtColumn.ToolTipText = "백그라운드 앱 추적이 실제로 마지막 관측한 시각입니다.";
            runtimeLastObservedAtColumn.Width = 110;
            //
            // runtimeDurationColumn
            //
            runtimeDurationColumn.DataPropertyName = "RuntimeText";
            runtimeDurationColumn.HeaderText = "실행 시간";
            runtimeDurationColumn.MinimumWidth = 110;
            runtimeDurationColumn.Name = "runtimeDurationColumn";
            runtimeDurationColumn.ReadOnly = true;
            runtimeDurationColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeDurationColumn.ToolTipText = "실행 중인 앱은 현재 시각 기준으로 계속 증가하고, 종료된 앱은 마지막 관측 시각까지 계산합니다.";
            runtimeDurationColumn.Width = 120;
            //
            // runtimeActiveUsageColumn
            //
            runtimeActiveUsageColumn.DataPropertyName = "ActiveUsageTimeText";
            runtimeActiveUsageColumn.HeaderText = "활성 사용 시간";
            runtimeActiveUsageColumn.MinimumWidth = 120;
            runtimeActiveUsageColumn.Name = "runtimeActiveUsageColumn";
            runtimeActiveUsageColumn.ReadOnly = true;
            runtimeActiveUsageColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeActiveUsageColumn.Width = 130;
            //
            // runtimeActualUsageRatioColumn
            //
            runtimeActualUsageRatioColumn.DataPropertyName = "ActualUsageRatioText";
            runtimeActualUsageRatioColumn.HeaderText = "실사용 비율";
            runtimeActualUsageRatioColumn.MinimumWidth = 100;
            runtimeActualUsageRatioColumn.Name = "runtimeActualUsageRatioColumn";
            runtimeActualUsageRatioColumn.ReadOnly = true;
            runtimeActualUsageRatioColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeActualUsageRatioColumn.ToolTipText = "실행 시간 중 실제 foreground 활성 사용 시간이 차지한 비율입니다.";
            runtimeActualUsageRatioColumn.Width = 110;
            //
            // runtimeSessionCountColumn
            //
            runtimeSessionCountColumn.DataPropertyName = "RuntimeSegmentCountText";
            runtimeSessionCountColumn.HeaderText = "실행 구간";
            runtimeSessionCountColumn.MinimumWidth = 80;
            runtimeSessionCountColumn.Name = "runtimeSessionCountColumn";
            runtimeSessionCountColumn.ReadOnly = true;
            runtimeSessionCountColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSessionCountColumn.ToolTipText = "앱이 이어서 실행된 것으로 관측된 구간 수입니다.";
            runtimeSessionCountColumn.Width = 90;
            //
            // runtimeStatusColumn
            //
            runtimeStatusColumn.DataPropertyName = "StatusText";
            runtimeStatusColumn.HeaderText = "상태";
            runtimeStatusColumn.MinimumWidth = 80;
            runtimeStatusColumn.Name = "runtimeStatusColumn";
            runtimeStatusColumn.ReadOnly = true;
            runtimeStatusColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeStatusColumn.ToolTipText = "현재 설정 기준으로 실행 중, 종료, 추적 범위 밖 상태를 표시합니다.";
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
            runtimeSegmentsGrid.Size = new Size(706, 133);
            runtimeSegmentsGrid.TabIndex = 0;
            runtimeSegmentsGrid.ColumnHeaderMouseClick += OnRuntimeSegmentsGridColumnHeaderMouseClick;
            //
            // runtimeSegmentStartedAtColumn
            //
            runtimeSegmentStartedAtColumn.DataPropertyName = "StartedAtText";
            runtimeSegmentStartedAtColumn.HeaderText = "시작";
            runtimeSegmentStartedAtColumn.MinimumWidth = 90;
            runtimeSegmentStartedAtColumn.Name = "runtimeSegmentStartedAtColumn";
            runtimeSegmentStartedAtColumn.ReadOnly = true;
            runtimeSegmentStartedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentStartedAtColumn.Width = 100;
            //
            // runtimeSegmentEndedAtColumn
            //
            runtimeSegmentEndedAtColumn.DataPropertyName = "EndedAtText";
            runtimeSegmentEndedAtColumn.HeaderText = "종료";
            runtimeSegmentEndedAtColumn.MinimumWidth = 90;
            runtimeSegmentEndedAtColumn.Name = "runtimeSegmentEndedAtColumn";
            runtimeSegmentEndedAtColumn.ReadOnly = true;
            runtimeSegmentEndedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentEndedAtColumn.Width = 100;
            //
            // runtimeSegmentDurationColumn
            //
            runtimeSegmentDurationColumn.DataPropertyName = "DurationText";
            runtimeSegmentDurationColumn.HeaderText = "시간";
            runtimeSegmentDurationColumn.MinimumWidth = 100;
            runtimeSegmentDurationColumn.Name = "runtimeSegmentDurationColumn";
            runtimeSegmentDurationColumn.ReadOnly = true;
            runtimeSegmentDurationColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentDurationColumn.Width = 110;
            //
            // runtimeSegmentStatusColumn
            //
            runtimeSegmentStatusColumn.DataPropertyName = "StatusText";
            runtimeSegmentStatusColumn.HeaderText = "상태";
            runtimeSegmentStatusColumn.MinimumWidth = 80;
            runtimeSegmentStatusColumn.Name = "runtimeSegmentStatusColumn";
            runtimeSegmentStatusColumn.ReadOnly = true;
            runtimeSegmentStatusColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentStatusColumn.Width = 90;
            //
            // runtimeSegmentObservationTypeColumn
            //
            runtimeSegmentObservationTypeColumn.DataPropertyName = "ObservationTypeText";
            runtimeSegmentObservationTypeColumn.HeaderText = "관측 기준";
            runtimeSegmentObservationTypeColumn.MinimumWidth = 110;
            runtimeSegmentObservationTypeColumn.Name = "runtimeSegmentObservationTypeColumn";
            runtimeSegmentObservationTypeColumn.ReadOnly = true;
            runtimeSegmentObservationTypeColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentObservationTypeColumn.Width = 130;
            //
            // runtimeSegmentProcessIdColumn
            //
            runtimeSegmentProcessIdColumn.DataPropertyName = "ProcessId";
            runtimeSegmentProcessIdColumn.HeaderText = "PID";
            runtimeSegmentProcessIdColumn.MinimumWidth = 70;
            runtimeSegmentProcessIdColumn.Name = "runtimeSegmentProcessIdColumn";
            runtimeSegmentProcessIdColumn.ReadOnly = true;
            runtimeSegmentProcessIdColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            runtimeSegmentProcessIdColumn.Width = 80;
            // 
            // timelineTab
            // 
            timelineTab.Controls.Add(timelineGrid);
            timelineTab.Location = new Point(4, 24);
            timelineTab.Name = "timelineTab";
            timelineTab.Padding = new Padding(3);
            timelineTab.Size = new Size(712, 420);
            timelineTab.TabIndex = 2;
            timelineTab.Text = "타임라인";
            timelineTab.UseVisualStyleBackColor = true;
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
            timelineGrid.Location = new Point(3, 3);
            timelineGrid.MultiSelect = false;
            timelineGrid.Name = "timelineGrid";
            timelineGrid.ReadOnly = true;
            timelineGrid.RowHeadersVisible = false;
            timelineGrid.ScrollBars = ScrollBars.Both;
            timelineGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            timelineGrid.Size = new Size(706, 414);
            timelineGrid.TabIndex = 0;
            timelineGrid.ColumnHeaderMouseClick += OnTimelineGridColumnHeaderMouseClick;
            // 
            // timelineTypeColumn
            // 
            timelineTypeColumn.DataPropertyName = "ActivityType";
            timelineTypeColumn.HeaderText = "유형";
            timelineTypeColumn.MinimumWidth = 80;
            timelineTypeColumn.Name = "timelineTypeColumn";
            timelineTypeColumn.ReadOnly = true;
            timelineTypeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineTypeColumn.Width = 90;
            // 
            // timelineStartedAtColumn
            // 
            timelineStartedAtColumn.DataPropertyName = "StartedAtText";
            timelineStartedAtColumn.HeaderText = "시작";
            timelineStartedAtColumn.MinimumWidth = 90;
            timelineStartedAtColumn.Name = "timelineStartedAtColumn";
            timelineStartedAtColumn.ReadOnly = true;
            timelineStartedAtColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            timelineStartedAtColumn.Width = 100;
            // 
            // timelineEndedAtColumn
            // 
            timelineEndedAtColumn.DataPropertyName = "EndedAtText";
            timelineEndedAtColumn.HeaderText = "종료";
            timelineEndedAtColumn.MinimumWidth = 90;
            timelineEndedAtColumn.Name = "timelineEndedAtColumn";
            timelineEndedAtColumn.ReadOnly = true;
            timelineEndedAtColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineEndedAtColumn.Width = 100;
            // 
            // timelineDurationColumn
            // 
            timelineDurationColumn.DataPropertyName = "DurationText";
            timelineDurationColumn.HeaderText = "시간";
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
            timelineDisplayNameColumn.HeaderText = "앱";
            timelineDisplayNameColumn.MinimumWidth = 220;
            timelineDisplayNameColumn.Name = "timelineDisplayNameColumn";
            timelineDisplayNameColumn.ReadOnly = true;
            timelineDisplayNameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            timelineDisplayNameColumn.Width = 260;
            // 
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 480);
            Controls.Add(mainTabs);
            Controls.Add(statusLabel);
            Controls.Add(mainMenuStrip);
            MainMenuStrip = mainMenuStrip;
            Name = "Form1";
            Text = "TimePilot";
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            mainTabs.ResumeLayout(false);
            summaryTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)usageGrid).EndInit();
            detailTab.ResumeLayout(false);
            detailFilterPanel.ResumeLayout(false);
            detailFilterPanel.PerformLayout();
            detailSplitContainer.Panel1.ResumeLayout(false);
            detailSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)detailSplitContainer).EndInit();
            detailSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)runtimeGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)runtimeSegmentsGrid).EndInit();
            timelineTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)timelineGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip mainMenuStrip;
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem exportCsvMenuItem;
        private ToolStripMenuItem exitMenuItem;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem preferencesMenuItem;
        private ToolStripMenuItem helpMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private Label statusLabel;
        private TabControl mainTabs;
        private TabPage summaryTab;
        private FlowLayoutPanel runtimeCoverageSummaryPanel;
        private ToolTip runtimeCoverageSummaryToolTip;
        private DataGridView usageGrid;
        private DataGridViewImageColumn appIconColumn;
        private DataGridViewTextBoxColumn appNameColumn;
        private DataGridViewTextBoxColumn firstStartedAtColumn;
        private DataGridViewTextBoxColumn lastObservedAtColumn;
        private DataGridViewTextBoxColumn activeUsageTimeColumn;
        private DataGridViewTextBoxColumn usageRatioColumn;
        private DataGridViewTextBoxColumn switchCountColumn;
        private TabPage detailTab;
        private Panel detailFilterPanel;
        private CheckBox currentTrackingScopeOnlyCheckBox;
        private CheckBox runningRuntimeOnlyCheckBox;
        private SplitContainer detailSplitContainer;
        private DataGridView runtimeGrid;
        private DataGridViewImageColumn runtimeAppIconColumn;
        private DataGridViewTextBoxColumn runtimeAppNameColumn;
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
