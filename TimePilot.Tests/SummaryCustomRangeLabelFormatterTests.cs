using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class SummaryCustomRangeLabelFormatterTests
    {
        public SummaryCustomRangeLabelFormatterTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void Format_UsesSingleDateForSameDayRange()
        {
            var label = SummaryCustomRangeLabelFormatter.Format(
                new DateTime(2026, 6, 24),
                new DateTime(2026, 6, 24));

            Assert.Contains("2026-06-24", label);
            Assert.DoesNotContain("~", label);
            Assert.Contains("1 day", label);
        }

        [Fact]
        public void Format_UsesRangeForMultiDayRange()
        {
            var label = SummaryCustomRangeLabelFormatter.Format(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 24));

            Assert.Contains("2026-06-01", label);
            Assert.Contains("~", label);
            Assert.Contains("2026-06-24", label);
            Assert.Contains("24 days", label);
        }
    }
}
