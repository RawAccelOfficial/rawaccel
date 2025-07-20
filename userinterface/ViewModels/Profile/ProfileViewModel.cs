using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileViewModel : ViewModelBase
    {
        public ProfileViewModel(BE.ProfileModel profileBE, INotificationService notificationService, IViewModelFactory viewModelFactory)
        {
            ProfileModelBE = profileBE;
            Settings = viewModelFactory.CreateProfileSettingsViewModel(profileBE);
            Chart = viewModelFactory.CreateProfileChartViewModel(profileBE);
        }

        protected BE.ProfileModel ProfileModelBE { get; }

        public string CurrentName => ProfileModelBE.Name.CurrentValidatedValue;

        public ProfileSettingsViewModel Settings { get; }

        public ProfileChartViewModel Chart { get; }
    }
}