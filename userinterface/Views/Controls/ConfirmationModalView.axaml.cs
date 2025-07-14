using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace userinterface.Views.Controls
{
    public partial class ConfirmationModalView : UserControl
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<ConfirmationModalView, string>(nameof(Title), "");

        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<ConfirmationModalView, string>(nameof(Message), "");

        public static readonly StyledProperty<string> ConfirmTextProperty =
            AvaloniaProperty.Register<ConfirmationModalView, string>(nameof(ConfirmText), "OK");

        public static readonly StyledProperty<string> CancelTextProperty =
            AvaloniaProperty.Register<ConfirmationModalView, string>(nameof(CancelText), "Cancel");

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public string ConfirmText
        {
            get => GetValue(ConfirmTextProperty);
            set => SetValue(ConfirmTextProperty, value);
        }

        public string CancelText
        {
            get => GetValue(CancelTextProperty);
            set => SetValue(CancelTextProperty, value);
        }

        public event Action? ConfirmClicked;

        public event Action? CancelClicked;

        public ConfirmationModalView()
        {
            InitializeComponent();
        }

        private void OnConfirmClick(object? sender, RoutedEventArgs e)
        {
            ConfirmClicked?.Invoke();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke();
        }
    }
}