using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using userinterface.Commands;
using userspace_backend;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;
        private readonly BE.ProfilesModel profilesModel;


        public ProfileListViewModel(BackEnd backEnd)
        {
            Debug.WriteLine("[Animation Debug] ProfileListViewModel constructor called");
            this.profilesModel = backEnd?.Profiles ?? throw new System.ArgumentNullException(nameof(backEnd));
            AddProfileCommand = new RelayCommand(() => TryAddProfile());
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;
        public ICommand AddProfileCommand { get; }

        public bool TryAddProfile()
        {
            for (int i = 1; i <= MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile {i}";
                if (profilesModel.TryAddNewDefaultProfile(newProfileName))
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryAddProfileAtPosition(int position)
        {
            for (int i = 1; i <= MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile {i}";
                if (TryAddNewDefaultProfileAtPosition(newProfileName, position))
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryAddNewDefaultProfileAtPosition(string profileName, int position)
        {
            // Create the profile first
            if (profilesModel.TryAddNewDefaultProfile(profileName))
            {
                // Move it to the desired position if it's not position 1 (inserting at beginning needs special handling)
                if (position == 1 && profilesModel.Profiles.Count > 1)
                {
                    var newProfile = profilesModel.Profiles[profilesModel.Profiles.Count - 1];
                    profilesModel.Profiles.RemoveAt(profilesModel.Profiles.Count - 1);
                    profilesModel.Profiles.Insert(1, newProfile);
                }
                return true;
            }
            return false;
        }

        public bool RemoveProfile(BE.ProfileModel profile)
        {
            return profile != null && profilesModel.RemoveProfile(profile);
        }


    }
}