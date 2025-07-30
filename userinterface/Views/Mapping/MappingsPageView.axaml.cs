using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using userinterface.ViewModels.Mapping;

namespace userinterface.Views.Mapping;

public partial class MappingsPageView : UserControl
{
    private MappingsPageViewModel? viewModel;
    private int lastKnownItemCount = 0;
    private bool isInitialLoad = true;
    
    private const int StaggerDelayMs = 50;

    public MappingsPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ItemsRepeater.ElementPrepared += OnElementPrepared;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel != null)
        {
            viewModel.MappingViews.CollectionChanged -= OnMappingsCollectionChanged;
        }

        if (DataContext is MappingsPageViewModel vm)
        {
            viewModel = vm;
            lastKnownItemCount = vm.MappingViews.Count;
            vm.MappingViews.CollectionChanged += OnMappingsCollectionChanged;
            
            StartInitialLoadAnimation();
        }
    }

    private void OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is Grid container)
        {
            bool isNewItem = e.Index >= lastKnownItemCount && !isInitialLoad;
            
            if (isNewItem)
            {
                RevealElementWithDelay(container, 50);
            }
        }
    }

    private void OnMappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (viewModel != null)
                {
                    lastKnownItemCount = viewModel.MappingViews.Count;
                }
            }, DispatcherPriority.Background);
        }
        else if (viewModel != null)
        {
            lastKnownItemCount = viewModel.MappingViews.Count;
        }
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
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(StaggerDelayMs) };
        int index = 0;
        
        timer.Tick += (s, e) =>
        {
            var element = GetElementAtIndex(index);
            if (element != null)
            {
                RevealElement(element);
                index++;
            }
            else
            {
                timer.Stop();
            }
        };
        
        timer.Start();
    }

    private void RevealElementWithDelay(Grid element, int delayMs)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            RevealElement(element);
        };
        timer.Start();
    }

    private void RevealElement(Grid element)
    {
        element.Classes.Add("Visible");
    }

    private Grid? GetElementAtIndex(int index)
    {
        if (viewModel?.MappingViews != null && index < viewModel.MappingViews.Count)
        {
            for (int i = 0; i < ItemsRepeater.Children.Count; i++)
            {
                if (ItemsRepeater.Children[i] is Grid grid && 
                    grid.DataContext == viewModel.MappingViews[index])
                {
                    return grid;
                }
            }
        }
        return null;
    }
}