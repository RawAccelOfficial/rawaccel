using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile
{
    public partial class ProfileListElementView : UserControl
    {
        public ProfileListElementView()
        {
            InitializeComponent();

            // Handle key events for editing
            KeyDown += OnKeyDown;
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

        private void RenameProfile(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                viewModel.StartEditing();
            }
        }

        private void RemoveProfile(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileListElementViewModel viewModel)
            {
                viewModel.DeleteProfile();
            }
        }
    }
}
