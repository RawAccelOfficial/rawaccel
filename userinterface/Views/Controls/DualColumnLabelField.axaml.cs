using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.ObjectModel;

namespace userinterface.Views.Controls;

public partial class DualColumnLabelField : UserControl
{
    public static readonly StyledProperty<double> LabelWidthProperty =
        AvaloniaProperty.Register<DualColumnLabelField, double>(nameof(LabelWidth), 100.0);

    public double LabelWidth
    {
        get => GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    public ObservableCollection<FieldItem> Fields { get; }

    public DualColumnLabelField()
    {
        Fields = new ObservableCollection<FieldItem>();
        InitializeComponent();
    }

    public DualColumnLabelField(params (string label, Control input)[] fields) : this()
    {
        foreach (var (label, input) in fields)
        {
            AddField(label, input);
        }
    }

    public void AddField(string label, Control input)
    {
        // Just ensure the input control stretches properly
        if (input is Control control)
        {
            control.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        var fieldItem = new FieldItem(label, input);
        Fields.Add(fieldItem);
    }

    public void RemoveField(int index)
    {
        if (index >= 0 && index < Fields.Count)
        {
            Fields.RemoveAt(index);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelWidthProperty)
        {
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        // Force layout update when properties change
        InvalidateArrange();
    }
}

public class FieldItem
{
    public string Label { get; }
    public Control Input { get; }

    public FieldItem(string label, Control input)
    {
        Label = label;
        Input = input;
    }
}
