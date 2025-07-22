using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using System;

namespace userinterface.Views
{
    public partial class AnimationExample : UserControl
    {
        private readonly Point PositionA = new Point(50, 50);
        private readonly Point PositionB = new Point(300, 200);
        
        public AnimationExample()
        {
            InitializeComponent();
            
            var moveToAButton = this.FindControl<Button>("MoveToPositionAButton");
            var moveToBButton = this.FindControl<Button>("MoveToPositionBButton");
            
            if (moveToAButton != null)
                moveToAButton.Click += OnMoveToPositionA;
                
            if (moveToBButton != null)
                moveToBButton.Click += OnMoveToPositionB;
        }
        
        private void OnMoveToPositionA(object? sender, RoutedEventArgs e)
        {
            AnimateToPosition(PositionA);
        }
        
        private void OnMoveToPositionB(object? sender, RoutedEventArgs e)
        {
            AnimateToPosition(PositionB);
        }
        
        private void AnimateToPosition(Point targetPosition)
        {
            var redBox = this.FindControl<Border>("RedBox");
            if (redBox == null) return;
            
            // Get current position
            var currentX = Canvas.GetLeft(redBox);
            var currentY = Canvas.GetTop(redBox);
            
            // Create keyframe animations
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(500),
                Easing = new CubicEaseInOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(Canvas.LeftProperty, currentX),
                            new Setter(Canvas.TopProperty, currentY)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(Canvas.LeftProperty, targetPosition.X),
                            new Setter(Canvas.TopProperty, targetPosition.Y)
                        }
                    }
                }
            };
            
            animation.RunAsync(redBox);
        }
    }
}