using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using userinterface.Controls;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    public ProfileListView()
    {
        InitializeComponent();
        
        // Set view reference in ViewModel when DataContext changes
        this.DataContextChanged += (sender, args) =>
        {
            if (DataContext is ProfileListViewModel viewModel)
            {
                viewModel.ProfileListViewRef = this;
            }
        };
        
        // Wire up test animation button
        var testButton = this.FindControl<Button>("TestAnimationButton");
        if (testButton != null)
        {
            testButton.Click += OnTestAnimationClick;
        }
    }

    public void OnItemPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ProfileListElementViewModel item)
        {
            if (DataContext is ProfileListViewModel viewModel)
            {
                viewModel.SetSelectedProfile(item);
            }
        }
    }
    
    private async void OnTestAnimationClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var canvas = GetAnimatedItemsCanvas();
        if (canvas == null) return;
        
        // Get the first profile item if available
        if (DataContext is ProfileListViewModel viewModel && viewModel.ProfileItems.Count >= 2)
        {
            var firstProfile = viewModel.ProfileItems[0];
            var secondProfile = viewModel.ProfileItems[1];
            
            // Test animation: swap positions of first two profiles
            System.Diagnostics.Debug.WriteLine("Test Animation: Swapping positions of first two profiles");
            
            var swapAnimations = new Dictionary<ProfileListElementViewModel, int>
            {
                { firstProfile, 1 },  // Move first to second position
                { secondProfile, 0 }  // Move second to first position  
            };
            
            await AnimateMultipleProfilesToIndicesAsync(swapAnimations, 600);
            
            await Task.Delay(1000);
            
            // Swap back
            System.Diagnostics.Debug.WriteLine("Test Animation: Swapping back to original positions");
            var swapBackAnimations = new Dictionary<ProfileListElementViewModel, int>
            {
                { firstProfile, 0 },  // Move back to first position
                { secondProfile, 1 }  // Move back to second position  
            };
            
            await AnimateMultipleProfilesToIndicesAsync(swapBackAnimations, 600);
        }
        else if (DataContext is ProfileListViewModel vm && vm.ProfileItems.Count > 0)
        {
            // Fallback to pixel-based animation if only one profile
            var firstProfile = vm.ProfileItems[0];
            await MoveProfileAsync(firstProfile, 50, 500);
            await Task.Delay(500);
            await MoveProfileAsync(firstProfile, -50, 500);
        }
    }
    
    /// <summary>
    /// Gets the AnimatedItemsCanvas used for the profile list
    /// </summary>
    public AnimatedItemsCanvas? GetAnimatedItemsCanvas()
    {
        return this.FindControl<AnimatedItemsCanvas>("ProfileItemsControl");
    }
    
    /// <summary>
    /// Gets the ContentPresenter element for a specific profile view model (for animation)
    /// </summary>
    public ContentPresenter? GetProfileContentPresenter(ProfileListElementViewModel profileViewModel)
    {
        var canvas = GetAnimatedItemsCanvas();
        return canvas?.GetPresenterForItem(profileViewModel);
    }
    
    /// <summary>
    /// Gets the Border element for a specific profile view model (for measurements)
    /// </summary>
    public Border? GetProfileBorder(ProfileListElementViewModel profileViewModel)
    {
        var presenter = GetProfileContentPresenter(profileViewModel);
        return presenter != null ? FindBorderInContentPresenter(presenter) : null;
    }
    
    /// <summary>
    /// Helper method to find the Border element inside a ContentPresenter
    /// </summary>
    private Border? FindBorderInContentPresenter(ContentPresenter presenter)
    {
        // The Border should be the direct child of the ContentPresenter based on our XAML structure
        if (presenter.Child is Border border)
        {
            return border;
        }
        
        // If not direct child, search recursively
        return FindBorderRecursive(presenter);
    }
    
    /// <summary>
    /// Recursively searches for a Border element in the visual tree
    /// </summary>
    private Border? FindBorderRecursive(Control parent)
    {
        if (parent is Border border)
        {
            return border;
        }
        
        // Search through logical children
        foreach (var child in parent.GetLogicalChildren())
        {
            if (child is Control childControl)
            {
                var result = FindBorderRecursive(childControl);
                if (result != null)
                {
                    return result;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Animates a profile to a specific index position in the list
    /// </summary>
    /// <param name="profileViewModel">The profile to move</param>
    /// <param name="targetIndex">Target index position (0-based)</param>
    /// <param name="durationMs">Duration of movement in milliseconds</param>
    public async Task AnimateProfileToIndexAsync(ProfileListElementViewModel profileViewModel, int targetIndex, int durationMs = 400)
    {
        System.Diagnostics.Debug.WriteLine($"AnimateProfileToIndex: Moving {profileViewModel.Profile.CurrentNameForDisplay} to index {targetIndex}");
        
        var canvas = GetAnimatedItemsCanvas();
        
        if (canvas != null)
        {
            await canvas.AnimateToIndexAsync(profileViewModel, targetIndex, TimeSpan.FromMilliseconds(durationMs));
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("AnimateProfileToIndex: Failed - canvas is null");
        }
    }

    /// <summary>
    /// Animates multiple profiles to their target indices simultaneously
    /// </summary>
    /// <param name="profileIndexPairs">Dictionary of profiles and their target indices</param>
    /// <param name="durationMs">Duration of movement in milliseconds</param>
    public async Task AnimateMultipleProfilesToIndicesAsync(Dictionary<ProfileListElementViewModel, int> profileIndexPairs, int durationMs = 400)
    {
        System.Diagnostics.Debug.WriteLine($"AnimateMultipleProfilesToIndices: Animating {profileIndexPairs.Count} profiles");
        
        var canvas = GetAnimatedItemsCanvas();
        
        if (canvas != null)
        {
            var itemIndexPairs = profileIndexPairs.ToDictionary(
                kvp => (object)kvp.Key, 
                kvp => kvp.Value
            );
            await canvas.AnimateMultipleToIndicesAsync(itemIndexPairs, TimeSpan.FromMilliseconds(durationMs));
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("AnimateMultipleProfilesToIndices: Failed - canvas is null");
        }
    }

    /// <summary>
    /// Gets the current index of a profile in the list
    /// </summary>
    /// <param name="profileViewModel">The profile to find</param>
    /// <returns>The index of the profile, or -1 if not found</returns>
    public int GetProfileIndex(ProfileListElementViewModel profileViewModel)
    {
        var canvas = GetAnimatedItemsCanvas();
        return canvas?.GetItemIndex(profileViewModel) ?? -1;
    }

    /// <summary>
    /// Moves a profile up or down by the specified pixel amount using smooth animation
    /// </summary>
    /// <param name="profileViewModel">The profile to move</param>
    /// <param name="pixelAmount">Amount to move (positive = down, negative = up)</param>
    /// <param name="durationMs">Duration of movement in milliseconds</param>
    public async Task MoveProfileAsync(ProfileListElementViewModel profileViewModel, double pixelAmount, int durationMs = 300)
    {
        System.Diagnostics.Debug.WriteLine($"MoveProfile: Attempting to move {profileViewModel.Profile.CurrentNameForDisplay} by {pixelAmount}px");
        
        var canvas = GetAnimatedItemsCanvas();
        
        System.Diagnostics.Debug.WriteLine($"MoveProfile: Canvas found: {canvas != null}");
        
        if (canvas != null)
        {
            System.Diagnostics.Debug.WriteLine($"MoveProfile: Animating item, amount={pixelAmount}");
            await canvas.AnimateItemByPixelsAsync(profileViewModel, pixelAmount, TimeSpan.FromMilliseconds(durationMs));
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("MoveProfile: Failed - canvas is null");
        }
    }
    
    
    /// <summary>
    /// Instantly moves a profile by the specified pixel amount
    /// </summary>
    /// <param name="profileViewModel">The profile to move</param>
    /// <param name="pixelAmount">Amount to move (positive = down, negative = up)</param>
    public void MoveProfileInstant(ProfileListElementViewModel profileViewModel, double pixelAmount)
    {
        var canvas = GetAnimatedItemsCanvas();
        var presenter = canvas?.GetPresenterForItem(profileViewModel);
        
        if (presenter != null)
        {
            var currentY = Canvas.GetTop(presenter);
            var currentX = Canvas.GetLeft(presenter);
            
            if (canvas.Orientation == Orientation.Vertical)
            {
                Canvas.SetTop(presenter, currentY + pixelAmount);
            }
            else
            {
                Canvas.SetLeft(presenter, currentX + pixelAmount);
            }
        }
    }
    
    /// <summary>
    /// Resets all profile positions to their natural layout positions
    /// </summary>
    public void ResetAllProfilePositions()
    {
        var canvas = GetAnimatedItemsCanvas();
        // Canvas automatically maintains proper positions, no manual reset needed
        // This method is kept for API compatibility
    }
    
    /// <summary>
    /// Animates removal of a profile using smooth Canvas animations
    /// </summary>
    /// <param name="profileViewModel">The profile to remove</param>
    /// <param name="onAnimationComplete">Callback when animation is complete</param>
    public async Task AnimateProfileRemoval(ProfileListElementViewModel profileViewModel, Action? onAnimationComplete = null)
    {
        var canvas = GetAnimatedItemsCanvas();
        
        if (canvas == null) 
        {
            onAnimationComplete?.Invoke();
            return;
        }
        
        try
        {
            // The Canvas control handles removal animation automatically
            // when the item is removed from the ItemsSource
            profileViewModel.IsHidden = true;
            onAnimationComplete?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during profile removal animation: {ex.Message}");
            profileViewModel.IsHidden = true;
            onAnimationComplete?.Invoke();
        }
    }
}