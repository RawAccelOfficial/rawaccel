using System.Diagnostics;
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
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"ProfileViewModel.Initialize started for '{profileModel.Name.CurrentValidatedValue}'");
            
            ProfileModelBE = profileModel;
            Debug.WriteLine($"ProfileModelBE set: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            Settings = viewModelFactory.CreateProfileSettingsViewModel(profileModel);
            Debug.WriteLine($"Settings created: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            Chart = viewModelFactory.CreateProfileChartViewModel(profileModel);
            Debug.WriteLine($"Chart created: {stopwatch.ElapsedMilliseconds}ms");
            
            Debug.WriteLine($"ProfileViewModel.Initialize completed for '{profileModel.Name.CurrentValidatedValue}': {stopwatch.ElapsedMilliseconds}ms total");
        }
    }
}