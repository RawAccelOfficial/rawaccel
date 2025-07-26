using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileViewModel : ViewModelBase
    {
        private readonly INotificationService notificationService;
        private readonly IViewModelFactory viewModelFactory;

        public ProfileViewModel(INotificationService notificationService, IViewModelFactory viewModelFactory)
        {
            this.notificationService = notificationService;
            this.viewModelFactory = viewModelFactory;
        }

        protected BE.ProfileModel ProfileModelBE { get; private set; } = null!;

        public string CurrentName => ProfileModelBE?.Name.CurrentValidatedValue ?? string.Empty;

        public ProfileSettingsViewModel Settings { get; private set; } = null!;

        public ProfileChartViewModel Chart { get; private set; } = null!;

        public void Initialize(BE.ProfileModel profileModel)
        {
            ProfileModelBE = profileModel;
            Settings = viewModelFactory.CreateProfileSettingsViewModel(profileModel);
            Chart = viewModelFactory.CreateProfileChartViewModel(profileModel);
        }
    }
}