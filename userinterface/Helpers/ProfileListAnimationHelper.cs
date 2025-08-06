using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Animation.Easings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Layout;
using Avalonia;
using System.Linq;
using Avalonia.Styling;
using System.Runtime.CompilerServices;
using userinterface.Services;

namespace userinterface.Helpers;

public class ProfileListAnimationHelper : IDisposable
{
    private readonly List<Border> profiles;
    private readonly Panel profileContainer;
    private readonly Border addProfileButton;
    private readonly IAnimationStateService animationStateService;
    private bool disposed = false;
    
    private readonly FrameTimerService frameTimer;
    
    // Object pools for memory optimization
    private readonly ObjectPool<Animation> animationPool = new(() => new Animation());
    private readonly ObjectPool<List<Task>> taskListPool = new(() => new List<Task>());
    // CancellationTokenSource can't be reused after cancellation, so we just dispose them
    
    // Caches for performance optimization
    private readonly Dictionary<int, double> positionCache = new();
    private readonly Dictionary<int, Button> deleteButtonCache = new();
    private readonly Dictionary<string, Animation> animationTemplateCache = new();
    
    // Pre-built animations for common operations
    private Animation? cachedProfileMoveAnimation;
    private Animation? cachedProfileCollapseAnimation;
    
    // Performance counters
    private volatile int activeAnimationCount = 0;

    public ProfileListAnimationHelper(List<Border> profiles, Panel profileContainer, Border addProfileButton, FrameTimerService frameTimer, IAnimationStateService animationStateService)
    {
        this.profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        this.profileContainer = profileContainer ?? throw new ArgumentNullException(nameof(profileContainer));
        this.addProfileButton = addProfileButton ?? throw new ArgumentNullException(nameof(addProfileButton));
        this.frameTimer = frameTimer ?? throw new ArgumentNullException(nameof(frameTimer));
        this.animationStateService = animationStateService ?? throw new ArgumentNullException(nameof(animationStateService));
    }

    public bool AreAnimationsActive
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => animationStateService.AreAnimationsActive;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CalculatePositionForIndex(int index, bool includeAddButton = true)
    {
        // Use cache for frequently accessed positions
        var cacheKey = includeAddButton ? index : index + 1000;
        if (positionCache.TryGetValue(cacheKey, out var cachedPosition))
            return cachedPosition;
            
        var adjustedIndex = includeAddButton ? index + 1 : index;
        var position = adjustedIndex == 0 ? 0 : (adjustedIndex * (animationStateService.Config.ProfileHeight + animationStateService.Config.ProfileSpacing)) + animationStateService.Config.FirstIndexOffset;
        
        positionCache[cacheKey] = position;
        return position;
    }
    
