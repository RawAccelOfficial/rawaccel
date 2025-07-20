using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using userinterface.Views.Controls;
using userinterface.Views;

namespace userinterface.Services
{
    public class ModalService : IModalService
    {
        private Control? currentModalContent;
        private TaskCompletionSource<bool>? currentConfirmationTask;
        private TaskCompletionSource<object?>? currentDialogTask;

        private ModalOverlay? GetModalOverlay()
        {
            // Get the main window and its modal overlay
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow as MainWindow;
                return mainWindow?.FindControl<ModalOverlay>("ModalOverlay");
            }
            return null;
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
        {
            var modalOverlay = GetModalOverlay();
            if (modalOverlay == null) return false;

            if (currentModalContent != null)
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

                modalOverlay.BackgroundClicked += () =>
                {
                    if (!currentConfirmationTask!.Task.IsCompleted)
                    {
                        currentConfirmationTask.SetResult(false);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = confirmationDialog;
                modalOverlay.ShowModal(confirmationDialog);
            });

            return await currentConfirmationTask.Task;
        }

        public async Task ShowMessageAsync(string title, string message, string okText = "OK")
        {
            var modalOverlay = GetModalOverlay();
            if (modalOverlay == null) return;

            if (currentModalContent != null)
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

                messageDialog.OkClicked += () =>
                {
                    messageTask.SetResult(true);
                    CloseCurrentModal();
                };

                modalOverlay.BackgroundClicked += () =>
                {
                    if (!messageTask.Task.IsCompleted)
                    {
                        messageTask.SetResult(true);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = messageDialog;
                modalOverlay.ShowModal(messageDialog);
            });

            await messageTask.Task;
        }

        public async Task<T?> ShowDialogAsync<T>(UserControl dialogContent, string title = "")
        {
            var modalOverlay = GetModalOverlay();
            if (modalOverlay == null) return default(T);

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            currentDialogTask = new TaskCompletionSource<object?>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                modalOverlay.BackgroundClicked += () =>
                {
                    if (!currentDialogTask!.Task.IsCompleted)
                    {
                        currentDialogTask.SetResult(null);
                        CloseCurrentModal();
                    }
                };

                currentModalContent = dialogContent;
                modalOverlay.ShowModal(dialogContent);
            });

            var result = await currentDialogTask.Task;
            return result is T typedResult ? typedResult : default(T);
        }

        public void CloseCurrentModal()
        {
            var modalOverlay = GetModalOverlay();
            if (modalOverlay != null && currentModalContent != null)
            {
                modalOverlay.HideModal();
                currentModalContent = null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
