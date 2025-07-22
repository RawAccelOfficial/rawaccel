using CommunityToolkit.Mvvm.ComponentModel;
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
        private const int UnitSize = 20;
        private readonly BE.ProfilesModel profilesModel;

        [ObservableProperty]
        private int interval = 5;

        public int CanvasHeight => Interval * UnitSize;

        partial void OnIntervalChanged(int value)
        {
            OnPropertyChanged(nameof(CanvasHeight));
        }

        public ProfileListViewModel(BackEnd backEnd)
        {
            this.profilesModel = backEnd?.Profiles ?? throw new System.ArgumentNullException(nameof(backEnd));
            AddProfileCommand = new RelayCommand(() => TryAddProfile());
            TestCommand = new RelayCommand(() => Test());
        }

        public ObservableCollection<BE.ProfileModel> Profiles => profilesModel.Profiles;
        public ICommand AddProfileCommand { get; }
        public ICommand TestCommand { get; }

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

        public void RemoveProfile(BE.ProfileModel profile)
        {
            if (profile != null)
            {
                _ = profilesModel.RemoveProfile(profile);
            }
        }

        // Test method - for test code only, do not change functionality
        private void Test()
        {
            Interval += 1;
        }
    }
}