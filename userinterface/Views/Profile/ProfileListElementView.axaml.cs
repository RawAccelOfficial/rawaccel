using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile
{
    public partial class ProfileListElementView : UserControl
    {
        // Store the previous ViewModel to unsubscribe from events without memory leaks
        private ProfileListElementViewModel? previousViewModel;

        public ProfileListElementView()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            DeleteButton.Click += OnDeleteButtonClick;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
        {
            // Removes the weird bug where the pressed animation carries over to the first element
            var listBoxItem = this.FindAncestorOfType<ListBoxItem>();
            if (listBoxItem != null)
            {
                listBoxItem.Classes.Add("StopAnimations");
            }

            if (DataContext is ProfileListElementViewModel viewModel)
            {
                viewModel.DeleteProfileCommand?.Execute(null);
            }

            if (listBoxItem != null)
            {
                listBoxItem.Classes.Remove("StopAnimations");
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Unsubscribe from previous ViewModel if it exists
            if (previousViewModel != null)
            {
                previousViewModel.SelectionChanged -= OnSelectionChanged;
                previousViewModel = null;
            }

            if (DataContext is ProfileListElementViewModel viewModel)
            {
                // Subscribe to the new ViewModel
                viewModel.SelectionChanged += OnSelectionChanged;
                // Store reference for cleanup
                previousViewModel = viewModel;
            }
        }

        private void OnSelectionChanged(ProfileListElementViewModel viewModel, bool isSelected)
        {
            System.Diagnostics.Debug.WriteLine($"Selection changed: {viewModel.CurrentNameForDisplay} is now {(isSelected ? "selected" : "deselected")}");
            var listBoxItem = this.FindLogicalAncestorOfType<ListBoxItem>();
            if (listBoxItem != null)
            {
                if (isSelected)
                {
                    listBoxItem.Classes.Add("CurrentlySelected");
                }
                else
                {
                    listBoxItem.Classes.Remove("CurrentlySelected");
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is ProfileListElementViewModel viewModel && viewModel.IsEditing)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        viewModel.StopEditing();
                        e.Handled = true;
                        break;

                    case Key.Escape:
                        viewModel.CancelEditing();
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}