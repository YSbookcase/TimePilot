using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class RestoreIntervalComparisonServiceTests
    {
        private static readonly DateTimeOffset BaseTime = new(2026, 6, 3, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Compare_TreatsNonOverlappingCandidateAsImportable()
        {
            var baseline = new[] { Interval(0, 10) };
            var candidates = new[] { Interval(10, 20) };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(1, result.ImportableCount);
            Assert.Equal(0, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_ExcludesPartiallyOverlappingCandidate()
        {
            var baseline = new[] { Interval(0, 10) };
            var candidates = new[] { Interval(9, 20) };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(0, result.ImportableCount);
            Assert.Equal(1, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_ExcludesCandidateFullyContainedInBaseline()
        {
            var baseline = new[] { Interval(0, 60) };
            var candidates = new[] { Interval(10, 20) };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(0, result.ImportableCount);
            Assert.Equal(1, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_ExcludesShortSecondLevelCandidateInsideLongBaseline()
        {
            var baseline = new[] { Interval(0, 3600) };
            var candidates = new[] { Interval(120, 122) };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(0, result.ImportableCount);
            Assert.Equal(1, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_TreatsTouchingBoundariesAsImportable()
        {
            var baseline = new[] { Interval(10, 20) };
            var candidates = new[]
            {
                Interval(0, 10),
                Interval(20, 30)
            };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(2, result.ImportableCount);
            Assert.Equal(0, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_TreatsDifferentMatchKeysAsImportable()
        {
            var baseline = new[] { Interval(0, 10, "chrome") };
            var candidates = new[] { Interval(5, 15, "devenv") };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(1, result.ImportableCount);
            Assert.Equal(0, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_ExcludesSameMatchKeyOverlap()
        {
            var baseline = new[] { Interval(0, 10, "chrome") };
            var candidates = new[] { Interval(5, 15, "Chrome") };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(0, result.ImportableCount);
            Assert.Equal(1, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_FallsBackToTimeOnlyWhenEitherMatchKeyIsMissing()
        {
            var baseline = new[] { Interval(0, 10, "chrome") };
            var candidates = new[] { Interval(5, 15) };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(0, result.ImportableCount);
            Assert.Equal(1, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_ReturnsStableResultForUnsortedInputs()
        {
            var baseline = new[]
            {
                Interval(40, 50),
                Interval(0, 10),
                Interval(20, 30)
            };
            var candidates = new[]
            {
                Interval(35, 39),
                Interval(25, 26),
                Interval(12, 15),
                Interval(45, 46)
            };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(2, result.ImportableCount);
            Assert.Equal(2, result.ExcludedByOverlapCount);
        }

        [Fact]
        public void Compare_IgnoresInvalidZeroOrNegativeLengthIntervals()
        {
            var baseline = new[]
            {
                Interval(0, 10),
                Interval(20, 20),
                Interval(30, 25)
            };
            var candidates = new[]
            {
                Interval(5, 5),
                Interval(6, 4),
                Interval(8, 9)
            };

            var result = RestoreIntervalComparisonService.Compare(baseline, candidates);

            Assert.Equal(0, result.ImportableCount);
            Assert.Equal(1, result.ExcludedByOverlapCount);
        }

        private static RestoreAnalysisInterval Interval(int startSecond, int endSecond, string? matchKey = null)
        {
            return new RestoreAnalysisInterval(
                BaseTime.AddSeconds(startSecond),
                BaseTime.AddSeconds(endSecond),
                matchKey);
        }
    }
}
