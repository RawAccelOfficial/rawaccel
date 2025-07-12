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

        [ObservableProperty]
        private bool isSelected;

        public BE.ProfileModel Profile { get; }

        public event Action<ProfileListElementViewModel>? ProfileDeleted;

        public event Action<ProfileListElementViewModel, bool>? SelectionChanged;

        public ICommand DeleteProfileCommand { get; }

        public ProfileListElementViewModel(BE.ProfileModel profile, bool showButtons = true, bool isDefault = false)
        {
            Profile = profile;
            ShowActionButtons = showButtons;
            IsDefaultProfile = isDefault;

            DeleteProfileCommand = new RelayCommand(() => DeleteProfile());
        }

        public string CurrentNameForDisplay => Profile.CurrentNameForDisplay;

        partial void OnIsSelectedChanged(bool value)
        {
            SelectionChanged?.Invoke(this, value);
        }

        public void UpdateSelection(bool selected)
        {
            IsSelected = selected;
        }

        public void UpdateSelectionVisual(bool isSelected)
        {
            SelectionChanged?.Invoke(this, isSelected);
        }

        public void DeleteProfile()
        {
            ProfileDeleted?.Invoke(this);
        }
    }
}