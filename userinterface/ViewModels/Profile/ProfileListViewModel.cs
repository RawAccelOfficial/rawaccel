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

        private readonly ObservableCollection<ProfileListElementViewModel> profileItems;

        private readonly Dictionary<BE.ProfileModel, ProfileListElementViewModel> profileViewModelCache;

        private BE.ProfilesModel ProfilesModel { get; }

        public ProfileListViewModel(BE.ProfilesModel profiles)
        {
            ProfilesModel = profiles;
            profileItems = new ObservableCollection<ProfileListElementViewModel>();
            profileViewModelCache = new Dictionary<BE.ProfileModel, ProfileListElementViewModel>();

            ProfilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;

            // Initial population
            UpdateProfileItems();

            AddProfileCommand = new RelayCommand(() => TryAddProfile());
        }

        private void OnProfilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateProfileItems();
        }

        public ObservableCollection<BE.ProfileModel> Profiles => ProfilesModel.Profiles;

        public ICommand AddProfileCommand { get; }

        public ObservableCollection<ProfileListElementViewModel> ProfileItems => profileItems;

        private void UpdateProfileItems()
        {
            // Clear the observable collection but keep the cache for reuse
            profileItems.Clear();

            // Clean up ViewModels that are no longer needed
            var profilesToRemove = profileViewModelCache.Keys.Except(Profiles).ToList();
            foreach (var profile in profilesToRemove)
            {
                if (profileViewModelCache.TryGetValue(profile, out var viewModel))
                {
                    viewModel.ProfileDeleted -= OnProfileDeleted;
                    profileViewModelCache.Remove(profile);
                }
            }

            // Add or reuse ViewModels for current profiles
            for (int i = 0; i < Profiles.Count; i++)
            {
                var profile = Profiles[i];

                if (!profileViewModelCache.TryGetValue(profile, out var elementViewModel))
                {
                    // Create new ViewModel if it doesn't exist
                    bool isDefault = i == 0;
                    elementViewModel = new ProfileListElementViewModel(profile, showButtons: true, isDefault: isDefault);

                    // Subscribe to events
                    elementViewModel.ProfileDeleted += OnProfileDeleted;

                    // Cache it
                    profileViewModelCache[profile] = elementViewModel;
                }
                else
                {
                    // Update existing ViewModel properties if needed
                    elementViewModel.IsDefaultProfile = i == 0;
                }

                profileItems.Add(elementViewModel);
            }
        }

        private void OnProfileDeleted(ProfileListElementViewModel elementViewModel)
        {
            RemoveProfile(elementViewModel.Profile);
        }

        public bool TryAddProfile()
        {
            for (int i = 0; i < MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile{i}";
                if (ProfilesModel.TryAddNewDefaultProfile(newProfileName))
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
                _ = ProfilesModel.RemoveProfile(profile);
            }
        }

        public BE.ProfileModel? GetSelectedProfile()
        {
            return ProfileItems.FirstOrDefault(vm => vm.IsSelected)?.Profile;
        }

        // Helper method to set selection on a specific profile
        public void SetSelectedProfile(BE.ProfileModel? profile)
        {
            foreach (var item in ProfileItems)
            {
                item.UpdateSelection(item.Profile == profile);
            }
        }

        // Cleanup method to prevent memory leaks
        public void Cleanup()
        {
            ProfilesModel.Profiles.CollectionChanged -= OnProfilesCollectionChanged;

            foreach (var viewModel in profileViewModelCache.Values)
            {
                viewModel.ProfileDeleted -= OnProfileDeleted;
            }

            profileViewModelCache.Clear();
            profileItems.Clear();
        }
    }
}