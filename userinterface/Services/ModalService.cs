using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using userinterface.Views.Controls;

namespace userinterface.Services
{
    public class ModalService : IModalService
    {
        private Window? currentModal;
        private TaskCompletionSource<bool>? currentConfirmationTask;
        private TaskCompletionSource<object?>? currentDialogTask;

        public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
        {
            if (currentModal != null)
            {
                CloseCurrentModal();
            }

            currentConfirmationTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var confirmationDialog = new ConfirmationModalView
                {
                    Title = title,
                    Message = message,
                    ConfirmText = confirmText,
                    CancelText = cancelText
                };

                currentModal = new Window
                {
                    Title = title,
                    Content = confirmationDialog,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    ShowInTaskbar = false,
                    Topmost = true
                };

                confirmationDialog.ConfirmClicked += () =>
                {
                    currentConfirmationTask?.SetResult(true);
                    CloseCurrentModal();
                };

                confirmationDialog.CancelClicked += () =>
                {
                    currentConfirmationTask?.SetResult(false);
                    CloseCurrentModal();
                };

                currentModal.Closed += (sender, e) =>
                {
                    if (!currentConfirmationTask!.Task.IsCompleted)
                    {
                        currentConfirmationTask.SetResult(false);
                    }
                };

                currentModal.Show();
            });

            return await currentConfirmationTask.Task;
        }

        public async Task ShowMessageAsync(string title, string message, string okText = "OK")
        {
            if (currentModal != null)
            {
                CloseCurrentModal();
            }

            var messageTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var messageDialog = new MessageModalView
                {
                    Title = title,
                    Message = message,
                    OkText = okText
                };

                currentModal = new Window
                {
                    Title = title,
                    Content = messageDialog,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    ShowInTaskbar = false,
                    Topmost = true
                };

                messageDialog.OkClicked += () =>
                {
                    messageTask.SetResult(true);
                    CloseCurrentModal();
                };

                currentModal.Closed += (sender, e) =>
                {
                    if (!messageTask.Task.IsCompleted)
                    {
                        messageTask.SetResult(true);
                    }
                };

                currentModal.Show();
            });

            await messageTask.Task;
        }

        public async Task<T?> ShowDialogAsync<T>(UserControl dialogContent, string title = "")
        {
            if (currentModal != null)
            {
                CloseCurrentModal();
            }

            currentDialogTask = new TaskCompletionSource<object?>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                currentModal = new Window
                {
                    Title = title,
                    Content = dialogContent,
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = true,
                    ShowInTaskbar = false,
                    Topmost = true
                };

                currentModal.Closed += (sender, e) =>
                {
                    if (!currentDialogTask!.Task.IsCompleted)
                    {
                        currentDialogTask.SetResult(null);
                    }
                };

                currentModal.Show();
            });

            var result = await currentDialogTask.Task;
            return result is T typedResult ? typedResult : default(T);
        }

        public void CloseCurrentModal()
        {
            if (currentModal != null)
            {
                currentModal.Close();
                currentModal = null;
            }
        }

        public void Dispose()
        {
            CloseCurrentModal();
        }
    }
}