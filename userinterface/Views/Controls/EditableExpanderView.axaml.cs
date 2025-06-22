using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        private void UpdateExpandedState()
        {
            var headerButton = this.FindControl<NoInteractionButtonView>("HeaderButton");
            var contentButton = this.FindControl<NoInteractionButtonView>("ContentButton");

            if (headerButton != null && contentButton != null)
            {
                contentButton.IsVisible = IsExpanded;

                if (IsExpanded)
                {
                    headerButton.Classes.Add("Expanded");
                    Angle = 90;          
                }
                else
                {
                    headerButton.Classes.Remove("Expanded");
                    Angle = 0;
                }
            }
        }


        private void ApplyHoverEffect()
        {
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

        private void RemoveHoverEffect()
        {
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

        private void HeaderButton_PointerEntered(object sender, PointerEventArgs e)
        {
            ApplyHoverEffect();
        }

        private void HeaderButton_PointerExited(object sender, PointerEventArgs e)
        {
            RemoveHoverEffect();
        }

        private void ContentButton_PointerEntered(object sender, PointerEventArgs e)
        {
            ApplyHoverEffect();
        }

        private void ContentButton_PointerExited(object sender, PointerEventArgs e)
        {
            RemoveHoverEffect();
        }
    }
}
