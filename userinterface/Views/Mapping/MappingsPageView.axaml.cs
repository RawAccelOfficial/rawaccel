using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using userinterface.ViewModels.Mapping;

namespace userinterface.Views.Mapping;

public partial class MappingsPageView : UserControl
{
    private MappingsPageViewModel? viewModel;
    private bool isInitialLoad = true;
    private DispatcherTimer? staggerTimer;
    
    private const int StaggerDelayMs = 50;
    private const int NewItemDelayMs = 50;

    public MappingsPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ItemsRepeater.ElementPrepared += OnElementPrepared;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        CleanupPreviousViewModel();

        if (DataContext is MappingsPageViewModel vm)
        {
            viewModel = vm;
            vm.MappingViews.CollectionChanged += OnMappingsCollectionChanged;
            
            StartInitialLoadAnimation();
        }
    }

    private void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is Grid container && !isInitialLoad)
        {
            RevealElementWithDelay(container, NewItemDelayMs);
        }
    }

    private void OnMappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // No additional logic needed - ElementPrepared handles new items
    }

    private void CleanupPreviousViewModel()
    {
        if (viewModel != null)
        {
            viewModel.MappingViews.CollectionChanged -= OnMappingsCollectionChanged;
        }
        
        CleanupTimer();
    }

    private void StartInitialLoadAnimation()
    {
        Dispatcher.UIThread.Post(() =>
        {
            isInitialLoad = false;
            RevealAllElementsStaggered();
        }, DispatcherPriority.Background);
    }

    private void RevealAllElementsStaggered()
    {
        CleanupTimer();
        
        staggerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(StaggerDelayMs) };
        int index = 0;
        int totalElements = viewModel?.MappingViews.Count ?? 0;
        
        staggerTimer.Tick += (s, e) =>
        {
            if (index < totalElements && index < ItemsRepeater.Children.Count)
            {
                if (ItemsRepeater.Children[index] is Grid element)
                {
                    RevealElement(element);
                }
                index++;
            }
            else
            {
                CleanupTimer();
            }
        };
        
        staggerTimer.Start();
    }

    private void RevealElementWithDelay(Grid element, int delayMs)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            timer = null;
            RevealElement(element);
        };
        timer.Start();
    }

    private void RevealElement(Grid element)
    {
        element.Classes.Add("Visible");
    }

    private void CleanupTimer()
    {
        if (staggerTimer != null)
        {
            staggerTimer.Stop();
            staggerTimer = null;
        }
    }
}