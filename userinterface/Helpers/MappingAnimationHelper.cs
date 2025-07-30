using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Shapes;
using Avalonia.Styling;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
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
            private bool isAnimating = false;
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
                if (disposed || isAnimating) return;

                if (isActive)
                {
                    AnimateSelection(animate);
                }
                else
                {
                    AnimateDeselection(animate);
                }
            }

            private void AnimateSelection(bool animate)
            {
                if (disposed) return;

                // Show the border immediately
                path.IsVisible = true;

                if (animate)
                {
                    isAnimating = true;

                    // Animate from hidden (1640) to visible (0)
                    var animation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(800),
                        Easing = new CubicEaseOut(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters = { new Setter(Path.StrokeDashOffsetProperty, 1640.0) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(1d),
                                Setters = { new Setter(Path.StrokeDashOffsetProperty, 0.0) }
                            }
                        }
                    };

                    var task = animation.RunAsync(path);
                    task.ContinueWith(_ =>
                    {
                        if (!disposed)
                            isAnimating = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    // Set immediately without animation
                    path.StrokeDashOffset = 0;
                }
            }

            private void AnimateDeselection(bool animate)
            {
                if (disposed) return;

                if (animate)
                {
                    isAnimating = true;

                    // Animate from visible (0) to hidden (1640), then hide
                    var animation = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(800),
                        Easing = new CubicEaseOut(),
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters = { new Setter(Path.StrokeDashOffsetProperty, 0.0) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(1d),
                                Setters = { new Setter(Path.StrokeDashOffsetProperty, 1640.0) }
                            }
                        }
                    };

                    var task = animation.RunAsync(path);
                    task.ContinueWith(_ =>
                    {
                        if (!disposed)
                        {
                            // Hide the border after animation completes
                            path.IsVisible = false;
                            isAnimating = false;
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
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

                path.DataContextChanged -= OnDataContextChanged;
                UnsubscribeFromViewModel();
            }
        }
    }
}