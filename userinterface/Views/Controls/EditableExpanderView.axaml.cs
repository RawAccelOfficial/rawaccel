using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace userinterface.Controls
{
    public partial class EditableExpanderView : UserControl, INotifyPropertyChanged
    {
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<EditableExpanderView, object>(nameof(Header));

        public static readonly StyledProperty<object> ExpanderContentProperty =
            AvaloniaProperty.Register<EditableExpanderView, object>(nameof(ExpanderContent));

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<EditableExpanderView, bool>(nameof(IsExpanded));

        private int _angle;
        private CancellationTokenSource? _hoverDelayCancellationTokenSource;

        // Remove flicker caused by two buttons
        private const int HoverDelayMs = 50;

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

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
            UpdateExpandedState();
        }

        private async void UpdateExpandedState()
        {
            var headerButton = this.FindControl<NoInteractionButtonView>("HeaderButton");
            var contentButton = this.FindControl<NoInteractionButtonView>("ContentButton");
            var expandIcon = this.FindControl<PathIcon>("ExpandIcon");

            if (headerButton != null && contentButton != null && expandIcon != null)
            {
                contentButton.IsVisible = IsExpanded;
                if (IsExpanded)
                {
                    headerButton.Classes.Add("Expanded");
                    await AnimateChevron(expandIcon, 90);
                }
                else
                {
                    headerButton.Classes.Remove("Expanded");
                    await AnimateChevron(expandIcon, 0);
                }
            }
        }

        private async System.Threading.Tasks.Task AnimateChevron(PathIcon expandIcon, double targetAngle)
        {
            if (expandIcon.RenderTransform is RotateTransform rotateTransform)
            {
                var currentAngle = rotateTransform.Angle;
                var animation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200),
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
        }

        private void ApplyHoverEffect()
        {
            _hoverDelayCancellationTokenSource?.Cancel();
            _hoverDelayCancellationTokenSource = null;

            var headerButton = this.FindControl<NoInteractionButtonView>("HeaderButton");
            var contentButton = this.FindControl<NoInteractionButtonView>("ContentButton");
            if (headerButton != null)
            {
                headerButton.Classes.Add("SyncHover");
            }
            if (contentButton != null)
            {
                contentButton.Classes.Add("SyncHover");
            }
        }

        private async void RemoveHoverEffectWithDelay()
        {
            _hoverDelayCancellationTokenSource?.Cancel();
            _hoverDelayCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(HoverDelayMs, _hoverDelayCancellationTokenSource.Token);

                var headerButton = this.FindControl<NoInteractionButtonView>("HeaderButton");
                var contentButton = this.FindControl<NoInteractionButtonView>("ContentButton");
                if (headerButton != null)
                {
                    headerButton.Classes.Remove("SyncHover");
                }
                if (contentButton != null)
                {
                    contentButton.Classes.Remove("SyncHover");
                }
            }
            catch (OperationCanceledException)
            {
                // Do nothing
            }
            finally
            {
                _hoverDelayCancellationTokenSource?.Dispose();
                _hoverDelayCancellationTokenSource = null;
            }
        }

        private void HeaderButton_PointerEntered(object sender, PointerEventArgs e)
        {
            ApplyHoverEffect();
        }

        private void HeaderButton_PointerExited(object sender, PointerEventArgs e)
        {
            RemoveHoverEffectWithDelay();
        }

        private void ContentButton_PointerEntered(object sender, PointerEventArgs e)
        {
            ApplyHoverEffect();
        }

        private void ContentButton_PointerExited(object sender, PointerEventArgs e)
        {
            RemoveHoverEffectWithDelay();
        }
    }
}
