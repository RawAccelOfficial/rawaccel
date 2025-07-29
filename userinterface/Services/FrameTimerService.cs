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
        private bool isRenderMonitoring = false;
        private readonly Stopwatch renderStopwatch = new();
        private readonly DispatcherTimer renderMonitorTimer;

        public FrameTimerService()
        {
            frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromTicks(83333) // ~8.33ms (120fps) for high refresh rate detection
            };
            frameTimer.Tick += OnFrameTick;

            // Use a high-priority dispatcher timer to monitor render performance
            renderMonitorTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromTicks(83333) // ~8.33ms (120fps)
            };
            renderMonitorTimer.Tick += OnRenderTick;
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

        public void StartRenderMonitoring(string context = "")
        {
            if (isRenderMonitoring) return;

            isRenderMonitoring = true;
            renderStopwatch.Restart();
            renderMonitorTimer.Start();
            Debug.WriteLine($"[RENDER MONITOR] Started render monitoring: {context}");
        }

        public void StopRenderMonitoring(string context = "")
        {
            if (!isRenderMonitoring) return;

            renderMonitorTimer.Stop();
            isRenderMonitoring = false;
            Debug.WriteLine($"[RENDER MONITOR] Stopped render monitoring: {context}");
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

        private void OnRenderTick(object? sender, EventArgs e)
        {
            if (!isRenderMonitoring) return;

            var elapsed = renderStopwatch.ElapsedMilliseconds;
            if (elapsed >= THRESHOLD_MS)
            {
                Debug.WriteLine($"[RENDER MONITOR] ⚠️ Render dispatch gap: {elapsed}ms - potential render blocking!");
            }
            
            renderStopwatch.Restart();
        }

        // Add a method to detect blocking during specific operations
        public void MonitorOperation(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.WriteLine($"[OPERATION MONITOR] Starting: {operationName}");
            
            StartMonitoring($"Operation: {operationName}");
            StartRenderMonitoring($"Operation: {operationName} render");
            
            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                StopMonitoring($"Operation: {operationName}");
                StopRenderMonitoring($"Operation: {operationName} render");
                Debug.WriteLine($"[OPERATION MONITOR] Completed: {operationName} in {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}