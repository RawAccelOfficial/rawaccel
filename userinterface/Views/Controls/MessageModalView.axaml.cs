using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace userinterface.Views.Controls
{
    public partial class MessageModalView : UserControl
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MessageModalView, string>(nameof(Title), "");

        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<MessageModalView, string>(nameof(Message), "");

        public static readonly StyledProperty<string> OkTextProperty =
            AvaloniaProperty.Register<MessageModalView, string>(nameof(OkText), "OK");

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

        public string OkText
        {
            get => GetValue(OkTextProperty);
            set => SetValue(OkTextProperty, value);
        }

        public event Action? OkClicked;

        public MessageModalView()
        {
            InitializeComponent();
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            OkClicked?.Invoke();
        }
    }
}