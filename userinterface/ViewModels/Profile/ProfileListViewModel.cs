using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;
        private readonly CurrentProfileService _currentProfileService;

        [ObservableProperty]
        public BE.ProfileModel? currentSelectedProfile;

        private BE.ProfilesModel profilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles, Action selectionChangeAction, CurrentProfileService currentProfileService)
        {
            profilesModel = profiles;
            SelectionChangeAction = selectionChangeAction;
            _currentProfileService = currentProfileService;

            if (Profiles?.Count > 0)
            {
                CurrentSelectedProfile = Profiles[0];
            }
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;

        public Action SelectionChangeAction { get; }

        partial void OnCurrentSelectedProfileChanged(BE.ProfileModel? value)
        {
            // Update the service with the new selected profile
            _currentProfileService.SetCurrentProfile(value);
            SelectionChangeAction.Invoke();
        }

        public bool TryAddProfile()
        {
            for (int i = 0; i < MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile{i}";
                if (profilesModel.TryAddNewDefaultProfile(newProfileName))
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveSelectedProfile()
        {
            if (CurrentSelectedProfile != null)
            {
                _ = profilesModel.RemoveProfile(CurrentSelectedProfile);
            }
        }
    }
}
