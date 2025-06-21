using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using userspace_backend.Model;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        [ObservableProperty]
        public BE.ProfileModel currentSelectedProfile;

        private readonly BE.ProfilesModel profilesModel;

        public ProfileListViewModel(BE.ProfilesModel profiles, Action selectionChangeAction)
        {
            profilesModel = profiles;
            SelectionChangeAction = selectionChangeAction;
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;

        public Action SelectionChangeAction { get; }

        partial void OnCurrentSelectedProfileChanged(ProfileModel value)
        {
            SelectionChangeAction.Invoke();
        }

        public bool TryAddProfile()
        {
            for (int i = 0; i < 10; i++)
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
            // pressing delete multiple times without re-selecting just does nothing
            _ = profilesModel.RemoveProfile(CurrentSelectedProfile);
        }
    }
}
