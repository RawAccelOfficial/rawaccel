using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace userinterface.Controls
{
    public class AnimatedItemsCanvas : Canvas
    {
        private readonly Dictionary<object, ContentPresenter> itemPresenters = new();
        private readonly Dictionary<ContentPresenter, AnimationState> animationStates = new();
        private readonly Dictionary<ContentPresenter, DispatcherTimer> activeAnimations = new();
        
        private IEnumerable? previousItemsSource;
        private bool isUpdating = false;

        // Animation configuration
        private static readonly TimeSpan EnterAnimationDuration = TimeSpan.FromMilliseconds(400);
        private static readonly TimeSpan MoveAnimationDuration = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan ExitAnimationDuration = TimeSpan.FromMilliseconds(250);
        private static readonly int AnimationFps = 120;
        private static readonly double ItemSpacing = 8.0;

        #region Dependency Properties

        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<AnimatedItemsCanvas, IEnumerable?>(
                nameof(ItemsSource),
                defaultBindingMode: BindingMode.OneWay);

        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.Register<AnimatedItemsCanvas, IDataTemplate?>(nameof(ItemTemplate));

        public IDataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<AnimatedItemsCanvas, Orientation>(
                nameof(Orientation), 
                Orientation.Vertical);

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        #endregion

        #region Animation State Tracking

        private class AnimationState
        {
            public double TargetX { get; set; }
            public double TargetY { get; set; }
            public double CurrentX { get; set; }
            public double CurrentY { get; set; }
            public bool IsAnimating { get; set; }
            public bool IsEntering { get; set; }
            public bool IsExiting { get; set; }
            public DateTime AnimationStartTime { get; set; }
            public TimeSpan AnimationDuration { get; set; }
            public IEasing Easing { get; set; } = new CubicEaseOut();
        }

        #endregion

        public AnimatedItemsCanvas()
        {
            ClipToBounds = true;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsSourceProperty)
            {
                OnItemsSourceChanged(change.OldValue as IEnumerable, change.NewValue as IEnumerable);
            }
            else if (change.Property == ItemTemplateProperty)
            {
                UpdateAllItemPresenters();
            }
            else if (change.Property == OrientationProperty)
            {
                InvalidateArrange();
            }
        }

        private void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
        {
            if (isUpdating) return;

            // Unsubscribe from old collection
            if (oldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= OnCollectionChanged;
            }

            // Subscribe to new collection
            if (newValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += OnCollectionChanged;
            }

            previousItemsSource = oldValue;
            UpdateItems();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (isUpdating) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    HandleItemsAdded(e.NewItems, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    HandleItemsRemoved(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    HandleItemsReplaced(e.OldItems, e.NewItems, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Move:
                    HandleItemsMoved(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    UpdateItems();
                    break;
            }
        }

        private void UpdateItems()
        {
            isUpdating = true;

            try
            {
                var currentItems = ItemsSource?.Cast<object>().ToList() ?? new List<object>();
                var existingItems = itemPresenters.Keys.ToHashSet();
                var newItems = currentItems.Where(item => !existingItems.Contains(item)).ToList();
                var removedItems = existingItems.Where(item => !currentItems.Contains(item)).ToList();

                // Remove items that are no longer in the collection
                foreach (var item in removedItems)
                {
                    if (itemPresenters.TryGetValue(item, out var presenter))
                    {
                        _ = AnimateRemovalAsync(presenter, () =>
                        {
                            RemoveItemPresenter(item);
                        });
                    }
                }

                // Add new items
                foreach (var item in newItems)
                {
                    AddItemPresenter(item);
                }

                // Update positions for all existing items
                UpdateItemPositions(animate: true);
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void AddItemPresenter(object item)
        {
            if (itemPresenters.ContainsKey(item)) return;

            var presenter = new ContentPresenter
            {
                Content = item,
                ContentTemplate = ItemTemplate,
                HorizontalAlignment = Orientation == Orientation.Vertical ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
                VerticalAlignment = Orientation == Orientation.Horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Top
            };

            itemPresenters[item] = presenter;
            Children.Add(presenter);

            // Initialize animation state
            var state = new AnimationState
            {
                IsEntering = true,
                AnimationStartTime = DateTime.Now,
                AnimationDuration = EnterAnimationDuration,
                Easing = new CubicEaseOut()
            };
            animationStates[presenter] = state;

            // Force measure the presenter first so we can position it properly
            presenter.Measure(Size.Infinity);
            
            // Calculate target position before setting initial state
            var targetPosition = CalculateTargetPosition(presenter);
            state.TargetX = targetPosition.X;
            state.TargetY = targetPosition.Y;

            // Set initial position for entrance animation AFTER measuring
            SetInitialEntranceState(presenter);
            
            System.Diagnostics.Debug.WriteLine($"AddItemPresenter: Starting entrance animation for item {item}");
            
            // Start entrance animation with a small delay to ensure layout is complete
            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(10); // Small delay to ensure layout
                await AnimateEntranceAsync(presenter);
            });
        }

        private void RemoveItemPresenter(object item)
        {
            if (!itemPresenters.TryGetValue(item, out var presenter)) return;

            StopAnimation(presenter);
            Children.Remove(presenter);
            itemPresenters.Remove(item);
            animationStates.Remove(presenter);
        }

        private void HandleItemsAdded(IList? newItems, int startingIndex)
        {
            if (newItems == null) return;

            foreach (var item in newItems)
            {
                AddItemPresenter(item);
            }

            UpdateItemPositions(animate: true);
        }

        private void HandleItemsRemoved(IList? oldItems)
        {
            if (oldItems == null) return;

            foreach (var item in oldItems)
            {
                if (itemPresenters.TryGetValue(item, out var presenter))
                {
                    _ = AnimateRemovalWithCompactionAsync(presenter, item);
                }
            }
        }

        private void HandleItemsReplaced(IList? oldItems, IList? newItems, int startingIndex)
        {
            if (oldItems != null)
            {
                foreach (var item in oldItems)
                {
                    RemoveItemPresenter(item);
                }
            }

            if (newItems != null)
            {
                foreach (var item in newItems)
                {
                    AddItemPresenter(item);
                }
            }

            UpdateItemPositions(animate: true);
        }

        private void HandleItemsMoved(int oldIndex, int newIndex)
        {
            UpdateItemPositions(animate: true);
        }

        private void UpdateAllItemPresenters()
        {
            foreach (var kvp in itemPresenters)
            {
                kvp.Value.ContentTemplate = ItemTemplate;
            }
        }

        private void UpdateItemPositions(bool animate = false)
        {
            if (ItemsSource == null) return;

            var items = ItemsSource.Cast<object>().ToList();
            double currentOffset = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!itemPresenters.TryGetValue(item, out var presenter)) continue;

                // Get or create animation state
                if (!animationStates.TryGetValue(presenter, out var state))
                {
                    state = new AnimationState();
                    animationStates[presenter] = state;
                }

                // Calculate target position
                double targetX = Orientation == Orientation.Horizontal ? currentOffset : 0;
                double targetY = Orientation == Orientation.Vertical ? currentOffset : 0;

                // Only skip position updates for items that are entering (size-up animation)
                // but still animate position changes for items that are just moving
                if (state.IsEntering)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateItemPositions: Skipping presenter {presenter.GetHashCode()} - entering (Opacity={presenter.Opacity}, Transform={presenter.RenderTransform})");
                    
                    // Update the target position in state for when entrance finishes
                    state.TargetX = targetX;
                    state.TargetY = targetY;
                    
                    // Calculate spacing but don't change position
                    var animatingItemSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
                    if (animatingItemSize == 0)
                    {
                        presenter.Measure(Size.Infinity);
                        animatingItemSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
                    }
                    currentOffset += animatingItemSize + ItemSpacing;
                    continue;
                }

                if (animate && !state.IsExiting)
                {
                    // Check if position actually needs to change
                    var currentX = Canvas.GetLeft(presenter);
                    var currentY = Canvas.GetTop(presenter);
                    
                    if (Math.Abs(currentX - targetX) > 1 || Math.Abs(currentY - targetY) > 1)
                    {
                        AnimateToPosition(presenter, targetX, targetY);
                    }
                }
                else if (!state.IsExiting)
                {
                    // Set position immediately
                    SetPosition(presenter, targetX, targetY);
                    state.TargetX = targetX;
                    state.TargetY = targetY;
                    state.CurrentX = targetX;
                    state.CurrentY = targetY;
                }

                // Calculate spacing for next item
                var itemSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
                if (itemSize == 0)
                {
                    presenter.Measure(Size.Infinity);
                    itemSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
                }
                currentOffset += itemSize + ItemSpacing;
            }
        }

        private void SetInitialEntranceState(ContentPresenter presenter)
        {
            if (!animationStates.TryGetValue(presenter, out var state))
                return;
                
            System.Diagnostics.Debug.WriteLine($"SetInitialEntranceState: Setting initial state for presenter");
            
            presenter.Opacity = 0;
            presenter.RenderTransform = new ScaleTransform(0.98, 0.98);
            
            // Use the target position we already calculated
            var offsetAmount = Orientation == Orientation.Vertical ? -20 : -20;
            
            if (Orientation == Orientation.Vertical)
            {
                SetPosition(presenter, state.TargetX, state.TargetY + offsetAmount);
                System.Diagnostics.Debug.WriteLine($"SetInitialEntranceState: Set initial position to ({state.TargetX}, {state.TargetY + offsetAmount})");
            }
            else
            {
                SetPosition(presenter, state.TargetX + offsetAmount, state.TargetY);
                System.Diagnostics.Debug.WriteLine($"SetInitialEntranceState: Set initial position to ({state.TargetX + offsetAmount}, {state.TargetY})");
            }
            
            // Force visual update
            presenter.InvalidateVisual();
            presenter.InvalidateArrange();
        }

        private Point CalculateTargetPosition(ContentPresenter presenter)
        {
            if (ItemsSource == null) return new Point(0, 0);

            var items = ItemsSource.Cast<object>().ToList();
            var itemIndex = -1;

            // Find the index of this presenter's item
            foreach (var kvp in itemPresenters)
            {
                if (kvp.Value == presenter)
                {
                    itemIndex = items.IndexOf(kvp.Key);
                    break;
                }
            }

            if (itemIndex == -1) return new Point(0, 0);

            // Calculate position based on index
            double offset = 0;
            for (int i = 0; i < itemIndex; i++)
            {
                var item = items[i];
                if (itemPresenters.TryGetValue(item, out var itemPresenter))
                {
                    var size = Orientation == Orientation.Vertical ? itemPresenter.DesiredSize.Height : itemPresenter.DesiredSize.Width;
                    offset += size + ItemSpacing;
                }
            }

            return Orientation == Orientation.Vertical 
                ? new Point(0, offset) 
                : new Point(offset, 0);
        }

        #region Animation Methods

        private async Task AnimateRemovalWithCompactionAsync(ContentPresenter removingPresenter, object removingItem)
        {
            if (!animationStates.TryGetValue(removingPresenter, out var removingState)) return;

            // Ensure the presenter is measured before calculating size
            if (removingPresenter.DesiredSize.Width == 0 || removingPresenter.DesiredSize.Height == 0)
            {
                removingPresenter.Measure(Size.Infinity);
            }

            // Calculate the height/width of the item being removed for compaction
            var removedItemSize = Orientation == Orientation.Vertical 
                ? Math.Max(removingPresenter.DesiredSize.Height, removingPresenter.Bounds.Height) + ItemSpacing
                : Math.Max(removingPresenter.DesiredSize.Width, removingPresenter.Bounds.Width) + ItemSpacing;

            // Get all current presenters that are positioned after the removing item
            var itemsToMove = new List<(ContentPresenter presenter, double moveAmount)>();
            var removingPosition = Orientation == Orientation.Vertical 
                ? Canvas.GetTop(removingPresenter) 
                : Canvas.GetLeft(removingPresenter);

            foreach (var kvp in itemPresenters)
            {
                if (kvp.Value == removingPresenter) continue; // Skip the item being removed
                
                var presenter = kvp.Value;
                
                // Skip if this presenter is already in another animation
                if (animationStates.TryGetValue(presenter, out var presenterState) && 
                    (presenterState.IsEntering || presenterState.IsExiting || presenterState.IsAnimating))
                {
                    System.Diagnostics.Debug.WriteLine($"Compaction: Skipping presenter {presenter.GetHashCode()} - already animating");
                    continue;
                }
                
                var presenterPosition = Orientation == Orientation.Vertical 
                    ? Canvas.GetTop(presenter) 
                    : Canvas.GetLeft(presenter);

                // If this item is positioned after the removing item, it needs to move up/left
                if (presenterPosition > removingPosition)
                {
                    var moveAmount = -removedItemSize; // Negative = move up/left
                    itemsToMove.Add((presenter, moveAmount));
                    System.Diagnostics.Debug.WriteLine($"Compaction: Queuing presenter {presenter.GetHashCode()} to move by {moveAmount}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"AnimateRemovalWithCompaction: Removing item with size {removedItemSize}, moving {itemsToMove.Count} items");

            // Start the removal animation for the item being removed
            var removalTask = AnimateRemovalAsync(removingPresenter, () =>
            {
                RemoveItemPresenter(removingItem);
            });

            // Start compaction animations for items that need to move
            var compactionTasks = new List<Task>();
            foreach (var (presenter, moveAmount) in itemsToMove)
            {
                var currentX = Canvas.GetLeft(presenter);
                var currentY = Canvas.GetTop(presenter);
                
                var targetX = Orientation == Orientation.Horizontal ? currentX + moveAmount : currentX;
                var targetY = Orientation == Orientation.Vertical ? currentY + moveAmount : currentY;

                System.Diagnostics.Debug.WriteLine($"Compaction: Moving item from ({currentX}, {currentY}) to ({targetX}, {targetY}), moveAmount={moveAmount}");
                
                var compactionTask = AnimateToPositionAsync(presenter, targetX, targetY, MoveAnimationDuration);
                compactionTasks.Add(compactionTask);
            }

            // Wait for all animations to complete
            await Task.WhenAll(new[] { removalTask }.Concat(compactionTasks));
        }

        private async Task AnimateToPositionAsync(ContentPresenter presenter, double targetX, double targetY, TimeSpan duration)
        {
            if (!animationStates.TryGetValue(presenter, out var state)) 
            {
                System.Diagnostics.Debug.WriteLine("AnimateToPositionAsync: No animation state found for presenter");
                return;
            }
            
            if (state.IsEntering || state.IsExiting) 
            {
                System.Diagnostics.Debug.WriteLine("AnimateToPositionAsync: Presenter is entering or exiting, skipping");
                return;
            }

            // Stop any existing animation on this presenter to prevent conflicts
            StopAnimation(presenter);

            var currentX = Canvas.GetLeft(presenter);
            var currentY = Canvas.GetTop(presenter);

            // Check if movement is significant enough to animate
            var deltaX = Math.Abs(targetX - currentX);
            var deltaY = Math.Abs(targetY - currentY);

            var presenterId = presenter.GetHashCode();
            System.Diagnostics.Debug.WriteLine($"AnimateToPositionAsync [{presenterId}]: Current({currentX}, {currentY}) -> Target({targetX}, {targetY}), Delta({deltaX}, {deltaY})");

            if (deltaX < 1 && deltaY < 1) 
            {
                System.Diagnostics.Debug.WriteLine($"AnimateToPositionAsync [{presenterId}]: Movement too small, skipping animation");
                return;
            }

            // Mark as animating to prevent interference
            state.IsAnimating = true;
            state.TargetX = targetX;
            state.TargetY = targetY;
            state.CurrentX = currentX;
            state.CurrentY = currentY;

            System.Diagnostics.Debug.WriteLine($"AnimateToPositionAsync [{presenterId}]: Starting interpolation animation over {duration.TotalMilliseconds}ms");

            await StartAnimation(presenter, duration, new CubicEaseOut(), (progress) =>
            {
                // Double-check we're still the active animation
                if (!state.IsAnimating) return;

                var newX = state.CurrentX + ((state.TargetX - state.CurrentX) * progress);
                var newY = state.CurrentY + ((state.TargetY - state.CurrentY) * progress);
                SetPosition(presenter, newX, newY);
                
                if (progress % 0.2 < 0.1) // Log every ~20% progress
                {
                    System.Diagnostics.Debug.WriteLine($"AnimateToPositionAsync [{presenterId}]: Progress {progress:F2}, Position({newX:F1}, {newY:F1})");
                }
            });

            state.IsAnimating = false;
            System.Diagnostics.Debug.WriteLine($"AnimateToPositionAsync [{presenterId}]: Animation completed");
        }

        private async Task AnimateEntranceAsync(ContentPresenter presenter)
        {
            if (!animationStates.TryGetValue(presenter, out var state)) 
            {
                System.Diagnostics.Debug.WriteLine("AnimateEntranceAsync: No animation state found");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"AnimateEntranceAsync: Starting entrance animation to ({state.TargetX}, {state.TargetY})");

            await StartAnimation(presenter, EnterAnimationDuration, new CubicEaseOut(), (progress) =>
            {
                // Animate opacity
                presenter.Opacity = progress;

                // Animate scale - more subtle
                var scale = 0.98 + (0.02 * progress);
                presenter.RenderTransform = new ScaleTransform(scale, scale);

                // Animate position
                var startOffset = Orientation == Orientation.Vertical ? -20 : -20;
                var currentOffset = startOffset * (1 - progress);
                
                if (Orientation == Orientation.Vertical)
                {
                    SetPosition(presenter, state.TargetX, state.TargetY + currentOffset);
                }
                else
                {
                    SetPosition(presenter, state.TargetX + currentOffset, state.TargetY);
                }

                state.CurrentX = Canvas.GetLeft(presenter);
                state.CurrentY = Canvas.GetTop(presenter);
                
                // CRITICAL: When animation completes, immediately set final state
                if (progress >= 1.0)
                {
                    System.Diagnostics.Debug.WriteLine("AnimateEntranceAsync: Animation progress reached 1.0, setting final state");
                    presenter.Opacity = 1.0;
                    presenter.RenderTransform = null;
                    SetPosition(presenter, state.TargetX, state.TargetY);
                }
                
                // Debug progress occasionally
                if (progress == 0 || progress >= 1.0 || (progress % 0.5 < 0.1))
                {
                    System.Diagnostics.Debug.WriteLine($"AnimateEntranceAsync: Progress {progress:F2}, Opacity {presenter.Opacity:F2}, Scale {scale:F2}, Transform={presenter.RenderTransform}");
                }
            });

            // CRITICAL: Ensure final state is completely reset with a small delay
            System.Diagnostics.Debug.WriteLine("AnimateEntranceAsync: Animation awaited, finalizing entrance state");
            
            // Wait a frame to ensure animation timer has stopped
            await Task.Delay(50);
            
            // Aggressively reset all animation-related properties
            presenter.Opacity = 1.0;
            presenter.RenderTransform = null;
            SetPosition(presenter, state.TargetX, state.TargetY);
            state.IsEntering = false;
            state.IsAnimating = false;
            
            // Force multiple visual updates to ensure changes stick
            presenter.InvalidateVisual();
            presenter.InvalidateArrange();
            presenter.InvalidateMeasure();
            
            // Double-check the state was actually applied
            System.Diagnostics.Debug.WriteLine($"AnimateEntranceAsync: Final state - Opacity={presenter.Opacity}, Transform={presenter.RenderTransform}, IsEntering={state.IsEntering}");
            
            // Schedule another cleanup to happen after the next layout pass
            Dispatcher.UIThread.Post(() =>
            {
                System.Diagnostics.Debug.WriteLine("AnimateEntranceAsync: Post-layout cleanup");
                presenter.Opacity = 1.0;
                presenter.RenderTransform = null;
                
                // Trigger a layout update to apply any pending position changes
                UpdateItemPositions(animate: false);
            }, DispatcherPriority.Loaded);
            
            System.Diagnostics.Debug.WriteLine("AnimateEntranceAsync: Entrance animation completed");
        }

        private async Task AnimateRemovalAsync(ContentPresenter presenter, Action onComplete)
        {
            if (!animationStates.TryGetValue(presenter, out var state)) return;

            state.IsExiting = true;

            await StartAnimation(presenter, ExitAnimationDuration, new CubicEaseIn(), (progress) =>
            {
                // Animate opacity
                presenter.Opacity = 1.0 - progress;

                // Animate scale
                var scale = 1.0 - (0.2 * progress);
                presenter.RenderTransform = new ScaleTransform(scale, scale);

                // Slight movement during exit
                var offset = 20 * progress;
                if (Orientation == Orientation.Vertical)
                {
                    SetPosition(presenter, state.CurrentX, state.CurrentY + offset);
                }
                else
                {
                    SetPosition(presenter, state.CurrentX + offset, state.CurrentY);
                }
            });

            onComplete?.Invoke();
        }

        private void AnimateToPosition(ContentPresenter presenter, double targetX, double targetY)
        {
            // Check if already animating to prevent conflicts
            if (animationStates.TryGetValue(presenter, out var state) && state.IsAnimating)
            {
                System.Diagnostics.Debug.WriteLine($"AnimateToPosition: Presenter {presenter.GetHashCode()} already animating, skipping");
                return;
            }
            
            _ = AnimateToPositionAsync(presenter, targetX, targetY, MoveAnimationDuration);
        }

        private async Task StartAnimation(ContentPresenter presenter, TimeSpan duration, IEasing easing, Action<double> updateAction)
        {
            StopAnimation(presenter);

            var frameInterval = TimeSpan.FromMilliseconds(1000.0 / AnimationFps);
            var startTime = DateTime.Now;
            var tcs = new TaskCompletionSource<bool>();

            var timer = new DispatcherTimer(frameInterval, DispatcherPriority.Render, (sender, args) =>
            {
                var elapsed = DateTime.Now - startTime;
                var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                var easedProgress = easing.Ease(progress);

                updateAction(easedProgress);

                if (progress >= 1.0)
                {
                    var animationTimer = sender as DispatcherTimer;
                    animationTimer?.Stop();
                    activeAnimations.Remove(presenter);
                    tcs.SetResult(true);
                }
            });

            activeAnimations[presenter] = timer;
            timer.Start();

            await tcs.Task;
        }

        private void StopAnimation(ContentPresenter presenter)
        {
            if (activeAnimations.TryGetValue(presenter, out var timer))
            {
                timer.Stop();
                activeAnimations.Remove(presenter);
            }
        }

        #endregion

        #region Positioning Helpers

        private static void SetPosition(ContentPresenter presenter, double x, double y)
        {
            Canvas.SetLeft(presenter, x);
            Canvas.SetTop(presenter, y);
            
            // Force visual update
            presenter.InvalidateVisual();
            presenter.InvalidateArrange();
        }

        public ContentPresenter? GetPresenterForItem(object item)
        {
            return itemPresenters.TryGetValue(item, out var presenter) ? presenter : null;
        }

        public async Task AnimateToIndexAsync(object item, int targetIndex, TimeSpan? duration = null)
        {
            if (!itemPresenters.TryGetValue(item, out var presenter))
            {
                System.Diagnostics.Debug.WriteLine("AnimateToIndexAsync: Item not found in presenters");
                return;
            }

            if (!animationStates.TryGetValue(presenter, out var state))
            {
                System.Diagnostics.Debug.WriteLine("AnimateToIndexAsync: No animation state found, creating one");
                state = new AnimationState();
                animationStates[presenter] = state;
            }

            // Calculate target position based on index
            var targetPosition = CalculatePositionForIndex(targetIndex);
            if (targetPosition == null)
            {
                System.Diagnostics.Debug.WriteLine($"AnimateToIndexAsync: Could not calculate position for index {targetIndex}");
                return;
            }

            var animationDuration = duration ?? MoveAnimationDuration;
            var currentX = Canvas.GetLeft(presenter);
            var currentY = Canvas.GetTop(presenter);

            System.Diagnostics.Debug.WriteLine($"AnimateToIndexAsync: Moving item to index {targetIndex}, position ({targetPosition.Value.X}, {targetPosition.Value.Y})");

            state.CurrentX = currentX;
            state.CurrentY = currentY;
            state.TargetX = targetPosition.Value.X;
            state.TargetY = targetPosition.Value.Y;
            state.IsAnimating = true;

            await StartAnimation(presenter, animationDuration, new CubicEaseOut(), (progress) =>
            {
                var newX = state.CurrentX + ((state.TargetX - state.CurrentX) * progress);
                var newY = state.CurrentY + ((state.TargetY - state.CurrentY) * progress);
                SetPosition(presenter, newX, newY);
            });

            state.IsAnimating = false;
            System.Diagnostics.Debug.WriteLine($"AnimateToIndexAsync: Animation to index {targetIndex} completed");
        }

        private Point? CalculatePositionForIndex(int index)
        {
            if (ItemsSource == null) return null;

            var items = ItemsSource.Cast<object>().ToList();
            if (index < 0 || index >= items.Count) return null;

            double offset = 0;
            for (int i = 0; i < index; i++)
            {
                var item = items[i];
                if (itemPresenters.TryGetValue(item, out var itemPresenter))
                {
                    var size = Orientation == Orientation.Vertical ? itemPresenter.DesiredSize.Height : itemPresenter.DesiredSize.Width;
                    if (size == 0)
                    {
                        itemPresenter.Measure(Size.Infinity);
                        size = Orientation == Orientation.Vertical ? itemPresenter.DesiredSize.Height : itemPresenter.DesiredSize.Width;
                    }
                    offset += size + ItemSpacing;
                }
            }

            return Orientation == Orientation.Vertical 
                ? new Point(0, offset) 
                : new Point(offset, 0);
        }

        public int GetItemIndex(object item)
        {
            if (ItemsSource == null) return -1;
            
            var items = ItemsSource.Cast<object>().ToList();
            return items.IndexOf(item);
        }

        public async Task AnimateMultipleToIndicesAsync(Dictionary<object, int> itemIndexPairs, TimeSpan? duration = null)
        {
            var animationTasks = new List<Task>();

            foreach (var kvp in itemIndexPairs)
            {
                var item = kvp.Key;
                var targetIndex = kvp.Value;
                animationTasks.Add(AnimateToIndexAsync(item, targetIndex, duration));
            }

            await Task.WhenAll(animationTasks);
            System.Diagnostics.Debug.WriteLine($"AnimateMultipleToIndicesAsync: All {animationTasks.Count} animations completed");
        }

        public async Task AnimateItemByPixelsAsync(object item, double pixelAmount, TimeSpan? duration = null)
        {
            System.Diagnostics.Debug.WriteLine($"AnimateItemByPixelsAsync: Starting animation for item, pixelAmount={pixelAmount}");
            
            if (!itemPresenters.TryGetValue(item, out var presenter))
            {
                System.Diagnostics.Debug.WriteLine("AnimateItemByPixelsAsync: Item not found in presenters");
                return;
            }
            
            if (!animationStates.TryGetValue(presenter, out var state))
            {
                System.Diagnostics.Debug.WriteLine("AnimateItemByPixelsAsync: No animation state found, creating one");
                state = new AnimationState();
                animationStates[presenter] = state;
            }

            var animationDuration = duration ?? MoveAnimationDuration;
            var currentX = Canvas.GetLeft(presenter);
            var currentY = Canvas.GetTop(presenter);

            System.Diagnostics.Debug.WriteLine($"AnimateItemByPixelsAsync: Current position ({currentX}, {currentY}), Orientation={Orientation}");

            var targetX = Orientation == Orientation.Horizontal ? currentX + pixelAmount : currentX;
            var targetY = Orientation == Orientation.Vertical ? currentY + pixelAmount : currentY;

            System.Diagnostics.Debug.WriteLine($"AnimateItemByPixelsAsync: Target position ({targetX}, {targetY})");

            state.CurrentX = currentX;
            state.CurrentY = currentY;
            state.TargetX = targetX;
            state.TargetY = targetY;
            state.IsAnimating = true; // Mark as animating to prevent interference

            await StartAnimation(presenter, animationDuration, new CubicEaseOut(), (progress) =>
            {
                var newX = state.CurrentX + ((state.TargetX - state.CurrentX) * progress);
                var newY = state.CurrentY + ((state.TargetY - state.CurrentY) * progress);
                
                System.Diagnostics.Debug.WriteLine($"AnimateItemByPixelsAsync: Calculated newX={newX:F1}, newY={newY:F1} (progress={progress:F2})");
                
                SetPosition(presenter, newX, newY);
                
                // Verify the position was actually set
                var actualX = Canvas.GetLeft(presenter);
                var actualY = Canvas.GetTop(presenter);
                System.Diagnostics.Debug.WriteLine($"AnimateItemByPixelsAsync: After SetPosition - actualX={actualX:F1}, actualY={actualY:F1}");
                
                if (progress % 0.25 < 0.1) // Log every ~25% progress
                {
                    System.Diagnostics.Debug.WriteLine($"AnimateItemByPixelsAsync: Progress {progress:F2}, Position({newX:F1}, {newY:F1})");
                }
            });
            
            state.IsAnimating = false; // Clear animation flag
            System.Diagnostics.Debug.WriteLine("AnimateItemByPixelsAsync: Animation completed");
        }

        /// <summary>
        /// Forces reset of animation state for an item that may be stuck
        /// </summary>
        public void ForceResetItemState(object item)
        {
            if (itemPresenters.TryGetValue(item, out var presenter))
            {
                StopAnimation(presenter);
                
                if (animationStates.TryGetValue(presenter, out var state))
                {
                    state.IsAnimating = false;
                    state.IsEntering = false;
                    state.IsExiting = false;
                }
                
                // Reset visual state
                presenter.Opacity = 1.0;
                presenter.RenderTransform = null;
                presenter.InvalidateVisual();
                presenter.InvalidateArrange();
                
                System.Diagnostics.Debug.WriteLine($"ForceResetItemState: Reset state for item {item}");
            }
        }

        /// <summary>
        /// Resets all animation states - useful for debugging stuck animations
        /// </summary>
        public void ForceResetAllStates()
        {
            foreach (var kvp in itemPresenters)
            {
                ForceResetItemState(kvp.Key);
            }
            
            System.Diagnostics.Debug.WriteLine("ForceResetAllStates: Reset all animation states");
        }

        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            double totalWidth = 0;
            double totalHeight = 0;
            double maxItemWidth = 0;
            double maxItemHeight = 0;

            foreach (var presenter in Children.OfType<ContentPresenter>())
            {
                // For vertical orientation, constrain width to available width to enable stretching
                var measureSize = Orientation == Orientation.Vertical 
                    ? new Size(availableSize.Width, availableSize.Height)
                    : availableSize;
                    
                presenter.Measure(measureSize);
                var itemSize = presenter.DesiredSize;

                maxItemWidth = Math.Max(maxItemWidth, itemSize.Width);
                maxItemHeight = Math.Max(maxItemHeight, itemSize.Height);

                if (Orientation == Orientation.Vertical)
                {
                    totalHeight += itemSize.Height + ItemSpacing;
                }
                else
                {
                    totalWidth += itemSize.Width + ItemSpacing;
                }
            }

            // Remove the last spacing
            if (Children.Count > 0)
            {
                if (Orientation == Orientation.Vertical)
                    totalHeight -= ItemSpacing;
                else
                    totalWidth -= ItemSpacing;
            }

            var finalWidth = Orientation == Orientation.Vertical ? maxItemWidth : totalWidth;
            var finalHeight = Orientation == Orientation.Horizontal ? maxItemHeight : totalHeight;

            return new Size(Math.Min(finalWidth, availableSize.Width), Math.Min(finalHeight, availableSize.Height));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // Arrange each presenter with full width for vertical orientation
            foreach (var presenter in Children.OfType<ContentPresenter>())
            {
                var x = Canvas.GetLeft(presenter);
                var y = Canvas.GetTop(presenter);
                
                // For vertical orientation, use full width; for horizontal, use desired size
                var arrangeSize = Orientation == Orientation.Vertical
                    ? new Size(finalSize.Width, presenter.DesiredSize.Height)
                    : presenter.DesiredSize;
                    
                presenter.Arrange(new Rect(x, y, arrangeSize.Width, arrangeSize.Height));
            }
            
            // Update positions after measurement
            UpdateItemPositions(animate: false);
            return finalSize;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            // Stop all animations
            foreach (var timer in activeAnimations.Values)
            {
                timer.Stop();
            }
            activeAnimations.Clear();

            // Unsubscribe from collection changes
            if (ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= OnCollectionChanged;
            }
        }
    }
}