using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileViewModel : ViewModelBase
    {
        public ProfileViewModel(BE.ProfileModel profileBE)
        {
            ProfileModelBE = profileBE;
            Settings = new ProfileSettingsViewModel(profileBE);
            Chart = new ProfileChartViewModel(profileBE.CurvePreview);
        }

        protected BE.ProfileModel ProfileModelBE { get; }

        public string CurrentName => ProfileModelBE.Name.CurrentValidatedValue;

        public ProfileSettingsViewModel Settings { get; set; }

        public ProfileChartViewModel Chart { get; set; }
    }
}
