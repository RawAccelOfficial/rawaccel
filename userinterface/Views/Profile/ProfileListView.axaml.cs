using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using userinterface.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Animation.Easings;
using userspace_backend;
using BE = userspace_backend.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Styling;
using Avalonia.Layout;
using Avalonia;
using Avalonia.Input;
using System.Diagnostics;
using userinterface.Services;
using userinterface.Helpers;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> profiles = [];
    private Panel profileContainer;
    private Border addProfileButton;
    private readonly BE.ProfilesModel profilesModel;
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private BE.ProfileModel selectedProfile;
    private readonly IModalService modalService;
    private ProfileListAnimationHelper animationHelper;

    public ProfileListView()
    {
        var backEnd = App.Services?.GetRequiredService<BackEnd>() ?? throw new InvalidOperationException("BackEnd service not available");
        profilesModel = backEnd.Profiles ?? throw new ArgumentNullException(nameof(backEnd.Profiles));
        modalService = App.Services?.GetRequiredService<IModalService>() ?? throw new InvalidOperationException("ModalService not available");
        profilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;
        
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    private void OnUnloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        animationHelper?.CancelAllAnimations();
        animationHelper?.Dispose();
        operationSemaphore?.Dispose();
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profileContainer = this.FindControl<Panel>("ProfileContainer");
        
        addProfileButton = CreateAddProfileButton();
        profileContainer.Children.Add(addProfileButton);
        
        // Initialize animation helper after UI elements are created
        animationHelper = new ProfileListAnimationHelper(profiles, profileContainer, addProfileButton);
        
        _ = CreateProfilesWithStagger();
        
        var defaultProfile = profilesModel.Profiles.FirstOrDefault(p => p == BE.ProfilesModel.DefaultProfile);
        if (defaultProfile != null)
        {
            SetSelectedProfile(defaultProfile);
        }
        else if (profilesModel.Profiles.Count > 0)
        {
            SetSelectedProfile(profilesModel.Profiles[0]);
        }
    }

    private async void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        await operationSemaphore.WaitAsync();
        try
        {   
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    await HandleProfilesAdded(e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    await HandleProfilesRemoved(e);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    await HandleProfilesReplaced(e);
                    break;
                case NotifyCollectionChangedAction.Move:
                    await HandleProfilesMoved(e);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    await HandleProfilesReset();
                    break;
            }
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    private async Task HandleProfilesAdded(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null) return;
        
        var collectionChangedTime = DateTime.Now;
        Debug.WriteLine($"[PROFILE_TIMING] ProfilesAdded collection changed event fired at: {collectionChangedTime:HH:mm:ss.fff}");
        
        // Add profiles at their actual positions in the backend collection
        int startIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : profilesModel.Profiles.Count - e.NewItems.Count;
        
        for (int i = 0; i < e.NewItems.Count; i++)
        {
            int profileIndex = startIndex + i;
            AddProfileAtPosition(profileIndex);
        }
        
        // Refresh all profile names to ensure they display correctly
        RefreshAllProfileNames();
        
        // Auto-select the newly added profile (the last one added)
        if (e.NewItems.Count > 0)
        {
            int lastAddedIndex = startIndex + e.NewItems.Count - 1;
            if (lastAddedIndex >= 0 && lastAddedIndex < profilesModel.Profiles.Count)
            {
                SetSelectedProfile(profilesModel.Profiles[lastAddedIndex]);
            }
        }
    }

    private async Task HandleProfilesRemoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null) return;
        
        int removeIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : profiles.Count - 1;
        int removeCount = e.OldItems.Count;
        
        // Animation cancellation is now handled by the animation helper
        
        // Remove UI elements
        for (int i = 0; i < removeCount && removeIndex >= 0 && removeIndex < profiles.Count; i++)
        {
            RemoveProfileAt(removeIndex);
        }
        
        animationHelper.UpdateAllZIndexes();
        
        await animationHelper.AnimateAllProfilesToCorrectPositions(removeIndex);
        
        if (selectedProfile != null && !profilesModel.Profiles.Contains(selectedProfile))
        {
            var defaultProfile = profilesModel.Profiles.FirstOrDefault(p => p == BE.ProfilesModel.DefaultProfile);
            if (defaultProfile != null)
            {
                SetSelectedProfile(defaultProfile);
            }
            else if (profilesModel.Profiles.Count > 0)
            {
                SetSelectedProfile(profilesModel.Profiles[0]);
            }
            else
            {
                SetSelectedProfile(null);
            }
        }
    }

    private async Task HandleProfilesReplaced(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null || e.NewItems == null || e.OldStartingIndex < 0) return;
        
        int replaceIndex = e.OldStartingIndex;
        int itemCount = Math.Min(e.OldItems.Count, e.NewItems.Count);
        
        for (int i = 0; i < itemCount && replaceIndex + i < profiles.Count; i++)
        {
            if (profiles[replaceIndex + i].Child is Button button)
            {
                button.Content = $"Profile {replaceIndex + i + 1}";
            }
        }
        
        animationHelper.UpdateAllZIndexes();
        
        await animationHelper.AnimateAllProfilesToCorrectPositions(replaceIndex);
    }

    private async Task HandleProfilesMoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldStartingIndex < 0 || e.NewStartingIndex < 0) return;
        
        MoveProfile(e.OldStartingIndex, e.NewStartingIndex);
        
        animationHelper.UpdateAllZIndexes();
        
        await animationHelper.AnimateAllProfilesToCorrectPositions(Math.Min(e.OldStartingIndex, e.NewStartingIndex));
    }

    private Task HandleProfilesReset()
    {
        animationHelper.CancelAllAnimations();
        profiles.Clear();
        profileContainer?.Children.Clear();
        
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            AddProfileAtPosition(i);
        }
        
        animationHelper.UpdateAllZIndexes();
        
        return Task.CompletedTask;
    }

    private void RemoveProfileAt(int index)
    {
        if (index < 0 || index >= profiles.Count) return;
        
        var profile = profiles[index];
        profiles.RemoveAt(index);
        profileContainer?.Children.Remove(profile);
    }

    private void MoveProfile(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= profiles.Count || 
            toIndex < 0 || toIndex >= profiles.Count || 
            fromIndex == toIndex) return;

        var profile = profiles[fromIndex];
        profiles.RemoveAt(fromIndex);
        profiles.Insert(toIndex, profile);
        
        // Account for Add Profile button at index 0
        profileContainer?.Children.RemoveAt(fromIndex + 1);
        profileContainer?.Children.Insert(toIndex + 1, profile);
    }

    private void AddProfileAtPosition(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > profiles.Count) return;

        var profileAddedTime = DateTime.Now;
        
        var profileBorder = CreateProfileBorder(null, targetIndex);
        
        // Set higher z-index for the new profile so it's visible during animation
        profileBorder.ZIndex = 1000;
        profileBorder.Opacity = 1.0; // Ensure full visibility
        
        profiles.Insert(targetIndex, profileBorder);
        // Insert into container at the correct position (Add Profile button is at index 0)
        int containerIndex = targetIndex + 1; // +1 because Add Profile button is at index 0
        profileContainer?.Children.Insert(containerIndex, profileBorder);
        
        var treeInsertedTime = DateTime.Now;
        
        animationHelper.UpdateAllZIndexes();
        
        _ = animationHelper.AnimateAllProfilesToCorrectPositions(targetIndex);
    }
    
    private Border CreateAddProfileButton()
    {
        // Create the add profile text
        var addText = new TextBlock
        {
            Text = "Add Profile",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeight.Medium
        };
        
        // Create a button-like border that responds to clicks
        var border = new Border
        {
            Classes = { "AddProfileButton" },
            Height = ProfileListAnimationHelper.ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 0, 8, 0),
            Child = addText
        };
        
        border.PointerPressed += (s, e) => OnAddProfileClicked(s, e);
        
        return border;
    }

    private Border CreateProfileBorder(IBrush color, int targetIndex)
    {
        var profileName = targetIndex < profilesModel.Profiles.Count ? profilesModel.Profiles[targetIndex].CurrentNameForDisplay : $"Profile {targetIndex + 1}";
        var isDefaultProfile = targetIndex < profilesModel.Profiles.Count && profilesModel.Profiles[targetIndex] == BE.ProfilesModel.DefaultProfile;
        
        // Create the profile name text
        var profileText = new TextBlock
        {
            Text = profileName,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        // Create a grid to hold the text and button
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        
        Grid.SetColumn(profileText, 0);
        grid.Children.Add(profileText);
        
        // Only add delete button for non-default profiles
        if (!isDefaultProfile)
        {
            // Create the delete button with icon using SimpleDeleteButton
            var deleteButton = new Button
            {
                Classes = { "SimpleDeleteButton" },
                VerticalAlignment = VerticalAlignment.Center,
                Content = new PathIcon
                {
                    Data = Application.Current?.FindResource("delete_regular") as StreamGeometry,
                    Width = 12,
                    Height = 12
                }
            };
            deleteButton.Click += OnDeleteButtonClicked;
            
            // Add second column for delete button
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetColumn(deleteButton, 1);
            grid.Children.Add(deleteButton);
        }
        
        var border = new Border
        {
            Classes = { "ProfileItem" },
            Height = ProfileListAnimationHelper.ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, ProfileListAnimationHelper.ProfileSpawnPosition, 8, 0), // Start at spawn position for animation
            Child = grid,
            Opacity = 1.0,
            ZIndex = targetIndex 
        };
        
        // Make the entire border clickable
        border.PointerPressed += OnProfileBorderClicked;
        
        return border;
    }
    

    private void OnProfileBorderClicked(object sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border)
        {
            var profileIndex = profiles.IndexOf(border);
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var clickedProfile = profilesModel.Profiles[profileIndex];
                SetSelectedProfile(clickedProfile);
            }
        }
    }
    
    private void OnAddProfileClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var buttonClickTime = DateTime.Now;
        Debug.WriteLine($"[PROFILE_TIMING] Add Profile button clicked at: {buttonClickTime:HH:mm:ss.fff}");
        
        // Prevent rapid clicking during active operations
        if (animationHelper.AreAnimationsActive)
        {
            Debug.WriteLine($"[PROFILE_TIMING] Add Profile button click ignored - animations are active");
            return;
        }
        
        // Use the ViewModel's TryAddProfile method (same as AddProfileCommand)
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.TryAddProfile();
        }
    }
    
    private async void OnDeleteButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Prevent deletion during animations to avoid bugs
        if (animationHelper.AreAnimationsActive)
        {
            Debug.WriteLine($"[PROFILE_TIMING] Delete button click ignored - animations are active");
            return;
        }
        
        // Find which profile this delete button belongs to
        if (sender is Button deleteButton && 
            deleteButton.Parent is Grid grid && 
            grid.Parent is Border border)
        {
            var profileIndex = profiles.IndexOf(border);
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var profileToDelete = profilesModel.Profiles[profileIndex];
                
                // Show confirmation modal
                var confirmed = await modalService.ShowConfirmationAsync(
                    "Delete Profile",
                    $"Are you sure you want to delete '{profileToDelete.CurrentNameForDisplay}'?",
                    "Delete",
                    "Cancel");
                
                if (confirmed)
                {
                    profilesModel.RemoveProfile(profileToDelete);
                }
            }
        }
    }

    
    private async Task CreateProfilesWithStagger()
    {
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            var profileBorder = CreateProfileBorder(null, i);
            profileBorder.ZIndex = 1000;
            profileBorder.Opacity = 1.0;
            profileBorder.Margin = new Thickness(8, ProfileListAnimationHelper.CalculatePositionForIndex(0, false), 8, 0);
            
            profiles.Insert(i, profileBorder);
            int containerIndex = i + 1; // +1 because Add Profile button is at index 0
            profileContainer?.Children.Insert(containerIndex, profileBorder);
            
        }
        
        animationHelper.UpdateAllZIndexes();
        RefreshAllProfileNames();
        animationHelper.UpdateDeleteButtonStates();
    }
    



    private void SetSelectedProfile(BE.ProfileModel profile)
    {
        if (selectedProfile == profile) return;

        // Clear selected class from all profiles first
        foreach (var profileBorder in profiles)
        {
            profileBorder.Classes.Remove("Selected");
        }

        // Set new selected profile
        selectedProfile = profile;

        // Update the ViewModel's selected profile
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.SelectedProfile = selectedProfile;
        }

        // Add selected class to newly selected profile
        if (selectedProfile != null)
        {
            var currentIndex = profilesModel.Profiles.IndexOf(selectedProfile);
            if (currentIndex >= 0 && currentIndex < profiles.Count)
            {
                profiles[currentIndex].Classes.Add("Selected");
            }
        }
    }

    public BE.ProfileModel GetSelectedProfile()
    {
        return selectedProfile;
    }
    
    public bool AreAnimationsActive => animationHelper?.AreAnimationsActive ?? false;

    private void RefreshAllProfileNames()
    {
        for (int i = 0; i < profiles.Count && i < profilesModel.Profiles.Count; i++)
        {
            var border = profiles[i];
            var profile = profilesModel.Profiles[i];
            
            if (border.Child is Grid grid)
            {
                var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = profile.CurrentNameForDisplay;
                }
            }
        }
    }
    
    public async Task ExpandProfileAnimation()
    {
        await animationHelper.ExpandProfileAnimation();
    }
    
    public async Task CollapseProfileAnimation()
    {
        await animationHelper.CollapseProfileAnimation();
    }
    

}