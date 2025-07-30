using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using userinterface.Services;
using userinterface.ViewModels.Profile;
using userspace_backend;
using BE = userspace_backend.Model;

namespace userinterface.Views.Profile;

public partial class ProfileListView : UserControl, INotifyPropertyChanged
{
    private readonly List<Border> allItems = [];
    private Panel? profileContainer;
    private readonly BE.ProfilesModel profilesModel;
    private readonly SemaphoreSlim operationSemaphore = new(1, 1);
    private BE.ProfileModel? selectedProfile;

    private int GetProfileCount() => allItems.Count - 1;
    private volatile bool areAnimationsActive = false;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private readonly IModalService modalService;
    private readonly LocalizationService localizationService;
    private TextBlock? addProfileTextBlock;

    private const double ProfileHeight = 38.0;
    private const double ProfileSpacing = 4.0;
    private const double FirstIndexOffset = 6;
    private const int StaggerDelayMs = 20;
    private const int CollapseStaggerDelayMs = 15;
    private const int ElementRenderDelayMs = 50;
    private const int AnimationCompleteDelayMs = 200;


    public ProfileListView()
    {
        var backEnd = App.Services?.GetRequiredService<BackEnd>() ?? throw new InvalidOperationException("BackEnd service not available");
        profilesModel = backEnd.Profiles ?? throw new ArgumentNullException(nameof(backEnd.Profiles));
        modalService = App.Services?.GetRequiredService<IModalService>() ?? throw new InvalidOperationException("ModalService not available");
        localizationService = App.Services?.GetRequiredService<LocalizationService>() ?? throw new InvalidOperationException("LocalizationService not available");
        localizationService.PropertyChanged += OnLocalizationPropertyChanged;
        profilesModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;

        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        operationSemaphore?.Dispose();
        if (localizationService != null)
        {
            localizationService.PropertyChanged -= OnLocalizationPropertyChanged;
        }
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        profileContainer = this.FindControl<Panel>("ProfileContainer");

        // Set the view reference in the ViewModel
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.SetView(this);
        }

        var addButton = CreateAddProfileButton();
        allItems.Add(addButton);
        profileContainer?.Children.Add(addButton);

        CreateProfilesWithStagger();

        _ = ExpandElements();

