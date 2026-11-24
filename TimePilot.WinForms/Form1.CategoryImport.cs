namespace TimePilot.WinForms;

public partial class Form1
{
    private async void OnImportAppCategoriesMenuItemClick(object? sender, EventArgs e)
    {
        if (storage is null || isExportRunning || isAutomaticBackupRunning || isCategoryTransferOpen || isBackupRemovalOpen) return;
        var text = new CategoryImportText(settings.UiLanguage);
        using var file = new OpenFileDialog { DefaultExt = "json", Filter = text.ImportFilter, Title = text.Title };
        if (file.ShowDialog(this) != DialogResult.OK) return;
        isCategoryTransferOpen = true;
        using var progress = new OperationProgressForm(text.Title, text.Analyzing);
        try
        {
            SetExportRunning(true, text.Analyzing);
            progress.ShowCentered(this);
            var service = new CategoryImportService(AppDataPaths.DatabasePath);
            var path = file.FileName;
            var plan = await Task.Run(() => service.Preview(path));
            progress.Close();
            SetExportRunning(false, null);
            if (isClosing || IsDisposed) return;
            using var preview = new CategoryImportForm(plan, settings.UiLanguage, async (approvedPlan, createBackup, overwriteApproved) =>
            {
                var wasEnabled = sampleTimer.Enabled;
                sampleTimer.Stop();
                try
                {
                    return await Task.Run(() =>
                    {
                        if (!createBackup)
                        {
                            service.Apply(approvedPlan, DateTimeOffset.UtcNow, overwriteApproved);
                            return (string?)null;
                        }
                        Directory.CreateDirectory(AppDataPaths.BackupDirectory);
                        var safetyPath = Path.Combine(AppDataPaths.BackupDirectory,
                            $"ActiveLogbook-before-category-import-{Guid.NewGuid():N}.zip");
                        service.ApplyWithSafetyBackup(approvedPlan, new DataBackupService(), safetyPath, DateTimeOffset.UtcNow, overwriteApproved);
                        return safetyPath;
                    });
                }
                finally
                {
                    if (wasEnabled && !isClosing && !IsDisposed) sampleTimer.Start();
                }
            });
            preview.ShowDialog(this);
            if (preview.Applied)
            {
                try { RefreshViews(DateTimeOffset.UtcNow); }
                catch (Exception ex)
                {
                    CenteredMessageDialog.Show(this, text.RefreshFailed(ex.Message), text.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            if (!isClosing && !IsDisposed)
                CenteredMessageDialog.Show(this, text.Failed(ex.Message), text.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            progress.Close();
            isCategoryTransferOpen = false;
            if (!IsDisposed) { SetExportRunning(false, null); ClearExportStatus(); }
        }
    }
}
