using Avalonia.Controls;
using Avalonia.Input;
using userinterface.Controls;
using userinterface.ViewModels.Profile;
using userinterface.Services;
using Microsoft.Extensions.DependencyInjection;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl
{
    private ProfileListViewModel? previousViewModel;
    
    public ProfileListView()
    {
        InitializeComponent();
        
        // Register this control with animation service when DataContext changes
        this.DataContextChanged += (sender, args) =>
        {
            // Unregister from previous view model
            if (previousViewModel != null)
            {
                var canvas = GetAnimatedItemsCanvas();
                if (canvas != null)
                {
                    previousViewModel.AnimationService.UnregisterAnimatedControl(canvas);
                }
            }
            
            // Register with new view model
            if (DataContext is ProfileListViewModel viewModel)
            {
                var animationService = viewModel.AnimationService;
                var canvas = GetAnimatedItemsCanvas();
                if (canvas != null)
                {
                    animationService.RegisterAnimatedControl(canvas);
                }
                previousViewModel = viewModel;
            }
            else
            {
                previousViewModel = null;
            }
        };
        
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
    
    
    private AnimatedItemsCanvas? GetAnimatedItemsCanvas()
    {
        return this.FindControl<AnimatedItemsCanvas>("ProfileItemsControl");
    }
    
    /// <summary>
    /// Move an item to a new position using the collection-based approach
    /// </summary>
    public void MoveItemToPosition(ProfileListElementViewModel item, int newIndex)
    {
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.MoveItemToIndex(item, newIndex);
        }
    }

    /// <summary>
    /// Swap two items using the collection-based approach
    /// </summary>
    public void SwapItems(ProfileListElementViewModel item1, ProfileListElementViewModel item2)
    {
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.SwapItems(item1, item2);
        }
    }
}