        // SetSelectedProfile(null);
    }

    private void OnLocalizationPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (addProfileTextBlock != null)
        {
            addProfileTextBlock.Text = localizationService?.GetText("ProfileAddNewProfile") ?? "Add New Profile";
        }
    }

    private async void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        await operationSemaphore.WaitAsync();
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    HandleProfilesAdded(e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    await HandleProfilesRemoved(e);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    await HandleProfilesReplaced(e);
                    break;
                case NotifyCollectionChangedAction.Move:
                    await HandleProfilesMoved(e);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    await HandleProfilesReset();
                    break;
            }
        }
        finally
        {
            operationSemaphore.Release();
        }
    }

    private void HandleProfilesAdded(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null) return;

        // Add profiles at their actual positions in the backend collection
        int startIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : profilesModel.Profiles.Count - e.NewItems.Count;

        for (int i = 0; i < e.NewItems.Count; i++)
        {
            int profileIndex = startIndex + i;
            AddProfileAtPosition(profileIndex);
        }

        RefreshAllProfileNames();

        if (e.NewItems.Count > 0)
        {
            int lastAddedIndex = startIndex + e.NewItems.Count - 1;
            if (lastAddedIndex >= 0 && lastAddedIndex < profilesModel.Profiles.Count)
            {
                SetSelectedProfile(profilesModel.Profiles[lastAddedIndex]);
            }
        }
    }

    private async Task HandleProfilesRemoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null) return;

        int removeIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : GetProfileCount() - 1;
        int removeCount = e.OldItems.Count;


        for (int i = 0; i < removeCount && removeIndex >= 0 && removeIndex < GetProfileCount(); i++)
        {
            RemoveProfileAt(removeIndex);
        }

        UpdateAllZIndexes();

        await AnimateAllElementsToPositions(removeIndex);

        if (selectedProfile != null && !profilesModel.Profiles.Contains(selectedProfile))
        {
            var defaultProfile = profilesModel.Profiles.FirstOrDefault(p => p == BE.ProfilesModel.DefaultProfile);
            if (defaultProfile != null)
            {
                SetSelectedProfile(defaultProfile);
            }
            else if (profilesModel.Profiles.Count > 0)
            {
                SetSelectedProfile(profilesModel.Profiles[0]);
            }
            else
            {
                SetSelectedProfile(null);
            }
        }
    }

    private async Task HandleProfilesReplaced(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems == null || e.NewItems == null || e.OldStartingIndex < 0) return;

        int replaceIndex = e.OldStartingIndex;
        int itemCount = Math.Min(e.OldItems.Count, e.NewItems.Count);

        for (int i = 0; i < itemCount && replaceIndex + i < GetProfileCount(); i++)
        {
            int itemIndex = replaceIndex + i + 1; // +1 for add button
            if (itemIndex < allItems.Count && allItems[itemIndex].Child is Grid grid)
            {
                var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = $"Profile {replaceIndex + i + 1}";
                }
            }
        }

        UpdateAllZIndexes();

        await AnimateAllElementsToPositions(replaceIndex);
    }

    private async Task HandleProfilesMoved(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldStartingIndex < 0 || e.NewStartingIndex < 0) return;

        MoveProfile(e.OldStartingIndex, e.NewStartingIndex);

        UpdateAllZIndexes();

        await AnimateAllElementsToPositions(Math.Min(e.OldStartingIndex, e.NewStartingIndex));
    }

    private Task HandleProfilesReset()
    {
        var addButton = allItems.Count > 0 ? allItems[0] : null;
        allItems.Clear();
        profileContainer?.Children.Clear();

        if (addButton != null)
        {
            allItems.Add(addButton);
            profileContainer?.Children.Add(addButton);
        }

        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            AddProfileAtPosition(i);
        }

        UpdateAllZIndexes();

        return Task.CompletedTask;
    }

    private void RemoveProfileAt(int index)
    {
        int itemIndex = index + 1;
        if (itemIndex < 0 || itemIndex >= allItems.Count) return;

        var item = allItems[itemIndex];
        allItems.RemoveAt(itemIndex);
        profileContainer?.Children.Remove(item);
    }

    private void MoveProfile(int fromIndex, int toIndex)
    {
        int fromItemIndex = fromIndex + 1;
        int toItemIndex = toIndex + 1;

        if (fromItemIndex < 1 || fromItemIndex >= allItems.Count ||
            toItemIndex < 1 || toItemIndex >= allItems.Count ||
            fromItemIndex == toItemIndex) return;

        var item = allItems[fromItemIndex];
        allItems.RemoveAt(fromItemIndex);
        allItems.Insert(toItemIndex, item);

        profileContainer?.Children.RemoveAt(fromItemIndex);
        profileContainer?.Children.Insert(toItemIndex, item);
    }

    private void AddProfileAtPosition(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex > GetProfileCount()) return;

        var profileBorder = CreateProfileBorder(null!, targetIndex);

        profileBorder.ZIndex = 1000;
        profileBorder.Opacity = 1.0; // Ensure full visibility

        int itemIndex = targetIndex + 1;
        allItems.Insert(itemIndex, profileBorder);
        profileContainer?.Children.Insert(itemIndex, profileBorder);

        UpdateAllZIndexes();

        _ = AnimateAllElementsToPositions(targetIndex);
    }

    private Border CreateAddProfileButton()
    {
        // Create the add profile text
        addProfileTextBlock = new TextBlock
        {
            Text = localizationService?.GetText("ProfileAddNewProfile") ?? "Add New Profile",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeight.Medium
        };

        var border = new Border
        {
            Classes = { "AddProfileButton" },
            Height = ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 0, 8, ProfileSpacing), // Start at collapsed position (Y=0)
            Child = addProfileTextBlock
        };

        border.PointerPressed += (s, e) => OnAddProfileClicked(s!, e);

        return border;
    }

    private Border CreateProfileBorder(IBrush color, int targetIndex)
    {
        var profileName = targetIndex < profilesModel.Profiles.Count ? profilesModel.Profiles[targetIndex].CurrentNameForDisplay : $"Profile {targetIndex + 1}";
        var isDefaultProfile = targetIndex < profilesModel.Profiles.Count && profilesModel.Profiles[targetIndex] == BE.ProfilesModel.DefaultProfile;

        var profileText = new TextBlock
        {
            Text = profileName,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        Grid.SetColumn(profileText, 0);
        grid.Children.Add(profileText);

        if (!isDefaultProfile)
        {
            // Create the delete button with icon using SimpleDeleteButton
            var deleteButton = new Button
            {
                Classes = { "SimpleDeleteButton" },
                VerticalAlignment = VerticalAlignment.Center,
                Content = new PathIcon
                {
                    Data = Application.Current?.FindResource("delete_regular") as StreamGeometry,
                    Width = 12,
                    Height = 12
                }
            };
            deleteButton.Click += OnDeleteButtonClicked;

                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetColumn(deleteButton, 1);
            grid.Children.Add(deleteButton);
        }

        var border = new Border
        {
            Classes = { "ProfileItem" },
            Height = ProfileHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 0, 8, ProfileSpacing), // Start at collapsed position (Y=0)
            Child = grid,
            Opacity = 1.0,
            ZIndex = targetIndex
        };

        border.PointerPressed += OnProfileBorderClicked;

        return border;
    }

    private void UpdateDeleteButtonStates()
    {
        for (int i = 2; i < allItems.Count; i++)
        {
            if (allItems[i].Child is Grid grid)
            {
                var deleteButton = grid.Children.OfType<Button>().FirstOrDefault(b => b.Classes.Contains("DeleteButton"));
                if (deleteButton != null)
                {
                    deleteButton.IsEnabled = !areAnimationsActive;
                }
            }
        }
    }

    private void OnProfileBorderClicked(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border)
        {
            int profileIndex = allItems.IndexOf(border) - 1; // Convert to profile index
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var clickedProfile = profilesModel.Profiles[profileIndex];
                SetSelectedProfile(clickedProfile);
            }
        }
    }

    private void OnAddProfileClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Prevent rapid clicking during active operations
        if (areAnimationsActive)
        {
            return;
        }

        // Use the ViewModel's TryAddProfile method
        if (DataContext is ProfileListViewModel viewModel)
        {
            viewModel.TryAddProfile();
        }
    }

    private async void OnDeleteButtonClicked(object? sender, RoutedEventArgs e)
    {
        // Prevent deletion during animations to avoid bugs
        if (areAnimationsActive)
        {
            return;
        }

        // Find which profile this delete button belongs to
        if (sender is Button deleteButton &&
            deleteButton.Parent is Grid grid &&
            grid.Parent is Border border)
        {
            var profileIndex = allItems.IndexOf(border) - 1; // Subtract 1 for add button
            if (profileIndex >= 0 && profileIndex < profilesModel.Profiles.Count)
            {
                var profileToDelete = profilesModel.Profiles[profileIndex];

                // Show confirmation modal
                var confirmed = await modalService.ShowConfirmationAsync(
                    "ProfileDeleteTitle",
                    "ProfileDeleteMessage",
                    "ProfileDeleteConfirm",
                    "ModalCancel");

                if (confirmed)
                {
                    profilesModel.RemoveProfile(profileToDelete);
                }
            }
        }
    }

    private static double CalculatePositionForIndex(int itemIndex)
    {
        return itemIndex == 0 ? 0 : (itemIndex * (ProfileHeight + ProfileSpacing)) + FirstIndexOffset;
    }

    private void UpdateAllZIndexes()
    {
        var itemCount = allItems.Count;
        for (int i = 0; i < itemCount; i++)
        {
            if (i >= allItems.Count) break;
            
            allItems[i].ZIndex = i;
        }
    }

    private void CreateProfilesWithStagger()
    {
        for (int i = 0; i < profilesModel.Profiles.Count; i++)
        {
            var profileBorder = CreateProfileBorder(null!, i);
            profileBorder.ZIndex = 1000;
            profileBorder.Opacity = 1.0;
            // Elements start in collapsed state with Y=0 margin (already set in CreateProfileBorder)

            int itemIndex = i + 1; // +1 for add button
            allItems.Insert(itemIndex, profileBorder);
            profileContainer?.Children.Insert(itemIndex, profileBorder);
        }

        UpdateAllZIndexes();
        RefreshAllProfileNames();
        UpdateDeleteButtonStates();
    }


    private async Task AnimateElementToMarginPosition(int elementIndex, int position, int staggerIndex = 0)
    {
        if (elementIndex >= allItems.Count) return;

        var element = allItems[elementIndex];
        var targetY = CalculatePositionForIndex(position);
        var targetMargin = new Thickness(8, targetY, 8, ProfileSpacing);
        
        // Get current margin to ensure we're changing from a different state
        var currentY = element.Margin.Top;
        
        // Skip animation if already at target position
        if (Math.Abs(currentY - targetY) < 0.1)
        {
            element.ZIndex = position;
            return;
        }
        
        // Add animation class to enable CSS transitions
        element.Classes.Add("animate-position");
        
        if (staggerIndex > 0)
        {
            await Task.Delay(staggerIndex * StaggerDelayMs);
        }

        element.Margin = targetMargin;
        element.ZIndex = position;
    }

    private async Task AnimateAllElementsToPositions(int focusIndex = -1)
    {
        areAnimationsActive = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreAnimationsActive)));
        UpdateDeleteButtonStates();

        var animationTasks = new List<Task>();

        var itemCount = allItems.Count;
        for (int i = 0; i < itemCount; i++)
        {
            if (i >= allItems.Count) break;
            
            int targetPosition = i + 1;
            var targetY = CalculatePositionForIndex(targetPosition);
            
            // Check if already at target position
            var currentY = allItems[i].Margin.Top;
            if (Math.Abs(currentY - targetY) < 0.1)
            {
                allItems[i].ZIndex = targetPosition;
                continue;
            }

            // Calculate stagger based on focus index
            int staggerIndex = 0;
            if (focusIndex >= 0)
            {
                int focusElementIndex = focusIndex + 1;
                staggerIndex = (i != focusElementIndex) ? Math.Min(Math.Abs(i - focusElementIndex), 2) : 0;
            }
            else
            {
                staggerIndex = Math.Min(i, 3);
            }

            animationTasks.Add(AnimateElementToMarginPosition(i, targetPosition, staggerIndex));
        }

        if (animationTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(animationTasks);
                
                await Task.Delay(AnimationCompleteDelayMs);
            }
            catch (Exception)
            {
            }
        }

        areAnimationsActive = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreAnimationsActive)));
        UpdateDeleteButtonStates();
    }

    public void SetSelectedProfile(BE.ProfileModel? profile, bool updateViewModel = true)
    {
        if (selectedProfile == profile) return;

        var itemCount = allItems.Count;
        for (int i = 1; i < itemCount; i++)
        {
            if (i >= allItems.Count) break;
            
            allItems[i].Classes.Remove("Selected");
        }

        selectedProfile = profile;

        if (updateViewModel && DataContext is ProfileListViewModel viewModel)
        {
            viewModel.SelectedProfile = selectedProfile;
        }

        if (selectedProfile != null)
        {
            var currentIndex = profilesModel.Profiles.IndexOf(selectedProfile);
            if (currentIndex >= 0 && currentIndex < GetProfileCount())
            {
                int itemIndex = currentIndex + 1; // Convert to item index
                
                // Bounds check to prevent IndexOutOfRangeException
                if (itemIndex < allItems.Count)
                {
                    allItems[itemIndex].Classes.Add("Selected");
                }
            }
        }
    }

    public BE.ProfileModel? GetSelectedProfile()
    {
        return selectedProfile;
    }


    private void RefreshAllProfileNames()
    {
        for (int i = 0; i < GetProfileCount() && i < profilesModel.Profiles.Count; i++)
        {
            int itemIndex = i + 1; // Convert to item index
            
            // Bounds check to prevent IndexOutOfRangeException
            if (itemIndex >= allItems.Count) break;
            
            var border = allItems[itemIndex];
            var profile = profilesModel.Profiles[i];

            if (border.Child is Grid grid)
            {
                var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                if (textBlock != null)
                {
                    textBlock.Text = profile.CurrentNameForDisplay;
                }
            }
        }
    }

    public async Task ExpandElements()
    {
        // Small delay to ensure elements are rendered before animating
        await Task.Delay(ElementRenderDelayMs);
        
        await AnimateAllElementsToPositions(-1);
    }

    public async Task CollapseElements()
    {
        if (allItems.Count == 0) return;

        areAnimationsActive = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreAnimationsActive)));
        UpdateDeleteButtonStates();

        var animationTasks = new List<Task>();

        var itemCount = allItems.Count;
        for (int i = 0; i < itemCount; i++)
        {
            if (i >= allItems.Count) break;
            
            animationTasks.Add(CollapseElementToMarginPosition(i, i * CollapseStaggerDelayMs));
        }

        try
        {
            await Task.WhenAll(animationTasks);
            await Task.Delay(AnimationCompleteDelayMs);
        }
        finally
        {
            areAnimationsActive = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreAnimationsActive)));
            UpdateDeleteButtonStates();
        }
    }

    private async Task CollapseElementToMarginPosition(int elementIndex, int delayMs = 0)
    {
        if (elementIndex >= allItems.Count) return;

        var element = allItems[elementIndex];
        
        // Add animation class to enable CSS transitions
        element.Classes.Add("animate-position");
        
        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        element.Margin = new Thickness(8, 0, 8, ProfileSpacing);
    }

    public bool AreAnimationsActive => areAnimationsActive;

}