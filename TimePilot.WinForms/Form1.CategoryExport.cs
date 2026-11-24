namespace TimePilot.WinForms;

public partial class Form1
{
    private async void OnExportAppCategoriesMenuItemClick(object? sender, EventArgs e)
    {
        if (storage is null || isExportRunning || isAutomaticBackupRunning || isCategoryTransferOpen || isBackupRemovalOpen) return;
        var text = new CategoryImportText(settings.UiLanguage);
        if (CenteredMessageDialog.Show(this, text.ExportWarning, text.ExportTitle, MessageBoxButtons.OKCancel,
            MessageBoxIcon.Information) != DialogResult.OK) return;
        using var file = new SaveFileDialog
        {
            DefaultExt = "json", AddExtension = true, Filter = text.ExportFilter, Title = text.ExportTitle,
            FileName = $"ActiveLogbook-categories-{DateTime.Now:yyyy-MM-dd-HHmmss}-{Guid.NewGuid():N}.json",
            OverwritePrompt = false
        };
        if (file.ShowDialog(this) != DialogResult.OK) return;
        isCategoryTransferOpen = true;
        using var progress = new OperationProgressForm(text.ExportTitle, text.Exporting);
        try
        {
            SetExportRunning(true, text.Exporting);
            progress.ShowCentered(this);
            var path = file.FileName;
            await Task.Run(() => CategoryTransferFile.Export(AppDataPaths.DatabasePath, path, DateTimeOffset.UtcNow));
            progress.Close();
            if (!isClosing && !IsDisposed)
                CenteredMessageDialog.Show(this, text.ExportCompleted(path), text.ExportTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            progress.Close();
            if (!isClosing && !IsDisposed)
                CenteredMessageDialog.Show(this, text.ExportFailed(ex.Message), text.ExportTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            progress.Close();
            isCategoryTransferOpen = false;
            if (!IsDisposed) { SetExportRunning(false, null); ClearExportStatus(); }
        }
    }
}
