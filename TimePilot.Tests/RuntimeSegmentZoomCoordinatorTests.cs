using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class RuntimeSegmentZoomCoordinatorTests
    {
        public RuntimeSegmentZoomCoordinatorTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void Initialize_WiresZoomButtonActions()
        {
            var controls = CreateControls();
            try
            {
                var zoomInRequested = false;
                var coordinator = new RuntimeSegmentZoomCoordinator(
                    controls,
                    CreateActions(zoomIn: () => zoomInRequested = true),
                    CreateState);

                coordinator.Initialize();
                controls.ZoomInButton.PerformClick();

                Assert.True(zoomInRequested);
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        [Fact]
        public void Update_AppliesViewStateToControls()
        {
            var controls = CreateControls();
            try
            {
                var coordinator = new RuntimeSegmentZoomCoordinator(
                    controls,
                    CreateActions(),
                    () => new TimelineZoomState(
                        "09:00-10:00",
                        IsZoomed: true,
                        CanPanPrevious: true,
                        CanPanNext: false,
                        ViewWidthRatio: 0.25,
                        ViewStartRatio: 0.5));

                coordinator.Update();

                Assert.Contains("Runtime view", controls.RangeLabel.Text);
                Assert.Contains("09:00-10:00", controls.RangeLabel.Text);
                Assert.True(controls.ZoomOutButton.Enabled);
                Assert.True(controls.PreviousButton.Enabled);
                Assert.False(controls.NextButton.Enabled);
                Assert.True(controls.ScrollBar.Visible);
                Assert.True(controls.ScrollBar.Enabled);
                Assert.Equal(250, controls.ScrollBar.LargeChange);
                Assert.Equal(500, controls.ScrollBar.Value);
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        [Fact]
        public void ApplyText_UsesRuntimeSegmentResetText()
        {
            var controls = CreateControls();
            try
            {
                var coordinator = new RuntimeSegmentZoomCoordinator(
                    controls,
                    CreateActions(),
                    CreateState);

                coordinator.ApplyText();

                Assert.False(string.IsNullOrWhiteSpace(controls.ZoomInButton.Text));
                Assert.Equal("Full", controls.ResetButton.Text);
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        private static TimelineZoomControls CreateControls()
        {
            return new TimelineZoomControls(
                new Label(),
                new Button(),
                new Button(),
                new Button(),
                new Button(),
                new Button(),
                new HScrollBar());
        }

        private static TimelineZoomActions CreateActions(Action? zoomIn = null)
        {
            return new TimelineZoomActions(
                () => { },
                zoomIn ?? (() => { }),
                () => { },
                () => { },
                () => { },
                _ => { });
        }

        private static TimelineZoomState CreateState()
        {
            return new TimelineZoomState(
                "Full day",
                IsZoomed: false,
                CanPanPrevious: false,
                CanPanNext: false,
                ViewWidthRatio: 1,
                ViewStartRatio: 0);
        }

        private static void DisposeControls(TimelineZoomControls controls)
        {
            controls.RangeLabel.Dispose();
            controls.ZoomOutButton.Dispose();
            controls.ZoomInButton.Dispose();
            controls.PreviousButton.Dispose();
            controls.NextButton.Dispose();
            controls.ResetButton.Dispose();
            controls.ScrollBar.Dispose();
        }
    }
}
