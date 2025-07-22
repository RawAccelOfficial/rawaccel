using Avalonia;
using Avalonia.Controls;
using System.Diagnostics.CodeAnalysis;

namespace userinterface.Extensions;

public static class ControlExtensions
{
    public static bool TryFindControl<T>(this Control element, string name, [NotNullWhen(true)] out T? control)
        where T : Control
    {
        control = element.FindControl<T>(name);
        return control != null;
    }
}