    private Animation GetOptimizedProfileMoveAnimation()
    {
        if (cachedProfileMoveAnimation == null)
        {
            cachedProfileMoveAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("0.25,0.1,0.25,1")
            };
        }
        return cachedProfileMoveAnimation;
    }
    
    private Animation GetOptimizedProfileCollapseAnimation()
    {
        if (cachedProfileCollapseAnimation == null)
        {
            cachedProfileCollapseAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("0.25,0.1,0.25,1")
            };
        }
        return cachedProfileCollapseAnimation;
    }
    
    public void UpdateAllZIndexes()
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            profiles[i].ZIndex = i;
        }
    }
    
    public void UpdateDeleteButtonStates()
    {
        var isActive = animationStateService.AreAnimationsActive;
        for (int i = 0; i < profiles.Count; i++)
        {
            // Use cached delete button reference if available
            if (deleteButtonCache.TryGetValue(i, out var cachedButton))
            {
                cachedButton.IsEnabled = !isActive;
                continue;
            }
            
            if (profiles[i].Child is Grid grid)
            {
                var deleteButton = grid.Children.OfType<Button>().FirstOrDefault(b => b.Classes.Contains("DeleteButton"));
                if (deleteButton != null)
                {
                    deleteButton.IsEnabled = !isActive;
                    deleteButtonCache[i] = deleteButton; // Cache for future use
                }
            }
        }
    }
    
    public void CancelAllAnimations()
    {
        animationStateService.CancelAllAnimations("ProfileListAnimationHelper");
        Interlocked.Exchange(ref activeAnimationCount, 0);
        UpdateDeleteButtonStates();
    }

    public async ValueTask AnimateProfileToPositionAsync(int profileIndex, int position, int staggerIndex = 0)
    {
        if (profileIndex >= profiles.Count) return;

        // Check if profile is already at correct position - skip animation if so
        var targetMargin = new Avalonia.Thickness(8, CalculatePositionForIndex(position + 1), 8, 0);
        if (profiles[profileIndex].Margin == targetMargin && profiles[profileIndex].ZIndex == position)
        {
            return;
        }

        // Register animation with the service
        var cancellationToken = await animationStateService.RegisterAnimationAsync("ProfileListAnimationHelper", profileIndex);
        Interlocked.Increment(ref activeAnimationCount);

        try
        {
            // Apply stagger delay only if not canceled
            if (staggerIndex > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(staggerIndex * animationStateService.Config.StaggerDelayMs, cancellationToken);
            }

            // Double-check cancellation after delay
            if (cancellationToken.IsCancellationRequested) return;

            var animation = GetOptimizedProfileMoveAnimation();
            animation.Children.Clear();
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter { Property = Avalonia.Controls.Border.OpacityProperty, Value = 1.0 }
                }
            });
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter { Property = Avalonia.Layout.Layoutable.MarginProperty, Value = targetMargin },
                    new Setter { Property = Avalonia.Controls.Border.OpacityProperty, Value = 1.0 }
                }
            });
            
            await animation.RunAsync(profiles[profileIndex], cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                profiles[profileIndex].Margin = targetMargin;
                profiles[profileIndex].ZIndex = position;
            }
        }
        catch (OperationCanceledException)
        {
            // Silently handle cancellation - this is expected behavior
            Debug.WriteLine($"[ANIMATION] Animation for profile {profileIndex} was canceled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ANIMATION] Unexpected error in animation for profile {profileIndex}: {ex.Message}");
        }
        finally
        {
            animationStateService.UnregisterAnimation("ProfileListAnimationHelper", profileIndex);
            
            var remainingCount = Interlocked.Decrement(ref activeAnimationCount);
            
            // Note: The service handles global animation state automatically
            Debug.WriteLine($"[ANIMATION] Cleaned up animation for profile {profileIndex}, remaining: {remainingCount}");
        }
    }

    public async ValueTask AnimateAllProfilesToCorrectPositionsAsync(int focusIndex = -1)
    {
        frameTimer.StartMonitoring($"AnimateAllProfilesToCorrectPositions focusIndex={focusIndex}");
        var animationTasks = taskListPool.Get();
        try
        {
            // Cancel any existing animations before starting new ones
            animationStateService.CancelAllAnimations("ProfileListAnimationHelper");
            
            // Batch process animations for better performance
            var animationsToRun = new List<(int index, int staggerIndex)>();
            
            for (int i = 0; i < profiles.Count; i++)
            {
                var targetMargin = new Thickness(8, CalculatePositionForIndex(i + 1), 8, 0);
                if (profiles[i].Margin == targetMargin) 
                {
                    profiles[i].ZIndex = i;
                    continue;
                }
                
                int staggerIndex = (focusIndex >= 0 && i != focusIndex) ? Math.Min(Math.Abs(i - focusIndex), 3) : i;
                animationsToRun.Add((i, staggerIndex));
            }
            
            // Deduplicate animations based on target positions
            var deduplicatedAnimations = animationsToRun
                .GroupBy(a => a.index)
                .Select(g => g.Last()) // Take the last animation for each index
                .OrderBy(a => a.staggerIndex)
                .ToList();
            
            foreach (var (index, staggerIndex) in deduplicatedAnimations)
            {
                animationTasks.Add(AnimateProfileToPositionAsync(index, index, staggerIndex).AsTask());
            }
            
            if (animationTasks.Count > 0)
            {
                animationStateService.SetAnimationsActive(true);
                UpdateDeleteButtonStates();
                
                try
                {
                    await Task.WhenAll(animationTasks);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ANIMATION] Error in AnimateAllProfilesToCorrectPositionsAsync: {ex.Message}");
                }
                finally
                {
                    animationStateService.SetAnimationsActive(false);
                    UpdateDeleteButtonStates();
                }
            }
        }
        finally
        {
            frameTimer.StopMonitoring($"AnimateAllProfilesToCorrectPositions focusIndex={focusIndex}");
            animationTasks.Clear();
            taskListPool.Return(animationTasks);
        }
    }
    
    public async ValueTask ExpandProfileAnimationAsync()
    {
        var animationTasks = taskListPool.Get();
        try
        {
            // Animate Add Profile button back to position 0 with includeAddButton = true (its normal position)
            if (addProfileButton != null)
            {
                animationTasks.Add(AnimateAddProfileButtonToPositionAsync(0, true));
            }
            
            // Animate profiles to their correct positions
            animationTasks.Add(AnimateAllProfilesToCorrectPositionsAsync(-1).AsTask());
            
            if (animationTasks.Count > 0)
            {
                await Task.WhenAll(animationTasks);
            }
        }
        finally
        {
            animationTasks.Clear();
            taskListPool.Return(animationTasks);
        }
    }
    
    public async ValueTask CollapseProfileAnimationAsync()
    {
        if (profiles.Count == 0) return;
        
        frameTimer.StartMonitoring("CollapseProfileAnimation");
        var animationTasks = taskListPool.Get();
        try
        {
            animationStateService.CancelAllAnimations("ProfileListAnimationHelper");
            animationStateService.SetAnimationsActive(true);
            
            UpdateDeleteButtonStates();
            
            // Animate Add Profile button to position 0
            if (addProfileButton != null)
            {
                animationTasks.Add(AnimateAddProfileButtonToPositionAsync(0, false));
            }
            
            // Animate all profiles to position 0
            for (int i = 0; i < profiles.Count; i++)
            {
                animationTasks.Add(CollapseProfileAnimationForIndexAsync(i, i));
            }
            
            if (animationTasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(animationTasks);
                }
                finally
                {
                    animationStateService.SetAnimationsActive(false);
                    UpdateDeleteButtonStates();
                }
            }
        }
        finally
        {
            frameTimer.StopMonitoring("CollapseProfileAnimation");
            animationTasks.Clear();
            taskListPool.Return(animationTasks);
        }
    }
    
    private async Task AnimateAddProfileButtonToPositionAsync(int targetPosition, bool includeAddButton)
    {
        if (addProfileButton == null) return;

        var targetMargin = new Thickness(8, CalculatePositionForIndex(targetPosition, includeAddButton), 8, 0);
        
        // Check if already at target position
        if (addProfileButton.Margin == targetMargin) return;

        var animation = GetOptimizedProfileMoveAnimation();
        animation.Children.Clear();
        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue(0d),
            Setters =
            {
                new Setter { Property = Avalonia.Layout.Layoutable.MarginProperty, Value = addProfileButton.Margin }
            }
        });
        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue(1d),
            Setters =
            {
                new Setter { Property = Avalonia.Layout.Layoutable.MarginProperty, Value = targetMargin }
            }
        });
        
        await animation.RunAsync(addProfileButton);
        addProfileButton.Margin = targetMargin;
    }
    
    private async Task CollapseProfileAnimationForIndexAsync(int profileIndex, int staggerIndex = 0)
    {
        if (profileIndex >= profiles.Count) return;

        var cancellationToken = await animationStateService.RegisterAnimationAsync("ProfileListAnimationHelper", profileIndex);
        Interlocked.Increment(ref activeAnimationCount);

        try
        {
            if (staggerIndex > 0 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(staggerIndex * animationStateService.Config.StaggerDelayMs, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested) return;

            var targetMargin = new Avalonia.Thickness(8, CalculatePositionForIndex(0, false), 8, 0);

            var animation = GetOptimizedProfileCollapseAnimation();
            animation.Children.Clear();
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter { Property = Avalonia.Layout.Layoutable.MarginProperty, Value = profiles[profileIndex].Margin }
                }
            });
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter { Property = Avalonia.Layout.Layoutable.MarginProperty, Value = targetMargin }
                }
            });
            
            await animation.RunAsync(profiles[profileIndex], cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                profiles[profileIndex].Margin = targetMargin;
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[ANIMATION] Collapse animation for profile {profileIndex} was canceled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ANIMATION] Error in collapse animation for profile {profileIndex}: {ex.Message}");
        }
        finally
        {
            animationStateService.UnregisterAnimation("ProfileListAnimationHelper", profileIndex);
            Interlocked.Decrement(ref activeAnimationCount);
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            
            // Cancel all animations first
            CancelAllAnimations();
            
            // Dispose of all pools and resources
            animationPool?.Dispose();
            taskListPool?.Dispose();
            
            // Clear caches
            positionCache.Clear();
            deleteButtonCache.Clear();
            animationTemplateCache.Clear();
            
            // Clear cached animations
            cachedProfileMoveAnimation = null;
            cachedProfileCollapseAnimation = null;
        }
    }
    
    // Backward compatibility methods
    public async Task AnimateProfileToPosition(int profileIndex, int position, int staggerIndex = 0)
    {
        await AnimateProfileToPositionAsync(profileIndex, position, staggerIndex);
    }
    
    public async Task AnimateAllProfilesToCorrectPositions(int focusIndex = -1)
    {
        await AnimateAllProfilesToCorrectPositionsAsync(focusIndex);
    }
    
    public async Task ExpandProfileAnimation()
    {
        await ExpandProfileAnimationAsync();
    }
    
    public async Task CollapseProfileAnimation()
    {
        await CollapseProfileAnimationAsync();
    }
}