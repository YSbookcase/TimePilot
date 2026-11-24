using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms;

internal sealed class CategoryImportForm : Form
{
    private readonly CategoryImportText text;
    private readonly Func<CategoryImportPlan, bool, bool, Task<string?>> apply;
    private readonly Func<bool> confirmWithoutBackup;
    private readonly Func<CategoryImportPlan, bool> confirmOverwrite;
    private CategoryImportPlan plan;
    private readonly ComboBox priority = new();
    private readonly CheckBox replaceColors = new();
    private readonly Label summary = new();
    private readonly DataGridView grid = new();
    private readonly CheckBox createBackup = new();
    private readonly Button applyButton = new();
    private readonly Button closeButton = new();
    private readonly TextBox status = new();
    private readonly ProgressBar progress = new();
    private bool busy;
    private bool started;
    public bool Applied { get; private set; }

    public CategoryImportForm(CategoryImportPlan plan, UiLanguage language, Func<CategoryImportPlan, bool, bool, Task<string?>> apply,
        Func<bool>? confirmWithoutBackup = null, Func<CategoryImportPlan, bool>? confirmOverwrite = null)
    {
        text = new(language);
        this.apply = apply;
        this.plan = plan;
        this.confirmWithoutBackup = confirmWithoutBackup ?? (() => CenteredMessageDialog.Show(this,
            text.SkipBackupConfirmation, text.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes);
        this.confirmOverwrite = confirmOverwrite ?? (approved => CenteredMessageDialog.Show(this,
            text.OverwriteConfirmation(approved), text.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes);
        Text = text.Title;
        Name = nameof(CategoryImportForm);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 660);
        MinimumSize = new Size(940, 612);
        ShowIcon = false;
        ShowInTaskbar = false;
        MinimizeBox = false;
        var hasChanges = plan.HasChanges;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, plan.ProcessNameOnly ? 112 : 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(new Label { Name = "categoryImportWarning", Dock = DockStyle.Fill,
            Text = plan.ProcessNameOnly ? text.Warning + "\n\n" + text.ProcessNameOnlyWarning : text.Warning }, 0, 0);
        var choices = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        choices.Controls.Add(new Label { AutoSize = true, Text = text.Priority, Margin = new Padding(0, 7, 8, 0) });
        priority.Name = "categoryImportPriority";
        priority.DropDownStyle = ComboBoxStyle.DropDownList;
        priority.Size = new Size(240, 28);
        priority.Items.AddRange(new object[] { text.CurrentPriority, text.FilePriority });
        priority.SelectedIndex = plan.Options?.PreferFile == true ? 1 : 0;
        replaceColors.Name = "categoryImportReplaceColors";
        replaceColors.Text = text.ReplaceColors;
        replaceColors.Size = new Size(480, 30);
        replaceColors.Checked = plan.Options?.ReplaceColors == true;
        choices.Controls.Add(priority);
        choices.Controls.Add(replaceColors);
        layout.Controls.Add(choices, 0, 1);
        summary.Name = "categoryImportSummary";
        summary.Dock = DockStyle.Fill;
        layout.Controls.Add(summary, 0, 2);
        grid.Name = "categoryImportGrid";
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        foreach (var (header, width) in new[] { (text.App, 18F), (text.Current, 16F), (text.Backup, 16F),
            (text.CurrentColor, 9F), (text.FileColor, 9F), (text.Result, 32F) })
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = width, SortMode = DataGridViewColumnSortMode.NotSortable });
        layout.Controls.Add(grid, 0, 3);
        createBackup.Name = "categoryImportCreateBackup";
        createBackup.Text = text.CreateBackup;
        createBackup.Dock = DockStyle.Fill;
        createBackup.Checked = true;
        createBackup.Enabled = hasChanges;
        createBackup.CheckedChanged += (_, _) => applyButton.Text = createBackup.Checked ? text.ApplyWithBackup : text.ApplyWithoutBackup;
        layout.Controls.Add(createBackup, 0, 4);
        status.Name = "categoryImportStatus";
        status.Dock = DockStyle.Fill;
        status.Multiline = true;
        status.ReadOnly = true;
        status.ScrollBars = ScrollBars.Vertical;
        if (!hasChanges) status.Text = text.NoChanges;
        layout.Controls.Add(status, 0, 5);
        progress.Dock = DockStyle.Fill;
        progress.Style = ProgressBarStyle.Marquee;
        progress.Visible = false;
        layout.Controls.Add(progress, 0, 6);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        closeButton.Name = "categoryImportClose";
        closeButton.Text = hasChanges ? text.Cancel : text.Close;
        closeButton.Size = new Size(90, 30);
        closeButton.Click += (_, _) => Close();
        applyButton.Name = "categoryImportApply";
        applyButton.Text = text.ApplyWithBackup;
        applyButton.Size = new Size(300, 30);
        applyButton.Enabled = hasChanges;
        applyButton.Click += async (_, _) => await ApplyAsync();
        footer.Controls.Add(closeButton);
        footer.Controls.Add(applyButton);
        layout.Controls.Add(footer, 0, 7);
        Controls.Add(layout);
        CancelButton = closeButton;
        priority.SelectedIndexChanged += (_, _) => UpdatePlan();
        replaceColors.CheckedChanged += (_, _) => UpdatePlan();
        UpdatePlan();
    }

    private void UpdatePlan()
    {
        if (started) return;
        plan = CategoryImportService.Replan(plan, new CategoryImportOptions(priority.SelectedIndex == 1, replaceColors.Checked));
        summary.Text = text.Summary(plan);
        grid.Rows.Clear();
        foreach (var row in plan.Rows.OrderBy(r => r.Action switch
        {
            CategoryImportAction.ReplaceUser => 0, CategoryImportAction.Replace => 1, CategoryImportAction.UpdateColor => 2,
            CategoryImportAction.AddCategory => 3, CategoryImportAction.Assign => 4, _ => 10 + (int)r.Action
        }))
        {
            var index = grid.Rows.Add(string.IsNullOrEmpty(row.App) ? text.CategoryRow : row.App,
                text.Category(row.Current), text.Category(row.Backup),
                row.AppId is null ? text.ColorValue(row.CurrentColor) : "",
                row.AppId is null ? text.ColorValue(row.FileColor) : "", text.Action(row.Action));
            if (row.AppId is null)
            {
                SetColorSwatch(grid.Rows[index].Cells[3], row.CurrentColor);
                SetColorSwatch(grid.Rows[index].Cells[4], row.FileColor);
            }
        }
        applyButton.Enabled = plan.HasChanges;
        createBackup.Enabled = plan.HasChanges;
        closeButton.Text = plan.HasChanges ? text.Cancel : text.Close;
        status.Text = plan.HasChanges ? "" : text.NoChanges;
    }

    private static void SetColorSwatch(DataGridViewCell cell, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var color = ColorTranslator.FromHtml(value);
        cell.Style.BackColor = Color.FromArgb(color.R, color.G, color.B);
        cell.Style.ForeColor = color.GetBrightness() < 0.5 ? Color.White : Color.Black;
        cell.ToolTipText = value;
    }

    internal async Task ApplyAsync()
    {
        if (busy || !applyButton.Enabled) return;
        var approvedPlan = plan;
        var overwriteApproved = approvedPlan.RequiresOverwriteApproval;
        if (overwriteApproved && !confirmOverwrite(approvedPlan)) return;
        var shouldCreateBackup = createBackup.Checked;
        if (!shouldCreateBackup && !confirmWithoutBackup()) return;
        busy = true;
        started = true;
        priority.Enabled = false;
        replaceColors.Enabled = false;
        applyButton.Enabled = false;
        closeButton.Enabled = false;
        createBackup.Enabled = false;
        progress.Visible = true;
        status.Text = shouldCreateBackup ? text.Applying : text.ApplyingWithoutBackup;
        try
        {
            var safetyPath = await apply(approvedPlan, shouldCreateBackup, overwriteApproved);
            Applied = true;
            if (!IsDisposed) status.Text = text.Completed(safetyPath);
        }
        catch (Exception ex)
        {
            if (!IsDisposed) status.Text = text.Failed(ex.Message);
        }
        finally
        {
            busy = false;
            if (!IsDisposed)
            {
                closeButton.Text = text.Close;
                closeButton.Enabled = true;
                progress.Visible = false;
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy && e.CloseReason is CloseReason.UserClosing or CloseReason.FormOwnerClosing) e.Cancel = true;
        base.OnFormClosing(e);
    }
}
