using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileListViewModel : ViewModelBase
    {
        private const int MaxProfileAttempts = 10;

        private readonly ObservableCollection<ProfileListElementViewModel> profileItems;

        // Just for optimization, only for cleaning up
        private readonly Dictionary<BE.ProfileModel, ProfileListElementViewModel> profileViewModelCache;

        public ProfileListViewModel()
        {
            profileItems = new ObservableCollection<ProfileListElementViewModel>();
            profileViewModelCache = new Dictionary<BE.ProfileModel, ProfileListElementViewModel>();

            ProfilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;

            UpdateProfileItems();

            AddProfileCommand = new RelayCommand(() => TryAddProfile());
        }

        private BE.ProfilesModel ProfilesModel =>
            App.Services!.GetRequiredService<userspace_backend.BackEnd>().Profiles;

        private void OnProfilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (BE.ProfileModel profile in e.NewItems)
                        {
                            AddProfileItem(profile, e.NewStartingIndex);
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        bool wasSelectedItemDeleted = false;
                        foreach (BE.ProfileModel profile in e.OldItems)
                        {
                            if (profileViewModelCache.TryGetValue(profile, out var viewModel) && viewModel.IsSelected)
                            {
                                wasSelectedItemDeleted = true;
                            }
                            RemoveProfileItem(profile);
                        }

                        // If we deleted the selected item, select the default
                        if (wasSelectedItemDeleted)
                        {
                            SelectDefaultItem();
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    UpdateProfileItems();
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException("Move action is not written yet.");
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    // For these cases, fall back to full update
                    UpdateProfileItems();
                    break;
            }

            // Update default profile status after any change
            UpdateDefaultProfileStatus();
        }

        private void AddProfileItem(BE.ProfileModel profile, int index)
        {
            if (!profileViewModelCache.TryGetValue(profile, out var elementViewModel))
            {
                bool isDefault = index == 0;
                elementViewModel = new ProfileListElementViewModel(profile, showButtons: true, isDefault: isDefault);
                elementViewModel.ProfileDeleted += OnProfileDeleted;
                profileViewModelCache[profile] = elementViewModel;
            }

            // Insert at the correct position
            if (index >= 0 && index < profileItems.Count)
            {
                profileItems.Insert(index, elementViewModel);
            }
            else
            {
                profileItems.Add(elementViewModel);
            }
        }

        private void RemoveProfileItem(BE.ProfileModel profile)
        {
            if (profileViewModelCache.TryGetValue(profile, out var elementViewModel))
            {
                profileItems.Remove(elementViewModel);

                // Clean up the view model
                elementViewModel.ProfileDeleted -= OnProfileDeleted;
                profileViewModelCache.Remove(profile);
            }
        }

        private void UpdateDefaultProfileStatus()
        {
            for (int i = 0; i < profileItems.Count; i++)
            {
                var item = profileItems[i];
                item.IsDefaultProfile = i == 0;
            }
        }

        private void SelectDefaultItem()
        {
            foreach (var item in profileItems)
            {
                if (item.IsDefaultProfile)
                {
                    item.UpdateSelection(true);
                    return; // Exit early - there can only be one default item
                }
                else
                {
                    item.UpdateSelection(false);
                }
            }
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

            for (int i = 0; i < Profiles.Count; i++)
            {
                var profile = Profiles[i];

                if (!profileViewModelCache.TryGetValue(profile, out var elementViewModel))
                {
                    bool isDefault = i == 0;
                    elementViewModel = new ProfileListElementViewModel(profile, showButtons: true, isDefault: isDefault);

                    elementViewModel.ProfileDeleted += OnProfileDeleted;

                    profileViewModelCache[profile] = elementViewModel;
                }
                else
                {
                    elementViewModel.IsDefaultProfile = i == 0;
                }

                profileItems.Add(elementViewModel);
            }

            SelectDefaultItem();
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
                    var newProfile = Profiles.FirstOrDefault(p => p.CurrentNameForDisplay == newProfileName);
                    if (newProfile != null)
                    {
                        var newProfileViewModel = ProfileItems.FirstOrDefault(vm => vm.Profile == newProfile);
                        if (newProfileViewModel != null)
                        {
                            SetSelectedProfile(newProfileViewModel);
                        }
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
                _ = ProfilesModel.RemoveProfile(profile);
            }
        }

        public BE.ProfileModel? GetSelectedProfile()
        {
            return ProfileItems.FirstOrDefault(vm => vm.IsSelected)?.Profile;
        }

        public void SetSelectedProfile(BE.ProfileModel? profile)
        {
            foreach (var item in ProfileItems)
            {
                item.UpdateSelection(item.Profile == profile);
            }
        }

        public void SetSelectedProfile(ProfileListElementViewModel? profileViewModel)
        {
            foreach (var item in ProfileItems)
            {
                item.UpdateSelection(item == profileViewModel);
            }
        }

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