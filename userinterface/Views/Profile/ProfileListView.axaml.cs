using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using userinterface.ViewModels.Profile;
using System;
using System.ComponentModel;
using Avalonia.Styling;
using Avalonia.Animation.Easings;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private Animation rectangleAnimation;
    private Rectangle animatedRectangle;

    public ProfileListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        animatedRectangle = this.FindControl<Rectangle>("AnimatedRectangle");
    }

    private void OnDataContextChanged(object sender, System.EventArgs e)
    {
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfileListViewModel.CurrentPosition))
        {
            var viewModel = (ProfileListViewModel)sender;
            AnimateRectangleToPosition(viewModel.CurrentPosition);
        }
    }

    private void AnimateRectangleToPosition(int position)
    {
        if (animatedRectangle == null) return;

        double targetMarginTop = position * 50d; // position 0 = 0px, position 1 = 50px

        rectangleAnimation = new Animation
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
        rectangleAnimation.RunAsync(animatedRectangle).ContinueWith(_ => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                animatedRectangle.Margin = new Avalonia.Thickness(0, targetMarginTop, 0, 0);
            });
        });
    }
}