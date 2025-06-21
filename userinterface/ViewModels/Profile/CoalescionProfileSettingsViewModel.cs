using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model.ProfileComponents;

namespace userinterface.ViewModels.Profile
{
    public partial class CoalescionProfileSettingsViewModel : ViewModelBase
    {
        public CoalescionProfileSettingsViewModel(BE.CoalescionModel coalescionBE)
        {
            CoalescionBE = coalescionBE;
            InputSmoothingHalfLife = new NamedEditableFieldViewModel(coalescionBE.InputSmoothingHalfLife);
            ScaleSmoothingHalfLife = new NamedEditableFieldViewModel(coalescionBE.ScaleSmoothingHalfLife);
        }

        protected BE.CoalescionModel CoalescionBE { get; set; }

        public NamedEditableFieldViewModel InputSmoothingHalfLife { get; set; }

        public NamedEditableFieldViewModel ScaleSmoothingHalfLife { get; set; }
    }
}
