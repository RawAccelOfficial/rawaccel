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
using System.Diagnostics;
using userinterface.Services;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> allItems = [];
    private Panel profileContainer;
    private readonly BE.ProfilesModel profilesModel;
    private readonly Dictionary<int, CancellationTokenSource> activeAnimations = [];
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private BE.ProfileModel selectedProfile;
    
    private int GetProfileCount() => allItems.Count - 1; // Subtract 1 for add button
    private volatile bool areAnimationsActive = false;
    private readonly object animationLock = new();
    private readonly IModalService modalService;
    private readonly LocalizationService localizationService;
    private TextBlock addProfileTextBlock;
    
    private const double ProfileHeight = 38.0;
    private const double ProfileSpacing = 4.0;
    private const int StaggerDelayMs = 20;
    private const double ProfileSpawnPosition = 0.0;
    private const double FirstIndexOffset = 6;
    

    public ProfileListView()
    {
        var backEnd = App.Services?.GetRequiredService<BackEnd>() ?? throw new InvalidOperationException("BackEnd service not available");
        profilesModel = backEnd.Profiles ?? throw new ArgumentNullException(nameof(backEnd.Profiles));
        modalService = App.Services?.GetRequiredService<IModalService>() ?? throw new InvalidOperationException("ModalService not available");
        localizationService = App.Services?.GetRequiredService<LocalizationService>() ?? throw new InvalidOperationException("LocalizationService not available");
        localizationService.PropertyChanged += OnLocalizationPropertyChanged;
        profilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;
        
        InitializeComponent();
        
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    private void OnUnloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelAllAnimations();
        operationSemaphore?.Dispose();
        if (localizationService != null)
        {
            localizationService.PropertyChanged -= OnLocalizationPropertyChanged;
        }
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profileContainer = this.FindControl<Panel>("ProfileContainer");
        
        // Set the view reference in the ViewModel
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.SetView(this);
        }
        
        var addButton = CreateAddProfileButton();
        allItems.Add(addButton);
        profileContainer.Children.Add(addButton);
        
        CreateProfilesWithStagger();
        
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

    private void OnLocalizationPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (addProfileTextBlock != null)
        {
            addProfileTextBlock.Text = localizationService?.GetText("ProfileAddNewProfile") ?? "Add New Profile";
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
                    HandleProfilesAdded(e);
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

    private void HandleProfilesAdded(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null) return;

        // Add profiles at their actual positions in the backend collection
        int startIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : profilesModel.Profiles.Count - e.NewItems.Count;

        for (int i = 0; i < e.NewItems.Count; i++)
        {
            int profileIndex = startIndex + i;
            AddProfileAtPosition(profileIndex);
        }

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
        
        int removeIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : GetProfileCount() - 1;
        int removeCount = e.OldItems.Count;
        
        // Cancel animations for removed profiles
        for (int i = 0; i < removeCount && removeIndex + i < GetProfileCount(); i++)
        {
            if (activeAnimations.TryGetValue(removeIndex + i, out var cts))
            {
                cts.Cancel();
                activeAnimations.Remove(removeIndex + i);
            }
        }
        
        // Remove UI elements
        for (int i = 0; i < removeCount && removeIndex >= 0 && removeIndex < GetProfileCount(); i++)
        {
            RemoveProfileAt(removeIndex);
        }
        
        UpdateAllZIndexes();
        
        await AnimateAllElementsToPositions(removeIndex);
        
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
        
        for (int i = 0; i < itemCount && replaceIndex + i < GetProfileCount(); i++)
        {
            int itemIndex = replaceIndex + i + 1; // +1 for add button
            if (itemIndex < allItems.Count && allItems[itemIndex].Child is Grid grid)
            {
                var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = $"Profile {replaceIndex + i + 1}";
                }
            }
        }
        
        UpdateAllZIndexes();
        
        await AnimateAllElementsToPositions(replaceIndex);
    }

    private async Task HandleProfilesMoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldStartingIndex < 0 || e.NewStartingIndex < 0) return;
        
        MoveProfile(e.OldStartingIndex, e.NewStartingIndex);
        
        UpdateAllZIndexes();
        
        await AnimateAllElementsToPositions(Math.Min(e.OldStartingIndex, e.NewStartingIndex));
    }

    private Task HandleProfilesReset()
    {
        CancelAllAnimations();
        // Keep add button, clear everything else
        var addButton = allItems.Count > 0 ? allItems[0] : null;
        allItems.Clear();
        profileContainer?.Children.Clear();
        
        if (addButton != null)
        {
            allItems.Add(addButton);
            profileContainer?.Children.Add(addButton);
        }
        
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            AddProfileAtPosition(i);
        }
        
        UpdateAllZIndexes();
        
        return Task.CompletedTask;
    }

    private void RemoveProfileAt(int index)
    {
        // Adjust index to account for add button at position 0
        int itemIndex = index + 1;
        if (itemIndex < 0 || itemIndex >= allItems.Count) return;
        
        var item = allItems[itemIndex];
        allItems.RemoveAt(itemIndex);
        profileContainer?.Children.Remove(item);
    }

    private void MoveProfile(int fromIndex, int toIndex)
    {
        // Adjust indices to account for add button at position 0
        int fromItemIndex = fromIndex + 1;
        int toItemIndex = toIndex + 1;
        
        if (fromItemIndex < 1 || fromItemIndex >= allItems.Count || 
            toItemIndex < 1 || toItemIndex >= allItems.Count || 
            fromItemIndex == toItemIndex) return;

        var item = allItems[fromItemIndex];
        allItems.RemoveAt(fromItemIndex);
        allItems.Insert(toItemIndex, item);
        
        profileContainer?.Children.RemoveAt(fromItemIndex);
        profileContainer?.Children.Insert(toItemIndex, item);
    }

    private void AddProfileAtPosition(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > GetProfileCount()) return;

        var profileBorder = CreateProfileBorder(null, targetIndex);
        
        // Set higher z-index for the new profile so it's visible during animation
        profileBorder.ZIndex = 1000;
        profileBorder.Opacity = 1.0; // Ensure full visibility
        
        // Insert into allItems at correct position (add button is at index 0)
        int itemIndex = targetIndex + 1;
        allItems.Insert(itemIndex, profileBorder);
        profileContainer?.Children.Insert(itemIndex, profileBorder);
        
        UpdateAllZIndexes();
        
        _ = AnimateAllElementsToPositions(targetIndex);
    }
    
    private Border CreateAddProfileButton()
    {
        // Create the add profile text
        addProfileTextBlock = new TextBlock
        {
            Text = localizationService?.GetText("ProfileAddNewProfile") ?? "Add New Profile",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeight.Medium
        };
        
        // Create a button-like border that responds to clicks
        var border = new Border
        {
            Classes = { "AddProfileButton" },
            Height = ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 0, 8, 0),
            Child = addProfileTextBlock
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
            Height = ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, ProfileSpawnPosition, 8, 0), // Start at spawn position for animation
            Child = grid,
            Opacity = 1.0,
            ZIndex = targetIndex
        };
        
        // Make the entire border clickable
        border.PointerPressed += OnProfileBorderClicked;
        
        return border;
    }
    
    private void UpdateDeleteButtonStates()
    {
        // Update delete buttons for all profile items (skip add and default profile)
        for (int i = 2; i < allItems.Count; i++)
        {
            if (allItems[i].Child is Grid grid)
            {
                var deleteButton = grid.Children.OfType<Button>().FirstOrDefault(b => b.Classes.Contains("DeleteButton"));
                if (deleteButton != null)
                {
                    deleteButton.IsEnabled = !areAnimationsActive;
                }
            }
        }
    }

    private void OnProfileBorderClicked(object sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border)
        {
            int profileIndex = allItems.IndexOf(border) - 1; // Convert to profile index
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var clickedProfile = profilesModel.Profiles[profileIndex];
                SetSelectedProfile(clickedProfile);
            }
        }
    }
    
    private void OnAddProfileClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Prevent rapid clicking during active operations
        bool animationsActive;
        lock (animationLock)
        {
            animationsActive = areAnimationsActive;
        }
        
        if (animationsActive)
        {
            return;
        }
        
        // Use the ViewModel's TryAddProfile method
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.TryAddProfile();
        }
    }
    
    private async void OnDeleteButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Prevent deletion during animations to avoid bugs
        if (areAnimationsActive)
        {
            return;
        }
        
        // Find which profile this delete button belongs to
        if (sender is Button deleteButton && 
            deleteButton.Parent is Grid grid && 
            grid.Parent is Border border)
        {
            var profileIndex = allItems.IndexOf(border) - 1; // Subtract 1 for add button
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var profileToDelete = profilesModel.Profiles[profileIndex];
                
                // Show confirmation modal
                var confirmed = await modalService.ShowConfirmationAsync(
                    "ProfileDeleteTitle",
                    "ProfileDeleteMessage",
                    "ProfileDeleteConfirm",
                    "ModalCancel");
                
                if (confirmed)
                {
                    profilesModel.RemoveProfile(profileToDelete);
                }
            }
        }
    }

    private static double CalculatePositionForIndex(int itemIndex)
    {
        return itemIndex == 0 ? 0 : (itemIndex * (ProfileHeight + ProfileSpacing)) + FirstIndexOffset;
    }
    
    private void UpdateAllZIndexes()
    {
        for (int i = 0; i < allItems.Count; i++)
        {
            allItems[i].ZIndex = i;
        }
    }

    private void CreateProfilesWithStagger()
    {
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            var profileBorder = CreateProfileBorder(null, i);
            profileBorder.ZIndex = 1000;
            profileBorder.Opacity = 1.0;
            profileBorder.Margin = new Thickness(8, CalculatePositionForIndex(0), 8, 0);

            int itemIndex = i + 1; // +1 for add button
            allItems.Insert(itemIndex, profileBorder);
            profileContainer?.Children.Insert(itemIndex, profileBorder);

        }

        UpdateAllZIndexes();
        RefreshAllProfileNames();
        UpdateDeleteButtonStates();
    }

    private void CancelAllAnimations()
    {
        lock (animationLock)
        {
            CancelAllAnimationsInternal();
            areAnimationsActive = false;
        }
        UpdateDeleteButtonStates();
    }
    
    private void CancelAllAnimationsInternal()
    {
        foreach (var cts in activeAnimations.Values)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Token was already disposed, ignore
            }
        }
        activeAnimations.Clear();
    }

    private async Task AnimateElementToPosition(int elementIndex, int position, int staggerIndex = 0)
    {
        if (elementIndex >= allItems.Count) return;

        // Cancel existing animation for this element
        if (activeAnimations.TryGetValue(elementIndex, out var existingCts))
        {
            try
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }
            catch (ObjectDisposedException) { }
            activeAnimations.Remove(elementIndex);
        }

        // Check if element is already at correct position - skip animation if so
        var targetMargin = new Thickness(8, CalculatePositionForIndex(position), 8, 0);
        if (allItems[elementIndex].Margin == targetMargin)
        {
            allItems[elementIndex].ZIndex = position;
            return;
        }

        var cts = new CancellationTokenSource();
        activeAnimations[elementIndex] = cts;

        // Temporarily disable CSS transitions for this element during programmatic animation
        var originalTransitions = allItems[elementIndex].Transitions;
        allItems[elementIndex].Transitions = null;

        try
        {
            // Apply stagger delay only if not canceled
            if (staggerIndex > 0 && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(staggerIndex * StaggerDelayMs, cts.Token);
            }

            // Double-check cancellation after delay
            if (cts.Token.IsCancellationRequested) return;

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Controls.Border.OpacityProperty,
                                Value = 1.0
                            }
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = targetMargin
                            },
                            new Setter
                            {
                                Property = Avalonia.Controls.Border.OpacityProperty,
                                Value = 1.0
                            }
                        }
                    }
                }
            };
            
            await animation.RunAsync(allItems[elementIndex], cts.Token);
            
            if (!cts.Token.IsCancellationRequested)
            {
                allItems[elementIndex].Margin = targetMargin;
                allItems[elementIndex].ZIndex = position;
            }
        }
        catch (OperationCanceledException)
        {
            // Silently handle cancellation - this is expected behavior
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error in animation for element {elementIndex}: {ex.Message}");
        }
        finally
        {
            // Restore CSS transitions after programmatic animation completes
            if (elementIndex < allItems.Count)
            {
                allItems[elementIndex].Transitions = originalTransitions;
            }
            
            lock (animationLock)
            {
                activeAnimations.Remove(elementIndex);
                
                // Check if this was the last animation
                if (activeAnimations.Count == 0 && areAnimationsActive)
                {
                    areAnimationsActive = false;
                }
            }
            try
            {
                cts?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }

    private async Task AnimateAllElementsToPositions(int focusIndex = -1)
    {
        var animationTasks = new List<Task>();
        
        lock (animationLock)
        {
            // Cancel any existing animations before starting new ones
            CancelAllAnimationsInternal();
        }
        
        // Animate all elements to their correct positions
        // Element 0 (add button) goes to position 1, elements 1+ go to positions 2+
        for (int i = 0; i < allItems.Count; i++)
        {
            int targetPosition = i + 1;
            var targetMargin = new Thickness(8, CalculatePositionForIndex(targetPosition), 8, 0);
            if (allItems[i].Margin == targetMargin) 
            {
                allItems[i].ZIndex = targetPosition;
                continue;
            }
            
            // Calculate stagger based on focus index
            int staggerIndex = 0;
            if (focusIndex >= 0)
            {
                int focusElementIndex = focusIndex + 1; // Convert profile index to element index
                staggerIndex = (i != focusElementIndex) ? Math.Min(Math.Abs(i - focusElementIndex), 3) : 0;
            }
            else
            {
                staggerIndex = i;
            }
            
            animationTasks.Add(AnimateElementToPosition(i, targetPosition, staggerIndex));
        }
        
        if (animationTasks.Count > 0)
        {
            lock (animationLock)
            {
                areAnimationsActive = true;
            }
            UpdateDeleteButtonStates();
            
            try
            {
                await Task.WhenAll(animationTasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AnimateAllProfilesToCorrectPositions: {ex.Message}");
            }
            finally
            {
                lock (animationLock)
                {
                    areAnimationsActive = false;
                }
                UpdateDeleteButtonStates();
            }
        }
    }

    private void SetSelectedProfile(BE.ProfileModel profile)
    {
        if (selectedProfile == profile) return;

        // Clear selected class from all profile items (skip add button at index 0)
        for (int i = 1; i < allItems.Count; i++)
        {
            allItems[i].Classes.Remove("Selected");
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
            if (currentIndex >= 0 && currentIndex < GetProfileCount())
            {
                int itemIndex = currentIndex + 1; // Convert to item index
                allItems[itemIndex].Classes.Add("Selected");
            }
        }
    }

    public BE.ProfileModel GetSelectedProfile()
    {
        return selectedProfile;
    }
    
    public bool AreAnimationsActive
    {
        get
        {
            lock (animationLock)
            {
                return areAnimationsActive;
            }
        }
    }

    private void RefreshAllProfileNames()
    {
        for (int i = 0; i < GetProfileCount() && i < profilesModel.Profiles.Count; i++)
        {
            int itemIndex = i + 1; // Convert to item index
            var border = allItems[itemIndex];
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
    
    public async Task ExpandElements()
    {
        await AnimateAllElementsToPositions(-1);
    }
    
    public async Task CollapseElements()
    {
        if (allItems.Count == 0) return;
        
        var animationTasks = new List<Task>();
        
        lock (animationLock)
        {
            CancelAllAnimationsInternal();
            areAnimationsActive = true;
        }
        UpdateDeleteButtonStates();
        
        // Animate all elements to position 0 (hidden)
        for (int i = 0; i < allItems.Count; i++)
        {
            animationTasks.Add(CollapseElementToPosition(i, i));
        }
        
        try
        {
            await Task.WhenAll(animationTasks);
        }
        finally
        {
            lock (animationLock)
            {
                areAnimationsActive = false;
            }
            UpdateDeleteButtonStates();
        }
    }
    
    
    private async Task CollapseElementToPosition(int elementIndex, int staggerIndex = 0)
    {
        if (elementIndex >= allItems.Count) return;

        var cts = new CancellationTokenSource();
        activeAnimations[elementIndex] = cts;

        // Temporarily disable CSS transitions for this element during programmatic animation
        var originalTransitions = allItems[elementIndex].Transitions;
        allItems[elementIndex].Transitions = null;

        try
        {
            if (staggerIndex > 0 && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(staggerIndex * StaggerDelayMs, cts.Token);
            }

            if (cts.Token.IsCancellationRequested) return;

            var targetMargin = new Avalonia.Thickness(8, CalculatePositionForIndex(0), 8, 0);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = allItems[elementIndex].Margin
                            }
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = targetMargin
                            }
                        }
                    }
                }
            };
            
            await animation.RunAsync(allItems[elementIndex], cts.Token);
            
            if (!cts.Token.IsCancellationRequested)
            {
                allItems[elementIndex].Margin = targetMargin;
            }
        }
        catch (OperationCanceledException)
        {
            // Silently handle cancellation - this is expected behavior
        }
        catch (Exception ex)
        {
            // Log unexpected errors without performance tags
            Debug.WriteLine($"Error in collapse animation for item {elementIndex}: {ex.Message}");
        }
        finally
        {
            // Restore CSS transitions after programmatic animation completes
            if (elementIndex < allItems.Count)
            {
                allItems[elementIndex].Transitions = originalTransitions;
            }
            
            lock (animationLock)
            {
                activeAnimations.Remove(elementIndex);
            }
            try
            {
                cts?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }

}