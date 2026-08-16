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

            controls.SystemEventFilterLabel.AutoSize = true;
            controls.SystemEventFilterLabel.Margin = new Padding(12, 7, 3, 0);
            controls.SystemEventFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            controls.SystemEventFilterComboBox.Width = 112;
            controls.SystemEventFilterComboBox.SelectedIndexChanged += systemEventFilterChanged;
            controls.ZoomPanel.Controls.Add(controls.SystemEventFilterLabel);
            controls.ZoomPanel.Controls.Add(controls.SystemEventFilterComboBox);
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
