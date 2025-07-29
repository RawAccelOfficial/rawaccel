using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using userinterface.Interfaces;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfilesPageViewModel : ViewModelBase, IAsyncInitializable
    {
        [ObservableProperty]
        public ProfileViewModel? selectedProfileView;

        private readonly INotificationService notificationService;
        private readonly BE.ProfilesModel profilesModel;
        private readonly ProfileListViewModel profileListView;
        private readonly IViewModelFactory viewModelFactory;

        public ProfilesPageViewModel(
            INotificationService notificationService,
            userspace_backend.BackEnd backEnd,
            ProfileListViewModel profileListView,
            IViewModelFactory viewModelFactory)
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("ProfilesPageViewModel constructor started");
            
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.profilesModel = backEnd?.Profiles ?? throw new ArgumentNullException(nameof(backEnd));
            this.profileListView = profileListView ?? throw new ArgumentNullException(nameof(profileListView));
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            Debug.WriteLine($"Dependencies set: {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            ProfileViewModels = [];
            UpdateProfileViewModels();
            Debug.WriteLine($"UpdateProfileViewModels: {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            profileListView.SelectedProfileChanged += OnProfileSelectionChanged;
            Debug.WriteLine($"Event subscription: {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            // Set initial selected profile view based on default profile
            var defaultProfile = ProfilesModel.Profiles.FirstOrDefault(p => p == BE.ProfilesModel.DefaultProfile);
            UpdateSelectedProfileView(defaultProfile ?? ProfilesModel.Profiles.FirstOrDefault());
            Debug.WriteLine($"Initial profile selection: {stopwatch.ElapsedMilliseconds}ms");
            
            Debug.WriteLine($"ProfilesPageViewModel constructor completed in total: {stopwatch.ElapsedMilliseconds}ms");
        }

        private INotificationService NotificationService => notificationService;
        private BE.ProfilesModel ProfilesModel => profilesModel;
        public ProfileListViewModel ProfileListView => profileListView;

        private IEnumerable<BE.ProfileModel> ProfileModels => ProfilesModel.Profiles;

        protected ObservableCollection<ProfileViewModel> ProfileViewModels { get; }

        public bool IsInitialized { get; private set; } = true; // Already initialized in constructor

        public bool IsInitializing { get; private set; }

        public Task InitializeAsync()
        {
            if (IsInitializing || IsInitialized)
                return Task.CompletedTask;

            IsInitializing = true;

            UpdateProfileViewModels();
            var defaultProfile = ProfilesModel.Profiles.FirstOrDefault(p => p == BE.ProfilesModel.DefaultProfile);
            UpdateSelectedProfileView(defaultProfile ?? ProfilesModel.Profiles.FirstOrDefault());

            IsInitializing = false;
            IsInitialized = true;

            return Task.CompletedTask;
        }


        public void UpdateCurrentProfile()
        {
            UpdateProfileViewModels();
            UpdateSelectedProfileView(ProfileListView.SelectedProfile);
        }

        private void UpdateSelectedProfileView(BE.ProfileModel? currentProfile)
        {
            if (currentProfile?.CurrentNameForDisplay != null)
            {
                SelectedProfileView = ProfileViewModels.FirstOrDefault(
                    p => string.Equals(p.CurrentName, currentProfile.CurrentNameForDisplay, StringComparison.InvariantCultureIgnoreCase))
                    ?? ProfileViewModels.FirstOrDefault();
            }
            else
            {
                SelectedProfileView = ProfileViewModels.FirstOrDefault();
            }
        }

        protected void UpdateProfileViewModels()
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine("UpdateProfileViewModels started");
            
            ProfileViewModels.Clear();
            Debug.WriteLine($"ProfileViewModels cleared: {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            var profileCount = ProfileModels.Count();
            Debug.WriteLine($"Profile count: {profileCount}, enumeration took: {stopwatch.ElapsedMilliseconds}ms");
            
            var totalCreationTime = 0L;
            foreach (var profileModelBE in ProfileModels)
            {
                stopwatch.Restart();
                var profileViewModel = viewModelFactory.CreateProfileViewModel(profileModelBE);
                var creationTime = stopwatch.ElapsedMilliseconds;
                totalCreationTime += creationTime;
                
                ProfileViewModels.Add(profileViewModel);
                Debug.WriteLine($"Created ProfileViewModel for '{profileModelBE.Name.CurrentValidatedValue}': {creationTime}ms");
            }
            
            Debug.WriteLine($"UpdateProfileViewModels completed. Total creation time: {totalCreationTime}ms for {profileCount} profiles");
        }

        private void OnProfileSelectionChanged(BE.ProfileModel selectedProfile)
        {
            UpdateSelectedProfileView(selectedProfile);
        }
    }
}