using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace userinterface.ViewModels.Controls;

public class DualColumnLabelFieldViewModel : ViewModelBase
{
    private const double DefaultLabelWidth = 100.0;
    private double LabelWidthValue = DefaultLabelWidth;
    private StackPanel? TargetStackPanel;

    public double LabelWidth
    {
        get => LabelWidthValue;
        set => SetProperty(ref LabelWidthValue, value);
    }

    public ObservableCollection<FieldItemViewModel> Fields { get; }

    public DualColumnLabelFieldViewModel()
    {
        Fields = [];
    }

    public void SetStackPanel(StackPanel stackPanel)
    {
        TargetStackPanel = stackPanel;
    }

    public void RegisterElement(Control element)
    {
        if (element == null || TargetStackPanel == null)
            return;

        TargetStackPanel.Children.Add(element);
    }

    public void AddField(string label, object inputControl)
    {
        if (string.IsNullOrWhiteSpace(label))
            return;

        var fieldItem = new FieldItemViewModel(label, inputControl);
        Fields.Add(fieldItem);
    }

    public void AddTextField(string label, string initialValue = "")
    {
        AddField(label, initialValue);
    }

    public void RemoveField(int index)
    {
        if (index >= 0 && index < Fields.Count)
        {
            Fields.RemoveAt(index);
        }
    }

    public void ClearFields() => Fields.Clear();
}

public record FieldItemViewModel(string Label, object InputControl);