using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class StatusLabelFormatterTests
    {
        [Theory]
        [InlineData("Ready", "Exporting", "Loading", "Slow", "Ready | Exporting | Loading | Slow")]
        [InlineData("준비", null, "요약", null, "준비 | 요약")]
        [InlineData(null, "Exporting", null, null, "Exporting")]
        [InlineData("", null, " \t ", "", "")]
        [InlineData("Ready", "", null, " \r\n ", "Ready")]
        [InlineData(" Ready ", null, null, " Slow ", " Ready  |  Slow ")]
        [InlineData("Same", "Same", null, null, "Same | Same")]
        public void Format_PreservesOrderAndTextWhileSkippingBlankParts(
            string? status,
            string? exportStatus,
            string? viewRefreshStatus,
            string? performanceStatus,
            string expected)
        {
            Assert.Equal(expected, StatusLabelFormatter.Format(
                status, exportStatus, viewRefreshStatus, performanceStatus));
        }
    }
}
