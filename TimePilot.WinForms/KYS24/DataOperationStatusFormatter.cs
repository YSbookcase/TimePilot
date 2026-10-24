using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal static class DataOperationStatusFormatter
    {
        public static string BuildBackupProgressStatus(DataBackupProgressStage stage, UiLanguage language)
        {
            if (language == UiLanguage.English)
            {
                return stage switch
                {
                    DataBackupProgressStage.CopyingDatabase => "Creating a database snapshot...",
                    DataBackupProgressStage.CompressingFiles => "Compressing backup files...",
                    DataBackupProgressStage.VerifyingBackup => "Verifying the backup...",
                    DataBackupProgressStage.Finalizing => "Finishing the backup...",
                    _ => "Preparing the backup..."
                };
            }

            return stage switch
            {
                DataBackupProgressStage.CopyingDatabase => "데이터베이스 복사본을 만드는 중...",
                DataBackupProgressStage.CompressingFiles => "백업 파일을 압축하는 중...",
                DataBackupProgressStage.VerifyingBackup => "백업 파일을 확인하는 중...",
                DataBackupProgressStage.Finalizing => "백업을 마무리하는 중...",
                _ => "백업을 준비하는 중..."
            };
        }

        public static string FormatCsvExportRangeForFileName(DateTime startDate, DateTime endDate)
        {
            return startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : $"{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";
        }

        public static string BuildInProgressStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} in progress..."
                : $"{title} 진행 중...";
        }

        public static string BuildCompletedStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} completed."
                : $"{title} 완료.";
        }

        public static string BuildFailedStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} failed."
                : $"{title} 실패.";
        }
    }
}
