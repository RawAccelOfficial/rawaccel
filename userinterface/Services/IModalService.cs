using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace userinterface.Services
{
    public interface IModalService : IDisposable
    {
        Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");
        Task ShowMessageAsync(string title, string message, string okText = "OK");
        Task<T?> ShowDialogAsync<T>(UserControl dialogContent, string title = "");
        void CloseCurrentModal();
    }
}