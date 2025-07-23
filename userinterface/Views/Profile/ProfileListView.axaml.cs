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

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private readonly List<Border> profiles = new List<Border>();
    private Panel profileContainer;
    private readonly BE.ProfilesModel profilesModel;

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
            AddProfile();
        }
        
        // If we have profiles, animate them to their positions
        for (int i = 0; i < profileCount && i < profiles.Count; i++)
        {
            // Animate each profile to its index position (0, 1, 2, etc.)
            AnimateProfileToPosition(i, i);
        }
    }

    private void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Add profile UI for each new profile
            foreach (var newProfile in e.NewItems)
            {
                var newProfileIndex = profiles.Count;
                AddProfile();
                
                // Animate new profile to its position
                AnimateProfileToPosition(newProfileIndex, newProfileIndex);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            // Remove profile UIs for removed profiles
            var removeCount = e.OldItems.Count;
            for (int i = 0; i < removeCount && profiles.Count > 0; i++)
            {
                var lastProfile = profiles[profiles.Count - 1];
                profiles.RemoveAt(profiles.Count - 1);
                profileContainer?.Children.Remove(lastProfile);
            }
        }
    }

    private void AddProfile()
    {
        // Alternate colors for visual distinction
        var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange };
        var colorIndex = profiles.Count % colors.Length;
        
        // Create a Border container with Button inside
        var profileBorder = new Border
        {
            Background = colors[colorIndex],
            Width = 400,
            Height = 50,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(0),
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        var button = new Button
        {
            Content = profiles.Count == 0 ? "Add Profile" : $"Profile {profiles.Count + 1}",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0)
        };

        button.Click += OnProfileButtonClicked;
        profileBorder.Child = button;

        profiles.Add(profileBorder);
        profileContainer?.Children.Add(profileBorder);
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

    private void AnimateProfileToPosition(int profileIndex, int position)
    {
        if (profiles.Count <= profileIndex) return;

        var animatedProfile = profiles[profileIndex];
        double targetMarginTop = position * 50d; // position 0 = 0px, position 1 = 50px

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300), // Faster animation
            FillMode = FillMode.Forward, // Keep final state
            Easing = Easing.Parse("0.25,0.1,0.25,1"), // Cubic bezier easing for smooth feel
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

        // Also set the margin directly after animation completes
        animation.RunAsync(animatedProfile).ContinueWith(_ => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                animatedProfile.Margin = new Avalonia.Thickness(0, targetMarginTop, 0, 0);
            });
        });
    }
}