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
        private readonly LocalizationService localizationService;

        public ModalService(LocalizationService localizationService)
        {
            this.localizationService = localizationService;
        }

        private bool TryGetModalOverlay(out ModalOverlay modalOverlay)
        {
            modalOverlay = null!;
            
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow as MainWindow;
                var overlay = mainWindow?.FindControl<ModalOverlay>("ModalOverlay");
                if (overlay != null)
                {
                    modalOverlay = overlay;
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> ShowConfirmationAsync(string titleKey, string messageKey, string confirmTextKey = "ModalOK", string cancelTextKey = "ModalCancel")
        {
            if (!TryGetModalOverlay(out var modalOverlay)) return false;

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            currentConfirmationTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var confirmationDialog = new ConfirmationModalView
                {
                    Title = localizationService.GetText(titleKey),
                    Message = localizationService.GetText(messageKey),
                    ConfirmText = localizationService.GetText(confirmTextKey),
                    CancelText = localizationService.GetText(cancelTextKey)
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

        public async Task ShowMessageAsync(string titleKey, string messageKey, string okTextKey = "ModalOK")
        {
            if (!TryGetModalOverlay(out var modalOverlay)) return;

            if (currentModalContent != null)
            {
                CloseCurrentModal();
            }

            var messageTask = new TaskCompletionSource<bool>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var messageDialog = new MessageModalView
                {
                    Title = localizationService.GetText(titleKey),
                    Message = localizationService.GetText(messageKey),
                    OkText = localizationService.GetText(okTextKey)
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

        public async Task<T?> ShowDialogAsync<T>(UserControl dialogContent, string titleKey = "")
        {
            if (!TryGetModalOverlay(out var modalOverlay)) return default(T);

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
            if (TryGetModalOverlay(out var modalOverlay) && currentModalContent != null)
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
