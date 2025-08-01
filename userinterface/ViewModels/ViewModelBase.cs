using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Diagnostics;

namespace userinterface.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected static void LogTiming(string operation, Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        Debug.WriteLine($"[PERF] {operation}: {sw.ElapsedMilliseconds}ms");
    }
}