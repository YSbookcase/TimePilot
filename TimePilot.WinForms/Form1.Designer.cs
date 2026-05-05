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
            usageListBox = new ListBox();
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
            // usageListBox
            // 
            usageListBox.Dock = DockStyle.Fill;
            usageListBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            usageListBox.FormattingEnabled = true;
            usageListBox.ItemHeight = 17;
            usageListBox.Location = new Point(0, 32);
            usageListBox.Name = "usageListBox";
            usageListBox.Size = new Size(720, 448);
            usageListBox.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 480);
            Controls.Add(usageListBox);
            Controls.Add(statusLabel);
            Name = "Form1";
            Text = "TimePilot";
            ResumeLayout(false);
        }

        private Label statusLabel;
        private ListBox usageListBox;

        #endregion
    }
}
