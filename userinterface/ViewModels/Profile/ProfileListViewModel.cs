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

        [ObservableProperty]
        public BE.ProfileModel? currentSelectedProfile;

        private BE.ProfilesModel ProfilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles)
        {
            ProfilesModel = profiles;
            SelectionChangeAction = () => { };
            if (Profiles?.Count > 0)
            {
                CurrentSelectedProfile = Profiles[0];
            }
        }

        public ObservableCollection<BE.ProfileModel> Profiles => ProfilesModel.Profiles;

        public Action SelectionChangeAction { get; set; }

        partial void OnCurrentSelectedProfileChanged(BE.ProfileModel? value)
        {
            SelectionChangeAction?.Invoke();
        }

        public bool TryAddProfile()
        {
            for (int i = 0; i < MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile{i}";
                if (ProfilesModel.TryAddNewDefaultProfile(newProfileName))
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
                _ = ProfilesModel.RemoveProfile(CurrentSelectedProfile);
            }
        }
    }
}
