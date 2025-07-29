using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;

namespace userinterface.Services
{
    public class FrameTimerService
    {
        private readonly Stopwatch frameStopwatch = new();
        private readonly DispatcherTimer frameTimer;
        private const double THRESHOLD_MS = 8.33; // Target 120fps, flag anything over ~8.5ms
        private bool isMonitoring = false;

        public FrameTimerService()
        {
            frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromTicks(83333) // ~8.33ms (120fps) for high refresh rate detection
            };
            frameTimer.Tick += OnFrameTick;
        }

        public void StartMonitoring(string context = "")
        {
            if (isMonitoring) return;
            
            isMonitoring = true;
            frameStopwatch.Restart();
            frameTimer.Start();
            Debug.WriteLine($"[FRAME TIMER] Started monitoring: {context}");
        }

        public void StopMonitoring(string context = "")
        {
            if (!isMonitoring) return;
            
            frameTimer.Stop();
            isMonitoring = false;
            Debug.WriteLine($"[FRAME TIMER] Stopped monitoring: {context}");
        }


        private void OnFrameTick(object? sender, EventArgs e)
        {
            if (!isMonitoring) return;

            var elapsed = frameStopwatch.ElapsedMilliseconds;
            if (elapsed >= THRESHOLD_MS)
            {
                Debug.WriteLine($"[FRAME TIMER] ⚠️ UI Thread blocked for {elapsed}ms - potential frame drop!");
            }
            
            frameStopwatch.Restart();
        }


        // Add a method to detect blocking during specific operations
        public void MonitorOperation(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"[OPERATION MONITOR] Starting: {operationName}");
            
            StartMonitoring($"Operation: {operationName}");
            
            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                StopMonitoring($"Operation: {operationName}");
                Debug.WriteLine($"[OPERATION MONITOR] Completed: {operationName} in {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}