using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileViewModel : ViewModelBase
    {
        public ProfileViewModel(BE.ProfileModel profileBE, INotificationService notificationService)
        {
            ProfileModelBE = profileBE;
            Settings = new ProfileSettingsViewModel(profileBE, notificationService);
            Chart = new ProfileChartViewModel(profileBE.XCurvePreview, profileBE.YCurvePreview, ProfileModelBE.YXRatio);
        }

        protected BE.ProfileModel ProfileModelBE { get; }

        public string CurrentName => ProfileModelBE.Name.CurrentValidatedValue;

        public ProfileSettingsViewModel Settings { get; }

        public ProfileChartViewModel Chart { get; }
    }
}