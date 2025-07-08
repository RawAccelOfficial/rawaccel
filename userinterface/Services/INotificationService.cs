using System;
using userinterface.Models;

namespace userinterface.Services
{
    public interface INotificationService
    {
        void ShowToast(string message, ToastType type, int durationMs = 5000);
        void HideToast();

        // Convenience methods
        void ShowSuccessToast(string message, int durationMs = 5000);
        void ShowErrorToast(string message, int durationMs = 8000);
        void ShowWarningToast(string message, int durationMs = 6000);
        void ShowInfoToast(string message, int durationMs = 4000);

        event EventHandler<ToastNotificationEventArgs> ToastRequested;
        event EventHandler ToastDismissed;
    }

    public class ToastNotificationEventArgs : EventArgs
    {
        public string Message { get; set; }
        public ToastType Type { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
    }
}
