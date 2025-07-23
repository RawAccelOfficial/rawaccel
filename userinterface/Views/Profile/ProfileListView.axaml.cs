using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using userinterface.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Avalonia.Styling;
using Avalonia.Animation.Easings;
using userspace_backend;
using BE = userspace_backend.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using Avalonia.Layout;
using System.Threading.Tasks;
using System.Linq;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> profiles = new List<Border>();
    private Panel profileContainer;
    private readonly BE.ProfilesModel profilesModel;
    
    // Animation management
    private const double ProfileHeight = 50.0;
    private const int StaggerDelayMs = 50;
    
    // Animation cancellation tracking
    private readonly Dictionary<int, CancellationTokenSource> activeAnimations = new Dictionary<int, CancellationTokenSource>();
    private readonly SemaphoreSlim operationSemaphore = new SemaphoreSlim(1, 1);
    
    // Starting position for all new profiles (they animate from here to their final position)
    private const double ProfileSpawnPosition = 0.0;

    public ProfileListView()
    {
        // Inject BackEnd via DI
        var backEnd = App.Services?.GetRequiredService<BackEnd>() ?? throw new InvalidOperationException("BackEnd service not available");
        profilesModel = backEnd.Profiles ?? throw new ArgumentNullException(nameof(backEnd.Profiles));
        
        // Listen for collection changes
        profilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;
        
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    private void OnUnloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Cancel all active animations and cleanup
        foreach (var kvp in activeAnimations.ToList())
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        activeAnimations.Clear();
        operationSemaphore?.Dispose();
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profileContainer = this.FindControl<Panel>("ProfileContainer");
        
        // Create profiles based on profiles count
        var profileCount = profilesModel.Profiles.Count;
        for (int i = 0; i < profileCount; i++)
        {
            AddProfileAtPosition(i); // Use direct animation method
        }
    }

    private async void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Debug.WriteLine($"[Animation Debug] Collection changed - Action: {e.Action}, NewStartingIndex: {e.NewStartingIndex}, OldStartingIndex: {e.OldStartingIndex}");
        
        await operationSemaphore.WaitAsync();
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.WriteLine($"[Animation Debug] Handling Add - {e.NewItems?.Count} items at index {e.NewStartingIndex}");
                    // Pass the target position from the collection change event
                    await HandleProfilesAdded(e, e.NewStartingIndex >= 0 ? e.NewStartingIndex : (int?)null);
                    break;
                    
                case NotifyCollectionChangedAction.Remove:
                    Debug.WriteLine($"[Animation Debug] Handling Remove - {e.OldItems?.Count} items from index {e.OldStartingIndex}");
                    await HandleProfilesRemoved(e);
                    break;
                    
                case NotifyCollectionChangedAction.Replace:
                    Debug.WriteLine($"[Animation Debug] Handling Replace");
                    await HandleProfilesReplaced(e);
                    break;
                    
                case NotifyCollectionChangedAction.Move:
                    Debug.WriteLine($"[Animation Debug] Handling Move - from {e.OldStartingIndex} to {e.NewStartingIndex}");
                    await HandleProfilesMoved(e);
                    break;
                    
                case NotifyCollectionChangedAction.Reset:
                    Debug.WriteLine($"[Animation Debug] Handling Reset");
                    await HandleProfilesReset(e);
                    break;
            }
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    private async Task HandleProfilesAdded(NotifyCollectionChangedEventArgs e, int? targetPosition = null)
    {
        if (e.NewItems == null) return;
        
        int insertIndex = targetPosition ?? (e.NewStartingIndex >= 0 ? e.NewStartingIndex : profiles.Count);
        
        // Add profile UI for each new profile - they will spawn at ProfileSpawnPosition and animate to target
        foreach (var newProfile in e.NewItems)
        {
            AddProfileAtPosition(insertIndex);
            insertIndex++; // For multiple additions, insert subsequent items at next index
        }
    }

    private async Task HandleProfilesRemoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null) return;
        
        int removeIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : profiles.Count - 1;
        
        // Cancel any animations for profiles being removed
        var removeCount = e.OldItems.Count;
        for (int i = 0; i < removeCount && removeIndex + i < profiles.Count; i++)
        {
            if (activeAnimations.TryGetValue(removeIndex + i, out var cts))
            {
                cts.Cancel();
                activeAnimations.Remove(removeIndex + i);
            }
        }
        
        // Remove profile UIs for removed profiles
        for (int i = 0; i < removeCount && removeIndex < profiles.Count && removeIndex >= 0; i++)
        {
            RemoveProfileAt(removeIndex);
            // Don't increment removeIndex as removing shifts everything down
        }
        
        // Animate all remaining profiles to correct positions with stagger from removal point
        await AnimateAllProfilesToCorrectPositions(removeIndex);
    }

    private async Task HandleProfilesReplaced(NotifyCollectionChangedEventArgs e)
    {
        // Handle profile replacement - update existing UI elements
        if (e.OldItems != null && e.NewItems != null && e.OldStartingIndex >= 0)
        {
            int replaceIndex = e.OldStartingIndex;
            int itemCount = Math.Min(e.OldItems.Count, e.NewItems.Count);
            
            // Replace existing profiles
            for (int i = 0; i < itemCount; i++)
            {
                if (replaceIndex + i < profiles.Count)
                {
                    // Update button content for replaced profile
                    var profile = profiles[replaceIndex + i];
                    if (profile.Child is Button button)
                    {
                        button.Content = $"Profile {replaceIndex + i + 1}";
                    }
                }
            }
            
            // Animate all profiles with focus on the replacement area
            await AnimateAllProfilesToCorrectPositions(replaceIndex);
        }
    }

    private async Task HandleProfilesMoved(NotifyCollectionChangedEventArgs e)
    {
        // Handle profile reordering - animate elements to new positions
        if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0)
        {
            MoveProfile(e.OldStartingIndex, e.NewStartingIndex);
            
            // Animate all profiles with focus on the move area
            var focusPoint = Math.Min(e.OldStartingIndex, e.NewStartingIndex);
            await AnimateAllProfilesToCorrectPositions(focusPoint);
        }
    }

    private async Task HandleProfilesReset(NotifyCollectionChangedEventArgs e)
    {
        // Cancel all active animations
        foreach (var kvp in activeAnimations.ToList())
        {
            kvp.Value.Cancel();
        }
        activeAnimations.Clear();
        
        // Handle complete collection reset - clear and rebuild all UI
        profiles.Clear();
        profileContainer?.Children.Clear();
        
        // Recreate all profiles using direct animation
        var profileCount = profilesModel.Profiles.Count;
        for (int i = 0; i < profileCount; i++)
        {
            AddProfileAtPosition(i);
        }
    }

    // Core profile management methods

    private void RemoveProfileAt(int index)
    {
        if (index < 0 || index >= profiles.Count) return;

        var profileToRemove = profiles[index];
        
        // Remove from collections
        profiles.RemoveAt(index);
        profileContainer?.Children.Remove(profileToRemove);
        
        // Note: Animation will be handled by the caller using AnimateAllProfilesToCorrectPositions
    }

    private void MoveProfile(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= profiles.Count || 
            toIndex < 0 || toIndex >= profiles.Count || 
            fromIndex == toIndex) return;

        var profileToMove = profiles[fromIndex];
        
        // Remove and reinsert
        profiles.RemoveAt(fromIndex);
        profiles.Insert(toIndex, profileToMove);
        
        profileContainer?.Children.RemoveAt(fromIndex);
        profileContainer?.Children.Insert(toIndex, profileToMove);

        // Note: Animation will be handled by the caller using AnimateAllProfilesToCorrectPositions
    }

    // Method for adding profile at specific position - spawns at ProfileSpawnPosition then animates to target
    private void AddProfileAtPosition(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > profiles.Count) return;

        var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange };
        var colorIndex = profiles.Count % colors.Length;
        
        var profileBorder = new Border
        {
            Background = colors[colorIndex],
            Width = 400,
            Height = ProfileHeight,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(0, ProfileSpawnPosition, 0, 0), // Always spawn at the same position
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        var button = new Button
        {
            Content = targetIndex == 0 && profiles.Count == 0 ? "Add Profile" : $"Profile {profiles.Count + 1}",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0)
        };

        button.Click += OnProfileButtonClicked;
        profileBorder.Child = button;

        // Insert at specific index
        profiles.Insert(targetIndex, profileBorder);
        profileContainer?.Children.Insert(targetIndex, profileBorder);

        // Animate all profiles to correct positions with focus on the insertion point
        _ = AnimateAllProfilesToCorrectPositions(targetIndex);
    }

    // Legacy method for backward compatibility
    private void AddProfile()
    {
        AddProfileAtPosition(profiles.Count);
    }

    private void OnProfileButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Get the ViewModel and call AddProfile
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.TryAddProfile();
        }
    }

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        // DataContext changed - could be used for additional setup if needed
    }

    // Position management methods
    private double CalculatePositionForIndex(int index)
    {
        return index * ProfileHeight;
    }

    // Enhanced animation method with stagger support and cancellation
    private async Task AnimateProfileToPosition(int profileIndex, int position, int staggerIndex = 0)
    {
        Debug.WriteLine($"[Animation Debug] AnimateProfileToPosition called - profileIndex: {profileIndex}, position: {position}, staggerIndex: {staggerIndex}");
        
        if (profiles.Count <= profileIndex) 
        {
            Debug.WriteLine($"[Animation Debug] Early return - profiles.Count ({profiles.Count}) <= profileIndex ({profileIndex})");
            return;
        }

        // Cancel any existing animation for this profile
        if (activeAnimations.TryGetValue(profileIndex, out var existingCts))
        {
            Debug.WriteLine($"[Animation Debug] Cancelling existing animation for profile {profileIndex}");
            existingCts.Cancel();
            activeAnimations.Remove(profileIndex);
        }

        // Create new cancellation token for this animation
        var cts = new CancellationTokenSource();
        activeAnimations[profileIndex] = cts;

        var animatedProfile = profiles[profileIndex];
        double targetMarginTop = CalculatePositionForIndex(position);
        
        Debug.WriteLine($"[Animation Debug] Profile {profileIndex} animating to position {position}, targetMarginTop: {targetMarginTop}");

        try
        {
            // Apply stagger delay with cancellation support
            if (staggerIndex > 0)
            {
                await Task.Delay(staggerIndex * StaggerDelayMs, cts.Token);
            }

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("0.25,0.1,0.25,1"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = new Avalonia.Thickness(0, targetMarginTop, 0, 0)
                            }
                        }
                    }
                }
            };

            Debug.WriteLine($"[Animation Debug] Starting animation for profile {profileIndex}");
            
            await animation.RunAsync(animatedProfile, cts.Token);
            
            Debug.WriteLine($"[Animation Debug] Animation completed for profile {profileIndex}");
            animatedProfile.Margin = new Avalonia.Thickness(0, targetMarginTop, 0, 0);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[Animation Debug] Animation cancelled for profile {profileIndex}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Animation Debug] Animation error for profile {profileIndex}: {ex.Message}");
        }
        finally
        {
            // Clean up the cancellation token
            activeAnimations.Remove(profileIndex);
            cts?.Dispose();
        }
    }

    // Unified method to animate all profiles to their correct positions with proper stagger
    private async Task AnimateAllProfilesToCorrectPositions(int focusIndex = -1)
    {
        Debug.WriteLine($"[Animation Debug] AnimateAllProfilesToCorrectPositions called with focusIndex: {focusIndex}");
        
        var animationTasks = new List<Task>();
        
        for (int i = 0; i < profiles.Count; i++)
        {
            // Calculate stagger based on distance from focus point
            int staggerIndex = 0;
            if (focusIndex >= 0)
            {
                staggerIndex = Math.Abs(i - focusIndex);
            }
            else
            {
                // Default stagger is just the index
                staggerIndex = i;
            }
            
            animationTasks.Add(AnimateProfileToPosition(i, i, staggerIndex));
        }
        
        if (animationTasks.Any())
        {
            await Task.WhenAll(animationTasks);
        }
    }


    // Test methods for operations
    private void TestAddProfile(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Create a new profile and add it to the model
        var newProfileName = $"TestProfile{profilesModel.Profiles.Count}";
        profilesModel.TryAddNewDefaultProfile(newProfileName);
    }

    private void TestInsertAtIndex(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Get the ViewModel and call TryAddProfileAtPosition for position 1
        if (DataContext is ProfileListViewModel viewModel && profilesModel.Profiles.Count >= 1)
        {
            viewModel.TryAddProfileAtPosition(1);
        }
    }

    private void TestRemoveFirst(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (profilesModel.Profiles.Count > 0)
        {
            var firstProfile = profilesModel.Profiles[0];
            profilesModel.RemoveProfile(firstProfile);
        }
    }

    private void TestRemoveLast(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (profilesModel.Profiles.Count > 0)
        {
            var lastProfile = profilesModel.Profiles[profilesModel.Profiles.Count - 1];
            profilesModel.RemoveProfile(lastProfile);
        }
    }

    private void TestRemoveMiddle(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (profilesModel.Profiles.Count >= 3)
        {
            var middleIndex = profilesModel.Profiles.Count / 2;
            var middleProfile = profilesModel.Profiles[middleIndex];
            profilesModel.RemoveProfile(middleProfile);
        }
    }

    private void TestMoveFirstToLast(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (profilesModel.Profiles.Count >= 2)
        {
            var firstProfile = profilesModel.Profiles[0];
            profilesModel.Profiles.RemoveAt(0);
            profilesModel.Profiles.Add(firstProfile);
        }
    }

    private void TestClearAll(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profilesModel.Profiles.Clear();
    }

}