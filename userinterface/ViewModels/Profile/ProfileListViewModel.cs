using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using BE = userspace_backend.Model;

/** 
 * This component is only used in the MainWindow under the Navigation however it is constructed under the profile page 
 */
namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;

        [ObservableProperty]
        public BE.ProfileModel? currentSelectedProfile;

        private BE.ProfilesModel profilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles, Action selectionChangeAction)
        {
            profilesModel = profiles;
            SelectionChangeAction = selectionChangeAction;

            if (Profiles?.Count > 0)
            {
                CurrentSelectedProfile = Profiles[0];
            }
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;

        public Action SelectionChangeAction { get; }

        partial void OnCurrentSelectedProfileChanged(BE.ProfileModel? value)
        {
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
