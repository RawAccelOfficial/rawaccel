using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.ViewModels.Controls;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListElementViewModel : ViewModelBase
    {
        private EditableFieldViewModel? FieldViewModel;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool showActionButtons = true;

        [ObservableProperty]
        private bool isDefaultProfile;

        [ObservableProperty]
        private bool isSelected;

        public BE.ProfileModel Profile { get; }

        public event Action<ProfileListElementViewModel>? ProfileDeleted;

        public event Action<ProfileListElementViewModel>? ProfileRenamed;

        public event Action<ProfileListElementViewModel>? EditingStarted;

        public event Action<ProfileListElementViewModel>? EditingFinished;

        public event Action<ProfileListElementViewModel, bool>? SelectionChanged;

        public ICommand StartEditingCommand { get; }

        public ICommand StopEditingCommand { get; }

        public ICommand CancelEditingCommand { get; }

        public ICommand DeleteProfileCommand { get; }

        public ProfileListElementViewModel(BE.ProfileModel profile, bool showButtons = true, bool isDefault = false)
        {
            Profile = profile;
            ShowActionButtons = showButtons;
            IsDefaultProfile = isDefault;

            StartEditingCommand = new RelayCommand(() => StartEditing());
            StopEditingCommand = new RelayCommand(() => StopEditing());
            CancelEditingCommand = new RelayCommand(() => CancelEditing());
            DeleteProfileCommand = new RelayCommand(() => DeleteProfile());
        }

        public string CurrentNameForDisplay => Profile.CurrentNameForDisplay;

        public EditableFieldViewModel? EditableFieldViewModel
        {
            get
            {
                // Only create the EditableFieldViewModel if the profile has an editable name setting
                if (FieldViewModel == null && Profile.Name is BE.EditableSettings.IEditableSetting editableSetting)
                {
                    FieldViewModel = new EditableFieldViewModel(editableSetting, UpdateMode.LostFocus);
                }
                return FieldViewModel;
            }
        }

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
            if (FieldViewModel != null)
            {
                FieldViewModel = null;
            }
            EditingFinished?.Invoke(this);
        }

        public void DeleteProfile()
        {
            ProfileDeleted?.Invoke(this);
        }

        public void UpdateIsEditing(bool editing)
        {
            IsEditing = editing;
        }

        public void Cleanup()
        {
            FieldViewModel = null;
        }
    }
}