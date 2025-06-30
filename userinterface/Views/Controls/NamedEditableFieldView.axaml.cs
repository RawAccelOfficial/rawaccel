using Avalonia;
using Avalonia.Controls;

namespace userinterface.Views.Controls;

public partial class NamedEditableFieldView : UserControl
{
    public static readonly StyledProperty<string> FieldNameProperty =
        AvaloniaProperty.Register<NamedEditableFieldView, string>(nameof(FieldName));

    public string FieldName
    {
        get => GetValue(FieldNameProperty);
        set
        {
            SetValue(FieldNameProperty, value);
            UpdateFieldNameDisplay(value);
        }
    }

    public NamedEditableFieldView()
    {
        InitializeComponent();
    }

    private void UpdateFieldNameDisplay(string fieldName)
    {
        var fieldNameTextBlock = this.FindControl<TextBlock>("FieldNameTextBlock");
        fieldNameTextBlock?.SetValue(TextBlock.TextProperty, fieldName ?? string.Empty);
    }
}
