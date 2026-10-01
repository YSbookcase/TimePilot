using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Details
{
    internal sealed class DetailRuntimeFilterCoordinator
    {
        private readonly ComboBox comboBox;

        public DetailRuntimeFilterCoordinator(ComboBox comboBox)
        {
            this.comboBox = comboBox;
        }

        public bool IsUpdating { get; private set; }

        public void RefreshOptions(DetailRuntimeFilter selectedFilter)
        {
            RunWithoutSelectionEvents(() =>
            {
                comboBox.BeginUpdate();
                try
                {
                    comboBox.Items.Clear();
                    comboBox.Items.AddRange(DetailRuntimeFilterOption.GetOptions().Cast<object>().ToArray());
                    SyncSelection(selectedFilter);
                }
                finally
                {
                    comboBox.EndUpdate();
                }
            });
        }

        public void SyncSelection(DetailRuntimeFilter selectedFilter)
        {
            if (comboBox.Items.Count == 0)
                return;

            var selectedIndex = -1;
            for (var i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is DetailRuntimeFilterOption option && option.Value == selectedFilter)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0)
                selectedIndex = 0;

            if (comboBox.SelectedIndex != selectedIndex)
                comboBox.SelectedIndex = selectedIndex;
        }

        public bool TryGetSelectedFilter(out DetailRuntimeFilter filter)
        {
            if (comboBox.SelectedItem is DetailRuntimeFilterOption option)
            {
                filter = option.Value;
                return true;
            }

            filter = DetailRuntimeFilter.SummaryApps;
            return false;
        }

        public void RunWithoutSelectionEvents(Action action)
        {
            IsUpdating = true;
            try
            {
                action();
            }
            finally
            {
                IsUpdating = false;
            }
        }
    }
}
