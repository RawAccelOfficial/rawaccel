using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;

        [ObservableProperty]
        public BE.ProfileModel? currentSelectedProfile;

        [ObservableProperty]
        public BE.ProfileModel? currentEditingProfile;

        private Dictionary<BE.ProfileModel, ProfileListElementViewModel> ProfileElementViewModels = [];

        private BE.ProfilesModel ProfilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles)
        {
            ProfilesModel = profiles;
            SelectionChangeAction = () => { };
            if (Profiles?.Count > 0)
            {
                CurrentSelectedProfile = Profiles[0];
            }

            AddProfileCommand = new RelayCommand(() => TryAddProfile());
            RemoveSelectedProfileCommand = new RelayCommand(() => RemoveSelectedProfile());
        }

        public ObservableCollection<BE.ProfileModel> Profiles => ProfilesModel.Profiles;

        // Set in main WindowViewModel
        public Action SelectionChangeAction { get; set; }

        public ICommand AddProfileCommand { get; }
        public ICommand RemoveSelectedProfileCommand { get; }

        public ObservableCollection<ProfileListElementViewModel> ProfileItems
        {
            get
            {
                var items = new ObservableCollection<ProfileListElementViewModel>();
                foreach (var profile in Profiles)
                {
                    if (!ProfileElementViewModels.TryGetValue(profile, out ProfileListElementViewModel? value))
                    {
                        var elementViewModel = new ProfileListElementViewModel(profile, showButtons: true);

                        // Subscribe to events
                        elementViewModel.ProfileDeleted += OnProfileDeleted;
                        elementViewModel.ProfileRenamed += OnProfileRenamed;
                        elementViewModel.EditingStarted += OnEditingStarted;
                        elementViewModel.EditingFinished += OnEditingFinished;
                        value = elementViewModel;
                        ProfileElementViewModels[profile] = value;
                    }
                    items.Add(value);
                }
                return items;
            }
        }

        private void OnProfileDeleted(ProfileListElementViewModel elementViewModel)
        {
            RemoveProfile(elementViewModel.Profile);
        }

        private void OnProfileRenamed(ProfileListElementViewModel elementViewModel)
        {
            // Handle profile renamed if needed
            OnPropertyChanged(nameof(ProfileItems));
        }

        private void OnEditingStarted(ProfileListElementViewModel elementViewModel)
        {
            StartEditing(elementViewModel.Profile);
        }

        private void OnEditingFinished(ProfileListElementViewModel elementViewModel)
        {
            StopEditing();
        }

        partial void OnCurrentSelectedProfileChanged(BE.ProfileModel? value)
        {
            SelectionChangeAction?.Invoke();
        }

        partial void OnCurrentEditingProfileChanged(BE.ProfileModel? value)
        {
            foreach (var item in ProfileElementViewModels.Values)
            {
                item.UpdateIsEditing(IsEditing(item.Profile));
            }
        }

        public void StartEditing(BE.ProfileModel profile)
        {
            // Stop editing any other profile first
            if (CurrentEditingProfile != null)
            {
                StopEditing();
            }
            CurrentEditingProfile = profile;
        }

        public void StopEditing()
        {
            CurrentEditingProfile = null;
        }

        public bool IsEditing(BE.ProfileModel profile)
        {
            return CurrentEditingProfile == profile;
        }

        public bool TryAddProfile()
        {
            for (int i = 0; i < MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile{i}";
                if (ProfilesModel.TryAddNewDefaultProfile(newProfileName))
                {
                    OnPropertyChanged(nameof(ProfileItems));
                    return true;
                }
            }
            return false;
        }

        public void RemoveSelectedProfile()
        {
            if (CurrentSelectedProfile != null)
            {
                RemoveProfile(CurrentSelectedProfile);
            }
        }

        public void RemoveProfile(BE.ProfileModel profile)
        {
            if (profile != null)
            {
                // Clean up the ProfileElementViewModel
                if (ProfileElementViewModels.TryGetValue(profile, out ProfileListElementViewModel? elementViewModel))
                {
                    // Unsubscribe from events
                    elementViewModel.ProfileDeleted -= OnProfileDeleted;
                    elementViewModel.ProfileRenamed -= OnProfileRenamed;
                    elementViewModel.EditingStarted -= OnEditingStarted;
                    elementViewModel.EditingFinished -= OnEditingFinished;

                    // Cleanup
                    elementViewModel.Cleanup();
                    ProfileElementViewModels.Remove(profile);
                }

                // Stop editing if this profile is being edited
                if (CurrentEditingProfile == profile)
                {
                    StopEditing();
                }

                // Clear selection if this profile is selected
                if (CurrentSelectedProfile == profile)
                {
                    CurrentSelectedProfile = null;
                }

                _ = ProfilesModel.RemoveProfile(profile);
                OnPropertyChanged(nameof(ProfileItems));
            }
        }
    }
}