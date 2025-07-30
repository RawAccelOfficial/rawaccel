using Avalonia;
using Avalonia.Controls.Shapes;
using System;
using System.ComponentModel;
using userinterface.ViewModels.Mapping;

namespace userinterface.Helpers
{
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
                if (disposed) return;

                UnsubscribeFromViewModel();
                viewModel = path.DataContext as MappingViewModel;

                if (viewModel != null)
                {
                    viewModel.PropertyChanged += OnViewModelPropertyChanged;
                    // Set initial state without animation
                    UpdateVisualState(viewModel.IsActiveMapping, animate: false);
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
                if (disposed) return;

                if (e.PropertyName == nameof(MappingViewModel.IsActiveMapping) && sender is MappingViewModel vm)
                {
                    UpdateVisualState(vm.IsActiveMapping, animate: true);
                }
            }

            private void UpdateVisualState(bool isActive, bool animate)
            {
                if (disposed) return;

                // Cancel any pending hide timer
                hideTimer?.Stop();
                hideTimer?.Dispose();
                hideTimer = null;

                if (isActive)
                {
                    ShowSelection(animate);
                }
                else
                {
                    HideSelection(animate);
                }
            }

            private void ShowSelection(bool animate)
            {
                if (disposed) return;

                // Show the border immediately
                path.IsVisible = true;
                
                if (animate)
                {
                    // Enable CSS transition and set target value
                    path.StrokeDashOffset = 0;
                }
                else
                {
                    // Set immediately without animation
                    path.StrokeDashOffset = 0;
                }
            }

            private void HideSelection(bool animate)
            {
                if (disposed) return;

                if (animate)
                {
                    // Set target value and let CSS transition handle the animation
                    path.StrokeDashOffset = 1640;
                    
                    // Hide the border after a delay to allow animation to complete
                    hideTimer = new System.Timers.Timer(800); // Match CSS transition duration
                    hideTimer.Elapsed += (s, e) =>
                    {
                        hideTimer?.Dispose();
                        hideTimer = null;
                        
                        if (!disposed)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                // Only hide if still in deselected state
                                if (!disposed && viewModel != null && !viewModel.IsActiveMapping)
                                {
                                    path.IsVisible = false;
                                }
                            });
                        }
                    };
                    hideTimer.Start();
                }
                else
                {
                    // Hide immediately without animation
                    path.IsVisible = false;
                    path.StrokeDashOffset = 1640;
                }
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;

                // Cancel and dispose any pending timer
                hideTimer?.Stop();
                hideTimer?.Dispose();
                hideTimer = null;

                path.DataContextChanged -= OnDataContextChanged;
                UnsubscribeFromViewModel();
            }
        }
    }
}