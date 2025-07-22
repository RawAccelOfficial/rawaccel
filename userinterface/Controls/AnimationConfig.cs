using Avalonia.Animation.Easings;
using System;

namespace userinterface.Controls
{
    public class AnimationConfig
    {
        public static readonly AnimationConfig Default = new();
        
        // Timing
        public TimeSpan EnterDuration { get; set; } = TimeSpan.FromMilliseconds(400);
        public TimeSpan MoveDuration { get; set; } = TimeSpan.FromMilliseconds(300);
        public TimeSpan ExitDuration { get; set; } = TimeSpan.FromMilliseconds(250);
        public int DelayMs { get; set; } = 10;
        public int FrameRate { get; set; } = 120;
        
        // Visual Properties
        public double InitialOpacity { get; set; } = 0.0;
        public double InitialScale { get; set; } = 0.85;
        public double TargetScale { get; set; } = 1.0;
        public double ExitScaleReduction { get; set; } = 0.2;
        
        // Layout
        public double ItemSpacing { get; set; } = 8.0;
        public double PositionThreshold { get; set; } = 1.0;
        public double SlideOffset { get; set; } = 30.0; // How far to slide from during intro
        
        // Easing
        public IEasing EnterEasing { get; set; } = new QuadraticEaseOut();
        public IEasing MoveEasing { get; set; } = new CubicEaseOut();
        public IEasing ExitEasing { get; set; } = new CubicEaseIn();
        
        public AnimationConfig Clone()
        {
            return new AnimationConfig
            {
                EnterDuration = EnterDuration,
                MoveDuration = MoveDuration,
                ExitDuration = ExitDuration,
                DelayMs = DelayMs,
                FrameRate = FrameRate,
                InitialOpacity = InitialOpacity,
                InitialScale = InitialScale,
                TargetScale = TargetScale,
                ExitScaleReduction = ExitScaleReduction,
                ItemSpacing = ItemSpacing,
                PositionThreshold = PositionThreshold,
                SlideOffset = SlideOffset,
                EnterEasing = EnterEasing,
                MoveEasing = MoveEasing,
                ExitEasing = ExitEasing
            };
        }
    }
}