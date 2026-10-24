using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DataOperationStatusFormatterTests
    {
        public DataOperationStatusFormatterTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void FormatCsvExportRangeForFileName_UsesSingleDateForSameDay()
        {
            var text = DataOperationStatusFormatter.FormatCsvExportRangeForFileName(
                new DateTime(2026, 6, 25),
                new DateTime(2026, 6, 25));

            Assert.Equal("2026-06-25", text);
        }

        [Fact]
        public void FormatCsvExportRangeForFileName_UsesDateRangeForMultipleDays()
        {
            var text = DataOperationStatusFormatter.FormatCsvExportRangeForFileName(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 25));

            Assert.Equal("2026-06-01_to_2026-06-25", text);
        }

        [Fact]
        public void BuildStatusMessages_ReturnsEnglishMessages()
        {
            Assert.Equal("Export in progress...", DataOperationStatusFormatter.BuildInProgressStatus("Export"));
            Assert.Equal("Export completed.", DataOperationStatusFormatter.BuildCompletedStatus("Export"));
            Assert.Equal("Export failed.", DataOperationStatusFormatter.BuildFailedStatus("Export"));
        }

        [Theory]
        [InlineData(0, "Preparing the backup...", "백업을 준비하는 중...")]
        [InlineData(1, "Creating a database snapshot...", "데이터베이스 복사본을 만드는 중...")]
        [InlineData(2, "Compressing backup files...", "백업 파일을 압축하는 중...")]
        [InlineData(3, "Verifying the backup...", "백업 파일을 확인하는 중...")]
        [InlineData(4, "Finishing the backup...", "백업을 마무리하는 중...")]
        [InlineData(-1, "Preparing the backup...", "백업을 준비하는 중...")]
        public void BuildBackupProgressStatus_PreservesMessagesInBothLanguages(
            int stageValue,
            string expectedEnglish,
            string expectedKorean)
        {
            var stage = (DataBackupProgressStage)stageValue;

            Assert.Equal(expectedEnglish,
                DataOperationStatusFormatter.BuildBackupProgressStatus(stage, UiLanguage.English));
            Assert.Equal(expectedKorean,
                DataOperationStatusFormatter.BuildBackupProgressStatus(stage, UiLanguage.Korean));
        }
    }
}
