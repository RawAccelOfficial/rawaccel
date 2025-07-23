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

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> profiles = [];
    private Panel profileContainer;
    private readonly BE.ProfilesModel profilesModel;
    private readonly Dictionary<int, CancellationTokenSource> activeAnimations = [];
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private BE.ProfileModel selectedProfile;
    
    private const double ProfileHeight = 38.0;
    private const double ProfileSpacing = 4.0;
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
        
        // Add the "Add Profile" button as the first item
        var addButton = CreateAddProfileButton();
        profileContainer.Children.Add(addButton);
        
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            AddProfileAtPosition(i);
        }
        
        // Refresh all profile names to ensure they display correctly
        RefreshAllProfileNames();
        
        // Auto-select the default profile if available
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
        
        // If the selected profile was removed, select the default profile or first available profile
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
        
        // Account for Add Profile button at index 0
        profileContainer?.Children.RemoveAt(fromIndex + 1);
        profileContainer?.Children.Insert(toIndex + 1, profile);
    }

    private void AddProfileAtPosition(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > profiles.Count) return;

        var profileBorder = CreateProfileBorder(null, targetIndex);
        
        // Set higher z-index for the new profile so it's visible during animation
        profileBorder.ZIndex = 1000;
        profileBorder.Opacity = 1.0; // Ensure full visibility
        
        profiles.Insert(targetIndex, profileBorder);
        // Insert into container at the correct position (Add Profile button is at index 0)
        int containerIndex = targetIndex + 1; // +1 because Add Profile button is at index 0
        profileContainer?.Children.Insert(containerIndex, profileBorder);
        _ = AnimateAllProfilesToCorrectPositions(targetIndex);
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
            Height = ProfileHeight,
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
        
        // Create the profile name text
        var profileText = new TextBlock
        {
            Text = profileName,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        // Create the delete button with icon
        var deleteButton = new Button
        {
            Classes = { "DeleteButton" },
            VerticalAlignment = VerticalAlignment.Center,
            Content = new PathIcon
            {
                Data = Application.Current?.FindResource("delete_regular") as StreamGeometry,
                Width = 12,
                Height = 12
            }
        };
        deleteButton.Click += OnDeleteButtonClicked;
        
        // Create a grid to hold the text and button
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        
        Grid.SetColumn(profileText, 0);
        Grid.SetColumn(deleteButton, 1);
        
        grid.Children.Add(profileText);
        grid.Children.Add(deleteButton);
        
        var border = new Border
        {
            Classes = { "ProfileItem" },
            Height = ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, ProfileSpawnPosition, 8, 0), // Start at spawn position for animation
            Child = grid,
            Opacity = 1.0 // Ensure it's fully visible
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
        // Use the ViewModel's TryAddProfile method (same as AddProfileCommand)
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.TryAddProfile();
        }
    }
    
    private void OnDeleteButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find which profile this delete button belongs to
        if (sender is Button deleteButton && 
            deleteButton.Parent is Grid grid && 
            grid.Parent is Border border)
        {
            var profileIndex = profiles.IndexOf(border);
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var profileToDelete = profilesModel.Profiles[profileIndex];
                profilesModel.RemoveProfile(profileToDelete);
            }
        }
    }

    private static double CalculatePositionForIndex(int index) => index * (ProfileHeight + ProfileSpacing);
    
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
                                Value = new Avalonia.Thickness(8, CalculatePositionForIndex(position + 1), 8, 0) // +1 for Add Profile button offset
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
            
            await animation.RunAsync(profiles[profileIndex], cts.Token);
            profiles[profileIndex].Margin = new Avalonia.Thickness(8, CalculatePositionForIndex(position + 1), 8, 0); // +1 for Add Profile button offset
            
            // Reset z-index after animation completes
            profiles[profileIndex].ZIndex = 0;
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

    private void RefreshAllProfileNames()
    {
        for (int i = 0; i < profiles.Count && i < profilesModel.Profiles.Count; i++)
        {
            var border = profiles[i];
            var profile = profilesModel.Profiles[i];
            
            // Find the TextBlock inside the border and update its text
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

}