using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model.ProfileComponents;

namespace userinterface.ViewModels.Profile
{
    public partial class CoalescionProfileSettingsViewModel : ViewModelBase
    {
        public CoalescionProfileSettingsViewModel(BE.CoalescionModel coalescionBE)
        {
            CoalescionBE = coalescionBE;
            InputSmoothingHalfLife = new EditableFieldViewModel(coalescionBE.InputSmoothingHalfLife);
            ScaleSmoothingHalfLife = new EditableFieldViewModel(coalescionBE.ScaleSmoothingHalfLife);
        }

        protected BE.CoalescionModel CoalescionBE { get; }

        public EditableFieldViewModel InputSmoothingHalfLife { get; set; }

        public EditableFieldViewModel ScaleSmoothingHalfLife { get; set; }
    }
}
