using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using System;

namespace userinterface.Views.Profile;

public partial class ProfileListElementView : UserControl
{
    private Animation elementAnimation;

    public ProfileListElementView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        StartElementAnimation();
    }

    private void StartElementAnimation()
    {
        var animatedElement = this.FindControl<Rectangle>("AnimatedElement");
        if (animatedElement?.RenderTransform is not TranslateTransform) return;

        elementAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(2),
            IterationCount = IterationCount.Infinite,
            PlaybackDirection = PlaybackDirection.Alternate,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter
                        {
                            Property = TranslateTransform.YProperty,
                            Value = 0d
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
                            Property = TranslateTransform.YProperty,
                            Value = 50d // One interval height down (rectangle height)
                        }
                    }
                }
            }
        };

        elementAnimation.RunAsync(animatedElement);
    }
}