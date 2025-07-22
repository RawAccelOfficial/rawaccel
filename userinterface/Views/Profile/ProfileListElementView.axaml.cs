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
    private Rectangle animatedElement;

    public ProfileListElementView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        animatedElement = this.FindControl<Rectangle>("AnimatedElement");
        // Start at position 0 (top) - no automatic animation
    }

    public void AnimateToPosition(int position)
    {
        if (animatedElement?.RenderTransform is not TranslateTransform transform) return;

        double targetY = position * 50d; // position 0 = 0px, position 1 = 50px

        elementAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            FillMode = FillMode.Forward, // Keep final state
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter
                        {
                            Property = TranslateTransform.YProperty,
                            Value = targetY
                        }
                    }
                }
            }
        };

        // Also set the transform directly after animation completes
        elementAnimation.RunAsync(animatedElement).ContinueWith(_ => 
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            {
                transform.Y = targetY;
            });
        });
    }
}