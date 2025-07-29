using Avalonia.Controls;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileChartView : UserControl
{
    private bool isChartInitialized = false;

    public ProfileChartView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (isChartInitialized || DataContext is not ProfileChartViewModel viewModel)
            return;

        isChartInitialized = true;
        
        if (!viewModel.IsInitialized)
        {
            await viewModel.InitializeAsync();
        }
    }
}