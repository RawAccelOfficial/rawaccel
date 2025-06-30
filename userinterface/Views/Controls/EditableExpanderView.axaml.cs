using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Styles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace userinterface.Controls;

// TODO: Disable when ignore set to true (DeviceView)
public partial class EditableExpanderView : UserControl, INotifyPropertyChanged, IDisposable
{
    private const int HoverDelayMilliseconds = 50;
    private const int ChevronAnimationDurationMilliseconds = 100;
    private const double ExpandedChevronAngle = 90.0;
    private const double CollapsedChevronAngle = 0.0;

    private const string ExpandedClass = "Expanded";
    private const string SyncHoverClass = "SyncHover";

    public static readonly StyledProperty<object> HeaderProperty =
        AvaloniaProperty.Register<EditableExpanderView, object>(nameof(Header));

    public static readonly StyledProperty<object> ExpanderContentProperty =
        AvaloniaProperty.Register<EditableExpanderView, object>(nameof(ExpanderContent));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<EditableExpanderView, bool>(nameof(IsExpanded));

    private int _angle;
    private CancellationTokenSource? _hoverDelayCancellationTokenSource;
    private bool _isDisposed;

    // Cached controls for performance
    private NoInteractionButtonView? _cachedHeaderButton;
    private NoInteractionButtonView? _cachedContentButton;
    private PathIcon? _cachedExpandIcon;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public object Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object ExpanderContent
    {
        get => GetValue(ExpanderContentProperty);
        set => SetValue(ExpanderContentProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public int Angle
    {
        get => _angle;
        set => RaiseAndSetIfChanged(ref _angle, value);
    }

    public EditableExpanderView()
    {
        InitializeComponent();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool RaiseAndSetIfChanged<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private NoInteractionButtonView? GetHeaderButton()
    {
        return _cachedHeaderButton ??= this.FindControl<NoInteractionButtonView>("HeaderButton");
    }

    private NoInteractionButtonView? GetContentButton()
    {
        return _cachedContentButton ??= this.FindControl<NoInteractionButtonView>("ContentButton");
    }

    private PathIcon? GetExpandIcon()
    {
        return _cachedExpandIcon ??= this.FindControl<PathIcon>("ExpandIcon");
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs eventArgs)
    {
        IsExpanded = !IsExpanded;
        UpdateExpandedState();
    }

    private async void UpdateExpandedState()
    {
        var headerButton = GetHeaderButton();
        var contentButton = GetContentButton();
        var expandIcon = GetExpandIcon();

        if (headerButton == null || contentButton == null || expandIcon == null)
            return;

        contentButton.IsVisible = IsExpanded;

        if (IsExpanded)
        {
            headerButton.Classes.Add(ExpandedClass);
            await AnimateChevron(expandIcon, ExpandedChevronAngle);
        }
        else
        {
            headerButton.Classes.Remove(ExpandedClass);
            await AnimateChevron(expandIcon, CollapsedChevronAngle);
        }
    }

    private async Task AnimateChevron(PathIcon expandIcon, double targetAngle)
    {
        if (expandIcon.RenderTransform is not RotateTransform rotateTransform)
            return;

        var currentAngle = rotateTransform.Angle;
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(ChevronAnimationDurationMilliseconds),
            Easing = new CubicEaseInOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter
                        {
                            Property = RotateTransform.AngleProperty,
                            Value = currentAngle
                        }
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter
                        {
                            Property = RotateTransform.AngleProperty,
                            Value = targetAngle
                        }
                    }
                }
            }
        };

        await animation.RunAsync(expandIcon, CancellationToken.None);
        rotateTransform.Angle = targetAngle;
    }

    private void ApplyHoverEffect()
    {
        _hoverDelayCancellationTokenSource?.Cancel();
        _hoverDelayCancellationTokenSource = null;

        var headerButton = GetHeaderButton();
        var contentButton = GetContentButton();

        headerButton?.Classes.Add(SyncHoverClass);
        contentButton?.Classes.Add(SyncHoverClass);
    }

    private async void RemoveHoverEffectWithDelay()
    {
        _hoverDelayCancellationTokenSource?.Cancel();
        _hoverDelayCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(HoverDelayMilliseconds, _hoverDelayCancellationTokenSource.Token);

            var headerButton = GetHeaderButton();
            var contentButton = GetContentButton();

            headerButton?.Classes.Remove(SyncHoverClass);
            contentButton?.Classes.Remove(SyncHoverClass);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            _hoverDelayCancellationTokenSource?.Dispose();
            _hoverDelayCancellationTokenSource = null;
        }
    }

    private void HeaderButton_PointerEntered(object sender, PointerEventArgs pointerEventArgs)
    {
        ApplyHoverEffect();
    }

    private void HeaderButton_PointerExited(object sender, PointerEventArgs pointerEventArgs)
    {
        RemoveHoverEffectWithDelay();
    }

    private void ContentButton_PointerEntered(object sender, PointerEventArgs pointerEventArgs)
    {
        ApplyHoverEffect();
    }

    private void ContentButton_PointerExited(object sender, PointerEventArgs pointerEventArgs)
    {
        RemoveHoverEffectWithDelay();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _hoverDelayCancellationTokenSource?.Cancel();
            _hoverDelayCancellationTokenSource?.Dispose();
            _hoverDelayCancellationTokenSource = null;
        }

        _isDisposed = true;
    }
}

