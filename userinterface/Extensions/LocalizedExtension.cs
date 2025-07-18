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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    private ResourceManager GetResourceManagerForKey(string key)
    {
        return key.ToUpper() switch
        {
            var k when k.StartsWith("MW_") => Properties.Resources.MainWindow.ResourceManager,
            var k when k.StartsWith("ST_") => Properties.Resources.Settings.ResourceManager,
            var k when k.StartsWith("DE_") => Properties.Resources.Device.ResourceManager,
            var k when k.StartsWith("MA_") => Properties.Resources.Mapping.ResourceManager,
            var k when k.StartsWith("PR_") => Properties.Resources.Profile.ResourceManager,
            var k when k.StartsWith("CT_") => Properties.Resources.Controls.ResourceManager,
            _ => Properties.Resources.MainWindow.ResourceManager // Default fallback
        };
    }

    private string GetActualKey(string key)
    {
        var parts = key.Split('_', 2);
        return parts.Length > 1 ? parts[1] : key;
    }
} 