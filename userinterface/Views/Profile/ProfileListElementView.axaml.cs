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
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                EnsureSubscription(viewModel);
            }
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                viewModel.SelectionChanged -= OnSelectionChanged;
                viewModel.HasViewSubscribed = false;
            }
        }

        private void EnsureSubscription(ProfileListElementViewModel viewModel)
        {
            if (!viewModel.HasViewSubscribed)
            {
                viewModel.SelectionChanged += OnSelectionChanged;
                viewModel.HasViewSubscribed = true;
                OnSelectionChanged(viewModel, viewModel.IsSelected);
            }
            else
            {
                OnSelectionChanged(viewModel, viewModel.IsSelected);
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                EnsureSubscription(viewModel);
            }
        }

        private void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                viewModel.DeleteProfileCommand?.Execute(null);
            }
        }

        private void OnSelectionChanged(ProfileListElementViewModel viewModel, bool isSelected)
        {
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