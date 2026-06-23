using TimePilot.WinForms.Details;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private DetailRuntimeFilterCoordinator CreateDetailRuntimeFilterCoordinator()
        {
            return new DetailRuntimeFilterCoordinator(detailRuntimeFilterComboBox);
        }

        private RuntimeSegmentObservationFilterCoordinator CreateRuntimeSegmentObservationFilterCoordinator()
        {
            return new RuntimeSegmentObservationFilterCoordinator(runtimeSegmentObservationFilterComboBox);
        }
    }
}
