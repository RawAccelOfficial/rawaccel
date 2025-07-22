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
        // Animation timing constants
        private static readonly TimeSpan EnterAnimationDuration = TimeSpan.FromMilliseconds(400);
        private static readonly TimeSpan MoveAnimationDuration = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan ExitAnimationDuration = TimeSpan.FromMilliseconds(250);
        private static readonly int AnimationFps = 120;
        
        // Layout constants
        private static readonly double ItemSpacing = 8.0;
        
        // Animation constants
        private static readonly int IntroAnimationDelay = 10;
        private static readonly double InitialOpacity = 0.0;
        private static readonly double InitialScale = 0.85;
        private static readonly double TargetScale = 1.0;
        private static readonly double ExitScaleReduction = 0.2;
        private static readonly double PositionThreshold = 1.0;
        private static readonly double FrameIntervalMs = 1000.0;

        // Animation state tracking
        public enum AnimationState
        {
            Idle,
            IntroAnimating,
            Moving,
            AccordionMoving,
            Exiting,
            Blocked
        }

        private readonly Dictionary<object, ContentPresenter> itemPresenters = new();
        private readonly Dictionary<ContentPresenter, AnimationContext> animationStates = new();
        private readonly List<object> visualItems = new(); // Internal representation separate from ItemsSource
        private readonly Queue<CollectionChange> pendingChanges = new();
        
        private ContentPresenter? addButtonPresenter;
        private IEnumerable? previousItemsSource;
        private bool isProcessingChanges = false;
        private readonly object AddButtonItemKey = new object();

        private struct CollectionChange
        {
            public CollectionChangeType Type;
            public object? Item;
            public int Index;
            public List<object>? Items;
        }

        private enum CollectionChangeType
        {
            Add,
            Remove,
            Replace,
            Move,
            Reset
        }

        private class AnimationContext
        {
            public AnimationState State { get; set; } = AnimationState.Idle;
            public DispatcherTimer? Timer { get; set; }
            public DateTime StartTime { get; set; }
            public bool CanBeInterrupted => State == AnimationState.Idle || State == AnimationState.Moving;
            public bool IsIntroAnimating => State == AnimationState.IntroAnimating;
        }

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

        public static readonly StyledProperty<IDataTemplate?> AddButtonTemplateProperty =
            AvaloniaProperty.Register<AnimatedItemsCanvas, IDataTemplate?>(nameof(AddButtonTemplate));

        public IDataTemplate? AddButtonTemplate
        {
            get => GetValue(AddButtonTemplateProperty);
            set => SetValue(AddButtonTemplateProperty, value);
        }

        public static readonly StyledProperty<bool> ShowAddButtonProperty =
            AvaloniaProperty.Register<AnimatedItemsCanvas, bool>(nameof(ShowAddButton), true);

        public bool ShowAddButton
        {
            get => GetValue(ShowAddButtonProperty);
            set => SetValue(ShowAddButtonProperty, value);
        }

        public static readonly StyledProperty<object?> AddButtonDataContextProperty =
            AvaloniaProperty.Register<AnimatedItemsCanvas, object?>(nameof(AddButtonDataContext));

        public object? AddButtonDataContext
        {
            get => GetValue(AddButtonDataContextProperty);
            set => SetValue(AddButtonDataContextProperty, value);
        }

        #endregion

        #region Animation State Management
        
        private AnimationContext GetOrCreateAnimationContext(ContentPresenter presenter)
        {
            if (!animationStates.TryGetValue(presenter, out var context))
            {
                context = new AnimationContext();
                animationStates[presenter] = context;
            }
            return context;
        }

        private void RemoveAnimationContext(ContentPresenter presenter)
        {
            if (animationStates.TryGetValue(presenter, out var context))
            {
                context.Timer?.Stop();
                animationStates.Remove(presenter);
            }
        }

        private bool CanStartAnimation(ContentPresenter presenter, AnimationState requestedState)
        {
            var context = GetOrCreateAnimationContext(presenter);
            
            return requestedState switch
            {
                AnimationState.IntroAnimating => context.State == AnimationState.Idle,
                AnimationState.Moving => context.CanBeInterrupted,
                AnimationState.AccordionMoving => context.CanBeInterrupted,
                AnimationState.Exiting => true,
                _ => context.State == AnimationState.Idle
            };
        }

        private void SetAnimationState(ContentPresenter presenter, AnimationState state)
        {
            var context = GetOrCreateAnimationContext(presenter);
            context.State = state;
            context.StartTime = DateTime.Now;
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
            else if (change.Property == ShowAddButtonProperty || 
                     change.Property == AddButtonTemplateProperty ||
                     change.Property == AddButtonDataContextProperty)
            {
                UpdateAddButton();
            }
        }

        private void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
        {
            if (isProcessingChanges) return;

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
            if (isProcessingChanges) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            pendingChanges.Enqueue(new CollectionChange
                            {
                                Type = CollectionChangeType.Add,
                                Item = item,
                                Index = e.NewStartingIndex
                            });
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            pendingChanges.Enqueue(new CollectionChange
                            {
                                Type = CollectionChangeType.Remove,
                                Item = item,
                                Index = e.OldStartingIndex
                            });
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    pendingChanges.Enqueue(new CollectionChange
                    {
                        Type = CollectionChangeType.Replace,
                        Items = e.OldItems?.Cast<object>().ToList(),
                        Index = e.OldStartingIndex
                    });
                    pendingChanges.Enqueue(new CollectionChange
                    {
                        Type = CollectionChangeType.Add,
                        Items = e.NewItems?.Cast<object>().ToList(),
                        Index = e.NewStartingIndex
                    });
                    break;
                case NotifyCollectionChangedAction.Move:
                    pendingChanges.Enqueue(new CollectionChange
                    {
                        Type = CollectionChangeType.Move,
                        Index = e.OldStartingIndex
                    });
                    break;
                case NotifyCollectionChangedAction.Reset:
                    pendingChanges.Clear();
                    pendingChanges.Enqueue(new CollectionChange { Type = CollectionChangeType.Reset });
                    break;
            }

            _ = ProcessPendingChangesAsync();
        }

        private async Task ProcessPendingChangesAsync()
        {
            if (isProcessingChanges || pendingChanges.Count == 0) return;

            isProcessingChanges = true;

            try
            {
                while (pendingChanges.Count > 0)
                {
                    var change = pendingChanges.Dequeue();
                    await ProcessSingleChangeAsync(change);
                }
            }
            finally
            {
                isProcessingChanges = false;
            }
        }

        private async Task ProcessSingleChangeAsync(CollectionChange change)
        {
            switch (change.Type)
            {
                case CollectionChangeType.Add:
                    if (change.Item != null)
                        await HandleItemAddedAsync(change.Item, change.Index);
                    else if (change.Items != null)
                    {
                        foreach (var item in change.Items)
                            await HandleItemAddedAsync(item, change.Index);
                    }
                    break;

                case CollectionChangeType.Remove:
                    if (change.Item != null)
                        await HandleItemRemovedAsync(change.Item);
                    else if (change.Items != null)
                    {
                        foreach (var item in change.Items)
                            await HandleItemRemovedAsync(item);
                    }
                    break;

                case CollectionChangeType.Move:
                    await HandleItemMovedAsync(change.Index);
                    break;

                case CollectionChangeType.Reset:
                    await HandleResetAsync();
                    break;
            }
        }

        private void UpdateItems()
        {
            // Initialize visual items from ItemsSource on first load
            if (visualItems.Count == 0 && ItemsSource != null)
            {
                var currentItems = ItemsSource.Cast<object>().ToList();
                visualItems.Clear();
                visualItems.AddRange(currentItems);

                // Create presenters for all items
                foreach (var item in currentItems)
                {
                    if (!itemPresenters.ContainsKey(item))
                        AddItemPresenter(item);
                }
            }

            // Update add button
            UpdateAddButton();

            // Update positions for all existing items
            UpdateItemPositions(animate: false);
        }

        private void UpdateAddButton()
        {
            if (ShowAddButton && AddButtonTemplate != null)
            {
                if (addButtonPresenter == null)
                {
                    addButtonPresenter = new ContentPresenter
                    {
                        Content = AddButtonDataContext,
                        ContentTemplate = AddButtonTemplate,
                        HorizontalAlignment = Orientation == Orientation.Vertical ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
                        VerticalAlignment = Orientation == Orientation.Horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Top
                    };

                    itemPresenters[AddButtonItemKey] = addButtonPresenter;
                    Children.Add(addButtonPresenter);
                }
                else
                {
                    addButtonPresenter.Content = AddButtonDataContext;
                    addButtonPresenter.ContentTemplate = AddButtonTemplate;
                }
            }
            else if (addButtonPresenter != null)
            {
                Children.Remove(addButtonPresenter);
                itemPresenters.Remove(AddButtonItemKey);
                RemoveAnimationContext(addButtonPresenter);
                addButtonPresenter = null;
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

            // Start intro animation immediately - just opacity and scale
            StartIntroAnimation(presenter);
        }

        private void RemoveItemPresenter(object item)
        {
            if (!itemPresenters.TryGetValue(item, out var presenter)) return;

            RemoveAnimationContext(presenter);
            Children.Remove(presenter);
            itemPresenters.Remove(item);
        }

        private async Task HandleItemAddedAsync(object item, int index)
        {
            // Add to visual items list
            visualItems.Add(item);

            // Create presenter and start intro animation
            AddItemPresenter(item);

            // Update positions of all items with animation
            UpdateItemPositions(animate: true);
        }

        private async Task HandleItemRemovedAsync(object item)
        {
            if (!itemPresenters.TryGetValue(item, out var presenter)) return;

            // Get the index before removal for accordion animation
            var removedIndex = visualItems.IndexOf(item);
            
            // Start removal animation
            await AnimateRemovalAsync(presenter, () =>
            {
                // Remove from visual items and presenters after animation completes
                RemoveItemPresenter(item);
                visualItems.Remove(item);
            });

            // Trigger accordion animation for remaining items
            if (removedIndex >= 0)
                await TriggerAccordionAnimationAsync(removedIndex);
        }

        private async Task HandleItemMovedAsync(int index)
        {
            // For moves, just update positions with animation
            UpdateItemPositions(animate: true);
        }

        private async Task HandleResetAsync()
        {
            // Clear all current items
            var currentPresenters = itemPresenters.Keys.Where(k => k != AddButtonItemKey).ToList();
            
            foreach (var item in currentPresenters)
            {
                RemoveItemPresenter(item);
            }
            
            visualItems.Clear();

            // Add new items from ItemsSource
            if (ItemsSource != null)
            {
                var newItems = ItemsSource.Cast<object>().ToList();
                visualItems.AddRange(newItems);
                
                foreach (var item in newItems)
                {
                    AddItemPresenter(item);
                }
            }

            UpdateItemPositions(animate: false);
        }

        private void UpdateAllItemPresenters()
        {
            foreach (var kvp in itemPresenters)
            {
                kvp.Value.ContentTemplate = ItemTemplate;
            }
        }

        private async Task TriggerAccordionAnimationAsync(int removedIndex)
        {
            var accordionTasks = new List<Task>();
            
            // Start accordion animation for items below the removed item
            for (int i = removedIndex; i < visualItems.Count; i++)
            {
                var item = visualItems[i];
                if (itemPresenters.TryGetValue(item, out var presenter))
                {
                    if (CanStartAnimation(presenter, AnimationState.AccordionMoving))
                    {
                        SetAnimationState(presenter, AnimationState.AccordionMoving);
                        accordionTasks.Add(AccordionAnimateToPositionAsync(presenter, i, TimeSpan.FromMilliseconds(150)));
                    }
                }
            }

            // Also animate the add button if it exists
            if (addButtonPresenter != null && CanStartAnimation(addButtonPresenter, AnimationState.AccordionMoving))
            {
                SetAnimationState(addButtonPresenter, AnimationState.AccordionMoving);
                accordionTasks.Add(AccordionAnimateAddButtonAsync());
            }

            // Wait for all accordion animations to complete
            if (accordionTasks.Count > 0)
                await Task.WhenAll(accordionTasks);
        }

        private async Task AccordionAnimateToPositionAsync(ContentPresenter presenter, int targetIndex, TimeSpan duration)
        {
            var targetPosition = CalculatePositionForIndex(targetIndex);
            if (!targetPosition.HasValue) return;

            var currentX = Canvas.GetLeft(presenter);
            var currentY = Canvas.GetTop(presenter);

            await StartAnimation(presenter, duration, new CubicEaseOut(), (progress) =>
            {
                var newX = currentX + ((targetPosition.Value.X - currentX) * progress);
                var newY = currentY + ((targetPosition.Value.Y - currentY) * progress);
                SetPosition(presenter, newX, newY);
            });

            SetAnimationState(presenter, AnimationState.Idle);
        }

        private async Task AccordionAnimateAddButtonAsync()
        {
            if (addButtonPresenter == null) return;

            var targetPosition = CalculateAddButtonPosition();
            if (!targetPosition.HasValue) return;

            var currentX = Canvas.GetLeft(addButtonPresenter);
            var currentY = Canvas.GetTop(addButtonPresenter);

            await StartAnimation(addButtonPresenter, TimeSpan.FromMilliseconds(150), new CubicEaseOut(), (progress) =>
            {
                var newX = currentX + ((targetPosition.Value.X - currentX) * progress);
                var newY = currentY + ((targetPosition.Value.Y - currentY) * progress);
                SetPosition(addButtonPresenter, newX, newY);
            });

            SetAnimationState(addButtonPresenter, AnimationState.Idle);
        }

        private void UpdateItemPositions(bool animate = false)
        {
            double currentOffset = 0;

            for (int i = 0; i < visualItems.Count; i++)
            {
                var item = visualItems[i];
                if (!itemPresenters.TryGetValue(item, out var presenter)) continue;

                // Calculate target position
                double targetX = Orientation == Orientation.Horizontal ? currentOffset : 0;
                double targetY = Orientation == Orientation.Vertical ? currentOffset : 0;

                if (animate)
                {
                    var currentX = Canvas.GetLeft(presenter);
                    var currentY = Canvas.GetTop(presenter);
                    
                    if (Math.Abs(currentX - targetX) > PositionThreshold || Math.Abs(currentY - targetY) > PositionThreshold)
                    {
                        if (CanStartAnimation(presenter, AnimationState.Moving))
                        {
                            AnimateToPosition(presenter, targetX, targetY);
                        }
                        else
                        {
                            SetPosition(presenter, targetX, targetY);
                        }
                    }
                }
                else
                {
                    SetPosition(presenter, targetX, targetY);
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

            // Position add button at the end
            if (addButtonPresenter != null)
            {
                var addButtonPosition = CalculateAddButtonPosition();
                if (addButtonPosition.HasValue)
                {
                    if (animate)
                    {
                        var currentX = Canvas.GetLeft(addButtonPresenter);
                        var currentY = Canvas.GetTop(addButtonPresenter);
                        
                        if (Math.Abs(currentX - addButtonPosition.Value.X) > PositionThreshold || 
                            Math.Abs(currentY - addButtonPosition.Value.Y) > PositionThreshold)
                        {
                            if (CanStartAnimation(addButtonPresenter, AnimationState.Moving))
                            {
                                AnimateToPosition(addButtonPresenter, addButtonPosition.Value.X, addButtonPosition.Value.Y);
                            }
                            else
                            {
                                SetPosition(addButtonPresenter, addButtonPosition.Value.X, addButtonPosition.Value.Y);
                            }
                        }
                    }
                    else
                    {
                        SetPosition(addButtonPresenter, addButtonPosition.Value.X, addButtonPosition.Value.Y);
                    }
                }
            }
        }

        private Point? CalculateAddButtonPosition()
        {
            double totalOffset = 0;

            foreach (var item in visualItems)
            {
                if (itemPresenters.TryGetValue(item, out var presenter))
                {
                    var itemSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
                    if (itemSize == 0)
                    {
                        presenter.Measure(Size.Infinity);
                        itemSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
                    }
                    totalOffset += itemSize + ItemSpacing;
                }
            }

            return Orientation == Orientation.Vertical 
                ? new Point(0, totalOffset) 
                : new Point(totalOffset, 0);
        }

        private void StartIntroAnimation(ContentPresenter presenter)
        {
            if (!CanStartAnimation(presenter, AnimationState.IntroAnimating))
                return;

            // Calculate the target position first
            var targetPosition = GetTargetPositionForPresenter(presenter);
            
            // Set initial state - opacity, scale, and position (slide from above)
            presenter.Opacity = InitialOpacity;
            presenter.RenderTransform = new ScaleTransform(InitialScale, InitialScale);
            
            // Position the item above its target (underneath the previous element)
            if (targetPosition.HasValue)
            {
                var startY = targetPosition.Value.Y - 30; // Start 30 pixels above target
                SetPosition(presenter, targetPosition.Value.X, startY);
            }
            
            SetAnimationState(presenter, AnimationState.IntroAnimating);
            
            // Start intro animation with movement
            _ = Task.Run(async () =>
            {
                await Task.Delay(IntroAnimationDelay);
                await Dispatcher.UIThread.InvokeAsync(() => AnimateIntroAsync(presenter, targetPosition));
            });
        }

        private Point? GetTargetPositionForPresenter(ContentPresenter presenter)
        {
            // Find the item associated with this presenter
            var targetItem = itemPresenters.FirstOrDefault(kvp => kvp.Value == presenter).Key;
            if (targetItem == null) return null;

            var index = visualItems.IndexOf(targetItem);
            
            return CalculatePositionForIndex(index);
        }


        #region Simplified Animation Methods

        private async Task AnimateIntroAsync(ContentPresenter presenter, Point? targetPosition)
        {
            if (!targetPosition.HasValue) return;

            var startX = Canvas.GetLeft(presenter);
            var startY = Canvas.GetTop(presenter);
            var targetX = targetPosition.Value.X;
            var targetY = targetPosition.Value.Y;

            await StartAnimation(presenter, EnterAnimationDuration, new QuadraticEaseOut(), (progress) =>
            {
                // Smooth opacity animation
                presenter.Opacity = Math.Min(1.0, progress * 1.2); // Opacity reaches full before animation completes
                
                // Smooth scale animation with gentle bounce-like easing
                var scaleProgress = progress;
                var scale = InitialScale + ((TargetScale - InitialScale) * scaleProgress);
                presenter.RenderTransform = progress >= 1.0 ? null : new ScaleTransform(scale, scale);

                // Animate position - slide down to target with easing
                var currentX = startX + ((targetX - startX) * progress);
                var currentY = startY + ((targetY - startY) * progress);
                SetPosition(presenter, currentX, currentY);
            });

            // Ensure final state is set
            presenter.Opacity = 1.0;
            presenter.RenderTransform = null;
            SetPosition(presenter, targetX, targetY);
            SetAnimationState(presenter, AnimationState.Idle);
        }

        private void AnimateToPosition(ContentPresenter presenter, double targetX, double targetY)
        {
            _ = AnimateToPositionAsync(presenter, targetX, targetY, MoveAnimationDuration);
        }

        private async Task AnimateToPositionAsync(ContentPresenter presenter, double targetX, double targetY, TimeSpan duration)
        {
            if (!CanStartAnimation(presenter, AnimationState.Moving))
                return;

            SetAnimationState(presenter, AnimationState.Moving);

            var currentX = Canvas.GetLeft(presenter);
            var currentY = Canvas.GetTop(presenter);

            await StartAnimation(presenter, duration, new CubicEaseOut(), (progress) =>
            {
                var newX = currentX + ((targetX - currentX) * progress);
                var newY = currentY + ((targetY - currentY) * progress);
                SetPosition(presenter, newX, newY);
            });

            SetAnimationState(presenter, AnimationState.Idle);
        }

        private async Task AnimateRemovalAsync(ContentPresenter presenter, Action onComplete)
        {
            SetAnimationState(presenter, AnimationState.Exiting);

            // Get the original size before animating
            var originalSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
            if (originalSize == 0)
            {
                presenter.Measure(Size.Infinity);
                originalSize = Orientation == Orientation.Vertical ? presenter.DesiredSize.Height : presenter.DesiredSize.Width;
            }

            // Animate both opacity/scale and size collapse
            await StartAnimation(presenter, ExitAnimationDuration, new CubicEaseIn(), (progress) =>
            {
                // Fade out and scale down
                presenter.Opacity = 1.0 - progress;
                var scale = 1.0 - (ExitScaleReduction * progress);
                presenter.RenderTransform = new ScaleTransform(scale, scale);
                
                // Collapse size smoothly
                var collapsedSize = originalSize * (1.0 - progress);
                if (Orientation == Orientation.Vertical)
                {
                    presenter.Height = Math.Max(0, collapsedSize);
                }
                else
                {
                    presenter.Width = Math.Max(0, collapsedSize);
                }
            });

            // Reset size properties and complete
            presenter.Height = double.NaN;
            presenter.Width = double.NaN;
            onComplete?.Invoke();
        }


        private async Task StartAnimation(ContentPresenter presenter, TimeSpan duration, IEasing easing, Action<double> updateAction)
        {
            var context = GetOrCreateAnimationContext(presenter);
            context.Timer?.Stop();

            var frameInterval = TimeSpan.FromMilliseconds(FrameIntervalMs / AnimationFps);
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
                    context.Timer = null;
                    tcs.SetResult(true);
                }
            });

            context.Timer = timer;
            timer.Start();

            await tcs.Task;
        }

        private void StopAnimation(ContentPresenter presenter)
        {
            if (animationStates.TryGetValue(presenter, out var context) && context.Timer != null)
            {
                context.Timer.Stop();
                context.Timer = null;
                context.State = AnimationState.Idle;
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

        public async Task AnimateItemByPixelsAsync(object item, double pixelAmount, TimeSpan? duration = null)
        {
            if (!itemPresenters.TryGetValue(item, out var presenter)) return;

            var currentX = Canvas.GetLeft(presenter);
            var currentY = Canvas.GetTop(presenter);
            var targetX = Orientation == Orientation.Horizontal ? currentX + pixelAmount : currentX;
            var targetY = Orientation == Orientation.Vertical ? currentY + pixelAmount : currentY;

            await AnimateToPositionAsync(presenter, targetX, targetY, duration ?? MoveAnimationDuration);
        }

        public int GetItemIndex(object item)
        {
            if (item == AddButtonItemKey) return -1; // Add button doesn't have an index
            return visualItems.IndexOf(item);
        }

        public async Task AnimateToIndexAsync(object item, int targetIndex, TimeSpan? duration = null)
        {
            if (!itemPresenters.TryGetValue(item, out var presenter)) return;

            var targetPosition = CalculatePositionForIndex(targetIndex);
            if (targetPosition == null) return;

            await AnimateToPositionAsync(presenter, targetPosition.Value.X, targetPosition.Value.Y, duration ?? MoveAnimationDuration);
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
        }

        private Point? CalculatePositionForIndex(int index)
        {
            if (index < 0 || index >= visualItems.Count) return null;

            double offset = 0;
            for (int i = 0; i < index; i++)
            {
                var item = visualItems[i];
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



        /// <summary>
        /// Forces reset of animation state for an item that may be stuck
        /// </summary>
        public void ForceResetItemState(object item)
        {
            if (itemPresenters.TryGetValue(item, out var presenter))
            {
                StopAnimation(presenter);
                SetAnimationState(presenter, AnimationState.Idle);
                
                // Reset visual state
                presenter.Opacity = 1.0;
                presenter.RenderTransform = null;
                presenter.InvalidateVisual();
                presenter.InvalidateArrange();
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
            // First update positions to ensure all items have valid coordinates
            UpdateItemPositions(animate: false);
            
            // Arrange each presenter with full width for vertical orientation
            foreach (var presenter in Children.OfType<ContentPresenter>())
            {
                var x = Canvas.GetLeft(presenter);
                var y = Canvas.GetTop(presenter);
                
                // Handle NaN values by defaulting to 0
                if (double.IsNaN(x)) x = 0;
                if (double.IsNaN(y)) y = 0;
                
                // For vertical orientation, use full width; for horizontal, use desired size
                var arrangeSize = Orientation == Orientation.Vertical
                    ? new Size(finalSize.Width, presenter.DesiredSize.Height)
                    : presenter.DesiredSize;
                    
                presenter.Arrange(new Rect(x, y, arrangeSize.Width, arrangeSize.Height));
            }
            
            return finalSize;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            // Stop all animations
            foreach (var context in animationStates.Values)
            {
                context.Timer?.Stop();
            }
            animationStates.Clear();

            // Unsubscribe from collection changes
            if (ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= OnCollectionChanged;
            }
        }
    }
}