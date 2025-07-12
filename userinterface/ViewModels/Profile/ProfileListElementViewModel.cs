using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListElementViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool showActionButtons = true;

        [ObservableProperty]
        private bool isDefaultProfile;

        private bool isSelected;

        public BE.ProfileModel Profile { get; }

        public event Action<ProfileListElementViewModel>? ProfileDeleted;

        public event Action<ProfileListElementViewModel, bool>? SelectionChanged;

        public ICommand DeleteProfileCommand { get; }

        // Track if a view has subscribed to this ViewModel
        public bool HasViewSubscribed { get; set; } = false;

        public ProfileListElementViewModel(BE.ProfileModel profile, bool showButtons = true, bool isDefault = false)
        {
            Profile = profile;
            ShowActionButtons = showButtons;
            IsDefaultProfile = isDefault;
            UpdateSelection(false);
            DeleteProfileCommand = new RelayCommand(() => DeleteProfile());
        }

        public string CurrentNameForDisplay => Profile.CurrentNameForDisplay;

        public bool IsSelected
        {
            get => isSelected;
            private set => isSelected = value;
        }

        public void UpdateSelection(bool selected)
        {
            if (selected) System.Diagnostics.Debug.WriteLine("New selected item: " + Profile.CurrentNameForDisplay);
            IsSelected = selected;
            SelectionChanged?.Invoke(this, selected);
        }

        public void DeleteProfile()
        {
            ProfileDeleted?.Invoke(this);
        }
    }
}