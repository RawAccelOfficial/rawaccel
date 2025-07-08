using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Models;
using userinterface.Services;

namespace userinterface.ViewModels.Controls
{
    public class ToastViewModel : INotifyPropertyChanged
    {
        private readonly INotificationService notificationService;
        private bool isVisible;
        private string message;
        private ToastType type;
        private double progress = 100;
        private DispatcherTimer progressTimer;
        private DateTime animationStartTime;
        private TimeSpan animationDuration;

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

        private async void OnToastRequested(object sender, ToastNotificationEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Message = e.Message;
                Type = e.Type;
                IsVisible = true;
                Progress = 100;

                // Start the progress animation
                StartProgressAnimation(e.Duration);
            });
        }

        private void OnToastDismissed(object sender, EventArgs e)
        {
            // Stop the animation
            progressTimer?.Stop();

            Dispatcher.UIThread.Post(() =>
            {
                IsVisible = false;
                Progress = 0;
            });
        }

        private void StartProgressAnimation(TimeSpan duration)
        {
            // Stop any existing timer
            progressTimer?.Stop();

            // Store animation parameters
            animationStartTime = DateTime.Now;
            animationDuration = duration;

            // Create timer for smooth animation (60 FPS)
            progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };

            progressTimer.Tick += (sender, e) =>
            {
                var elapsed = DateTime.Now - animationStartTime;
                var progressRatio = elapsed.TotalMilliseconds / animationDuration.TotalMilliseconds;

                if (progressRatio >= 1.0)
                {
                    // Animation complete
                    Progress = 0;
                    progressTimer?.Stop();

                    // Auto-close when animation completes
                    if (IsVisible)
                    {
                        notificationService.HideToast();
                    }
                }
                else
                {
                    // Update progress (100% to 0%)
                    Progress = 100 * (1.0 - progressRatio);
                }
            };

            progressTimer.Start();
        }

        private void Close()
        {
            notificationService.HideToast();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
