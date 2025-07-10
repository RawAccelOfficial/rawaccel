using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using userinterface.Commands;

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

    public ICommand AddFieldCommand { get; }
    public ICommand AddTextFieldCommand { get; }
    public ICommand RemoveFieldCommand { get; }
    public ICommand ClearFieldsCommand { get; }
    public ICommand RegisterElementCommand { get; }
    public ICommand SetStackPanelCommand { get; }

    public DualColumnLabelFieldViewModel()
    {
        Fields = [];

        AddFieldCommand = new RelayCommand<(string label, object inputControl)>(
            param => AddField(param.label, param.inputControl));

        AddTextFieldCommand = new RelayCommand<(string label, string initialValue)>(
            param => AddTextField(param.label, param.initialValue ?? ""));

        RemoveFieldCommand = new RelayCommand<int>(
            index => RemoveField(index),
            index => index >= 0 && index < Fields.Count);

        ClearFieldsCommand = new RelayCommand(
            ClearFields);

        RegisterElementCommand = new RelayCommand<Control>(
            element => RegisterElement(element),
            element => element != null && TargetStackPanel != null);

        SetStackPanelCommand = new RelayCommand<StackPanel>(
            stackPanel => SetStackPanel(stackPanel),
            stackPanel => stackPanel != null);
    }

    public void SetStackPanel(StackPanel stackPanel)
    {
        TargetStackPanel = stackPanel;
        (RegisterElementCommand as RelayCommand<Control>)?.RaiseCanExecuteChanged();
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

        (RemoveFieldCommand as RelayCommand<int>)?.RaiseCanExecuteChanged();
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
            (RemoveFieldCommand as RelayCommand<int>)?.RaiseCanExecuteChanged();
        }
    }

    public void ClearFields()
    {
        Fields.Clear();
        (RemoveFieldCommand as RelayCommand<int>)?.RaiseCanExecuteChanged();
    }
}

public record FieldItemViewModel(string Label, object InputControl);