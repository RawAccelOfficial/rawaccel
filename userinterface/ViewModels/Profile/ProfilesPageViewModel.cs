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
        [ObservableProperty]
        public ProfileViewModel? selectedProfileView;

        private readonly INotificationService notificationService;

        public ProfilesPageViewModel(BE.ProfilesModel profileModels, ProfileListViewModel profileListView, INotificationService notificationService)
        {
            this.notificationService = notificationService;
            ProfileModels = profileModels.Profiles;
            ProfileViewModels = [];
            UpdateProfileViewModels();
            SelectedProfileView = ProfileViewModels.FirstOrDefault();
            ProfileListView = profileListView;
            ActiveProfilesListView = new ActiveProfilesListViewModel();
        }

        protected IEnumerable<BE.ProfileModel> ProfileModels { get; }

        protected ObservableCollection<ProfileViewModel> ProfileViewModels { get; }

        public ProfileListViewModel ProfileListView { get; }

        public ActiveProfilesListViewModel ActiveProfilesListView { get; }

        public void UpdateCurrentProfile()
        {
            UpdateProfileViewModels();

            var selectedProfile = ProfileListView.SelectedProfileData;

            UpdateSelectedProfileView(selectedProfile);
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
                ProfileViewModels.Add(new ProfileViewModel(profileModelBE, notificationService));
            }
        }
    }
}