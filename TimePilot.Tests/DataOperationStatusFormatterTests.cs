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
    }
}
