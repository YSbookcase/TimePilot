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
            statusLabel = new Label();
            mainTabs = new TabControl();
            summaryTab = new TabPage();
            usageGrid = new DataGridView();
            appIconColumn = new DataGridViewImageColumn();
            appNameColumn = new DataGridViewTextBoxColumn();
            firstStartedAtColumn = new DataGridViewTextBoxColumn();
            lastObservedAtColumn = new DataGridViewTextBoxColumn();
            activeUsageTimeColumn = new DataGridViewTextBoxColumn();
            usageRatioColumn = new DataGridViewTextBoxColumn();
            timelineTab = new TabPage();
            timelineGrid = new DataGridView();
            timelineTypeColumn = new DataGridViewTextBoxColumn();
            timelineStartedAtColumn = new DataGridViewTextBoxColumn();
            timelineEndedAtColumn = new DataGridViewTextBoxColumn();
            timelineDurationColumn = new DataGridViewTextBoxColumn();
            timelineAppIconColumn = new DataGridViewImageColumn();
            timelineDisplayNameColumn = new DataGridViewTextBoxColumn();
            mainTabs.SuspendLayout();
            summaryTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)usageGrid).BeginInit();
            timelineTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)timelineGrid).BeginInit();
            SuspendLayout();
            // 
            // statusLabel
            // 
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Location = new Point(0, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Padding = new Padding(10, 0, 0, 0);
            statusLabel.Size = new Size(720, 32);
            statusLabel.TabIndex = 0;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // mainTabs
            // 
            mainTabs.Controls.Add(summaryTab);
            mainTabs.Controls.Add(timelineTab);
            mainTabs.Dock = DockStyle.Fill;
            mainTabs.Location = new Point(0, 32);
            mainTabs.Name = "mainTabs";
            mainTabs.SelectedIndex = 0;
            mainTabs.Size = new Size(720, 448);
            mainTabs.TabIndex = 1;
            // 
            // summaryTab
            // 
            summaryTab.Controls.Add(usageGrid);
            summaryTab.Location = new Point(4, 24);
            summaryTab.Name = "summaryTab";
            summaryTab.Padding = new Padding(3);
            summaryTab.Size = new Size(712, 420);
            summaryTab.TabIndex = 0;
            summaryTab.Text = "요약";
            summaryTab.UseVisualStyleBackColor = true;
            // 
            // usageGrid
            // 
            usageGrid.AllowUserToAddRows = false;
            usageGrid.AllowUserToDeleteRows = false;
            usageGrid.AllowUserToResizeRows = false;
            usageGrid.AutoGenerateColumns = false;
            usageGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            usageGrid.BackgroundColor = SystemColors.Window;
            usageGrid.BorderStyle = BorderStyle.None;
            usageGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            usageGrid.Columns.AddRange(new DataGridViewColumn[] { appIconColumn, appNameColumn, firstStartedAtColumn, lastObservedAtColumn, activeUsageTimeColumn, usageRatioColumn });
            usageGrid.Dock = DockStyle.Fill;
            usageGrid.Location = new Point(3, 3);
            usageGrid.MultiSelect = false;
            usageGrid.Name = "usageGrid";
            usageGrid.ReadOnly = true;
            usageGrid.RowHeadersVisible = false;
            usageGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            usageGrid.Size = new Size(706, 414);
            usageGrid.TabIndex = 1;
            // 
            // appIconColumn
            // 
            appIconColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            appIconColumn.DataPropertyName = "AppIcon";
            appIconColumn.HeaderText = "";
            appIconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            appIconColumn.Name = "appIconColumn";
            appIconColumn.ReadOnly = true;
            appIconColumn.Width = 36;
            // 
            // appNameColumn
            // 
            appNameColumn.DataPropertyName = "AppName";
            appNameColumn.HeaderText = "앱";
            appNameColumn.Name = "appNameColumn";
            appNameColumn.ReadOnly = true;
            // 
            // firstStartedAtColumn
            // 
            firstStartedAtColumn.DataPropertyName = "FirstStartedAtText";
            firstStartedAtColumn.HeaderText = "첫 시작";
            firstStartedAtColumn.Name = "firstStartedAtColumn";
            firstStartedAtColumn.ReadOnly = true;
            // 
            // lastObservedAtColumn
            // 
            lastObservedAtColumn.DataPropertyName = "LastObservedAtText";
            lastObservedAtColumn.HeaderText = "마지막 감지";
            lastObservedAtColumn.Name = "lastObservedAtColumn";
            lastObservedAtColumn.ReadOnly = true;
            // 
            // activeUsageTimeColumn
            // 
            activeUsageTimeColumn.DataPropertyName = "ActiveUsageTimeText";
            activeUsageTimeColumn.HeaderText = "활성 사용 시간";
            activeUsageTimeColumn.Name = "activeUsageTimeColumn";
            activeUsageTimeColumn.ReadOnly = true;
            // 
            // usageRatioColumn
            // 
            usageRatioColumn.DataPropertyName = "UsageRatioText";
            usageRatioColumn.HeaderText = "비율";
            usageRatioColumn.Name = "usageRatioColumn";
            usageRatioColumn.ReadOnly = true;
            // 
            // timelineTab
            // 
            timelineTab.Controls.Add(timelineGrid);
            timelineTab.Location = new Point(4, 24);
            timelineTab.Name = "timelineTab";
            timelineTab.Padding = new Padding(3);
            timelineTab.Size = new Size(712, 420);
            timelineTab.TabIndex = 1;
            timelineTab.Text = "타임라인";
            timelineTab.UseVisualStyleBackColor = true;
            // 
            // timelineGrid
            // 
            timelineGrid.AllowUserToAddRows = false;
            timelineGrid.AllowUserToDeleteRows = false;
            timelineGrid.AllowUserToResizeRows = false;
            timelineGrid.AutoGenerateColumns = false;
            timelineGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            timelineGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            timelineGrid.Size = new Size(706, 414);
            timelineGrid.TabIndex = 0;
            // 
            // timelineTypeColumn
            // 
            timelineTypeColumn.DataPropertyName = "ActivityType";
            timelineTypeColumn.HeaderText = "유형";
            timelineTypeColumn.Name = "timelineTypeColumn";
            timelineTypeColumn.ReadOnly = true;
            // 
            // timelineStartedAtColumn
            // 
            timelineStartedAtColumn.DataPropertyName = "StartedAtText";
            timelineStartedAtColumn.HeaderText = "시작";
            timelineStartedAtColumn.Name = "timelineStartedAtColumn";
            timelineStartedAtColumn.ReadOnly = true;
            // 
            // timelineEndedAtColumn
            // 
            timelineEndedAtColumn.DataPropertyName = "EndedAtText";
            timelineEndedAtColumn.HeaderText = "종료";
            timelineEndedAtColumn.Name = "timelineEndedAtColumn";
            timelineEndedAtColumn.ReadOnly = true;
            // 
            // timelineDurationColumn
            // 
            timelineDurationColumn.DataPropertyName = "DurationText";
            timelineDurationColumn.HeaderText = "시간";
            timelineDurationColumn.Name = "timelineDurationColumn";
            timelineDurationColumn.ReadOnly = true;
            // 
            // timelineAppIconColumn
            // 
            timelineAppIconColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            timelineAppIconColumn.DataPropertyName = "AppIcon";
            timelineAppIconColumn.HeaderText = "";
            timelineAppIconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            timelineAppIconColumn.Name = "timelineAppIconColumn";
            timelineAppIconColumn.ReadOnly = true;
            timelineAppIconColumn.Width = 36;
            // 
            // timelineDisplayNameColumn
            // 
            timelineDisplayNameColumn.DataPropertyName = "DisplayName";
            timelineDisplayNameColumn.HeaderText = "앱";
            timelineDisplayNameColumn.Name = "timelineDisplayNameColumn";
            timelineDisplayNameColumn.ReadOnly = true;
            // 
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 480);
            Controls.Add(mainTabs);
            Controls.Add(statusLabel);
            Name = "Form1";
            Text = "TimePilot";
            mainTabs.ResumeLayout(false);
            summaryTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)usageGrid).EndInit();
            timelineTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)timelineGrid).EndInit();
            ResumeLayout(false);
        }

        private Label statusLabel;
        private TabControl mainTabs;
        private TabPage summaryTab;
        private DataGridView usageGrid;
        private DataGridViewImageColumn appIconColumn;
        private DataGridViewTextBoxColumn appNameColumn;
        private DataGridViewTextBoxColumn firstStartedAtColumn;
        private DataGridViewTextBoxColumn lastObservedAtColumn;
        private DataGridViewTextBoxColumn activeUsageTimeColumn;
        private DataGridViewTextBoxColumn usageRatioColumn;
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
