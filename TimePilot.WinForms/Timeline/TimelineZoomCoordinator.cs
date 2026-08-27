using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed class TimelineZoomCoordinator
    {
        private const int ScrollScale = 1000;

        private readonly TimelineZoomControls controls;
        private readonly TimelineZoomActions actions;
        private readonly Func<TimelineZoomState> getState;
        private bool isUpdatingScrollBar;

        public TimelineZoomCoordinator(
            TimelineZoomControls controls,
            TimelineZoomActions actions,
            Func<TimelineZoomState> getState)
        {
            this.controls = controls;
            this.actions = actions;
            this.getState = getState;
        }

        public void Initialize()
        {
            controls.ZoomOutButton.Click += (_, _) => actions.ZoomOut();
            controls.ZoomInButton.Click += (_, _) => actions.ZoomIn();
            controls.PreviousButton.Click += (_, _) => actions.PanPrevious();
            controls.NextButton.Click += (_, _) => actions.PanNext();
            controls.ResetButton.Click += (_, _) => actions.ResetView();
            controls.ScrollBar.Scroll += OnScrollBarScroll;
        }

        public void ApplyText()
        {
            controls.ZoomOutButton.Text = UiText.Main.TimelineZoomOut;
            controls.ZoomInButton.Text = UiText.Main.TimelineZoomIn;
            controls.PreviousButton.Text = UiText.Main.TimelinePanPrevious;
            controls.NextButton.Text = UiText.Main.TimelinePanNext;
            controls.ResetButton.Text = UiText.Main.TimelineResetView;
        }

        public void Update()
        {
            var state = getState();
            var viewRangeText = UiText.Main.TimelineViewRange(state.ViewRangeText);
            controls.RangeLabel.Text = viewRangeText;
            controls.RangeLabel.AccessibleDescription = viewRangeText;
            controls.ZoomOutButton.Enabled = state.IsZoomed;
            controls.ZoomInButton.Enabled = true;
            controls.PreviousButton.Enabled = state.CanPanPrevious;
            controls.NextButton.Enabled = state.CanPanNext;
            controls.ResetButton.Enabled = state.IsZoomed;
            UpdateScrollBar(state);
        }

        private void OnScrollBarScroll(object? sender, ScrollEventArgs e)
        {
            if (isUpdatingScrollBar)
                return;

            actions.SetViewStartRatio(e.NewValue / (double)ScrollScale);
        }

        private void UpdateScrollBar(TimelineZoomState state)
        {
            isUpdatingScrollBar = true;
            try
            {
                var width = Math.Clamp((int)Math.Round(state.ViewWidthRatio * ScrollScale), 1, ScrollScale);
                var maxValue = Math.Max(0, ScrollScale - width);
                var value = Math.Clamp((int)Math.Round(state.ViewStartRatio * ScrollScale), 0, maxValue);

                controls.ScrollBar.Visible = state.IsZoomed;
                controls.ScrollBar.Enabled = state.IsZoomed;
                controls.ScrollBar.Minimum = 0;
                controls.ScrollBar.Maximum = ScrollScale;
                controls.ScrollBar.LargeChange = width;
                controls.ScrollBar.SmallChange = Math.Max(1, width / 10);
                controls.ScrollBar.Value = value;
            }
            finally
            {
                isUpdatingScrollBar = false;
            }
        }
    }
}
