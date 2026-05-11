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
            usageGrid = new DataGridView();
            appNameColumn = new DataGridViewTextBoxColumn();
            activeUsageTimeColumn = new DataGridViewTextBoxColumn();
            usageRatioColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)usageGrid).BeginInit();
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
            usageGrid.Columns.AddRange(new DataGridViewColumn[] { appNameColumn, activeUsageTimeColumn, usageRatioColumn });
            usageGrid.Dock = DockStyle.Fill;
            usageGrid.Location = new Point(0, 32);
            usageGrid.MultiSelect = false;
            usageGrid.Name = "usageGrid";
            usageGrid.ReadOnly = true;
            usageGrid.RowHeadersVisible = false;
            usageGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            usageGrid.Size = new Size(720, 448);
            usageGrid.TabIndex = 1;
            // 
            // appNameColumn
            // 
            appNameColumn.DataPropertyName = "AppName";
            appNameColumn.HeaderText = "앱";
            appNameColumn.Name = "appNameColumn";
            appNameColumn.ReadOnly = true;
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
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 480);
            Controls.Add(usageGrid);
            Controls.Add(statusLabel);
            Name = "Form1";
            Text = "TimePilot";
            ((System.ComponentModel.ISupportInitialize)usageGrid).EndInit();
            ResumeLayout(false);
        }

        private Label statusLabel;
        private DataGridView usageGrid;
        private DataGridViewTextBoxColumn appNameColumn;
        private DataGridViewTextBoxColumn activeUsageTimeColumn;
        private DataGridViewTextBoxColumn usageRatioColumn;

        #endregion
    }
}
