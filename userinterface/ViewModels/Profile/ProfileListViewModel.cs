using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IViewModelFactory viewModelFactory;
        private readonly BE.ProfilesModel profilesModel;
        private readonly IProfileAnimationService animationService;

        // Just for optimization, only for cleaning up
        private readonly Dictionary<BE.ProfileModel, ProfileListElementViewModel> profileViewModelCache;
        
        // Animation service for decoupled animation handling
        public IProfileAnimationService AnimationService => animationService;
        
        // Track profiles that are being animated for removal to prevent immediate UI removal
        private readonly HashSet<BE.ProfileModel> profilesBeingRemoved = new();

        public ProfileListViewModel(userspace_backend.BackEnd backEnd, IViewModelFactory viewModelFactory, IProfileAnimationService animationService)
        {
            System.Diagnostics.Debug.WriteLine("ProfileListViewModel: Constructor started");

            this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            this.profilesModel = backEnd?.Profiles ?? throw new ArgumentNullException(nameof(backEnd));
            this.animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));

            profileItems = new ObservableCollection<ProfileListElementViewModel>();
            profileViewModelCache = new Dictionary<BE.ProfileModel, ProfileListElementViewModel>();

            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: ProfilesModel null check: {ProfilesModel == null}");
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: ProfilesModel.Profiles null check: {ProfilesModel?.Profiles == null}");
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: ProfilesModel.Profiles count: {ProfilesModel?.Profiles?.Count ?? -1}");

            ProfilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;

            UpdateProfileItems();

            AddProfileCommand = new RelayCommand(() => TryAddProfile());

            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Constructor completed. ProfileItems count: {profileItems.Count}");
        }

        // Access ProfilesModel via constructor injection
        private BE.ProfilesModel ProfilesModel => profilesModel;

        private void OnProfilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: OnProfilesCollectionChanged - Action: {e.Action}");

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Adding {e.NewItems.Count} items");
                        foreach (BE.ProfileModel profile in e.NewItems)
                        {
                            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Adding profile: {profile.CurrentNameForDisplay}");
                            AddProfileItem(profile, e.NewStartingIndex);
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Removing {e.OldItems.Count} items");
                        bool wasSelectedItemDeleted = false;
                        foreach (BE.ProfileModel profile in e.OldItems)
                        {
                            // Skip removal if this profile is being animated
                            if (profilesBeingRemoved.Contains(profile))
                            {
                                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Skipping UI removal for {profile.CurrentNameForDisplay} (being animated)");
                                profilesBeingRemoved.Remove(profile); // Clean up the flag
                                continue;
                            }
                            
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
                    System.Diagnostics.Debug.WriteLine("ProfileListViewModel: Reset action - calling UpdateProfileItems");
                    UpdateProfileItems();
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException("Move action is not written yet.");
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    System.Diagnostics.Debug.WriteLine("ProfileListViewModel: Replace action - calling UpdateProfileItems");
                    // For these cases, fall back to full update
                    UpdateProfileItems();
                    break;
            }

            // Update default profile status after any change
            UpdateDefaultProfileStatus();
        }

        private void AddProfileItem(BE.ProfileModel profile, int index)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: AddProfileItem - Profile: {profile.CurrentNameForDisplay}, Index: {index}");

            if (!profileViewModelCache.TryGetValue(profile, out var elementViewModel))
            {
                bool isDefault = index == 0;
                elementViewModel = viewModelFactory.CreateProfileListElementViewModel(profile, showButtons: true, isDefault: isDefault);
                elementViewModel.ProfileDeleted += OnProfileDeleted;
                profileViewModelCache[profile] = elementViewModel;
                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Created new ProfileListElementViewModel for {profile.CurrentNameForDisplay}");
            }

            // Insert at the correct position
            if (index >= 0 && index < profileItems.Count)
            {
                profileItems.Insert(index, elementViewModel);
                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Inserted at index {index}");
            }
            else
            {
                profileItems.Add(elementViewModel);
                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Added to end. Total count: {profileItems.Count}");
            }
        }

        private void RemoveProfileItem(BE.ProfileModel profile)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: RemoveProfileItem - Profile: {profile.CurrentNameForDisplay}");

            if (profileViewModelCache.TryGetValue(profile, out var elementViewModel))
            {
                profileItems.Remove(elementViewModel);

                // Clean up the view model
                elementViewModel.ProfileDeleted -= OnProfileDeleted;
                profileViewModelCache.Remove(profile);

                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Removed profile. Remaining count: {profileItems.Count}");
            }
        }

        private void UpdateDefaultProfileStatus()
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: UpdateDefaultProfileStatus - Processing {profileItems.Count} items");

            for (int i = 0; i < profileItems.Count; i++)
            {
                var item = profileItems[i];
                item.IsDefaultProfile = i == 0;
            }
        }

        private void SelectDefaultItem()
        {
            System.Diagnostics.Debug.WriteLine("ProfileListViewModel: SelectDefaultItem called");

            foreach (var item in profileItems)
            {
                if (item.IsDefaultProfile)
                {
                    item.UpdateSelection(true);
                    System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Selected default item: {item.Profile.CurrentNameForDisplay}");
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
            System.Diagnostics.Debug.WriteLine("ProfileListViewModel: UpdateProfileItems started");
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Profiles count: {Profiles?.Count ?? -1}");

            // Clear the observable collection but keep the cache for reuse
            profileItems.Clear();

            // Clean up ViewModels that are no longer needed
            var profilesToRemove = profileViewModelCache.Keys.Except(Profiles).ToList();
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Profiles to remove from cache: {profilesToRemove.Count}");

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
                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Processing profile {i}: {profile.CurrentNameForDisplay}");

                if (!profileViewModelCache.TryGetValue(profile, out var elementViewModel))
                {
                    bool isDefault = i == 0;
                    elementViewModel = viewModelFactory.CreateProfileListElementViewModel(profile, showButtons: true, isDefault: isDefault);

                    elementViewModel.ProfileDeleted += OnProfileDeleted;

                    profileViewModelCache[profile] = elementViewModel;
                    System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Created new ViewModel for {profile.CurrentNameForDisplay}");
                }
                else
                {
                    elementViewModel.IsDefaultProfile = i == 0;
                    System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Reused existing ViewModel for {profile.CurrentNameForDisplay}");
                }

                profileItems.Add(elementViewModel);
            }

            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: UpdateProfileItems completed. Final count: {profileItems.Count}");

            SelectDefaultItem();
        }

        private async void OnProfileDeleted(ProfileListElementViewModel elementViewModel)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: OnProfileDeleted - {elementViewModel.Profile.CurrentNameForDisplay}");
            
            // Mark this profile as being animated for removal
            profilesBeingRemoved.Add(elementViewModel.Profile);
            
            try
            {
                // Use animation service for removal
                await animationService.AnimateRemoveAsync(elementViewModel);
                
                // After animation completes, actually remove from backend
                RemoveProfile(elementViewModel.Profile);
                
                // Also manually remove from UI since we skipped the automatic removal
                RemoveProfileItem(elementViewModel.Profile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Animation failed - {ex.Message}");
                // Fallback to immediate removal
                RemoveProfile(elementViewModel.Profile);
            }
        }

        public bool TryAddProfile()
        {
            System.Diagnostics.Debug.WriteLine("ProfileListViewModel: TryAddProfile called");

            for (int i = 0; i < MaxProfileAttempts; i++)
            {
                string newProfileName = $"Profile{i}";
                System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Trying to add profile: {newProfileName}");

                if (ProfilesModel.TryAddNewDefaultProfile(newProfileName))
                {
                    System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Successfully added profile: {newProfileName}");
                    var newProfile = Profiles.FirstOrDefault(p => p.CurrentNameForDisplay == newProfileName);
                    if (newProfile != null)
                    {
                        var newProfileViewModel = ProfileItems.FirstOrDefault(vm => vm.Profile == newProfile);
                        if (newProfileViewModel != null)
                        {
                            SetSelectedProfile(newProfileViewModel);
                            
                            _ = Task.Delay(100).ContinueWith(async _ =>
                            {
                                await Dispatcher.UIThread.InvokeAsync(async () =>
                                {
                                    await InsertNewProfileBelowDefaultAsync(newProfileViewModel);
                                });
                            });
                        }
                    }
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Failed to add profile: {newProfileName}");
                }
            }
            return false;
        }

        private async Task InsertNewProfileBelowDefaultAsync(ProfileListElementViewModel newProfileViewModel)
        {
            var currentIndex = ProfileItems.IndexOf(newProfileViewModel);
            var targetIndex = 1; // Position below Default profile (index 0)

            System.Diagnostics.Debug.WriteLine($"InsertNewProfileBelowDefault: Current index={currentIndex}, Target index={targetIndex}");

            if (currentIndex == -1)
            {
                System.Diagnostics.Debug.WriteLine("InsertNewProfileBelowDefault: Could not find current index, skipping animation");
                return;
            }

            if (currentIndex == targetIndex)
            {
                System.Diagnostics.Debug.WriteLine("InsertNewProfileBelowDefault: Already at target position");
                return;
            }

            // Get all profiles that need to move down (originally at indices 1 through currentIndex-1)
            var profilesToMoveDown = new Dictionary<ProfileListElementViewModel, int>();
            
            for (int i = targetIndex; i < currentIndex; i++)
            {
                if (i < ProfileItems.Count)
                {
                    var profileToMove = ProfileItems[i];
                    profilesToMoveDown[profileToMove] = i + 1; // Move each profile down by 1
                    System.Diagnostics.Debug.WriteLine($"InsertNewProfileBelowDefault: Will move {profileToMove.Profile.CurrentNameForDisplay} from index {i} to {i + 1}");
                }
            }

            // Add the new profile to move to its target position
            profilesToMoveDown[newProfileViewModel] = targetIndex;
            System.Diagnostics.Debug.WriteLine($"InsertNewProfileBelowDefault: Will move new profile {newProfileViewModel.Profile.CurrentNameForDisplay} to index {targetIndex}");

            // Animate all profiles to their new positions simultaneously using animation service
            if (profilesToMoveDown.Count > 0)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"InsertNewProfileBelowDefault: Starting animation for {profilesToMoveDown.Count} profiles");
                    await animationService.AnimateMultipleAsync(profilesToMoveDown);
                    System.Diagnostics.Debug.WriteLine("InsertNewProfileBelowDefault: Animation completed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"InsertNewProfileBelowDefault: Animation failed - {ex.Message}");
                }
            }
        }

        public void RemoveProfile(BE.ProfileModel profile)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: RemoveProfile called for {profile?.CurrentNameForDisplay ?? "null"}");

            if (profile != null)
            {
                _ = ProfilesModel.RemoveProfile(profile);
            }
        }

        public BE.ProfileModel? GetSelectedProfile()
        {
            var selected = ProfileItems.FirstOrDefault(vm => vm.IsSelected)?.Profile;
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: GetSelectedProfile returning: {selected?.CurrentNameForDisplay ?? "null"}");
            return selected;
        }

        public void SetSelectedProfile(BE.ProfileModel? profile)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: SetSelectedProfile called for: {profile?.CurrentNameForDisplay ?? "null"}");

            foreach (var item in ProfileItems)
            {
                item.UpdateSelection(item.Profile == profile);
            }
        }

        public void SetSelectedProfile(ProfileListElementViewModel? profileViewModel)
        {
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: SetSelectedProfile (ViewModel) called for: {profileViewModel?.Profile.CurrentNameForDisplay ?? "null"}");

            foreach (var item in ProfileItems)
            {
                item.UpdateSelection(item == profileViewModel);
            }
        }

        /// <summary>
        /// Moves an item to a new position in the collection. This automatically triggers animations.
        /// </summary>
        public void MoveItemToIndex(ProfileListElementViewModel item, int newIndex)
        {
            var currentIndex = ProfileItems.IndexOf(item);
            if (currentIndex == -1 || currentIndex == newIndex || newIndex < 0 || newIndex >= ProfileItems.Count)
                return;

            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Moving {item.Profile.CurrentNameForDisplay} from index {currentIndex} to {newIndex}");
            
            // Remove from current position
            ProfileItems.RemoveAt(currentIndex);
            
            // Insert at new position
            ProfileItems.Insert(newIndex, item);
            
            // Update backend model order to match
            SynchronizeBackendOrder();
        }

        /// <summary>
        /// Swaps two items in the collection
        /// </summary>
        public void SwapItems(ProfileListElementViewModel item1, ProfileListElementViewModel item2)
        {
            var index1 = ProfileItems.IndexOf(item1);
            var index2 = ProfileItems.IndexOf(item2);
            
            if (index1 == -1 || index2 == -1) return;
            
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Swapping {item1.Profile.CurrentNameForDisplay} (index {index1}) with {item2.Profile.CurrentNameForDisplay} (index {index2})");
            
            ProfileItems[index1] = item2;
            ProfileItems[index2] = item1;
            
            SynchronizeBackendOrder();
        }

        /// <summary>
        /// Ensures the backend ProfilesModel order matches the UI collection order
        /// </summary>
        private void SynchronizeBackendOrder()
        {
            // Create a new list with the current UI order
            var reorderedProfiles = ProfileItems.Select(vm => vm.Profile).ToList();
            
            // Clear and rebuild the backend collection
            var backendProfiles = ProfilesModel.Profiles;
            backendProfiles.Clear();
            
            foreach (var profile in reorderedProfiles)
            {
                backendProfiles.Add(profile);
            }
            
            // Update default profile status
            UpdateDefaultProfileStatus();
            
            System.Diagnostics.Debug.WriteLine($"ProfileListViewModel: Synchronized backend order with {reorderedProfiles.Count} profiles");
        }

        public void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("ProfileListViewModel: Cleanup called");

            ProfilesModel.Profiles.CollectionChanged -= OnProfilesCollectionChanged;

            foreach (var viewModel in profileViewModelCache.Values)
            {
                viewModel.ProfileDeleted -= OnProfileDeleted;
            }

            profileViewModelCache.Clear();
            profileItems.Clear();

            System.Diagnostics.Debug.WriteLine("ProfileListViewModel: Cleanup completed");
        }
    }
}