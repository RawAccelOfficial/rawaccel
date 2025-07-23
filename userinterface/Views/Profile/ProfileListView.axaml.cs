using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using userinterface.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Styling;
using Avalonia.Animation.Easings;
using userspace_backend;
using BE = userspace_backend.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using Avalonia.Layout;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> profiles = new List<Border>();
    private Panel profileContainer;
    private readonly BE.ProfilesModel profilesModel;
    
    // Operation queue and animation management
    private readonly ConcurrentQueue<AnimationOperation> operationQueue = new ConcurrentQueue<AnimationOperation>();
    private bool isProcessingOperations = false;
    private const double ProfileHeight = 50.0;
    private const int StaggerDelayMs = 50;
    
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
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profileContainer = this.FindControl<Panel>("ProfileContainer");
        
        // Create profiles based on profiles count
        var profileCount = profilesModel.Profiles.Count;
        for (int i = 0; i < profileCount; i++)
        {
            InsertProfile(i); // Use InsertProfile for consistent creation and animation
        }
    }

    private void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                HandleProfilesAdded(e);
                break;
                
            case NotifyCollectionChangedAction.Remove:
                HandleProfilesRemoved(e);
                break;
                
            case NotifyCollectionChangedAction.Replace:
                HandleProfilesReplaced(e);
                break;
                
            case NotifyCollectionChangedAction.Move:
                HandleProfilesMoved(e);
                break;
                
            case NotifyCollectionChangedAction.Reset:
                HandleProfilesReset(e);
                break;
        }
    }

    private void HandleProfilesAdded(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null) return;
        
        int insertIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : profiles.Count;
        
        // Add profile UI for each new profile at the correct index
        foreach (var newProfile in e.NewItems)
        {
            InsertProfile(insertIndex);
            insertIndex++; // For multiple additions, insert subsequent items at next index
        }
    }

    private void HandleProfilesRemoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null) return;
        
        int removeIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : profiles.Count - 1;
        
        // Remove profile UIs for removed profiles
        var removeCount = e.OldItems.Count;
        for (int i = 0; i < removeCount && removeIndex < profiles.Count && removeIndex >= 0; i++)
        {
            RemoveProfileAt(removeIndex);
            // Don't increment removeIndex as removing shifts everything down
        }
    }

    private void HandleProfilesReplaced(NotifyCollectionChangedEventArgs e)
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
        }
    }

    private void HandleProfilesMoved(NotifyCollectionChangedEventArgs e)
    {
        // Handle profile reordering - animate elements to new positions
        if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0)
        {
            MoveProfile(e.OldStartingIndex, e.NewStartingIndex);
        }
    }

    private void HandleProfilesReset(NotifyCollectionChangedEventArgs e)
    {
        // Handle complete collection reset - clear and rebuild all UI
        profiles.Clear();
        profileContainer?.Children.Clear();
        
        // Recreate all profiles using InsertProfile for proper animation queuing
        var profileCount = profilesModel.Profiles.Count;
        for (int i = 0; i < profileCount; i++)
        {
            InsertProfile(i);
        }
    }

    // Core profile management methods
    private void InsertProfile(int index)
    {
        if (index < 0 || index > profiles.Count) return;

        var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange };
        var colorIndex = profiles.Count % colors.Length;
        
        var profileBorder = new Border
        {
            Background = colors[colorIndex],
            Width = 400,
            Height = ProfileHeight,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(0, ProfileSpawnPosition, 0, 0), // Start at spawn position
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        var button = new Button
        {
            Content = index == 0 && profiles.Count == 0 ? "Add Profile" : $"Profile {profiles.Count + 1}",
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
        profiles.Insert(index, profileBorder);
        profileContainer?.Children.Insert(index, profileBorder);

        // Queue animation for affected profiles (those at index and below)
        var affectedIndices = new List<int>();
        for (int i = index; i < profiles.Count; i++)
        {
            affectedIndices.Add(i);
        }

        var operation = new AnimationOperation
        {
            Type = OperationType.Insert,
            Index = index,
            Profile = profileBorder,
            AffectedIndices = affectedIndices
        };

        operationQueue.Enqueue(operation);
        _ = ProcessOperationQueue();
    }

    private void RemoveProfileAt(int index)
    {
        if (index < 0 || index >= profiles.Count) return;

        var profileToRemove = profiles[index];
        
        // Remove from collections
        profiles.RemoveAt(index);
        profileContainer?.Children.Remove(profileToRemove);

        // Queue animation for affected profiles (those below the removed one)
        var affectedIndices = new List<int>();
        for (int i = index; i < profiles.Count; i++)
        {
            affectedIndices.Add(i);
        }

        var operation = new AnimationOperation
        {
            Type = OperationType.Remove,
            Index = index,
            Profile = profileToRemove,
            AffectedIndices = affectedIndices
        };

        operationQueue.Enqueue(operation);
        _ = ProcessOperationQueue();
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

        // Calculate affected range
        var minIndex = Math.Min(fromIndex, toIndex);
        var maxIndex = Math.Max(fromIndex, toIndex);
        var affectedIndices = new List<int>();
        for (int i = minIndex; i <= maxIndex; i++)
        {
            affectedIndices.Add(i);
        }

        var operation = new AnimationOperation
        {
            Type = OperationType.Move,
            Index = fromIndex,
            NewIndex = toIndex,
            Profile = profileToMove,
            AffectedIndices = affectedIndices
        };

        operationQueue.Enqueue(operation);
        _ = ProcessOperationQueue();
    }

    // Legacy method for backward compatibility
    private void AddProfile()
    {
        InsertProfile(profiles.Count);
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

    private void RecalculateAllPositions()
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            var targetPosition = CalculatePositionForIndex(i);
            profiles[i].Margin = new Avalonia.Thickness(0, targetPosition, 0, 0);
        }
    }

    // Enhanced animation method with stagger support
    private async Task AnimateProfileToPosition(int profileIndex, int position, int staggerIndex = 0)
    {
        if (profiles.Count <= profileIndex) return;

        var animatedProfile = profiles[profileIndex];
        double targetMarginTop = CalculatePositionForIndex(position);

        // Apply stagger delay
        if (staggerIndex > 0)
        {
            await Task.Delay(staggerIndex * StaggerDelayMs);
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

        animation.RunAsync(animatedProfile).ContinueWith(_ => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                animatedProfile.Margin = new Avalonia.Thickness(0, targetMarginTop, 0, 0);
            });
        });
    }

    // Operation queue processing
    private async Task ProcessOperationQueue()
    {
        if (isProcessingOperations) return;
        isProcessingOperations = true;

        var operations = new List<AnimationOperation>();
        while (operationQueue.TryDequeue(out var operation))
        {
            operations.Add(operation);
        }

        foreach (var operation in operations)
        {
            await ProcessOperation(operation);
        }

        isProcessingOperations = false;
    }

    private async Task ProcessOperation(AnimationOperation operation)
    {
        switch (operation.Type)
        {
            case OperationType.Insert:
                await ProcessInsertOperation(operation);
                break;
            case OperationType.Remove:
                await ProcessRemoveOperation(operation);
                break;
            case OperationType.Move:
                await ProcessMoveOperation(operation);
                break;
            case OperationType.Reposition:
                await ProcessRepositionOperation(operation);
                break;
        }
    }

    private async Task ProcessInsertOperation(AnimationOperation operation)
    {
        // Animate all affected profiles to their new positions with stagger
        for (int i = 0; i < operation.AffectedIndices.Count; i++)
        {
            var index = operation.AffectedIndices[i];
            if (index < profiles.Count)
            {
                _ = AnimateProfileToPosition(index, index, i);
            }
        }
    }

    private async Task ProcessRemoveOperation(AnimationOperation operation)
    {
        // Animate all affected profiles to close the gap with stagger
        for (int i = 0; i < operation.AffectedIndices.Count; i++)
        {
            var index = operation.AffectedIndices[i];
            if (index < profiles.Count)
            {
                _ = AnimateProfileToPosition(index, index, i);
            }
        }
    }

    private async Task ProcessMoveOperation(AnimationOperation operation)
    {
        // Handle move operations with smooth animations
        if (operation.NewIndex.HasValue)
        {
            for (int i = 0; i < operation.AffectedIndices.Count; i++)
            {
                var index = operation.AffectedIndices[i];
                if (index < profiles.Count)
                {
                    _ = AnimateProfileToPosition(index, index, i);
                }
            }
        }
    }

    private async Task ProcessRepositionOperation(AnimationOperation operation)
    {
        // Reposition all profiles with stagger
        for (int i = 0; i < profiles.Count; i++)
        {
            _ = AnimateProfileToPosition(i, i, i);
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
        if (profilesModel.Profiles.Count >= 1)
        {
            var newProfileName = $"InsertedProfile{profilesModel.Profiles.Count}";
            // Use TryAddNewDefaultProfile and then move it, since we need the proper validator
            if (profilesModel.TryAddNewDefaultProfile(newProfileName))
            {
                // Move the newly added profile (last) to index 1
                var newProfile = profilesModel.Profiles[profilesModel.Profiles.Count - 1];
                profilesModel.Profiles.RemoveAt(profilesModel.Profiles.Count - 1);
                profilesModel.Profiles.Insert(1, newProfile);
            }
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

    // Animation operation types
    private enum OperationType
    {
        Insert,
        Remove,
        Move,
        Reposition
    }

    private class AnimationOperation
    {
        public OperationType Type { get; set; }
        public int Index { get; set; }
        public int? NewIndex { get; set; } // For move operations
        public Border Profile { get; set; }
        public List<int> AffectedIndices { get; set; } = new List<int>();
    }
}