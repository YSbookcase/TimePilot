using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed class TimelineSelectorCoordinator
    {
        private readonly TimelineSelectorControls controls;

        public TimelineSelectorCoordinator(TimelineSelectorControls controls)
        {
            this.controls = controls;
        }

        public void Initialize(
            EventHandler categoryBucketChanged,
            EventHandler typeHighlightChanged,
            EventHandler typeHighlightDropDownClosed,
            EventHandler systemEventFilterChanged)
        {
            controls.ZoomPanel.AutoSize = true;
            controls.ZoomPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            controls.ZoomPanel.WrapContents = true;

            controls.CategoryBucketComboBox.SelectedIndexChanged += categoryBucketChanged;
            controls.TypeHighlightComboBox.SelectedIndexChanged += typeHighlightChanged;
            controls.TypeHighlightComboBox.DropDownClosed += typeHighlightDropDownClosed;

            controls.ZoomPanel.Controls.Remove(controls.CategoryBucketLabel);
            controls.ZoomPanel.Controls.Remove(controls.CategoryBucketComboBox);
            controls.ZoomPanel.Controls.Remove(controls.TypeHighlightLabel);
            controls.ZoomPanel.Controls.Remove(controls.TypeHighlightComboBox);

            controls.CategoryBucketLabel.AutoSize = true;
            controls.CategoryBucketLabel.Margin = new Padding(0, 7, 3, 0);
            controls.CategoryBucketComboBox.Margin = new Padding(0, 3, 0, 0);

            controls.TypeHighlightLabel.AutoSize = true;
            controls.TypeHighlightLabel.Margin = new Padding(0, 7, 3, 0);
            controls.TypeHighlightComboBox.Margin = new Padding(0, 3, 0, 0);

            controls.SystemEventFilterLabel.AutoSize = true;
            controls.SystemEventFilterLabel.Margin = new Padding(0, 7, 3, 0);
            controls.SystemEventFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            controls.SystemEventFilterComboBox.Margin = new Padding(0, 3, 0, 0);
            controls.SystemEventFilterComboBox.Width = 112;
            controls.SystemEventFilterComboBox.SelectedIndexChanged += systemEventFilterChanged;

            controls.ZoomPanel.Controls.Add(CreateSelectorGroup(
                controls.CategoryBucketLabel,
                controls.CategoryBucketComboBox));
            controls.ZoomPanel.Controls.Add(CreateSelectorGroup(
                controls.TypeHighlightLabel,
                controls.TypeHighlightComboBox));
            controls.ZoomPanel.Controls.Add(CreateSelectorGroup(
                controls.SystemEventFilterLabel,
                controls.SystemEventFilterComboBox));
        }

        private static FlowLayoutPanel CreateSelectorGroup(Label label, ComboBox comboBox)
        {
            var group = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(8, 0, 0, 0),
                Padding = Padding.Empty,
                WrapContents = false
            };

            group.Controls.Add(label);
            group.Controls.Add(comboBox);
            return group;
        }

        public int RefreshCategoryBucketOptions(int selectedMinutes)
        {
            var options = TimelineCategoryBucketOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Minutes == selectedMinutes);
            controls.CategoryBucketComboBox.BeginUpdate();
            try
            {
                controls.CategoryBucketComboBox.Items.Clear();
                controls.CategoryBucketComboBox.Items.AddRange(options.Cast<object>().ToArray());
                controls.CategoryBucketComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 1;
                return ((TimelineCategoryBucketOption)controls.CategoryBucketComboBox.SelectedItem!).Minutes;
            }
            finally
            {
                controls.CategoryBucketComboBox.EndUpdate();
            }
        }

        public TimelineActivityTypeHighlight RefreshTypeHighlightOptions(
            TimelineActivityTypeHighlight selectedValue)
        {
            var options = TimelineActivityTypeHighlightOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Value == selectedValue);
            controls.TypeHighlightComboBox.BeginUpdate();
            try
            {
                controls.TypeHighlightComboBox.Items.Clear();
                controls.TypeHighlightComboBox.Items.AddRange(options.Cast<object>().ToArray());
                controls.TypeHighlightComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                return ((TimelineActivityTypeHighlightOption)controls.TypeHighlightComboBox.SelectedItem!).Value;
            }
            finally
            {
                controls.TypeHighlightComboBox.EndUpdate();
            }
        }

        public TimelineSystemEventFilter RefreshSystemEventFilterOptions(
            TimelineSystemEventFilter selectedValue)
        {
            var options = TimelineSystemEventFilterOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Value == selectedValue);
            controls.SystemEventFilterComboBox.BeginUpdate();
            try
            {
                controls.SystemEventFilterComboBox.Items.Clear();
                controls.SystemEventFilterComboBox.Items.AddRange(options.Cast<object>().ToArray());
                controls.SystemEventFilterComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                return ((TimelineSystemEventFilterOption)controls.SystemEventFilterComboBox.SelectedItem!).Value;
            }
            finally
            {
                controls.SystemEventFilterComboBox.EndUpdate();
            }
        }

        public void ApplyText(UiLanguage language)
        {
            controls.CategoryBucketLabel.Text = UiText.Main.TimelineCategoryBucket;
            controls.TypeHighlightLabel.Text = language == UiLanguage.English ? "Highlight type" : "유형 강조";
            controls.SystemEventFilterLabel.Text = language == UiLanguage.English ? "System event" : "시스템 이벤트";
        }
    }
}
