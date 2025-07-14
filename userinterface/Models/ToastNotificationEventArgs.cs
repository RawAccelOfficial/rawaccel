using System;

namespace userinterface.Models
{
    public class ToastNotificationEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public ToastType Type { get; set; }
        public TimeSpan Duration { get; set; }
    }
}