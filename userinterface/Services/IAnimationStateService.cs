using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace userinterface.Services
{
    public interface IAnimationStateService
    {
        bool AreAnimationsActive { get; }
        
        void SetAnimationsActive(bool active);
        
        event EventHandler<bool>? AnimationStateChanged;

        AnimationConfig Config { get; }

        Task<CancellationToken> RegisterAnimationAsync(string context, int index);

        void UnregisterAnimation(string context, int index);

        void CancelAnimation(string context, int index);

        void CancelAllAnimations(string? context = null);

        bool IsAnimationActive(string context, int index);

        Task<T> ExecuteWithSemaphoreAsync<T>(Func<Task<T>> operation);

        Task ExecuteWithSemaphoreAsync(Func<Task> operation);

        Animation CreateOpacityAnimation(double from, double to, int durationMs, Easing? easing = null);

        TranslateTransform EnsureTranslateTransform(Control control, double x, double y);

        Task AnimateTransformAsync(TranslateTransform transform, TransformAxis axis, double from, double to, int durationMs, Func<double, double>? easingFunction, CancellationToken cancellationToken);
    }

    public enum TransformAxis
    {
        X,
        Y
    }
}