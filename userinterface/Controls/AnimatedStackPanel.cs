using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace userinterface.Controls
{
    /// <summary>
    /// A StackPanel that animates items when they are added, removed, or repositioned
    /// </summary>
    public class AnimatedStackPanel : StackPanel
    {
        private readonly Dictionary<Control, Rect> previousBounds = new();
        private readonly Dictionary<Control, bool> newItems = new();
        private readonly Dictionary<Control, bool> removingItems = new();
        private bool isAnimating = false;
        private bool isArranging = false;
        private int lastChildCount = 0;

        public AnimatedStackPanel()
        {
            // Set up default transitions on the panel itself
            Transitions = new Transitions();
            Transitions.Add(new TransformOperationsTransition 
            { 
                Property = RenderTransformProperty, 
                Duration = TimeSpan.FromMilliseconds(300) 
            });
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (isArranging) return base.ArrangeOverride(finalSize);
            isArranging = true;
            
            try
            {
                // Detect collection changes
                bool itemsAdded = Children.Count > lastChildCount;
                bool itemsRemoved = Children.Count < lastChildCount;
                
                // First arrange to get actual bounds
                var result = base.ArrangeOverride(finalSize);
                
                // Skip animation logic during initial load
                if (previousBounds.Count == 0)
                {
                    UpdateBoundsTracking();
                    lastChildCount = Children.Count;
                    return result;
                }
                
                // Get current bounds after arrange
                var currentBounds = new Dictionary<Control, Rect>();
                foreach (Control child in Children)
                {
                    currentBounds[child] = child.Bounds;
                }
                
                // Handle new items with stagger delays
                if (itemsAdded)
                {
                    var newItemsList = new List<Control>();
                    foreach (Control child in Children)
                    {
                        if (!previousBounds.ContainsKey(child) && !newItems.ContainsKey(child))
                        {
                            newItems[child] = true;
                            newItemsList.Add(child);
                        }
                    }
                    
                    // Animate with stagger delays
                    for (int i = 0; i < newItemsList.Count; i++)
                    {
                        var delay = i * 50; // 50ms stagger between items
                        AnimateNewItemWithDelay(newItemsList[i], delay);
                    }
                }
                
                // Handle moved items (but not newly added ones)
                foreach (Control child in Children)
                {
                    if (previousBounds.ContainsKey(child) && !newItems.ContainsKey(child))
                    {
                        var oldBounds = previousBounds[child];
                        var newBounds = currentBounds[child];
                        
                        var positionDiff = Orientation == Orientation.Vertical 
                            ? Math.Abs(oldBounds.Y - newBounds.Y)
                            : Math.Abs(oldBounds.X - newBounds.X);
                        
                        if (positionDiff > 1)
                        {
                            AnimateItemMove(child, oldBounds, newBounds);
                        }
                    }
                }
                
                UpdateBoundsTracking();
                lastChildCount = Children.Count;
                return result;
            }
            finally
            {
                isArranging = false;
            }
        }
        
        private void UpdateBoundsTracking()
        {
            previousBounds.Clear();
            foreach (Control child in Children)
            {
                previousBounds[child] = child.Bounds;
            }
            
            // Clean up removed items
            var currentChildren = Children.Cast<Control>().ToHashSet();
            var keysToRemove = previousBounds.Keys.Where(k => !currentChildren.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                previousBounds.Remove(key);
                newItems.Remove(key);
                removingItems.Remove(key);
            }
        }

        private void AnimateNewItem(Control item)
        {
            AnimateNewItemWithDelay(item, 0);
        }
        
        private async void AnimateNewItemWithDelay(Control item, int delayMs)
        {
            if (!newItems.ContainsKey(item)) return;
            
            // Set initial state
            item.Opacity = 0;
            var builder = new TransformOperations.Builder(1);
            builder.AppendScale(0.8, 0.8);
            if (Orientation == Orientation.Vertical)
                builder.AppendTranslate(0, -20);
            else
                builder.AppendTranslate(-20, 0);
            item.RenderTransform = builder.Build();
            
            // Add transitions
            EnsureNewItemTransitions(item);
            
            // Wait for stagger delay
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
            
            // Animate to final state
            if (newItems.ContainsKey(item))
            {
                item.Opacity = 1.0;
                item.RenderTransform = TransformOperations.Identity;
                
                // Remove from tracking after a short delay to avoid conflicts
                _ = Task.Delay(100).ContinueWith(_ => newItems.Remove(item));
            }
        }
        
        private void EnsureNewItemTransitions(Control item)
        {
            var transitions = new List<ITransition>
            {
                new DoubleTransition 
                { 
                    Property = OpacityProperty, 
                    Duration = TimeSpan.FromMilliseconds(400),
                    Easing = new CubicEaseOut()
                },
                new TransformOperationsTransition 
                { 
                    Property = RenderTransformProperty, 
                    Duration = TimeSpan.FromMilliseconds(400),
                    Easing = new CubicEaseOut()
                }
            };
            item.Transitions = new Transitions();
            foreach (var transition in transitions)
            {
                item.Transitions.Add(transition);
            }
        }
        
        public async Task AnimateItemRemoval(Control item)
        {
            if (removingItems.ContainsKey(item)) return;
            removingItems[item] = true;
            
            // Add removal transitions
            var transitions = new List<ITransition>
            {
                new DoubleTransition 
                { 
                    Property = OpacityProperty, 
                    Duration = TimeSpan.FromMilliseconds(300),
                    Easing = new CubicEaseIn()
                },
                new TransformOperationsTransition 
                { 
                    Property = RenderTransformProperty, 
                    Duration = TimeSpan.FromMilliseconds(300),
                    Easing = new CubicEaseIn()
                }
            };
            item.Transitions = new Transitions();
            foreach (var transition in transitions)
            {
                item.Transitions.Add(transition);
            }
            
            // Animate out
            item.Opacity = 0;
            var builder = new TransformOperations.Builder(1);
            builder.AppendScale(0.8, 0.8);
            if (Orientation == Orientation.Vertical)
                builder.AppendTranslate(0, 20);
            else
                builder.AppendTranslate(20, 0);
            item.RenderTransform = builder.Build();
            
            // Wait for animation to complete
            await Task.Delay(350);
            
            removingItems.Remove(item);
        }

        private void AnimateItemMove(Control item, Rect fromBounds, Rect toBounds)
        {
            var offsetY = Orientation == Orientation.Vertical ? fromBounds.Y - toBounds.Y : 0;
            var offsetX = Orientation == Orientation.Horizontal ? fromBounds.X - toBounds.X : 0;
            
            if (Math.Abs(offsetY) < 1 && Math.Abs(offsetX) < 1) return;
            
            // Set initial offset position
            item.RenderTransform = new TranslateTransform(offsetX, offsetY);
            
            // Ensure transitions exist
            EnsureTransitions(item);
            
            // Animate to final position on next tick
            Dispatcher.UIThread.Post(() =>
            {
                item.RenderTransform = new TranslateTransform(0, 0);
            }, DispatcherPriority.Render);
        }
        
        private void EnsureTransitions(Control item)
        {
            bool hasTransformTransition = false;
            if (item.Transitions != null)
            {
                foreach (var t in item.Transitions)
                {
                    if (t is TransformOperationsTransition)
                    {
                        hasTransformTransition = true;
                        break;
                    }
                }
            }
            
            if (!hasTransformTransition)
            {
                var transitions = item.Transitions?.ToList() ?? new List<ITransition>();
                transitions.Add(new TransformOperationsTransition
                {
                    Property = RenderTransformProperty,
                    Duration = TimeSpan.FromMilliseconds(400),
                    Easing = new CubicEaseOut()
                });
                item.Transitions = new Transitions();
            foreach (var transition in transitions)
            {
                item.Transitions.Add(transition);
            }
            }
        }
        
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            previousBounds.Clear();
            newItems.Clear();
            removingItems.Clear();
        }
    }
}