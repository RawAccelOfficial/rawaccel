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

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private Animation rectangleAnimation;
    private readonly List<Rectangle> rectangles = new List<Rectangle>();
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
        
        // Create rectangles based on profiles count
        var profileCount = profilesModel.Profiles.Count;
        for (int i = 0; i < profileCount; i++)
        {
            AddRectangle();
        }
        
        // If we have profiles, animate them to their positions
        for (int i = 0; i < profileCount && i < rectangles.Count; i++)
        {
            // Animate each rectangle to its index position (0, 1, 2, etc.)
            AnimateRectangleToPosition(i, i);
        }
    }

    private void OnProfilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Add rectangle for each new profile
            foreach (var newProfile in e.NewItems)
            {
                var newRectangleIndex = rectangles.Count;
                AddRectangle();
                
                // Animate new rectangle to its position
                AnimateRectangleToPosition(newRectangleIndex, newRectangleIndex);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            // Remove rectangles for removed profiles
            var removeCount = e.OldItems.Count;
            for (int i = 0; i < removeCount && rectangles.Count > 0; i++)
            {
                var lastRectangle = rectangles[rectangles.Count - 1];
                rectangles.RemoveAt(rectangles.Count - 1);
                profileContainer?.Children.Remove(lastRectangle);
            }
        }
    }

    private void AddRectangle()
    {
        // Alternate colors for visual distinction
        var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange };
        var colorIndex = rectangles.Count % colors.Length;
        
        var rectangle = new Rectangle
        {
            Fill = colors[colorIndex],
            Width = 400,
            Height = 50,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(0)
        };

        rectangles.Add(rectangle);
        profileContainer?.Children.Add(rectangle);
    }

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        // DataContext changed - could be used for additional setup if needed
    }

    private void AnimateRectangleToPosition(int rectangleIndex, int position)
    {
        if (rectangles.Count <= rectangleIndex) return;

        var animatedRectangle = rectangles[rectangleIndex];
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
        animation.RunAsync(animatedRectangle).ContinueWith(_ => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                animatedRectangle.Margin = new Avalonia.Thickness(0, targetMarginTop, 0, 0);
            });
        });
    }
}