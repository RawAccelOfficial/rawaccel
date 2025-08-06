namespace userinterface.Services
{
    public class AnimationConfig
    {
        public int StaggerDelayMs { get; set; } = 30;

        public int AnimationDurationMs { get; set; } = 400;

        public int InitialLoadAnimationDurationMs { get; set; } = 200;

        public int DeleteAnimationDurationMs { get; set; } = 180;

        public int HideOthersAnimationDurationMs { get; set; } = 100;

        public int CollapseStaggerDelayMs { get; set; } = 15;

        public int ElementRenderDelayMs { get; set; } = 50;

        public int AnimationCompleteDelayMs { get; set; } = 200;

        public double SlideUpDistance { get; set; } = 30.0;

        public double SlideLeftDistance { get; set; } = 120.0;

        public double ProfileHeight { get; set; } = 38.0;

        public double ProfileSpacing { get; set; } = 4.0;

        public double ProfileSpawnPosition { get; set; } = 0.0;

        public double FirstIndexOffset { get; set; } = 6.0;

        public int TargetFps { get; set; } = 120;

        public int FrameDelayMs => 1000 / TargetFps;
    }
}