using System;
using System.Collections.Specialized;
using System.Diagnostics;
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
            
            if (isNewItem)
            {
                Debug.WriteLine($"[DevicesListView] New container prepared at index {e.Index}, setting invisible for animation");
                container.Opacity = 0;
                
                // Animate after a short delay
                Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await AnimateContainerIn(container, e.Index);
                });
            }
            else
            {
                Debug.WriteLine($"[DevicesListView] Existing container prepared at index {e.Index}, keeping visible");
                container.Opacity = 1;
            }
        }
    }

    private async void OnDevicesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Debug.WriteLine($"[DevicesListView] Collection changed: {e.Action}");

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            Debug.WriteLine($"[DevicesListView] Device added, keeping known count at {lastKnownItemCount} for container detection");
            // Don't update lastKnownItemCount yet - let ContainerPrepared detect new items first
            
            // Update count after a delay to allow ContainerPrepared to see the difference
            Task.Run(async () =>
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
        else
        {
            // For other actions, update immediately
            if (viewModel != null)
            {
                lastKnownItemCount = viewModel.DeviceViews.Count;
            }
        }
    }

    private async Task AnimateContainerIn(Control container, int index)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Debug.WriteLine($"[DevicesListView] Starting fade-in animation for container at index {index}");

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
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

            await animation.RunAsync(container);
            Debug.WriteLine($"[DevicesListView] Animation completed for container at index {index}");
        });
    }
}