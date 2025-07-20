using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Services;
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
            DeleteProfileCommand = new AsyncRelayCommand(DeleteProfile);
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

        public async Task DeleteProfile()
        {
            var modalService = App.Services?.GetService<IModalService>();
            if (modalService != null)
            {
                var confirmed = await modalService.ShowConfirmationAsync(
                    "Delete Profile", 
                    $"Are you sure you want to delete the profile '{Profile.CurrentNameForDisplay}'?",
                    "Delete",
                    "Cancel");
                
                if (confirmed)
                {
                    ProfileDeleted?.Invoke(this);
                }
            }
            else
            {
                // Fallback if modal service is not available
                ProfileDeleted?.Invoke(this);
            }
        }
    }
}