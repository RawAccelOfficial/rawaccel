using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
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

        public ProfilesPageViewModel()
        {
            ProfileViewModels = [];
            UpdateProfileViewModels();
            SelectedProfileView = ProfileViewModels.FirstOrDefault();
            ActiveProfilesListView = new ActiveProfilesListViewModel();
        }

        private INotificationService NotificationService =>
            App.Services!.GetRequiredService<INotificationService>();

        private BE.ProfilesModel ProfilesModel =>
            App.Services!.GetRequiredService<userspace_backend.BackEnd>().Profiles;

        private ProfileListViewModel ProfileListView =>
            App.Services!.GetRequiredService<ProfileListViewModel>();

        private IEnumerable<BE.ProfileModel> ProfileModels => ProfilesModel.Profiles;

        protected ObservableCollection<ProfileViewModel> ProfileViewModels { get; }

        public ActiveProfilesListViewModel ActiveProfilesListView { get; }

        public void UpdateCurrentProfile()
        {
            UpdateProfileViewModels();

            var selectedProfile = ProfileListView.GetSelectedProfile();

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
                ProfileViewModels.Add(new ProfileViewModel(profileModelBE, NotificationService));
            }
        }
    }
}