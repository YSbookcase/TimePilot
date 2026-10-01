using TimePilot.WinForms.Refresh;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class ViewRefreshGenerationTests
    {
        [Fact]
        public void Invalidate_MakesCapturedGenerationStale()
        {
            var generation = new ViewRefreshGeneration();
            var captured = generation.Capture();

            generation.Invalidate();

            Assert.False(generation.IsCurrent(captured));
        }

        [Fact]
        public void Capture_AfterInvalidationIsCurrent()
        {
            var generation = new ViewRefreshGeneration();
            generation.Invalidate();

            var captured = generation.Capture();

            Assert.True(generation.IsCurrent(captured));
        }
    }
}
