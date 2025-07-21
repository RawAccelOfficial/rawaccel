using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using userinterface.Services;

namespace userinterface.Views.Controls
{
    public partial class AlphaBuildWarningView : UserControl
    {
        public AlphaBuildWarningView()
        {
            InitializeComponent();
        }

        private void OnBugReportLinkClicked(object? sender, RoutedEventArgs e)
        {
            App.OpenBugReportUrl();
        }

        private void OnDiscordLinkClicked(object? sender, RoutedEventArgs e)
        {
            App.OpenDiscordUrl();
        }

        private void OnUnderstandClicked(object? sender, RoutedEventArgs e)
        {
            var modalService = App.Services?.GetService<IModalService>();
            modalService?.CloseCurrentModal();
        }
    }
}