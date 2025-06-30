using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls;

public partial class EditableFieldView : UserControl
{
    public EditableFieldView()
    {
        InitializeComponent();
    }

    public void TextBox_KeyDown(object sender, KeyEventArgs keyEventArgs)
    {
        if (keyEventArgs.Key == Key.Return)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            topLevel?.FocusManager?.ClearFocus();
        }
    }

    public void LostFocusHandler(object sender, RoutedEventArgs routedEventArgs)
    {
        if (DataContext is EditableFieldViewModel editableFieldViewModel)
        {
            editableFieldViewModel.TrySetFromInterface();
        }
    }
}