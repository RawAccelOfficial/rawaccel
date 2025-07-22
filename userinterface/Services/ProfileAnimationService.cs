using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using userinterface.ViewModels.Profile;
using userinterface.Controls;
using System.ComponentModel;
using Avalonia.Threading;

namespace userinterface.Services
{
    public class ProfileAnimationService : IProfileAnimationService, INotifyPropertyChanged
    {
        private readonly Dictionary<object, WeakReference> registeredControls = new();
        private bool isAnimationEnabled = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsAnimationEnabled
        {
            get => isAnimationEnabled;
            private set
            {
                if (isAnimationEnabled != value)
                {
                    isAnimationEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAnimationEnabled)));
                }
            }
        }

        public void SetAnimationEnabled(bool enabled)
        {
            IsAnimationEnabled = enabled;
        }

        public void RegisterAnimatedControl(object control)
        {
            if (control != null)
            {
                registeredControls[control] = new WeakReference(control);
                CleanupDeadReferences();
            }
        }

        public void UnregisterAnimatedControl(object control)
        {
            if (control != null)
            {
                registeredControls.Remove(control);
            }
        }

        public async Task AnimateAddAsync(ProfileListElementViewModel item, int index)
        {
            if (!IsAnimationEnabled) return;

            await ExecuteOnAnimatedControlsAsync(async control =>
            {
                if (control is AnimatedItemsCanvas canvas)
                {
                    // The canvas will handle the intro animation automatically when the item is added
                    // We just need to ensure proper positioning
                    await Task.Delay(AnimationConfig.Default.DelayMs);
                }
            });
        }

        public async Task AnimateRemoveAsync(ProfileListElementViewModel item)
        {
            if (!IsAnimationEnabled) return;

            await ExecuteOnAnimatedControlsAsync(async control =>
            {
                if (control is AnimatedItemsCanvas canvas)
                {
                    var presenter = canvas.GetPresenterForItem(item);
                    if (presenter != null)
                    {
                        // Mark as pending removal
                        item.IsHidden = true;
                        
                        // Let the canvas handle the exit animation
                        await Task.Delay((int)AnimationConfig.Default.ExitDuration.TotalMilliseconds);
                    }
                }
            });
        }

        public async Task AnimateMoveAsync(ProfileListElementViewModel item, int fromIndex, int toIndex)
        {
            if (!IsAnimationEnabled || fromIndex == toIndex) return;

            await ExecuteOnAnimatedControlsAsync(async control =>
            {
                if (control is AnimatedItemsCanvas canvas)
                {
                    await canvas.AnimateToIndexAsync(item, toIndex, AnimationConfig.Default.MoveDuration);
                }
            });
        }

        public async Task AnimateMultipleAsync(Dictionary<ProfileListElementViewModel, int> itemIndexPairs)
        {
            if (!IsAnimationEnabled || itemIndexPairs.Count == 0) return;

            await ExecuteOnAnimatedControlsAsync(async control =>
            {
                if (control is AnimatedItemsCanvas canvas)
                {
                    var objectPairs = itemIndexPairs.ToDictionary(
                        kvp => (object)kvp.Key,
                        kvp => kvp.Value
                    );
                    await canvas.AnimateMultipleToIndicesAsync(objectPairs, AnimationConfig.Default.MoveDuration);
                }
            });
        }

        private async Task ExecuteOnAnimatedControlsAsync(Func<object, Task> action)
        {
            var controlsToRemove = new List<object>();

            // Execute actions sequentially on UI thread for simplicity
            foreach (var kvp in registeredControls)
            {
                var controlRef = kvp.Value;
                if (controlRef.Target is { } control)
                {
                    try
                    {
                        // Ensure we're on the UI thread
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            await action(control);
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(async () => await action(control));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Animation action failed: {ex.Message}");
                    }
                }
                else
                {
                    controlsToRemove.Add(kvp.Key);
                }
            }

            // Clean up dead references
            foreach (var key in controlsToRemove)
            {
                registeredControls.Remove(key);
            }
        }

        private void CleanupDeadReferences()
        {
            var deadKeys = registeredControls
                .Where(kvp => !kvp.Value.IsAlive)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in deadKeys)
            {
                registeredControls.Remove(key);
            }
        }
    }
}