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

    public void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
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