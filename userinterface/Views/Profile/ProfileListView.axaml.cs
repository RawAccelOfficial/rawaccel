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

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> profiles = [];
    private Panel profileContainer;
    private readonly BE.ProfilesModel profilesModel;
    private readonly Dictionary<int, CancellationTokenSource> activeAnimations = [];
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    
    private const double ProfileHeight = 50.0;
    private const int StaggerDelayMs = 50;
    private const double ProfileSpawnPosition = 0.0;

    public ProfileListView()
    {
        var backEnd = App.Services?.GetRequiredService<BackEnd>() ?? throw new InvalidOperationException("BackEnd service not available");
        profilesModel = backEnd.Profiles ?? throw new ArgumentNullException(nameof(backEnd.Profiles));
        profilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;
        
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    private void OnUnloaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CancelAllAnimations();
        operationSemaphore?.Dispose();
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profileContainer = this.FindControl<Panel>("ProfileContainer");
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            AddProfileAtPosition(i);
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
        
        int insertIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : profiles.Count;
        foreach (var _ in e.NewItems)
        {
            AddProfileAtPosition(insertIndex++);
        }
    }

    private async Task HandleProfilesRemoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null) return;
        
        int removeIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : profiles.Count - 1;
        int removeCount = e.OldItems.Count;
        
        // Cancel animations for removed profiles
        for (int i = 0; i < removeCount && removeIndex + i < profiles.Count; i++)
        {
            if (activeAnimations.TryGetValue(removeIndex + i, out var cts))
            {
                cts.Cancel();
                activeAnimations.Remove(removeIndex + i);
            }
        }
        
        // Remove UI elements
        for (int i = 0; i < removeCount && removeIndex >= 0 && removeIndex < profiles.Count; i++)
        {
            RemoveProfileAt(removeIndex);
        }
        
        await AnimateAllProfilesToCorrectPositions(removeIndex);
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
        
        await AnimateAllProfilesToCorrectPositions(replaceIndex);
    }

    private async Task HandleProfilesMoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldStartingIndex < 0 || e.NewStartingIndex < 0) return;
        
        MoveProfile(e.OldStartingIndex, e.NewStartingIndex);
        await AnimateAllProfilesToCorrectPositions(Math.Min(e.OldStartingIndex, e.NewStartingIndex));
    }

    private Task HandleProfilesReset()
    {
        CancelAllAnimations();
        profiles.Clear();
        profileContainer?.Children.Clear();
        
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            AddProfileAtPosition(i);
        }
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
        
        profileContainer?.Children.RemoveAt(fromIndex);
        profileContainer?.Children.Insert(toIndex, profile);
    }

    private void AddProfileAtPosition(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > profiles.Count) return;

        var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange };
        var profileBorder = CreateProfileBorder(colors[profiles.Count % colors.Length], targetIndex);
        
        profiles.Insert(targetIndex, profileBorder);
        profileContainer?.Children.Insert(targetIndex, profileBorder);
        _ = AnimateAllProfilesToCorrectPositions(targetIndex);
    }
    
    private Border CreateProfileBorder(IBrush color, int targetIndex)
    {
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
        
        return new Border
        {
            Background = color,
            Width = 400,
            Height = ProfileHeight,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(0, ProfileSpawnPosition, 0, 0),
            CornerRadius = new Avalonia.CornerRadius(4),
            Child = button
        };
    }

    private void OnProfileButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Get the ViewModel and call AddProfile
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.TryAddProfile();
        }
    }

    private static double CalculatePositionForIndex(int index) => index * ProfileHeight;
    
    private void CancelAllAnimations()
    {
        foreach (var cts in activeAnimations.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        activeAnimations.Clear();
    }

    private async Task AnimateProfileToPosition(int profileIndex, int position, int staggerIndex = 0)
    {
        if (profileIndex >= profiles.Count) return;

        if (activeAnimations.TryGetValue(profileIndex, out var existingCts))
        {
            existingCts.Cancel();
            activeAnimations.Remove(profileIndex);
        }

        var cts = new CancellationTokenSource();
        activeAnimations[profileIndex] = cts;

        try
        {
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
                                Value = new Avalonia.Thickness(0, CalculatePositionForIndex(position), 0, 0)
                            }
                        }
                    }
                }
            };
            
            await animation.RunAsync(profiles[profileIndex], cts.Token);
            profiles[profileIndex].Margin = new Avalonia.Thickness(0, CalculatePositionForIndex(position), 0, 0);
        }
        catch (OperationCanceledException) { }
        finally
        {
            activeAnimations.Remove(profileIndex);
            cts?.Dispose();
        }
    }

    private async Task AnimateAllProfilesToCorrectPositions(int focusIndex = -1)
    {
        var tasks = new Task[profiles.Count];
        for (int i = 0; i < profiles.Count; i++)
        {
            int staggerIndex = focusIndex >= 0 ? Math.Abs(i - focusIndex) : i;
            tasks[i] = AnimateProfileToPosition(i, i, staggerIndex);
        }
        
        if (tasks.Length > 0)
        {
            await Task.WhenAll(tasks);
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