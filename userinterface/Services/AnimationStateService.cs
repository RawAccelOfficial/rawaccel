using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace userinterface.Services
{
    public class AnimationStateService : IAnimationStateService
    {
        private volatile bool areAnimationsActive = false;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, CancellationTokenSource>> contextAnimations = new();
        private readonly SemaphoreSlim operationSemaphore = new(1, 1);
        private readonly object animationLock = new();

        public AnimationConfig Config { get; } = new AnimationConfig();

        public bool AreAnimationsActive => areAnimationsActive;

        public event EventHandler<bool>? AnimationStateChanged;

        public void SetAnimationsActive(bool active)
        {
            if (areAnimationsActive != active)
            {
                areAnimationsActive = active;
                AnimationStateChanged?.Invoke(this, active);
            }
        }

        public async Task<CancellationToken> RegisterAnimationAsync(string context, int index)
        {
            var contextDict = contextAnimations.GetOrAdd(context, _ => new ConcurrentDictionary<int, CancellationTokenSource>());
            
            CancellationTokenSource cts;
            lock (animationLock)
            {
                if (contextDict.TryGetValue(index, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                
                cts = new CancellationTokenSource();
                contextDict[index] = cts;
                SetAnimationsActive(true);
            }
            
            await Task.Yield();
            return cts.Token;
        }

        public void UnregisterAnimation(string context, int index)
        {
            lock (animationLock)
            {
                if (contextAnimations.TryGetValue(context, out var contextDict))
                {
                    if (contextDict.TryRemove(index, out var cts))
                    {
                        cts?.Dispose();
                    }
                    
                    if (contextDict.IsEmpty)
                    {
                        contextAnimations.TryRemove(context, out _);
                    }
                }
                
                CheckAndUpdateAnimationState();
            }
        }

        public void CancelAnimation(string context, int index)
        {
            if (contextAnimations.TryGetValue(context, out var contextDict))
            {
                if (contextDict.TryGetValue(index, out var cts))
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        public void CancelAllAnimations(string? context = null)
        {
            lock (animationLock)
            {
                if (context != null)
                {
                    if (contextAnimations.TryGetValue(context, out var contextDict))
                    {
                        CancelAndClearContext(contextDict);
                        contextAnimations.TryRemove(context, out _);
                    }
                }
                else
                {
                    foreach (var kvp in contextAnimations)
                    {
                        CancelAndClearContext(kvp.Value);
                    }
                    contextAnimations.Clear();
                }
                
                CheckAndUpdateAnimationState();
            }
        }

        public bool IsAnimationActive(string context, int index)
        {
            return contextAnimations.TryGetValue(context, out var contextDict) && 
                   contextDict.ContainsKey(index);
        }

        public async Task<T> ExecuteWithSemaphoreAsync<T>(Func<Task<T>> operation)
        {
            await operationSemaphore.WaitAsync();
            try
            {
                return await operation();
            }
            finally
            {
                operationSemaphore.Release();
            }
        }

        public async Task ExecuteWithSemaphoreAsync(Func<Task> operation)
        {
            await operationSemaphore.WaitAsync();
            try
            {
                await operation();
            }
            finally
            {
                operationSemaphore.Release();
            }
        }

        public Animation CreateOpacityAnimation(double from, double to, int durationMs, Easing? easing = null)
        {
            return new Animation
            {
                Duration = TimeSpan.FromMilliseconds(durationMs),
                FillMode = FillMode.Forward,
                Easing = easing ?? new LinearEasing(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = from } }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = to } }
                    }
                }
            };
        }

        public TranslateTransform EnsureTranslateTransform(Control control, double x, double y)
        {
            if (control.RenderTransform is TranslateTransform transform)
            {
                transform.X = x;
                transform.Y = y;
                return transform;
            }
            
            transform = new TranslateTransform(x, y);
            control.RenderTransform = transform;
            return transform;
        }

        public async Task AnimateTransformAsync(TranslateTransform transform, TransformAxis axis, double from, double to, int durationMs, Func<double, double>? easingFunction, CancellationToken cancellationToken)
        {
            easingFunction ??= t => t;
            var stopwatch = Stopwatch.StartNew();
            var duration = TimeSpan.FromMilliseconds(durationMs);
            
            while (stopwatch.Elapsed < duration && !cancellationToken.IsCancellationRequested)
            {
                var progress = Math.Min(stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                var easedProgress = easingFunction(progress);
                var currentValue = from + (to - from) * easedProgress;
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (axis == TransformAxis.X)
                        transform.X = currentValue;
                    else
                        transform.Y = currentValue;
                });
                
                await Task.Delay(Config.FrameDelayMs, cancellationToken);
            }
            
            if (!cancellationToken.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (axis == TransformAxis.X)
                        transform.X = to;
                    else
                        transform.Y = to;
                });
            }
        }

        private void CancelAndClearContext(ConcurrentDictionary<int, CancellationTokenSource> contextDict)
        {
            foreach (var kvp in contextDict)
            {
                try
                {
                    kvp.Value.Cancel();
                    kvp.Value.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
            contextDict.Clear();
        }

        private void CheckAndUpdateAnimationState()
        {
            bool hasActiveAnimations = false;
            foreach (var context in contextAnimations.Values)
            {
                if (!context.IsEmpty)
                {
                    hasActiveAnimations = true;
                    break;
                }
            }
            
            if (!hasActiveAnimations)
            {
                SetAnimationsActive(false);
            }
        }
    }
}