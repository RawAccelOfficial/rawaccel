using Avalonia.Controls;
using System.Diagnostics;
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
        
        try
        {
            if (!viewModel.IsInitialized)
            {
                await viewModel.InitializeAsync();
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[CHART INIT] Error during initialization: {ex.Message}");
        }
    }
}