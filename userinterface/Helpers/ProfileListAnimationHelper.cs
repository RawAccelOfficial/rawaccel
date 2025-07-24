using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Animation.Easings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Layout;
using Avalonia;
using System.Linq;
using Avalonia.Styling;

namespace userinterface.Helpers;

public class ProfileListAnimationHelper : IDisposable
{
    private readonly List<Border> profiles;
    private readonly Panel profileContainer;
    private readonly Border addProfileButton;
    private readonly Dictionary<int, CancellationTokenSource> activeAnimations = [];
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private volatile bool areAnimationsActive = false;
    private readonly object animationLock = new();
    private bool disposed = false;
    
    public static double ProfileHeight => 38.0;
    public static double ProfileSpacing => 4.0;
    public static int StaggerDelayMs => 20;
    public static double ProfileSpawnPosition => 0.0;
    public static double FirstIndexOffset => 6;

    public ProfileListAnimationHelper(List<Border> profiles, Panel profileContainer, Border addProfileButton)
    {
        this.profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        this.profileContainer = profileContainer ?? throw new ArgumentNullException(nameof(profileContainer));
        this.addProfileButton = addProfileButton ?? throw new ArgumentNullException(nameof(addProfileButton));
    }

    public bool AreAnimationsActive
    {
        get
        {
            lock (animationLock)
            {
                return areAnimationsActive;
            }
        }
    }

    public static double CalculatePositionForIndex(int index, bool includeAddButton = true)
    {
        var adjustedIndex = includeAddButton ? index + 1 : index;
        return adjustedIndex == 0 ? 0 : (adjustedIndex * (ProfileHeight + ProfileSpacing)) + FirstIndexOffset;
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
        foreach (var profileBorder in profiles)
        {
            if (profileBorder.Child is Grid grid)
            {
                var deleteButton = grid.Children.OfType<Button>().FirstOrDefault(b => b.Classes.Contains("DeleteButton"));
                if (deleteButton != null)
                {
                    deleteButton.IsEnabled = !areAnimationsActive;
                }
            }
        }
    }
    
    public void CancelAllAnimations()
    {
        lock (animationLock)
        {
            CancelAllAnimationsInternal();
            areAnimationsActive = false;
        }
        UpdateDeleteButtonStates();
    }
    
