using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;

        [ObservableProperty]
        public BE.ProfileModel? selectedProfileData;  // Backend data model

        [ObservableProperty]
        public BE.ProfileModel? currentEditingProfile;

        [ObservableProperty]
        private ProfileListElementViewModel? selectedProfileViewModel;  // UI ViewModel wrapper

        private Dictionary<BE.ProfileModel, ProfileListElementViewModel> ProfileElementViewModels = [];

        private ObservableCollection<ProfileListElementViewModel>? profileItems;

        private BE.ProfilesModel ProfilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles)
        {
            ProfilesModel = profiles;
            SelectionChangeAction = () => { };

            // Subscribe to collection changes to invalidate cache
            ProfilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;

            if (Profiles?.Count > 0)
            {
                SelectedProfileData = Profiles[0];
                UpdateSelectedProfileViewModel();
            }

            AddProfileCommand = new RelayCommand(() => TryAddProfile());
        }

        private void OnProfilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Invalidate the cached ProfileItems
            profileItems = null;
            OnPropertyChanged(nameof(ProfileItems));
        }

        public ObservableCollection<BE.ProfileModel> Profiles => ProfilesModel.Profiles;

        // Set in main WindowViewModel
        public Action SelectionChangeAction { get; set; }

        public ICommand AddProfileCommand { get; }

        public ObservableCollection<ProfileListElementViewModel> ProfileItems
        {
            get
            {
                if (profileItems == null)
                {
                    profileItems = new ObservableCollection<ProfileListElementViewModel>();
                    UpdateProfileItems();
                }
                return profileItems;
            }
        }

        private void UpdateProfileItems()
        {
            if (profileItems == null) return;

            profileItems.Clear();

            for (int i = 0; i < Profiles.Count; i++)
            {
                var profile = Profiles[i];
                if (!ProfileElementViewModels.TryGetValue(profile, out ProfileListElementViewModel? value))
                {
                    // Consider the first profile as the default profile
                    bool isDefault = i == 0;
                    var elementViewModel = new ProfileListElementViewModel(profile, showButtons: true, isDefault: isDefault);

                    // Subscribe to events
                    elementViewModel.ProfileDeleted += OnProfileDeleted;
                    elementViewModel.ProfileRenamed += OnProfileRenamed;
                    elementViewModel.EditingStarted += OnEditingStarted;
                    elementViewModel.EditingFinished += OnEditingFinished;
                    value = elementViewModel;
                    ProfileElementViewModels[profile] = value;
                }
                profileItems.Add(value);
            }
        }

        partial void OnSelectedProfileViewModelChanged(ProfileListElementViewModel? value)
        {
            foreach (var item in ProfileElementViewModels.Values)
            {
                item.UpdateSelectionVisual(item == value);
            }

            if (value?.Profile != null)
            {
                SelectedProfileData = value.Profile;
            }
        }

        // Called when backend data selection changes (programmatically)
        partial void OnSelectedProfileDataChanged(BE.ProfileModel? value)
        {
            // Update all profile ViewModels selection class (for view)
            foreach (var kvp in ProfileElementViewModels)
            {
                kvp.Value.UpdateSelection(kvp.Key == value);
            }

            // Update the UI selection to match backend data
            UpdateSelectedProfileViewModel();
            SelectionChangeAction?.Invoke();
        }

        // Syncs UI selection with backend data selection
        private void UpdateSelectedProfileViewModel()
        {
            if (SelectedProfileViewModel?.Profile == SelectedProfileData)
            {
                return;
            }

            if (SelectedProfileData != null && ProfileElementViewModels.TryGetValue(SelectedProfileData, out var elementViewModel))
            {
                SelectedProfileViewModel = elementViewModel;
            }
            else
            {
                SelectedProfileViewModel = null;
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
                    // The OnProfilesCollectionChanged will handle updating ProfileItems
                    // Find the newly added profile by name
                    var newProfile = Profiles.FirstOrDefault(p => p.CurrentNameForDisplay == newProfileName);
                    if (newProfile != null)
                    {
                        SelectedProfileData = newProfile;
                    }

                    return true;
                }
            }
            return false;
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

                    elementViewModel.Cleanup();
                    ProfileElementViewModels.Remove(profile);
                }

                // Stop editing if this profile is being edited
                if (CurrentEditingProfile == profile)
                {
                    StopEditing();
                }

                // Clear selection if this profile is selected
                if (SelectedProfileData == profile)
                {
                    SelectedProfileData = null;
                }

                _ = ProfilesModel.RemoveProfile(profile);
                // OnProfilesCollectionChanged will handle updating ProfileItems
            }
        }
    }
}