using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile
{
    public partial class ProfileListElementView : UserControl
    {
        public ProfileListElementView()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            DeleteButton.Click += OnDeleteButtonClick;
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
