using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms;

internal sealed class CategoryImportForm : Form
{
    private readonly CategoryImportText text;
    private readonly Func<Task<string>> apply;
    private readonly Button applyButton = new();
    private readonly Button closeButton = new();
    private readonly TextBox status = new();
    private readonly ProgressBar progress = new();
    private bool busy;
    public bool Applied { get; private set; }

    public CategoryImportForm(CategoryImportPlan plan, UiLanguage language, Func<Task<string>> apply)
    {
        text = new(language);
        this.apply = apply;
        Text = text.Title;
        Name = nameof(CategoryImportForm);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 580);
        MinimumSize = new Size(740, 540);
        ShowIcon = false;
        ShowInTaskbar = false;
        MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, plan.ProcessNameOnly ? 112 : 66));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(new Label { Name = "categoryImportWarning", Dock = DockStyle.Fill,
            Text = plan.ProcessNameOnly ? text.Warning + "\n\n" + text.ProcessNameOnlyWarning : text.Warning }, 0, 0);
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = text.Summary(plan) }, 0, 1);
        var grid = new DataGridView
        {
            Name = "categoryImportGrid", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        foreach (var (header, width) in new[] { (text.App, 25F), (text.Current, 20F), (text.Backup, 20F), (text.Result, 35F) })
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = width, SortMode = DataGridViewColumnSortMode.NotSortable });
        foreach (var row in plan.Rows.OrderBy(r => r.Action == CategoryImportAction.MatchCategory ? int.MaxValue : (int)r.Action))
            grid.Rows.Add(string.IsNullOrEmpty(row.App) ? (language == UiLanguage.English ? "Category" : "분류") : row.App,
                text.Category(row.Current), text.Category(row.Backup), text.Action(row.Action));
        layout.Controls.Add(grid, 0, 2);
        status.Name = "categoryImportStatus";
        status.Dock = DockStyle.Fill;
        status.Multiline = true;
        status.ReadOnly = true;
        status.ScrollBars = ScrollBars.Vertical;
        layout.Controls.Add(status, 0, 3);
        progress.Dock = DockStyle.Fill;
        progress.Style = ProgressBarStyle.Marquee;
        progress.Visible = false;
        layout.Controls.Add(progress, 0, 4);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        closeButton.Name = "categoryImportClose";
        closeButton.Text = text.Cancel;
        closeButton.Size = new Size(90, 30);
        closeButton.Click += (_, _) => Close();
        applyButton.Name = "categoryImportApply";
        applyButton.Text = text.Apply;
        applyButton.Size = new Size(300, 30);
        applyButton.Enabled = plan.AddedCategories + plan.AssignedApps > 0;
        applyButton.Click += async (_, _) => await ApplyAsync();
        footer.Controls.Add(closeButton);
        footer.Controls.Add(applyButton);
        layout.Controls.Add(footer, 0, 5);
        Controls.Add(layout);
        CancelButton = closeButton;
    }

    internal async Task ApplyAsync()
    {
        if (busy || !applyButton.Enabled) return;
        busy = true;
        applyButton.Enabled = false;
        closeButton.Enabled = false;
        progress.Visible = true;
        status.Text = text.Applying;
        try
        {
            var safetyPath = await apply();
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
