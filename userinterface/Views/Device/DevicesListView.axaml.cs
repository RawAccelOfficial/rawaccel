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
    
    private readonly ConcurrentDictionary<int, CancellationTokenSource> activeAnimations = new();
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private readonly object animationLock = new();
    private volatile bool areAnimationsActive = false;
    private volatile bool isCustomDeleteInProgress = false;
    
    private const int AnimationDurationMs = 400;
    private const int DeleteAnimationDurationMs = 180;
    private const int HideOthersAnimationDurationMs = 100;
    private const int StaggerDelayMs = 50;
    private const double SlideUpDistance = 30.0;
    private const double SlideLeftDistance = 120.0;
    private const int TargetFps = 120;
    private const int FrameDelayMs = 1000 / TargetFps;
    
    public bool AreAnimationsActive => areAnimationsActive;

    public async Task AnimateDeviceDelete(DeviceViewModel deviceViewModel)
    {
        if (viewModel == null) return;
        
        int index = viewModel.DeviceViews.IndexOf(deviceViewModel);
        if (index < 0) return;
        
        Debug.WriteLine($"[DevicesListView] AnimateDeviceDelete called for device at index {index}");
        
        var container = DevicesListInView.ContainerFromIndex(index) as Control;
        if (container != null)
        {
            // Set flag to prevent normal collection change animations
            isCustomDeleteInProgress = true;
            
            try
            {
                // First, hide all other items
                await HideAllOtherDevices(index);
                
                // Then animate the target container out
                await AnimateDeviceOut(container, index);
                
                // After animation completes, remove from the backend collection
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    deviceViewModel.DeleteSelf();
                });
                
                // Finally, show all remaining devices with stagger
                await AnimateAllDevicesIn();
            }
            finally
            {
                isCustomDeleteInProgress = false;
            }
        }
        else
        {
            Debug.WriteLine($"[DevicesListView] Could not find container for device at index {index}, performing direct delete");
            deviceViewModel.DeleteSelf();
        }
    }

    public DevicesListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DevicesListInView.ContainerPrepared += OnContainerPrepared;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[DevicesListView] DataContext changed to: {DataContext?.GetType().Name}");
        
        if (viewModel != null)
        {
            viewModel.DeviceViews.CollectionChanged -= OnDevicesCollectionChanged;
        }

        if (DataContext is DevicesListViewModel vm)
        {
            viewModel = vm;
            lastKnownItemCount = vm.DeviceViews.Count;
            Debug.WriteLine($"[DevicesListView] Subscribing to DeviceViews.CollectionChanged. Current count: {vm.DeviceViews.Count}");
            vm.DeviceViews.CollectionChanged += OnDevicesCollectionChanged;
            vm.SetView(this);
        }
    }

    private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is Control container && viewModel != null)
        {
            bool isNewItem = e.Index >= lastKnownItemCount;
            
            Debug.WriteLine($"[DevicesListView] Container prepared at index {e.Index}, lastKnownItemCount: {lastKnownItemCount}, isNewItem: {isNewItem}");
            
            if (isNewItem)
            {
                Debug.WriteLine($"[DevicesListView] New container prepared at index {e.Index}, setting up for slide-up animation");
                
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
                Debug.WriteLine($"[DevicesListView] Existing container prepared at index {e.Index}, keeping visible");
                container.Opacity = 1;
                container.RenderTransform = new TranslateTransform(0, 0);
            }
        }
    }

    private async void OnDevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Debug.WriteLine($"[DevicesListView] Collection changed: {e.Action}, isCustomDeleteInProgress: {isCustomDeleteInProgress}");

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            Debug.WriteLine($"[DevicesListView] Device added at index {e.NewStartingIndex}, count was {lastKnownItemCount}");
            
            // Update known count after a delay to allow ContainerPrepared to handle the animation
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (viewModel != null)
                    {
                        lastKnownItemCount = viewModel.DeviceViews.Count;
                        Debug.WriteLine($"[DevicesListView] Updated known count to {lastKnownItemCount}");
                    }
                });
            });
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null && e.OldStartingIndex >= 0)
        {
            if (isCustomDeleteInProgress)
            {
                Debug.WriteLine($"[DevicesListView] Device removed at index {e.OldStartingIndex} - part of custom delete sequence, ignoring");
            }
            else
            {
                Debug.WriteLine($"[DevicesListView] Device removed at index {e.OldStartingIndex} - animation already handled by delete button");
            }
            
            if (viewModel != null)
            {
                lastKnownItemCount = viewModel.DeviceViews.Count;
            }
        }
        else
        {
            if (viewModel != null)
            {
                lastKnownItemCount = viewModel.DeviceViews.Count;
            }
        }
    }

    private async Task AnimateDeviceIn(Control container, int index)
    {
        Debug.WriteLine($"[DevicesListView] AnimateDeviceIn called for index {index}, container type: {container.GetType().Name}");
        
        await operationSemaphore.WaitAsync();
        
        try
        {
            CancellationTokenSource? cts = null;
            
            lock (animationLock)
            {
                if (activeAnimations.TryGetValue(index, out var existingCts))
                {
                    Debug.WriteLine($"[DevicesListView] Cancelling existing animation for index {index}");
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                
                cts = new CancellationTokenSource();
                activeAnimations[index] = cts;
                areAnimationsActive = true;
            }

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                Debug.WriteLine($"[DevicesListView] Starting slide-up + fade-in animation for container at index {index}");
                Debug.WriteLine($"[DevicesListView] Container initial opacity: {container.Opacity}, RenderTransform: {container.RenderTransform}");

                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    // Ensure we have a TranslateTransform to animate
                    var transform = container.RenderTransform as TranslateTransform;
                    if (transform == null)
                    {
                        transform = new TranslateTransform(0, SlideUpDistance);
                        container.RenderTransform = transform;
                    }

                    var opacityAnimation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = new QuadraticEaseOut(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = OpacityProperty,
                                        Value = 0.0
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
                                        Property = OpacityProperty,
                                        Value = 1.0
                                    }
                                }
                            }
                        }
                    };

                    var translateAnimation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = new QuadraticEaseOut(),
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
                                        Value = SlideUpDistance
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
                                        Value = 0.0
                                    }
                                }
                            }
                        }
                    };

                    Debug.WriteLine($"[DevicesListView] Running animations...");
                    
                    // Run opacity animation on container
                    var opacityTask = opacityAnimation.RunAsync(container);
                    
                    // For transform animation, we need to animate the transform properties through the container
                    // by creating a composite animation that targets the transform through the container
                    var compositeAnimation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = new QuadraticEaseOut(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = OpacityProperty,
                                        Value = 0.0
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
                                        Property = OpacityProperty,
                                        Value = 1.0
                                    }
                                }
                            }
                        }
                    };
                    
                    // Manually animate the transform Y property
                    var startY = SlideUpDistance;
                    var endY = 0.0;
                    var startTime = DateTime.Now;
                    var duration = TimeSpan.FromMilliseconds(AnimationDurationMs);
                    
                    var transformTask = Task.Run(async () =>
                    {
                        while (DateTime.Now - startTime < duration)
                        {
                            var elapsed = DateTime.Now - startTime;
                            var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                            
                            // Apply smooth ease out with back effect
                            var easedProgress = EaseOutBack(progress);
                            var currentY = startY + (endY - startY) * easedProgress;
                            
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (container.RenderTransform is TranslateTransform t)
                                {
                                    t.Y = currentY;
                                }
                            });
                            
                            await Task.Delay(FrameDelayMs); // 120fps
                        }
                        
                        // Ensure final position
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (container.RenderTransform is TranslateTransform t)
                            {
                                t.Y = endY;
                            }
                        });
                    });
                    
                    await Task.WhenAll(opacityTask, transformTask);
                    Debug.WriteLine($"[DevicesListView] Animation completed for container at index {index}");
                    Debug.WriteLine($"[DevicesListView] Container final opacity: {container.Opacity}, RenderTransform: {container.RenderTransform}");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"[DevicesListView] Animation cancelled for container at index {index}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DevicesListView] Animation error for container at index {index}: {ex.Message}");
                }
                finally
                {
                    container.Transitions = originalTransitions;
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DevicesListView] AnimateDeviceIn error for index {index}: {ex.Message}");
        }
        finally
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
                Debug.WriteLine($"[DevicesListView] Starting slide-left + fade-out animation for container at index {index}");

                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    // Ensure we have a TranslateTransform to animate
                    var transform = container.RenderTransform as TranslateTransform;
                    if (transform == null)
                    {
                        transform = new TranslateTransform(0, 0);
                        container.RenderTransform = transform;
                    }

                    var opacityAnimation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(DeleteAnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = new QuadraticEaseIn(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = OpacityProperty,
                                        Value = 1.0
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
                                        Property = OpacityProperty,
                                        Value = 0.0
                                    }
                                }
                            }
                        }
                    };

                    var translateAnimation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(DeleteAnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = new QuadraticEaseIn(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = TranslateTransform.XProperty,
                                        Value = 0.0
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
                                        Property = TranslateTransform.XProperty,
                                        Value = -SlideLeftDistance
                                    }
                                }
                            }
                        }
                    };

                    Debug.WriteLine($"[DevicesListView] Running slide-left animations...");
                    
                    // Run opacity animation on container
                    var opacityTask = opacityAnimation.RunAsync(container);
                    
                    // Manually animate the transform X property
                    var startX = 0.0;
                    var endX = -SlideLeftDistance;
                    var startTime = DateTime.Now;
                    var duration = TimeSpan.FromMilliseconds(DeleteAnimationDurationMs);
                    
                    var transformTask = Task.Run(async () =>
                    {
                        while (DateTime.Now - startTime < duration)
                        {
                            var elapsed = DateTime.Now - startTime;
                            var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                            
                            // Accelerating easing for fast deletion that speeds up
                            var easedProgress = EaseInQuad(progress);
                            var currentX = startX + (endX - startX) * easedProgress;
                            
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (container.RenderTransform is TranslateTransform t)
                                {
                                    t.X = currentX;
                                }
                            });
                            
                            await Task.Delay(FrameDelayMs); // 120fps
                        }
                        
                        // Ensure final position
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (container.RenderTransform is TranslateTransform t)
                            {
                                t.X = endX;
                            }
                        });
                    });
                    
                    await Task.WhenAll(opacityTask, transformTask);
                    Debug.WriteLine($"[DevicesListView] Slide-left animation completed for container at index {index}");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"[DevicesListView] Animation cancelled for container at index {index}");
                }
                finally
                {
                    container.Transitions = originalTransitions;
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        finally
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
            
            operationSemaphore.Release();
        }
    }
    
    private async Task HideAllOtherDevices(int excludeIndex)
    {
        if (viewModel == null) return;
        
        Debug.WriteLine($"[DevicesListView] Hiding all devices except index {excludeIndex}");
        
        var hideTasks = new List<Task>();
        
        for (int i = 0; i < viewModel.DeviceViews.Count; i++)
        {
            if (i == excludeIndex) continue; // Skip the item being deleted
            
            var container = DevicesListInView.ContainerFromIndex(i) as Control;
            if (container != null)
            {
                hideTasks.Add(HideDevice(container, i));
            }
        }
        
        await Task.WhenAll(hideTasks);
        Debug.WriteLine($"[DevicesListView] All other devices hidden");
    }
    
    private async Task HideDevice(Control container, int index)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Debug.WriteLine($"[DevicesListView] Hiding device at index {index}");
            
            var originalTransitions = container.Transitions;
            container.Transitions = null;
            
            try
            {
                var hideAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(HideOthersAnimationDurationMs),
                    FillMode = FillMode.Forward,
                    Easing = new LinearEasing(),
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter
                                {
                                    Property = OpacityProperty,
                                    Value = container.Opacity
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
                                    Property = OpacityProperty,
                                    Value = 0.0
                                }
                            }
                        }
                    }
                };
                
                await hideAnimation.RunAsync(container);
                Debug.WriteLine($"[DevicesListView] Device at index {index} hidden");
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
        
        Debug.WriteLine($"[DevicesListView] Starting staggered fade-in for {viewModel.DeviceViews.Count} devices");
        
        var showTasks = new List<Task>();
        
        for (int i = 0; i < viewModel.DeviceViews.Count; i++)
        {
            var container = DevicesListInView.ContainerFromIndex(i) as Control;
            if (container != null)
            {
                int delay = i * StaggerDelayMs;
                showTasks.Add(ShowDeviceWithDelay(container, i, delay));
            }
        }
        
        await Task.WhenAll(showTasks);
        Debug.WriteLine($"[DevicesListView] All devices shown with stagger");
    }
    
    private async Task ShowDeviceWithDelay(Control container, int index, int delayMs)
    {
        await Task.Delay(delayMs);
        await ShowDevice(container, index);
    }
    
    private async Task ShowDevice(Control container, int index)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Debug.WriteLine($"[DevicesListView] Showing device at index {index}");
            
            var originalTransitions = container.Transitions;
            container.Transitions = null;
            
            try
            {
                // Ensure we have a TranslateTransform for the slide effect
                var transform = container.RenderTransform as TranslateTransform;
                if (transform == null)
                {
                    transform = new TranslateTransform(0, SlideUpDistance);
                    container.RenderTransform = transform;
                }
                else
                {
                    transform.Y = SlideUpDistance;
                }
                
                container.Opacity = 0;
                
                var showAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                    FillMode = FillMode.Forward,
                    Easing = new QuadraticEaseOut(),
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter
                                {
                                    Property = OpacityProperty,
                                    Value = 0.0
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
                                    Property = OpacityProperty,
                                    Value = 1.0
                                }
                            }
                        }
                    }
                };
                
                // Manual Y transform animation with EaseOutBack
                var startY = SlideUpDistance;
                var endY = 0.0;
                var startTime = DateTime.Now;
                var duration = TimeSpan.FromMilliseconds(AnimationDurationMs);
                
                var opacityTask = showAnimation.RunAsync(container);
                var transformTask = Task.Run(async () =>
                {
                    while (DateTime.Now - startTime < duration)
                    {
                        var elapsed = DateTime.Now - startTime;
                        var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                        
                        var easedProgress = EaseOutBack(progress);
                        var currentY = startY + (endY - startY) * easedProgress;
                        
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (container.RenderTransform is TranslateTransform t)
                            {
                                t.Y = currentY;
                            }
                        });
                        
                        await Task.Delay(FrameDelayMs);
                    }
                    
                    // Ensure final position
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (container.RenderTransform is TranslateTransform t)
                        {
                            t.Y = endY;
                        }
                    });
                });
                
                await Task.WhenAll(opacityTask, transformTask);
                Debug.WriteLine($"[DevicesListView] Device at index {index} shown");
            }
            finally
            {
                container.Transitions = originalTransitions;
            }
        });
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
    
    private static double EaseOutQuint(double t)
    {
        return 1 - Math.Pow(1 - t, 5);
    }
}