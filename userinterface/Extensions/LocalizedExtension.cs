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
    private readonly ResourceManager _resourceManager;

    public LocalizedExtension(string key)
    {
        this.key = key;
        _resourceManager = GetResourceManagerForKey(key);

        // Subscribe to language changes from the DI service
        var localizationService = App.Services?.GetRequiredService<LocalizationService>();
        if (localizationService != null)
        {
            localizationService.PropertyChanged += OnLanguageChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Value => _resourceManager.GetString(GetActualKey(key)) ?? key;

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
        // Only respond to language change notifications, not all property changes
        if (e.PropertyName == LocalizationService.LanguageChangedPropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    private ResourceManager GetResourceManagerForKey(string key)
    {
        return Properties.Resources.Strings.ResourceManager;
    }

    private string GetActualKey(string key)
    {
        return key;
    }
} 