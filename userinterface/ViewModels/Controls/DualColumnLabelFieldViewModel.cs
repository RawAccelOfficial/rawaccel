using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace userinterface.ViewModels.Controls;

public class DualColumnLabelFieldViewModel : ViewModelBase
{
    private const double DefaultLabelWidth = 100.0;

    private double _labelWidth = DefaultLabelWidth;

    public double LabelWidth
    {
        get => _labelWidth;
        set => SetProperty(ref _labelWidth, value);
    }

    public ObservableCollection<FieldItemViewModel> Fields { get; }

    public DualColumnLabelFieldViewModel()
    {
        Fields = new ObservableCollection<FieldItemViewModel>();
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
