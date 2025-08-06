using System;
using System.Threading;
using userinterface.Models;

namespace userinterface.Services
{
    public class NotificationService : INotificationService
    {
        private Timer? timer;
        private readonly LocalizationService localizationService;
        private readonly ISettingsService settingsService;

        public NotificationService(LocalizationService localizationService, ISettingsService settingsService)
        {
            this.localizationService = localizationService;
            this.settingsService = settingsService;
        }

        public event EventHandler<ToastNotificationEventArgs>? ToastRequested;

        public event EventHandler? ToastDismissed;

        public void ShowToast(string messageKey, ToastType type, int durationMs = 5000)
        {
            ShowToast(messageKey, type, durationMs, new object[0]);
        }

        public void ShowToast(string messageKey, ToastType type, int durationMs = 5000, params object[] formatArgs)
        {
            if (!settingsService.ShowToastNotifications)
            {
                return;
            }

            timer?.Dispose();

            var localizedMessage = localizationService.GetText(messageKey);
            if (formatArgs.Length > 0)
            {
                localizedMessage = string.Format(localizedMessage, formatArgs);
            }

            ToastRequested?.Invoke(this, new ToastNotificationEventArgs
            {
                Message = localizedMessage,
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

        public void ShowSuccessToast(string messageKey, int durationMs = 5000)
        {
            ShowToast(messageKey, ToastType.Success, durationMs);
        }

        public void ShowSuccessToast(string messageKey, int durationMs = 5000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Success, durationMs, formatArgs);
        }

        public void ShowErrorToast(string messageKey, int durationMs = 8000)
        {
            ShowToast(messageKey, ToastType.Error, durationMs);
        }

        public void ShowErrorToast(string messageKey, int durationMs = 8000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Error, durationMs, formatArgs);
        }

        public void ShowWarningToast(string messageKey, int durationMs = 6000)
        {
            ShowToast(messageKey, ToastType.Warning, durationMs);
        }

        public void ShowWarningToast(string messageKey, int durationMs = 6000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Warning, durationMs, formatArgs);
        }

        public void ShowInfoToast(string messageKey, int durationMs = 4000)
        {
            ShowToast(messageKey, ToastType.Info, durationMs);
        }

        public void ShowInfoToast(string messageKey, int durationMs = 4000, params object[] formatArgs)
        {
            ShowToast(messageKey, ToastType.Info, durationMs, formatArgs);
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}