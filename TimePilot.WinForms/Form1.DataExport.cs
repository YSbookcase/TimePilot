namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private async void OnExportCsvMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null || isExportRunning)
                return;

            var storageSnapshot = storage;
            var now = DateTimeOffset.UtcNow;
            var today = now.ToLocalTime().Date;
            using var rangeDialog = new CsvExportRangeForm(today, settings.UiLanguage, GetRecordedDates);
            if (rangeDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var rangeText = DataOperationStatusFormatter.FormatCsvExportRangeForFileName(
                rangeDialog.StartDate,
                rangeDialog.EndDate);
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "csv",
                FileName = $"ActiveLogbook-usage-{rangeText}.csv",
                Filter = UiText.Main.CsvFilter,
                OverwritePrompt = false,
                Title = UiText.Main.CsvExportTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var startDate = rangeDialog.StartDate;
            var endDate = rangeDialog.EndDate;
            try
            {
                SetExportRunning(
                    true,
                    DataOperationStatusFormatter.BuildInProgressStatus(UiText.Main.CsvExportTitle));
                var fileName = dialog.FileName;
                var exportedFiles = await Task.Run(() =>
                {
                    var exporter = new UsageCsvExporter(storageSnapshot);
                    return exporter.ExportRange(fileName, startDate, endDate, now);
                });

                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.CsvExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.CsvExportCompleted(
                        exportedFiles.Count,
                        Path.GetDirectoryName(dialog.FileName)),
                    UiText.Main.CsvExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.CsvExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.CsvExportFailed(ex.Message),
                    UiText.Main.CsvExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
        }

        private void SetExportRunning(bool isRunning, string? message)
        {
            isExportRunning = isRunning;
            exportStatusText = message;
            mainMenuController.SetDataOperationsEnabled(!isRunning);
            UpdateWaitCursor();
            RefreshStatusLabel();
        }

        private void ClearExportStatus()
        {
            exportStatusText = null;
            RefreshStatusLabel();
        }

        private async void OnExportRawDataMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null || isExportRunning)
                return;

            var storageSnapshot = storage;
            var confirm = CenteredMessageDialog.Show(
                this,
                UiText.Main.RawDataExportWarning,
                UiText.Main.RawDataExportTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.OK)
                return;

            var now = DateTimeOffset.UtcNow;
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "zip",
                FileName = $"ActiveLogbook-raw-data-{now.ToLocalTime():yyyy-MM-dd}.zip",
                Filter = UiText.Main.ZipFilter,
                OverwritePrompt = true,
                Title = UiText.Main.RawDataExportTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                SetExportRunning(
                    true,
                    DataOperationStatusFormatter.BuildInProgressStatus(UiText.Main.RawDataExportTitle));
                var fileName = dialog.FileName;
                var exportedFiles = await Task.Run(() =>
                {
                    var exporter = new RawDataZipExporter(storageSnapshot);
                    return exporter.Export(fileName);
                });

                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.RawDataExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.RawDataExportCompleted(dialog.FileName, exportedFiles.Count),
                    UiText.Main.RawDataExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                SetExportRunning(
                    false,
                    DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.RawDataExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.RawDataExportFailed(ex.Message),
                    UiText.Main.RawDataExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
        }
    }
}
