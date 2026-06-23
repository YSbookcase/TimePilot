using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineSelectorCoordinatorTests
    {
        [Fact]
        public void RefreshCategoryBucketOptions_SelectsRequestedBucket()
        {
            var controls = CreateControls();
            try
            {
                var coordinator = new TimelineSelectorCoordinator(controls);

                var selectedMinutes = coordinator.RefreshCategoryBucketOptions(60);

                Assert.Equal(60, selectedMinutes);
                Assert.IsType<TimelineCategoryBucketOption>(controls.CategoryBucketComboBox.SelectedItem);
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        [Fact]
        public void RefreshTypeHighlightOptions_SelectsRequestedValue()
        {
            var controls = CreateControls();
            try
            {
                var coordinator = new TimelineSelectorCoordinator(controls);

                var selectedValue = coordinator.RefreshTypeHighlightOptions(TimelineActivityTypeHighlight.Idle);

                Assert.Equal(TimelineActivityTypeHighlight.Idle, selectedValue);
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        [Fact]
        public void Initialize_WiresSelectorEvents()
        {
            var controls = CreateControls();
            try
            {
                var coordinator = new TimelineSelectorCoordinator(controls);
                var categoryChanged = false;

                coordinator.Initialize(
                    (_, _) => categoryChanged = true,
                    (_, _) => { },
                    (_, _) => { },
                    (_, _) => { });
                coordinator.RefreshCategoryBucketOptions(30);

                controls.CategoryBucketComboBox.SelectedIndex = 1;

                Assert.True(categoryChanged);
                Assert.Contains(controls.SystemEventFilterLabel, controls.ZoomPanel.Controls.Cast<Control>());
                Assert.Contains(controls.SystemEventFilterComboBox, controls.ZoomPanel.Controls.Cast<Control>());
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        [Fact]
        public void ApplyText_UpdatesTimelineSelectorLabels()
        {
            var controls = CreateControls();
            try
            {
                var coordinator = new TimelineSelectorCoordinator(controls);

                coordinator.ApplyText(UiLanguage.English);

                Assert.Equal("Highlight type", controls.TypeHighlightLabel.Text);
                Assert.Equal("System event", controls.SystemEventFilterLabel.Text);
            }
            finally
            {
                DisposeControls(controls);
            }
        }

        private static TimelineSelectorControls CreateControls()
        {
            return new TimelineSelectorControls(
                new FlowLayoutPanel(),
                new Label(),
                new ComboBox(),
                new Label(),
                new ComboBox(),
                new Label(),
                new ComboBox());
        }

        private static void DisposeControls(TimelineSelectorControls controls)
        {
            controls.ZoomPanel.Dispose();
            controls.CategoryBucketLabel.Dispose();
            controls.CategoryBucketComboBox.Dispose();
            controls.TypeHighlightLabel.Dispose();
            controls.TypeHighlightComboBox.Dispose();
            controls.SystemEventFilterLabel.Dispose();
            controls.SystemEventFilterComboBox.Dispose();
        }
    }
}