    private void CancelAllAnimationsInternal()
    {
        foreach (var cts in activeAnimations.Values)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Token was already disposed, ignore
            }
        }
        activeAnimations.Clear();
    }

    public async Task AnimateProfileToPosition(int profileIndex, int position, int staggerIndex = 0)
    {
        if (profileIndex >= profiles.Count) return;

        // Cancel existing animation for this profile
        if (activeAnimations.TryGetValue(profileIndex, out var existingCts))
        {
            try
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }
            catch (ObjectDisposedException) { }
            activeAnimations.Remove(profileIndex);
        }

        // Check if profile is already at correct position - skip animation if so
        var targetMargin = new Avalonia.Thickness(8, CalculatePositionForIndex(position + 1), 8, 0);
        if (profiles[profileIndex].Margin == targetMargin)
        {
            profiles[profileIndex].ZIndex = position; // Set z-index based on position
            return;
        }

        var cts = new CancellationTokenSource();
        activeAnimations[profileIndex] = cts;

        try
        {
            // Apply stagger delay only if not canceled
            if (staggerIndex > 0 && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(staggerIndex * StaggerDelayMs, cts.Token);
            }

            // Double-check cancellation after delay
            if (cts.Token.IsCancellationRequested) return;

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("0.25,0.1,0.25,1"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Controls.Border.OpacityProperty,
                                Value = 1.0
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
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = targetMargin
                            },
                            new Setter
                            {
                                Property = Avalonia.Controls.Border.OpacityProperty,
                                Value = 1.0
                            }
                        }
                    }
                }
            };
            
            await animation.RunAsync(profiles[profileIndex], cts.Token);
            
            if (!cts.Token.IsCancellationRequested)
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
            lock (animationLock)
            {
                activeAnimations.Remove(profileIndex);
                
                // Check if this was the last animation
                if (activeAnimations.Count == 0 && areAnimationsActive)
                {
                    areAnimationsActive = false;
                    Debug.WriteLine($"[ANIMATION] All animations completed, re-enabling interactions at {DateTime.Now:HH:mm:ss.fff}");
                }
            }
            try
            {
                cts?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }

    public async Task AnimateAllProfilesToCorrectPositions(int focusIndex = -1)
    {
        var animationTasks = new List<Task>();
        
        lock (animationLock)
        {
            // Cancel any existing animations before starting new ones
            CancelAllAnimationsInternal();
        }
        
        for (int i = 0; i < profiles.Count; i++)
        {
            var targetMargin = new Thickness(8, CalculatePositionForIndex(i + 1), 8, 0);
            if (profiles[i].Margin == targetMargin) 
            {
                profiles[i].ZIndex = i;
                continue;
            }
            
            int staggerIndex = (focusIndex >= 0 && i != focusIndex) ? Math.Min(Math.Abs(i - focusIndex), 3) : i;
            animationTasks.Add(AnimateProfileToPosition(i, i, staggerIndex));
        }
        
        if (animationTasks.Count > 0)
        {
            lock (animationLock)
            {
                areAnimationsActive = true;
            }
            UpdateDeleteButtonStates();
            
            try
            {
                await Task.WhenAll(animationTasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ANIMATION] Error in AnimateAllProfilesToCorrectPositions: {ex.Message}");
            }
            finally
            {
                lock (animationLock)
                {
                    areAnimationsActive = false;
                }
                UpdateDeleteButtonStates();
            }
        }
    }
    
    public async Task ExpandProfileAnimation()
    {
        var animationTasks = new List<Task>();
        
        // Animate Add Profile button back to position 0 with includeAddButton = true (its normal position)
        if (addProfileButton != null)
        {
            animationTasks.Add(AnimateAddProfileButtonToPosition(0, true));
        }
        
        // Animate profiles to their correct positions
        animationTasks.Add(AnimateAllProfilesToCorrectPositions(-1));
        
        await Task.WhenAll(animationTasks);
    }
    
    public async Task CollapseProfileAnimation()
    {
        if (profiles.Count == 0) return;
        
        var animationTasks = new List<Task>();
        
        lock (animationLock)
        {
            CancelAllAnimationsInternal();
            areAnimationsActive = true;
        }
        UpdateDeleteButtonStates();
        
        // Animate Add Profile button to position 0
        if (addProfileButton != null)
        {
            animationTasks.Add(AnimateAddProfileButtonToPosition(0, false));
        }
        
        // Animate all profiles to position 0
        for (int i = 0; i < profiles.Count; i++)
        {
            animationTasks.Add(CollapseProfileAnimationForIndex(i, i));
        }
        
        try
        {
            await Task.WhenAll(animationTasks);
        }
        finally
        {
            lock (animationLock)
            {
                areAnimationsActive = false;
            }
            UpdateDeleteButtonStates();
        }
    }
    
    private async Task AnimateAddProfileButtonToPosition(int targetPosition, bool includeAddButton)
    {
        if (addProfileButton == null) return;

        var targetMargin = new Thickness(8, CalculatePositionForIndex(targetPosition, includeAddButton), 8, 0);
        
        // Check if already at target position
        if (addProfileButton.Margin == targetMargin) return;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            FillMode = FillMode.Forward,
            Easing = Easing.Parse("0.25,0.1,0.25,1"),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter
                        {
                            Property = Avalonia.Layout.Layoutable.MarginProperty,
                            Value = addProfileButton.Margin
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
                            Property = Avalonia.Layout.Layoutable.MarginProperty,
                            Value = targetMargin
                        }
                    }
                }
            }
        };
        
        await animation.RunAsync(addProfileButton);
        addProfileButton.Margin = targetMargin;
    }
    
    private async Task CollapseProfileAnimationForIndex(int profileIndex, int staggerIndex = 0)
    {
        if (profileIndex >= profiles.Count) return;

        var cts = new CancellationTokenSource();
        activeAnimations[profileIndex] = cts;

        try
        {
            if (staggerIndex > 0 && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(staggerIndex * StaggerDelayMs, cts.Token);
            }

            if (cts.Token.IsCancellationRequested) return;

            var targetMargin = new Avalonia.Thickness(8, CalculatePositionForIndex(0, false), 8, 0);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("0.25,0.1,0.25,1"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter
                            {
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = profiles[profileIndex].Margin
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
                                Property = Avalonia.Layout.Layoutable.MarginProperty,
                                Value = targetMargin
                            }
                        }
                    }
                }
            };
            
            await animation.RunAsync(profiles[profileIndex], cts.Token);
            
            if (!cts.Token.IsCancellationRequested)
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
            lock (animationLock)
            {
                activeAnimations.Remove(profileIndex);
            }
            try
            {
                cts?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            CancelAllAnimations();
            operationSemaphore?.Dispose();
            disposed = true;
        }
    }
}