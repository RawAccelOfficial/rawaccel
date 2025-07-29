using System;
using System.Collections.Concurrent;
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
    
    private const int AnimationDurationMs = 300;
    private const double SlideUpDistance = 20.0;
    private const double SlideLeftDistance = 100.0;
    
    public bool AreAnimationsActive => areAnimationsActive;

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
        Debug.WriteLine($"[DevicesListView] Collection changed: {e.Action}");

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            Debug.WriteLine($"[DevicesListView] Device added, keeping known count at {lastKnownItemCount} for container detection");
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (viewModel != null)
                    {
                        lastKnownItemCount = viewModel.DeviceViews.Count;
                        Debug.WriteLine($"[DevicesListView] Updated known count to {lastKnownItemCount} after delay");
                    }
                });
            });
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null && e.OldStartingIndex >= 0)
        {
            Debug.WriteLine($"[DevicesListView] Device removed at index {e.OldStartingIndex}, starting slide-left animation");
            
            var container = DevicesListInView.ContainerFromIndex(e.OldStartingIndex) as Control;
            if (container != null)
            {
                _ = Task.Run(async () => await AnimateDeviceOut(container, e.OldStartingIndex));
            }
            else
            {
                Debug.WriteLine($"[DevicesListView] Could not find container for removed device at index {e.OldStartingIndex}");
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
                Debug.WriteLine($"[DevicesListView] Starting slide-up + fade-in animation for container at index {index}");

                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    var animation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = Easing.Parse("CubicEaseOut"),
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
                                    },
                                    new Setter
                                    {
                                        Property = RenderTransformProperty,
                                        Value = new TranslateTransform(0, SlideUpDistance)
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
                                    },
                                    new Setter
                                    {
                                        Property = RenderTransformProperty,
                                        Value = new TranslateTransform(0, 0)
                                    }
                                }
                            }
                        }
                    };

                    await animation.RunAsync(container);
                    Debug.WriteLine($"[DevicesListView] Animation completed for container at index {index}");
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
                    var animation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                        FillMode = FillMode.Forward,
                        Easing = Easing.Parse("CubicEaseOut"),
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
                                    },
                                    new Setter
                                    {
                                        Property = RenderTransformProperty,
                                        Value = new TranslateTransform(0, 0)
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
                                    },
                                    new Setter
                                    {
                                        Property = RenderTransformProperty,
                                        Value = new TranslateTransform(-SlideLeftDistance, 0)
                                    }
                                }
                            }
                        }
                    };

                    await animation.RunAsync(container);
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
}