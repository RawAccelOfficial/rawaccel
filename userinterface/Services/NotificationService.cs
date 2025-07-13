using System;
using System.Threading;
using userinterface.Models;

namespace userinterface.Services
{
    public class NotificationService : INotificationService
    {
        private Timer? timer;

        public event EventHandler<ToastNotificationEventArgs>? ToastRequested;

        public event EventHandler? ToastDismissed;

        public void ShowToast(string message, ToastType type, int durationMs = 5000)
        {
            timer?.Dispose();

            ToastRequested?.Invoke(this, new ToastNotificationEventArgs
            {
                Message = message,
                Type = type,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            });

            timer = new Timer(state => HideToast(), null, durationMs, Timeout.Infinite);
        }

        public void HideToast()
        {
            timer?.Dispose();
            ToastDismissed?.Invoke(this, EventArgs.Empty);
        }

        // Convenience methods
        public void ShowSuccessToast(string message, int durationMs = 5000)
        {
            ShowToast(message, ToastType.Success, durationMs);
        }

        public void ShowErrorToast(string message, int durationMs = 8000)
        {
            ShowToast(message, ToastType.Error, durationMs);
        }

        public void ShowWarningToast(string message, int durationMs = 6000)
        {
            ShowToast(message, ToastType.Warning, durationMs);
        }

        public void ShowInfoToast(string message, int durationMs = 4000)
        {
            ShowToast(message, ToastType.Info, durationMs);
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}