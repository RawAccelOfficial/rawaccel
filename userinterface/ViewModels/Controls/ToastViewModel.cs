using Avalonia.Animation;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Models;
using userinterface.Services;

namespace userinterface.ViewModels.Controls
{
    public class ToastViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly INotificationService notificationService;
        private bool isVisible;
        private string message = string.Empty;
        private ToastType type;
        private double progress = 100;
        private CancellationTokenSource? animationCancellation;

        public ToastViewModel(INotificationService notificationService)
        {
            this.notificationService = notificationService;
            this.notificationService.ToastRequested += OnToastRequested;
            this.notificationService.ToastDismissed += OnToastDismissed;
            CloseCommand = new RelayCommand(Close);
        }

        public bool IsVisible
        {
            get => isVisible;
            set
            {
                isVisible = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => message;
            set
            {
                message = value;
                OnPropertyChanged();
            }
        }

        public ToastType Type
        {
            get => type;
            set
            {
                type = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        public ICommand CloseCommand { get; }

        private async void OnToastRequested(object? sender, ToastNotificationEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                animationCancellation?.Cancel();

                Message = e.Message;
                Type = e.Type;
                IsVisible = true;
                Progress = 100;

                await StartProgressAnimation(e.Duration);
            });
        }

        private void OnToastDismissed(object? sender, EventArgs e)
        {
            animationCancellation?.Cancel();
            Dispatcher.UIThread.Post(() =>
            {
                IsVisible = false;
                Progress = 0;
            });
        }

        private async Task StartProgressAnimation(TimeSpan duration)
        {
            animationCancellation = new CancellationTokenSource();
            var token = animationCancellation.Token;

            try
            {
                var startTime = DateTime.UtcNow;
                var totalMilliseconds = duration.TotalMilliseconds;

                while (!token.IsCancellationRequested)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    var progressRatio = elapsed.TotalMilliseconds / totalMilliseconds;

                    if (progressRatio >= 1.0)
                    {
                        if (!token.IsCancellationRequested)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                Progress = 0;
                                if (IsVisible)
                                {
                                    notificationService.HideToast();
                                }
                            });
                        }
                        break;
                    }

                    var newProgress = 100 * (1.0 - progressRatio);
                    await Dispatcher.UIThread.InvokeAsync(() => Progress = newProgress);

                    await Task.Delay(8, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Close()
        {
            notificationService.HideToast();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            animationCancellation?.Cancel();
            animationCancellation?.Dispose();

            if (notificationService != null)
            {
                notificationService.ToastRequested -= OnToastRequested;
                notificationService.ToastDismissed -= OnToastDismissed;
            }
        }
    }
}