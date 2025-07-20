using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace userinterface.Views.Controls
{
    public partial class ModalOverlay : UserControl
    {
        public static readonly StyledProperty<bool> IsModalVisibleProperty =
            AvaloniaProperty.Register<ModalOverlay, bool>(nameof(IsModalVisible));

        public static readonly StyledProperty<Control?> ModalContentProperty =
            AvaloniaProperty.Register<ModalOverlay, Control?>(nameof(ModalContent));

        public bool IsModalVisible
        {
            get => GetValue(IsModalVisibleProperty);
            set => SetValue(IsModalVisibleProperty, value);
        }

        public Control? ModalContent
        {
            get => GetValue(ModalContentProperty);
            set => SetValue(ModalContentProperty, value);
        }

        public event Action? BackgroundClicked;

        public ModalOverlay()
        {
            InitializeComponent();
        }

        private void OnBackgroundPressed(object? sender, PointerPressedEventArgs e)
        {
            // Only close on background click, not modal content click
            if (e.Source == sender)
            {
                BackgroundClicked?.Invoke();
            }
        }

        public void ShowModal(Control content)
        {
            ModalContent = content;
            IsModalVisible = true;
        }

        public void HideModal()
        {
            IsModalVisible = false;
            ModalContent = null;
        }
    }
}
