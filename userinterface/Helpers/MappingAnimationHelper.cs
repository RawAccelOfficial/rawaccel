using Avalonia;
using Avalonia.Controls.Shapes;
using System;
using System.ComponentModel;
using userinterface.ViewModels.Mapping;

namespace userinterface.Helpers
{
    public static class AnimationConstants
    {
        public const int AnimationDurationMs = 800;
        public const double DashLength = 1640;
    }

    public static class MappingAnimationHelper
    {
        public static readonly AttachedProperty<bool> EnableAnimationProperty =
            AvaloniaProperty.RegisterAttached<Path, bool>("EnableAnimation", typeof(MappingAnimationHelper));

        private static readonly AttachedProperty<AnimationHandler> HandlerProperty =
            AvaloniaProperty.RegisterAttached<Path, AnimationHandler>("Handler", typeof(MappingAnimationHelper));

        public static bool GetEnableAnimation(Path element) => element.GetValue(EnableAnimationProperty);
        public static void SetEnableAnimation(Path element, bool value) => element.SetValue(EnableAnimationProperty, value);

        static MappingAnimationHelper()
        {
            EnableAnimationProperty.Changed.AddClassHandler<Path>((path, e) =>
            {
                var enabled = (bool)e.NewValue!;
                var handler = path.GetValue(HandlerProperty);

                if (enabled && handler == null)
                {
                    handler = new AnimationHandler(path);
                    path.SetValue(HandlerProperty, handler);
                }
                else if (!enabled && handler != null)
                {
                    handler.Dispose();
                    path.SetValue(HandlerProperty, null);
                }
            });
        }

        private class AnimationHandler : IDisposable
        {
            private readonly Path path;
            private MappingViewModel? viewModel;
            private System.Timers.Timer? hideTimer;
            private bool disposed = false;
            private readonly object lockObject = new object();

            public AnimationHandler(Path path)
            {
                this.path = path;
                path.DataContextChanged += OnDataContextChanged;
                UpdateViewModel();
            }

            private void OnDataContextChanged(object? sender, EventArgs e)
            {
                UpdateViewModel();
            }

            private void UpdateViewModel()
            {
                lock (lockObject)
                {
                    if (disposed) return;

                    UnsubscribeFromViewModel();
                    viewModel = path.DataContext as MappingViewModel;

                    if (viewModel != null)
                    {
                        viewModel.PropertyChanged += OnViewModelPropertyChanged;
                        UpdateVisualState(viewModel.IsActiveMapping, animate: false);
                    }
                }
            }

            private void UnsubscribeFromViewModel()
            {
                if (viewModel != null)
                {
                    viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                    viewModel = null;
                }
            }

            private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                lock (lockObject)
                {
                    if (disposed) return;

                    if (e.PropertyName == nameof(MappingViewModel.IsActiveMapping) && sender is MappingViewModel vm)
                    {
                        UpdateVisualState(vm.IsActiveMapping, animate: true);
                    }
                }
            }

            private void UpdateVisualState(bool isActive, bool animate)
            {
                lock (lockObject)
                {
                    if (disposed) return;

                    CancelHideTimer();

                    if (isActive)
                    {
                        ShowSelection(animate);
                    }
                    else
                    {
                        HideSelection(animate);
                    }
                }
            }

            private void ShowSelection(bool animate)
            {
                if (disposed) return;

                path.IsVisible = true;
                path.StrokeDashOffset = 0;
            }

            private void HideSelection(bool animate)
            {
                if (disposed) return;

                if (animate)
                {
                    path.StrokeDashOffset = AnimationConstants.DashLength;
                    StartHideTimer();
                }
                else
                {
                    path.IsVisible = false;
                    path.StrokeDashOffset = AnimationConstants.DashLength;
                }
            }

            private void CancelHideTimer()
            {
                if (hideTimer != null)
                {
                    hideTimer.Stop();
                    hideTimer.Dispose();
                    hideTimer = null;
                }
            }

            private void StartHideTimer()
            {
                lock (lockObject)
                {
                    if (disposed) return;

                    hideTimer = new System.Timers.Timer(AnimationConstants.AnimationDurationMs);
                    hideTimer.Elapsed += (s, e) =>
                    {
                        lock (lockObject)
                        {
                            if (hideTimer != null)
                            {
                                hideTimer.Dispose();
                                hideTimer = null;
                            }
                            
                            if (!disposed)
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    lock (lockObject)
                                    {
                                        if (!disposed && viewModel != null && !viewModel.IsActiveMapping)
                                        {
                                            path.IsVisible = false;
                                        }
                                    }
                                });
                            }
                        }
                    };
                    hideTimer.Start();
                }
            }

            public void Dispose()
            {
                lock (lockObject)
                {
                    if (disposed) return;
                    disposed = true;

                    CancelHideTimer();
                    path.DataContextChanged -= OnDataContextChanged;
                    UnsubscribeFromViewModel();
                }
            }
        }
    }
}