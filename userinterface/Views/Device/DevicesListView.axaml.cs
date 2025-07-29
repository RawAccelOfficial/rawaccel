using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using userinterface.ViewModels.Device;

namespace userinterface.Views.Device;

public partial class DevicesListView : UserControl
{
    private DevicesListViewModel? viewModel;
    private int lastKnownItemCount = 0;
    private bool isInitialLoad = true;
    
    private readonly ConcurrentDictionary<int, CancellationTokenSource> activeAnimations = new();
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private readonly object animationLock = new();
    private volatile bool areAnimationsActive = false;
    private volatile bool isCustomDeleteInProgress = false;
    
    private const int AnimationDurationMs = 400;
    private const int InitialLoadAnimationDurationMs = 200;
    private const int DeleteAnimationDurationMs = 180;
    private const int HideOthersAnimationDurationMs = 100;
    private const int StaggerDelayMs = 50;
    private const double SlideUpDistance = 30.0;
    private const double SlideLeftDistance = 120.0;
    private const int TargetFps = 120;
    private const int FrameDelayMs = 1000 / TargetFps;
    
    public bool AreAnimationsActive => areAnimationsActive;

    public DevicesListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DevicesListInView.ContainerPrepared += OnContainerPrepared;
    }

    public async Task AnimateDeviceDelete(DeviceViewModel deviceViewModel)
    {
        if (viewModel == null) return;
        
        int index = viewModel.DeviceViews.IndexOf(deviceViewModel);
        if (index < 0) return;
        
        var container = DevicesListInView.ContainerFromIndex(index) as Control;
        if (container != null)
        {
            isCustomDeleteInProgress = true;
            
            try
            {
                await HideAllOtherDevices(index);
                await AnimateDeviceOut(container, index);
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    deviceViewModel.DeleteSelf();
                });
                
                await AnimateAllDevicesIn();
            }
            finally
            {
                isCustomDeleteInProgress = false;
            }
        }
        else
        {
            deviceViewModel.DeleteSelf();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel != null)
        {
            viewModel.DeviceViews.CollectionChanged -= OnDevicesCollectionChanged;
        }

        if (DataContext is DevicesListViewModel vm)
        {
            viewModel = vm;
            lastKnownItemCount = vm.DeviceViews.Count;
            vm.DeviceViews.CollectionChanged += OnDevicesCollectionChanged;
            vm.SetView(this);
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(vm.DeviceViews.Count * StaggerDelayMs + AnimationDurationMs);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    isInitialLoad = false;
                });
            });
        }
    }

    private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is Control container && viewModel != null)
        {
            bool isNewItem = e.Index >= lastKnownItemCount && !isInitialLoad;
            
            if (isInitialLoad)
            {
                container.Opacity = 0;
                container.RenderTransform = new TranslateTransform(0, SlideUpDistance);
                
                _ = Task.Run(async () =>
                {
                    int delay = e.Index * StaggerDelayMs;
                    await Task.Delay(delay);
                    await ShowDevice(container, e.Index);
                });
            }
            else if (isNewItem)
            {
                container.Opacity = 0;
                container.RenderTransform = new TranslateTransform(0, SlideUpDistance);
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await AnimateDeviceIn(container, e.Index);
                });
            }
            else
            {
                container.Opacity = 1;
                container.RenderTransform = new TranslateTransform(0, 0);
            }
        }
    }

    private void OnDevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (viewModel != null)
                    {
                        lastKnownItemCount = viewModel.DeviceViews.Count;
                    }
                });
            });
        }
        else if (viewModel != null)
        {
            lastKnownItemCount = viewModel.DeviceViews.Count;
        }
    }

    private async Task AnimateDeviceIn(Control container, int index)
    {
        await operationSemaphore.WaitAsync();
        
        try
        {
            CancellationTokenSource? cts = null;
            
            lock (animationLock)
            {
                if (activeAnimations.TryGetValue(index, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                
                cts = new CancellationTokenSource();
                activeAnimations[index] = cts;
                areAnimationsActive = true;
            }

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    var transform = EnsureTranslateTransform(container, 0, SlideUpDistance);

                    var opacityAnimation = CreateOpacityAnimation(0.0, 1.0, InitialLoadAnimationDurationMs, new QuadraticEaseOut());
                    
                    var opacityTask = opacityAnimation.RunAsync(container, cts.Token);
                    var transformTask = AnimateTransformAsync(transform, Axis.Y, SlideUpDistance, 0.0, InitialLoadAnimationDurationMs, EaseOutBack, cts.Token);
                    
                    await Task.WhenAll(opacityTask, transformTask);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    container.Transitions = originalTransitions;
                }
            });
        }
        finally
        {
            CleanupAnimation(index);
            operationSemaphore.Release();
        }
    }

    private async Task AnimateDeviceOut(Control container, int index)
    {
        await operationSemaphore.WaitAsync();
        
        try
        {
            CancellationTokenSource? cts = null;
            
            lock (animationLock)
            {
                if (activeAnimations.TryGetValue(index, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                
                cts = new CancellationTokenSource();
                activeAnimations[index] = cts;
                areAnimationsActive = true;
            }

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    var transform = EnsureTranslateTransform(container, 0, 0);

                    var opacityAnimation = CreateOpacityAnimation(1.0, 0.0, DeleteAnimationDurationMs, new QuadraticEaseIn());
                    
                    var opacityTask = opacityAnimation.RunAsync(container, cts.Token);
                    var transformTask = AnimateTransformAsync(transform, Axis.X, 0.0, -SlideLeftDistance, DeleteAnimationDurationMs, EaseInQuad, cts.Token);
                    
                    await Task.WhenAll(opacityTask, transformTask);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    container.Transitions = originalTransitions;
                }
            });
        }
        finally
        {
            CleanupAnimation(index);
            operationSemaphore.Release();
        }
    }
    
    private async Task HideAllOtherDevices(int excludeIndex)
    {
        if (viewModel == null) return;
        
        var hideTasks = new List<Task>();
        
        for (int i = 0; i < viewModel.DeviceViews.Count; i++)
        {
            if (i == excludeIndex) continue;
            
            var container = DevicesListInView.ContainerFromIndex(i) as Control;
            if (container != null)
            {
                hideTasks.Add(HideDevice(container, i));
            }
        }
        
        await Task.WhenAll(hideTasks);
    }
    
    private async Task HideDevice(Control container, int index)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var originalTransitions = container.Transitions;
            container.Transitions = null;
            
            try
            {
                var hideAnimation = CreateOpacityAnimation(container.Opacity, 0.0, HideOthersAnimationDurationMs, new LinearEasing());
                await hideAnimation.RunAsync(container);
            }
            finally
            {
                container.Transitions = originalTransitions;
            }
        });
    }
    
    private async Task AnimateAllDevicesIn()
    {
        if (viewModel == null) return;
        
        var showTasks = new List<Task>();
        
        for (int i = 0; i < viewModel.DeviceViews.Count; i++)
        {
            var container = DevicesListInView.ContainerFromIndex(i) as Control;
            if (container != null)
            {
                int delay = i * StaggerDelayMs;
                showTasks.Add(Task.Delay(delay).ContinueWith(_ => ShowDevice(container, i)).Unwrap());
            }
        }
        
        await Task.WhenAll(showTasks);
    }
    
    private async Task ShowDevice(Control container, int index)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var originalTransitions = container.Transitions;
            container.Transitions = null;
            
            try
            {
                var transform = EnsureTranslateTransform(container, 0, SlideUpDistance);
                container.Opacity = 0;
                
                var showAnimation = CreateOpacityAnimation(0.0, 1.0, AnimationDurationMs, new QuadraticEaseOut());
                
                var opacityTask = showAnimation.RunAsync(container);
                var transformTask = AnimateTransformAsync(transform, Axis.Y, SlideUpDistance, 0.0, AnimationDurationMs, EaseOutBack, CancellationToken.None);
                
                await Task.WhenAll(opacityTask, transformTask);
            }
            finally
            {
                container.Transitions = originalTransitions;
            }
        });
    }

    private TranslateTransform EnsureTranslateTransform(Control container, double x, double y)
    {
        if (container.RenderTransform is TranslateTransform transform)
        {
            transform.X = x;
            transform.Y = y;
            return transform;
        }
        
        transform = new TranslateTransform(x, y);
        container.RenderTransform = transform;
        return transform;
    }

    private static Animation CreateOpacityAnimation(double from, double to, int durationMs, Easing easing)
    {
        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            FillMode = FillMode.Forward,
            Easing = easing,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter { Property = OpacityProperty, Value = from } }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter { Property = OpacityProperty, Value = to } }
                }
            }
        };
    }

    private enum Axis { X, Y }

    private async Task AnimateTransformAsync(TranslateTransform transform, Axis axis, double from, double to, int durationMs, Func<double, double> easingFunction, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var duration = TimeSpan.FromMilliseconds(durationMs);
        
        while (stopwatch.Elapsed < duration && !cancellationToken.IsCancellationRequested)
        {
            var progress = Math.Min(stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
            var easedProgress = easingFunction(progress);
            var currentValue = from + (to - from) * easedProgress;
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (axis == Axis.X)
                    transform.X = currentValue;
                else
                    transform.Y = currentValue;
            });
            
            await Task.Delay(FrameDelayMs, cancellationToken);
        }
        
        if (!cancellationToken.IsCancellationRequested)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (axis == Axis.X)
                    transform.X = to;
                else
                    transform.Y = to;
            });
        }
    }

    private void CleanupAnimation(int index)
    {
        lock (animationLock)
        {
            if (activeAnimations.TryRemove(index, out var cts))
            {
                cts?.Dispose();
            }
            
            if (activeAnimations.IsEmpty)
            {
                areAnimationsActive = false;
            }
        }
    }
    
    private static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
    }
    
    private static double EaseInQuad(double t)
    {
        return t * t;
    }
}