using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
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
            AttachedToVisualTree += OnAttachedToVisualTree;
            DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // Ensure subscription when control is attached to visual tree
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"Control attached to visual tree, ensuring subscription for: {viewModel.CurrentNameForDisplay}");
                EnsureSubscription(viewModel);
            }
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // Clean up when detached
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"Control detached from visual tree, cleaning up subscription for: {viewModel.CurrentNameForDisplay}");
                viewModel.SelectionChanged -= OnSelectionChanged;
                viewModel.HasViewSubscribed = false;
            }
        }

        private void EnsureSubscription(ProfileListElementViewModel viewModel)
        {
            if (!viewModel.HasViewSubscribed)
            {
                System.Diagnostics.Debug.WriteLine($"Ensuring subscription for: {viewModel.CurrentNameForDisplay}");
                viewModel.SelectionChanged += OnSelectionChanged;
                viewModel.HasViewSubscribed = true;
                OnSelectionChanged(viewModel, viewModel.IsSelected);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Already subscribed for: {viewModel.CurrentNameForDisplay}");
                // Still force UI update to ensure sync
                OnSelectionChanged(viewModel, viewModel.IsSelected);
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DataContextChanged fired. DataContext type: {DataContext?.GetType().Name ?? "null"}");

            if (DataContext is ProfileListElementViewModel viewModel)
            {
                EnsureSubscription(viewModel);
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
            System.Diagnostics.Debug.WriteLine($"OnSelectionChanged called for: {viewModel.CurrentNameForDisplay}, isSelected: {isSelected}");

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
                System.Diagnostics.Debug.WriteLine($"Selection changed: {viewModel.CurrentNameForDisplay} is now {(isSelected ? "selected" : "deselected")}");
                System.Diagnostics.Debug.WriteLine(string.Join(",", listBoxItem.Classes.ToList()));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Could not find ListBoxItem for: {viewModel.CurrentNameForDisplay}");
            }
        }
    }
}