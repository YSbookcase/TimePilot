using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Details
{
    internal sealed class RuntimeSegmentObservationFilterCoordinator
    {
        private readonly ComboBox comboBox;

        public RuntimeSegmentObservationFilterCoordinator(ComboBox comboBox)
        {
            this.comboBox = comboBox;
        }

        public void RefreshOptions(RuntimeSegmentObservationFilter selectedFilter)
        {
            var options = RuntimeSegmentObservationFilterOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Value == selectedFilter);

            comboBox.BeginUpdate();
            try
            {
                comboBox.Items.Clear();
                comboBox.Items.AddRange(options.Cast<object>().ToArray());
                comboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            }
            finally
            {
                comboBox.EndUpdate();
            }
        }

        public bool TryGetSelectedFilter(out RuntimeSegmentObservationFilter filter)
        {
            if (comboBox.SelectedItem is RuntimeSegmentObservationFilterOption option)
            {
                filter = option.Value;
                return true;
            }

            filter = RuntimeSegmentObservationFilter.All;
            return false;
        }
    }
}
