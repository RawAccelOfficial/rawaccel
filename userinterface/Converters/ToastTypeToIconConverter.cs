using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using userinterface.Models;

namespace userinterface.Converters
{
    public class ToastTypeToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ToastType toastType)
            {
                string resourceKey = toastType switch
                {
                    ToastType.Success => "checkmark_underline_circle_regular",
                    ToastType.Error => "error_circle_regular",
                    ToastType.Warning => "error_circle_regular",
                    ToastType.Info => "info_regular",
                    _ => "info_regular"
                };

                // Try to find the resource
                if (Application.Current?.TryFindResource(resourceKey, out var resource) == true)
                {
                    return resource;
                }
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}