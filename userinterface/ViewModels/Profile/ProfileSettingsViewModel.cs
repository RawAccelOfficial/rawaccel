using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileSettingsViewModel : ViewModelBase
    {
        public ProfileSettingsViewModel(BE.ProfileModel profileBE)
        {
            ProfileModelBE = profileBE;
            OutputDPIField = new EditableFieldViewModel(profileBE.OutputDPI);
            YXRatioField = new EditableFieldViewModel(profileBE.YXRatio);
            AccelerationSettings = new AccelerationProfileSettingsViewModel(profileBE.Acceleration);
            HiddenSettings = new HiddenProfileSettingsViewModel(profileBE.Hidden);
        }

        protected BE.ProfileModel ProfileModelBE { get; }

        public EditableFieldViewModel OutputDPIField { get; set; }

        public EditableFieldViewModel YXRatioField { get; set; }

        public AccelerationProfileSettingsViewModel AccelerationSettings { get; }

        public HiddenProfileSettingsViewModel HiddenSettings { get; }
    }
}
