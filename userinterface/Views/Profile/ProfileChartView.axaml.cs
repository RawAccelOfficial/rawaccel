using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using userinterface.Services;
using userinterface.ViewModels.Profile;

namespace userinterface.Views.Profile;

public partial class ProfileChartView : UserControl
{
    private bool isChartInitialized = false;
    private readonly FrameTimerService? frameTimer;

    public ProfileChartView()
    {
        try
        {
            frameTimer = App.Services?.GetRequiredService<FrameTimerService>();
        }
        catch
        {
            frameTimer = null;
        }
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (isChartInitialized || DataContext is not ProfileChartViewModel viewModel)
            return;

        Debug.WriteLine("[CHART INIT] ProfileChartView attached to visual tree");
        var stopwatch = Stopwatch.StartNew();
        
        isChartInitialized = true;
        
        // Monitor UI thread for blocking issues
        frameTimer?.StartMonitoring("ProfileChartView initialization");
        
        if (!viewModel.IsInitialized)
        {
            await viewModel.InitializeAsync();
        }
        
        // Stop monitoring after initialization completes
        _ = System.Threading.Tasks.Task.Delay(50).ContinueWith(_ => 
        {
            frameTimer?.StopMonitoring("ProfileChartView initialization");
        });
        
        Debug.WriteLine($"[CHART INIT] ProfileChartView initialization completed in {stopwatch.ElapsedMilliseconds}ms");
    }
}