using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using userinterface.Models;

namespace userinterface.Converters
{
    public class ToastTypeToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ToastType type)
            {
                return type switch
                {
                    ToastType.Success => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Green
                    ToastType.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),     // Red
                    ToastType.Warning => new SolidColorBrush(Color.FromRgb(255, 152, 0)),   // Orange
                    ToastType.Info => new SolidColorBrush(Color.FromRgb(33, 150, 243)),     // Blue
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))                  // Gray fallback
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}