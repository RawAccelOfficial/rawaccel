using Avalonia;
using Avalonia.Controls;
using System.Diagnostics.CodeAnalysis;

namespace userinterface.Extensions;

public static class ControlExtensions
{
    public static bool TryFindControl<T>(this StyledElement element, string name, [NotNullWhen(true)] out T? control)
        where T : class
    {
        control = element.FindControl<T>(name);
        return control != null;
    }
}