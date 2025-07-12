using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile
{
    public partial class ProfileListElementView : UserControl
    {
        public ProfileListElementView()
        {
            InitializeComponent();
            DeleteButton.Click += OnDeleteButtonClick;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Subscribe to selection changes once when DataContext is first set
            if (DataContext is ProfileListElementViewModel viewModel && !viewModel.HasViewSubscribed)
            {
                viewModel.SelectionChanged += OnSelectionChanged;
                viewModel.HasViewSubscribed = true;

                // Set initial selection state
                OnSelectionChanged(viewModel, viewModel.IsSelected);
            }
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
    }
}