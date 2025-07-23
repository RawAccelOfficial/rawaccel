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

        public bool RemoveProfile(BE.ProfileModel profile)
        {
            return profile != null && profilesModel.RemoveProfile(profile);
        }


    }
}