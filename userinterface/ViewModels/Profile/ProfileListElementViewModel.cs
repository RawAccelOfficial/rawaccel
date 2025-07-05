using CommunityToolkit.Mvvm.ComponentModel;
using System;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListElementViewModel : ViewModelBase
    {
        private EditableFieldViewModel? _fieldViewModel;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool showActionButtons = true;

        public BE.ProfileModel Profile { get; }

        // Events for parent components to handle
        public event Action<ProfileListElementViewModel>? ProfileDeleted;
        public event Action<ProfileListElementViewModel>? ProfileRenamed;
        public event Action<ProfileListElementViewModel>? EditingStarted;
        public event Action<ProfileListElementViewModel>? EditingFinished;

        public ProfileListElementViewModel(BE.ProfileModel profile, bool showButtons = true)
        {
            Profile = profile;
            ShowActionButtons = showButtons;
        }

        public string CurrentNameForDisplay => Profile.CurrentNameForDisplay;

        public EditableFieldViewModel? EditableFieldViewModel
        {
            get
            {
                // Only create the EditableFieldViewModel if the profile has an editable name setting
                if (_fieldViewModel == null && Profile.Name is userspace_backend.Model.EditableSettings.IEditableSetting editableSetting)
                {
                    _fieldViewModel = new EditableFieldViewModel(editableSetting, UpdateMode.LostFocus);
                }
                return _fieldViewModel;
            }
        }

        public void StartEditing()
        {
            if (EditableFieldViewModel != null)
            {
                IsEditing = true;
                EditingStarted?.Invoke(this);
            }
        }

        public void StopEditing()
        {
            // Try to save the changes if editing
            if (IsEditing && EditableFieldViewModel != null)
            {
                bool wasUpdated = EditableFieldViewModel.TrySetFromInterface();
                if (wasUpdated)
                {
                    ProfileRenamed?.Invoke(this);
                }
            }

            IsEditing = false;
            EditingFinished?.Invoke(this);
        }

        public void CancelEditing()
        {
            IsEditing = false;
            // Reset any changes by recreating the EditableFieldViewModel
            if (_fieldViewModel != null)
            {
                _fieldViewModel = null;
            }
            EditingFinished?.Invoke(this);
        }

        public void DeleteProfile()
        {
            ProfileDeleted?.Invoke(this);
        }

        // Method to update editing state from parent
        public void UpdateIsEditing(bool editing)
        {
            IsEditing = editing;
        }

        // Cleanup method
        public void Cleanup()
        {
            _fieldViewModel = null;
        }
    }
}
