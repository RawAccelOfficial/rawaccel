using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;
using System.Resources;
using userinterface.Services;
using Microsoft.Extensions.DependencyInjection;

namespace userinterface.Extensions;

public class LocalizedExtension : MarkupExtension, INotifyPropertyChanged
{
    private readonly string key;
    private readonly ResourceManager resourceManager;

    public LocalizedExtension(string key)
    {
        this.key = key;
        resourceManager = Properties.Resources.MainWindow.ResourceManager;

        // Subscribe to language changes from the DI service
        var localizationService = App.Services?.GetRequiredService<LocalizationService>();
        if (localizationService != null)
        {
            localizationService.PropertyChanged += OnLanguageChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Value => resourceManager.GetString(key) ?? key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = this,
            Path = nameof(Value),
            Mode = BindingMode.OneWay
        };
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}