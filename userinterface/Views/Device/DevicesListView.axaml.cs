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
using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;
using userinterface.ViewModels.Device;

namespace userinterface.Views.Device;

public partial class DevicesListView : UserControl
{
    private DevicesListViewModel? viewModel;
    private int lastKnownItemCount = 0;
    private bool isInitialLoad = true;
    
    private readonly IAnimationStateService animationStateService;
    
    public bool AreAnimationsActive => animationStateService.AreAnimationsActive;

    public DevicesListView()
    {
        animationStateService = App.Services?.GetRequiredService<IAnimationStateService>() ?? throw new InvalidOperationException("AnimationStateService not available");
        
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Animation error: {ex.Message}");
                deviceViewModel.DeleteSelf();
            }
        }
        else
        {
            deviceViewModel.DeleteSelf();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DevicesListViewModel vm)
        {
            if (viewModel != null)
            {
                viewModel.DeviceViews.CollectionChanged -= OnDevicesCollectionChanged;
            }
            
            viewModel = vm;
            lastKnownItemCount = vm.DeviceViews.Count;
            
            vm.DeviceViews.CollectionChanged += OnDevicesCollectionChanged;
            
            _ = Task.Run(async () =>
            {
                await Task.Delay(vm.DeviceViews.Count * animationStateService.Config.StaggerDelayMs + animationStateService.Config.AnimationDurationMs);
                isInitialLoad = false;
            });
        }
    }
    
    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                isInitialLoad = false;
            });
        });
    }

    private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is Control container && viewModel != null)
        {
            bool isNewItem = e.Index >= lastKnownItemCount && !isInitialLoad;
            
            if (isInitialLoad)
            {
                container.Opacity = 0;
                container.RenderTransform = new TranslateTransform(0, animationStateService.Config.SlideUpDistance);
                
                _ = Task.Run(async () =>
                {
                    int delay = e.Index * animationStateService.Config.StaggerDelayMs;
                    await Task.Delay(delay);
                    await ShowDevice(container, e.Index);
                });
            }
            else if (isNewItem)
            {
                container.Opacity = 0;
                container.RenderTransform = new TranslateTransform(0, animationStateService.Config.SlideUpDistance);
                
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
                    lastKnownItemCount = viewModel?.DeviceViews.Count ?? 0;
                });
            });
        }
    }

    private async Task AnimateDeviceIn(Control container, int index)
    {
        await animationStateService.ExecuteWithSemaphoreAsync(async () =>
        {
            var cancellationToken = await animationStateService.RegisterAnimationAsync("DevicesListView", index);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    var transform = animationStateService.EnsureTranslateTransform(container, 0, animationStateService.Config.SlideUpDistance);

                    var opacityAnimation = animationStateService.CreateOpacityAnimation(0.0, 1.0, animationStateService.Config.InitialLoadAnimationDurationMs, new QuadraticEaseOut());
                    
                    var opacityTask = opacityAnimation.RunAsync(container, cancellationToken);
                    var transformTask = animationStateService.AnimateTransformAsync(transform, TransformAxis.Y, animationStateService.Config.SlideUpDistance, 0.0, animationStateService.Config.InitialLoadAnimationDurationMs, EaseOutBack, cancellationToken);
                    
                    await Task.WhenAll(opacityTask, transformTask);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    container.Transitions = originalTransitions;
                    animationStateService.UnregisterAnimation("DevicesListView", index);
                }
            });
        });
    }

    private async Task AnimateDeviceOut(Control container, int index)
    {
        await animationStateService.ExecuteWithSemaphoreAsync(async () =>
        {
            var cancellationToken = await animationStateService.RegisterAnimationAsync("DevicesListView", index);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var originalTransitions = container.Transitions;
                container.Transitions = null;

                try
                {
                    var transform = animationStateService.EnsureTranslateTransform(container, 0, 0);

                    var opacityAnimation = animationStateService.CreateOpacityAnimation(1.0, 0.0, animationStateService.Config.DeleteAnimationDurationMs, new QuadraticEaseIn());
                    
                    var opacityTask = opacityAnimation.RunAsync(container, cancellationToken);
                    var transformTask = animationStateService.AnimateTransformAsync(transform, TransformAxis.X, 0.0, -animationStateService.Config.SlideLeftDistance, animationStateService.Config.DeleteAnimationDurationMs, EaseInQuad, cancellationToken);
                    
                    await Task.WhenAll(opacityTask, transformTask);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    container.Transitions = originalTransitions;
                    animationStateService.UnregisterAnimation("DevicesListView", index);
                }
            });
        });
    }

    private async Task HideAllOtherDevices(int exceptIndex)
    {
        if (viewModel == null) return;
        
        var hideTasks = new List<Task>();
        
        for (int i = 0; i < viewModel.DeviceViews.Count; i++)
        {
            if (i != exceptIndex)
            {
                var container = DevicesListInView.ContainerFromIndex(i) as Control;
                if (container != null && container.Opacity > 0)
                {
                    hideTasks.Add(HideDevice(container, i));
                }
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
                var hideAnimation = animationStateService.CreateOpacityAnimation(container.Opacity, 0.0, animationStateService.Config.HideOthersAnimationDurationMs, new LinearEasing());
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
        
        await Task.Delay(100);
        
        var showTasks = new List<Task>();
        
        for (int i = 0; i < viewModel.DeviceViews.Count; i++)
        {
            var container = DevicesListInView.ContainerFromIndex(i) as Control;
            if (container != null)
            {
                int delay = i * animationStateService.Config.StaggerDelayMs;
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
                var transform = animationStateService.EnsureTranslateTransform(container, 0, animationStateService.Config.SlideUpDistance);
                container.Opacity = 0;
                
                var showAnimation = animationStateService.CreateOpacityAnimation(0.0, 1.0, animationStateService.Config.AnimationDurationMs, new QuadraticEaseOut());
                
                var cancellationToken = await animationStateService.RegisterAnimationAsync("DevicesListView", index);
                var opacityTask = showAnimation.RunAsync(container);
                var transformTask = animationStateService.AnimateTransformAsync(transform, TransformAxis.Y, animationStateService.Config.SlideUpDistance, 0.0, animationStateService.Config.AnimationDurationMs, EaseOutBack, cancellationToken);
                
                await Task.WhenAll(opacityTask, transformTask);
            }
            finally
            {
                container.Transitions = originalTransitions;
                animationStateService.UnregisterAnimation("DevicesListView", index);
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
}