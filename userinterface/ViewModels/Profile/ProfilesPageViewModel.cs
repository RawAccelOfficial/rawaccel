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
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.profilesModel = backEnd?.Profiles ?? throw new ArgumentNullException(nameof(backEnd));
            this.profileListView = profileListView ?? throw new ArgumentNullException(nameof(profileListView));
            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

            ProfileViewModels = [];
            UpdateProfileViewModels();
            SelectedProfileView = ProfileViewModels.FirstOrDefault();
        }

        private INotificationService NotificationService => notificationService;
        private BE.ProfilesModel ProfilesModel => profilesModel;
        public ProfileListViewModel ProfileListView => profileListView;

        private IEnumerable<BE.ProfileModel> ProfileModels => ProfilesModel.Profiles;

        protected ObservableCollection<ProfileViewModel> ProfileViewModels { get; }


        public void UpdateCurrentProfile()
        {
            UpdateProfileViewModels();

            //var selectedProfile = ProfileListView.GetSelectedProfile();

            //UpdateSelectedProfileView(selectedProfile);
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
                ProfileViewModels.Add(viewModelFactory.CreateProfileViewModel(profileModelBE));
            }
        }
    }
}