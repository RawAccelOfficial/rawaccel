using System.Collections.ObjectModel;
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
            profilesModel = backEnd?.Profiles ?? throw new System.ArgumentNullException(nameof(backEnd));
            AddProfileCommand = new RelayCommand(TryAddProfile);
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;
        public ICommand AddProfileCommand { get; }

        public void TryAddProfile()
        {
            TryAddProfileWithName(GenerateProfileName());
        }

        public bool TryAddProfileAtPosition(int position)
        {
            var profileName = GenerateProfileName();
            if (!profilesModel.TryAddNewDefaultProfile(profileName)) return false;
            
            if (position == 1 && profilesModel.Profiles.Count > 1)
            {
                var newProfile = profilesModel.Profiles[^1];
                profilesModel.Profiles.RemoveAt(profilesModel.Profiles.Count - 1);
                profilesModel.Profiles.Insert(1, newProfile);
            }
            return true;
        }
        
        private bool TryAddProfileWithName(string profileName)
        {
            return !string.IsNullOrEmpty(profileName) && profilesModel.TryAddNewDefaultProfile(profileName);
        }
        
        private string GenerateProfileName()
        {
            for (int i = 1; i <= MaxProfileAttempts; i++)
            {
                string name = $"Profile {i}";
                if (!profilesModel.TryGetProfile(name, out _))
                {
                    return name;
                }
            }
            return string.Empty;
        }

        public bool RemoveProfile(BE.ProfileModel profile) => profile != null && profilesModel.RemoveProfile(profile);
    }
}