using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace userinterface.ViewModels.Controls;

public class DualColumnLabelFieldViewModel : INotifyPropertyChanged
{
    private const double DefaultLabelWidth = 120.0;
    private double labelWidth = DefaultLabelWidth;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double LabelWidth
    {
        get => labelWidth;
        set => SetProperty(ref labelWidth, value);
    }

    public ObservableCollection<FieldItemViewModel> Fields { get; }

    public DualColumnLabelFieldViewModel()
    {
        Fields = [];
    }

    public void AddField(string label, object inputControl)
    {
        if (string.IsNullOrWhiteSpace(label) || inputControl == null)
            return;

        var fieldItem = new FieldItemViewModel(label, inputControl);
        Fields.Add(fieldItem);
    }

    public void RemoveField(int index)
    {
        if (index >= 0 && index < Fields.Count)
        {
            Fields.RemoveAt(index);
        }
    }

    public void RemoveField(FieldItemViewModel field)
    {
        if (field != null)
        {
            Fields.Remove(field);
        }
    }

    public void ClearFields()
    {
        Fields.Clear();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public record FieldItemViewModel(string Label, object InputControl)
{
    public string Label { get; } = Label ?? throw new ArgumentNullException(nameof(Label));
    public object InputControl { get; } = InputControl ?? throw new ArgumentNullException(nameof(InputControl));
};