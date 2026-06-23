using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DetailRuntimeFilterCoordinatorTests
    {
        public DetailRuntimeFilterCoordinatorTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void RefreshOptions_SelectsCurrentFilter()
        {
            using var comboBox = CreateComboBox();
            var coordinator = new DetailRuntimeFilterCoordinator(comboBox);

            coordinator.RefreshOptions(DetailRuntimeFilter.VisibleApps);

            Assert.True(coordinator.TryGetSelectedFilter(out var selectedFilter));
            Assert.Equal(DetailRuntimeFilter.VisibleApps, selectedFilter);
            Assert.False(coordinator.IsUpdating);
        }

        [Fact]
        public void SyncSelection_UpdatesSelectedFilterWithoutRebuildingOptions()
        {
            using var comboBox = CreateComboBox();
            var coordinator = new DetailRuntimeFilterCoordinator(comboBox);
            coordinator.RefreshOptions(DetailRuntimeFilter.SummaryApps);

            coordinator.SyncSelection(DetailRuntimeFilter.AllRecords);

            Assert.True(coordinator.TryGetSelectedFilter(out var selectedFilter));
            Assert.Equal(DetailRuntimeFilter.AllRecords, selectedFilter);
        }

        [Fact]
        public void RunWithoutSelectionEvents_ResetsUpdatingFlag()
        {
            using var comboBox = CreateComboBox();
            var coordinator = new DetailRuntimeFilterCoordinator(comboBox);
            var wasUpdatingInsideAction = false;

            coordinator.RunWithoutSelectionEvents(() => wasUpdatingInsideAction = coordinator.IsUpdating);

            Assert.True(wasUpdatingInsideAction);
            Assert.False(coordinator.IsUpdating);
        }

        private static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }
    }
}
