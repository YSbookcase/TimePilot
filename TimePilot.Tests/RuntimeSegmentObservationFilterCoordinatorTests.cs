using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class RuntimeSegmentObservationFilterCoordinatorTests
    {
        public RuntimeSegmentObservationFilterCoordinatorTests()
        {
            UiText.UseLanguage(UiLanguage.English);
        }

        [Fact]
        public void RefreshOptions_SelectsCurrentFilter()
        {
            using var comboBox = CreateComboBox();
            var coordinator = new RuntimeSegmentObservationFilterCoordinator(comboBox);

            coordinator.RefreshOptions(RuntimeSegmentObservationFilter.UserProcesses);

            Assert.True(coordinator.TryGetSelectedFilter(out var selectedFilter));
            Assert.Equal(RuntimeSegmentObservationFilter.UserProcesses, selectedFilter);
        }

        [Fact]
        public void RefreshOptions_FallsBackToFirstOptionWhenSelectionIsMissing()
        {
            using var comboBox = CreateComboBox();
            var coordinator = new RuntimeSegmentObservationFilterCoordinator(comboBox);

            coordinator.RefreshOptions((RuntimeSegmentObservationFilter)999);

            Assert.True(coordinator.TryGetSelectedFilter(out var selectedFilter));
            Assert.Equal(RuntimeSegmentObservationFilter.All, selectedFilter);
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
