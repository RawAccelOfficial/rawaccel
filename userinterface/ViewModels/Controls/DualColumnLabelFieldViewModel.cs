using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using userinterface.Services;
using BE = userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels.Controls;

public class DualColumnLabelFieldViewModel : INotifyPropertyChanged
{
    private const double DefaultLabelWidth = 120.0;
    private double labelWidth = DefaultLabelWidth;
    private readonly LocalizationService localizationService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double LabelWidth
    {
        get => labelWidth;
        set => SetProperty(ref labelWidth, value);
    }

    public ObservableCollection<FieldItemViewModel> Fields { get; }

    public DualColumnLabelFieldViewModel(LocalizationService localizationService)
    {
        Fields = [];
        this.localizationService = localizationService;

        // Subscribe to language changes to update field labels
        if (localizationService != null)
        {
            localizationService.PropertyChanged += OnLanguageChanged;
        }
    }

    public void AddField(BE.IEditableSetting setting, object inputControl)
    {
        if (setting == null || inputControl == null)
            return;

        var fieldItem = new FieldItemViewModel(setting, inputControl, localizationService);
        Fields.Add(fieldItem);
    }

    public void AddField(string localizationKey, object inputControl)
    {
        if (string.IsNullOrWhiteSpace(localizationKey) || inputControl == null)
            return;

        var fieldItem = new FieldItemViewModel(localizationKey, inputControl, localizationService);
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


    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == LocalizationService.LanguageChangedPropertyName)
        {
            // Notify all FieldItemViewModel instances to update their labels
            foreach (var field in Fields)
            {
                field.UpdateLabel();
            }
        }
    }
}

public class FieldItemViewModel : INotifyPropertyChanged
{
    private readonly BE.IEditableSetting? setting;
    private readonly string? localizationKey;
    private readonly LocalizationService localizationService;
    private string cachedLabel;

    // Constructor for EditableSetting
    public FieldItemViewModel(BE.IEditableSetting setting, object inputControl, LocalizationService localizationService)
    {
        this.setting = setting ?? throw new ArgumentNullException(nameof(setting));
        InputControl = inputControl ?? throw new ArgumentNullException(nameof(inputControl));
        this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        cachedLabel = GetLocalizedLabel();
    }

    // Constructor for localization key
    public FieldItemViewModel(string localizationKey, object inputControl, LocalizationService localizationService)
    {
        this.localizationKey = localizationKey ?? throw new ArgumentNullException(nameof(localizationKey));
        InputControl = inputControl ?? throw new ArgumentNullException(nameof(inputControl));
        this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        cachedLabel = GetLocalizedLabel();
    }

    public string Label => cachedLabel;
    public object InputControl { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdateLabel()
    {
        var newLabel = GetLocalizedLabel();
        if (cachedLabel != newLabel)
        {
            cachedLabel = newLabel;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
        }
    }

    private string GetLocalizedLabel()
    {
        // If we have a direct localization key, use it
        if (!string.IsNullOrEmpty(localizationKey))
        {
            return localizationService?.GetText(localizationKey) ?? localizationKey;
        }

        // If we have an EditableSetting with a localization key, use it
        if (setting != null && !string.IsNullOrEmpty(setting.LocalizationKey))
        {
            return localizationService?.GetText(setting.LocalizationKey) ?? setting.DisplayText;
        }

        // Otherwise, use the display name directly (for user input settings)
        return setting?.DisplayText ?? string.Empty;
    }
};