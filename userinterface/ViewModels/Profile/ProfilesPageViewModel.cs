using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfilesPageViewModel : ViewModelBase
    {
        private readonly CurrentProfileService _currentProfileService;

        [ObservableProperty]
        public ProfileViewModel? selectedProfileView;

        public ProfilesPageViewModel(BE.ProfilesModel profileModels, ProfileListViewModel profileListView, CurrentProfileService currentProfileService)
        {
            ProfileModels = profileModels.Profiles;
            ProfileViewModels = new ObservableCollection<ProfileViewModel>();
            _currentProfileService = currentProfileService;

            UpdateProfileViewModels();
            SelectedProfileView = ProfileViewModels.FirstOrDefault();

            ProfileListView = profileListView;
            ActiveProfilesListView = new ActiveProfilesListViewModel();

            // Subscribe to current profile changes
            _currentProfileService.CurrentProfileChanged += OnCurrentProfileChanged;

            // Initialize with current profile if available
            UpdateSelectedProfileView(_currentProfileService.CurrentProfile);
        }

        protected IEnumerable<BE.ProfileModel> ProfileModels { get; }

        protected ObservableCollection<ProfileViewModel> ProfileViewModels { get; }

        public ProfileListViewModel ProfileListView { get; }

        public ActiveProfilesListViewModel ActiveProfilesListView { get; }

        private void OnCurrentProfileChanged(object? sender, BE.ProfileModel? profile)
        {
            UpdateSelectedProfileView(profile);
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
            ProfileViewModels.Clear();
            foreach (var profileModelBE in ProfileModels)
            {
                ProfileViewModels.Add(new ProfileViewModel(profileModelBE));
            }
        }

        protected void OnDispose()
        {
            // Unsubscribe to prevent memory leaks
            _currentProfileService.CurrentProfileChanged -= OnCurrentProfileChanged;
        }
    }
}
