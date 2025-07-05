using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using userinterface.Services;
using userinterface.ViewModels.Controls;
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

        private Dictionary<BE.ProfileModel, ProfileItemViewModel> ProfileItemViewModels = new();

        private BE.ProfilesModel ProfilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles)
        {
            ProfilesModel = profiles;
            SelectionChangeAction = () => { };
            if (Profiles?.Count > 0)
            {
                CurrentSelectedProfile = Profiles[0];
            }
        }

        public ObservableCollection<BE.ProfileModel> Profiles => ProfilesModel.Profiles;
        public Action SelectionChangeAction { get; set; }

        public ObservableCollection<ProfileItemViewModel> ProfileItems
        {
            get
            {
                var items = new ObservableCollection<ProfileItemViewModel>();
                foreach (var profile in Profiles)
                {
                    if (!ProfileItemViewModels.ContainsKey(profile))
                    {
                        ProfileItemViewModels[profile] = new ProfileItemViewModel(profile, this);
                    }
                    items.Add(ProfileItemViewModels[profile]);
                }
                return items;
            }
        }

        partial void OnCurrentSelectedProfileChanged(BE.ProfileModel? value)
        {
            SelectionChangeAction?.Invoke();
        }

        partial void OnCurrentEditingProfileChanged(BE.ProfileModel? value)
        {
            foreach (var item in ProfileItemViewModels.Values)
            {
                item.UpdateIsEditing();
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
                _ = ProfilesModel.RemoveProfile(CurrentSelectedProfile);
            }
        }

        public void RemoveProfile(BE.ProfileModel profile)
        {
            if (profile != null)
            {
                // Clean up the ProfileItemViewModel
                if (ProfileItemViewModels.ContainsKey(profile))
                {
                    ProfileItemViewModels.Remove(profile);
                }

                // Stop editing if this profile is being edited
                if (CurrentEditingProfile == profile)
                {
                    StopEditing();
                }

                _ = ProfilesModel.RemoveProfile(profile);
                OnPropertyChanged(nameof(ProfileItems));
            }
        }
    }

    // Wrapper ViewModel for individual profile items
    public partial class ProfileItemViewModel : ViewModelBase
    {
        private readonly ProfileListViewModel ParentViewModel;
        private EditableFieldViewModel? FieldViewModel;

        public BE.ProfileModel Profile { get; }

        [ObservableProperty]
        public bool isEditing;

        public ProfileItemViewModel(BE.ProfileModel profile, ProfileListViewModel parentViewModel)
        {
            Profile = profile;
            ParentViewModel = parentViewModel;
        }

        public string CurrentNameForDisplay => Profile.CurrentNameForDisplay;

        public EditableFieldViewModel EditableFieldViewModel
        {
            get
            {
                if (FieldViewModel == null)
                {
                    FieldViewModel = new EditableFieldViewModel(
                        Profile.Name,
                        UpdateMode.LostFocus);
                }
                return FieldViewModel;
            }
        }

        public void UpdateIsEditing()
        {
            IsEditing = ParentViewModel.IsEditing(Profile);
        }
    }
}
