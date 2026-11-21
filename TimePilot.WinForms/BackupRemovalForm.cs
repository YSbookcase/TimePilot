using System.Diagnostics;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms;

internal sealed class BackupRemovalForm : Form
{
    private readonly BackupRemovalPreparation preparation;
    private readonly BackupRemovalText text;
    private readonly UiLanguage language;
    private readonly Action<RemovalBackupReceipt>? recordSuccess;
    private readonly Action openInstalledApps;
    private readonly Button createButton = new();
    private readonly Button openSettingsButton = new();
    private readonly Button closeButton = new();
    private readonly Label statusLabel = new();
    private readonly Label historyLabel = new();
    private readonly TextBox backupPath = new();
    private readonly ProgressBar progressBar = new();
    private string? initialDirectory;
    private bool busy;

    internal BackupRemovalForm(BackupRemovalPreparation preparation, UiLanguage language,
        string? initialDirectory = null, DateTimeOffset? lastSuccessAt = null, string? lastPath = null,
        Action<RemovalBackupReceipt>? recordSuccess = null, Action? openInstalledApps = null)
    {
        this.preparation = preparation;
        this.language = language;
        this.initialDirectory = initialDirectory;
        this.recordSuccess = recordSuccess;
        this.openInstalledApps = openInstalledApps ?? (() => Process.Start(new ProcessStartInfo(BackupRemovalPreparation.InstalledAppsUri) { UseShellExecute = true }));
        text = new BackupRemovalText(language);
        Text = text.Title;
        Name = nameof(BackupRemovalForm);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(660, 520);
        MinimumSize = new Size(676, 559);
        StartPosition = FormStartPosition.CenterParent;
        ShowIcon = false;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 100F, 82F, 38F, 58F, 76F, 28F })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Name = "removalWarningLabel", Dock = DockStyle.Fill, Text = text.Warning }, 0, 0);
        historyLabel.Name = "removalHistoryLabel";
        historyLabel.Dock = DockStyle.Fill;
        historyLabel.AutoEllipsis = true;
        historyLabel.Text = lastSuccessAt is { } at && !string.IsNullOrWhiteSpace(lastPath)
            ? text.History(at, lastPath) : text.HistoryUnknown;
        layout.Controls.Add(historyLabel, 0, 1);
        createButton.Name = "createRemovalBackupButton";
        createButton.Text = text.Create;
        createButton.Size = new Size(260, 30);
        createButton.Click += OnCreateClick;
        layout.Controls.Add(createButton, 0, 2);
        backupPath.Name = "removalBackupPath";
        backupPath.Dock = DockStyle.Fill;
        backupPath.ReadOnly = true;
        backupPath.Multiline = true;
        backupPath.ScrollBars = ScrollBars.Vertical;
        layout.Controls.Add(backupPath, 0, 3);
        statusLabel.Name = "removalStatusLabel";
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Text = text.AwaitingBackup;
        layout.Controls.Add(statusLabel, 0, 4);
        progressBar.Name = "removalProgressBar";
        progressBar.Dock = DockStyle.Fill;
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.Visible = false;
        layout.Controls.Add(progressBar, 0, 5);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        closeButton.Name = "closeRemovalButton";
        closeButton.Text = text.Close;
        closeButton.Size = new Size(90, 30);
        closeButton.DialogResult = DialogResult.Cancel;
        openSettingsButton.Name = "openInstalledAppsButton";
        openSettingsButton.Text = text.OpenSettings;
        openSettingsButton.Size = new Size(240, 30);
        openSettingsButton.Enabled = false;
        openSettingsButton.Click += async (_, _) => await OpenSettingsAsync();
        footer.Controls.Add(closeButton);
        footer.Controls.Add(openSettingsButton);
        layout.Controls.Add(footer, 0, 6);
        Controls.Add(layout);
        CancelButton = closeButton;
    }

    private async void OnCreateClick(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = text.Create, Filter = UiText.Main.ZipFilter, DefaultExt = "zip", AddExtension = true,
            FileName = $"ActiveLogbook-before-removal-{DateTime.Now:yyyy-MM-dd-HHmmss}-{Guid.NewGuid():N}.zip",
            OverwritePrompt = false
        };
        if (Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await CreateBackupAsync(dialog.FileName);
    }

    internal async Task CreateBackupAsync(string path)
    {
        if (busy)
            return;
        SetBusy(true);
        backupPath.Clear();
        statusLabel.Text = DataOperationStatusFormatter.BuildBackupProgressStatus(DataBackupProgressStage.Preparing, language);
        try
        {
            var progress = new Progress<DataBackupProgressStage>(stage =>
            {
                if (!IsDisposed && busy)
                    statusLabel.Text = DataOperationStatusFormatter.BuildBackupProgressStatus(stage, language);
            });
            var receipt = await Task.Run(() => preparation.CreateBackup(path, DateTimeOffset.UtcNow, progress));
            if (IsDisposed)
                return;
            backupPath.Text = receipt.Path;
            initialDirectory = Path.GetDirectoryName(receipt.Path);
            statusLabel.Text = text.Ready;
            historyLabel.Text = text.VerifiedHistory(receipt.CreatedAt, receipt.Path);
            try { recordSuccess?.Invoke(receipt); }
            catch (Exception ex)
            {
                Debug.WriteLine($"Removal backup history could not be saved: {ex}");
                statusLabel.Text += "\n" + text.HistorySaveFailed;
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                statusLabel.Text = text.Failed(ex.Message);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    internal async Task OpenSettingsAsync()
    {
        if (busy || !preparation.CanOpenInstalledApps)
            return;
        SetBusy(true);
        statusLabel.Text = text.Opening;
        try
        {
            await Task.Run(() => preparation.VerifyBeforeOpeningSettings());
            if (IsDisposed)
                return;
            openInstalledApps();
            statusLabel.Text = text.SettingsOpened;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                statusLabel.Text = preparation.CanOpenInstalledApps ? text.OpenFailed(ex.Message) : text.Failed(ex.Message);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        busy = value;
        createButton.Enabled = !value;
        closeButton.Enabled = !value;
        openSettingsButton.Enabled = !value && preparation.CanOpenInstalledApps;
        progressBar.Visible = value;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy && e.CloseReason is CloseReason.UserClosing or CloseReason.FormOwnerClosing)
            e.Cancel = true;
        base.OnFormClosing(e);
    }
}
