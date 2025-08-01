using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace userinterface.Services
{
    public interface IModalService : IDisposable
    {
        Task<bool> ShowConfirmationAsync(string titleKey, string messageKey, string confirmTextKey = "ModalOK", string cancelTextKey = "ModalCancel");

        Task ShowMessageAsync(string titleKey, string messageKey, string okTextKey = "ModalOK");

        Task<T?> ShowDialogAsync<T>(UserControl dialogContent, string titleKey = "");

        void CloseCurrentModal();
    }
}