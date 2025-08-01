using Avalonia.Threading;
using System;
using System.Diagnostics;

namespace userinterface.Services
{
    // Service for monitoring UI thread blocking during performance-critical operations.
    // Usage:
    // - Call StartMonitoring("context") before performance-critical operations  
    // - Call StopMonitoring("context") after completion
    // - Use MonitorOperation("name", action) for automatic monitoring
    // 
    // To re-enable debug logging, uncomment Debug.WriteLine calls and add using System.Diagnostics;
    public class FrameTimerService
    {
        private readonly Stopwatch frameStopwatch = new();
        private readonly DispatcherTimer frameTimer;
        private const double THRESHOLD_MS = 8.33;
        private bool isMonitoring = false;

        public FrameTimerService()
        {
            frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromTicks(83333)
            };
            frameTimer.Tick += OnFrameTick;
        }

        public void StartMonitoring(string context = "")
        {
            if (isMonitoring) return;
            
            isMonitoring = true;
            frameStopwatch.Restart();
            frameTimer.Start();
            // Debug.WriteLine($"[FRAME TIMER] Started monitoring: {context}");
        }

        public void StopMonitoring(string context = "")
        {
            if (!isMonitoring) return;
            
            frameTimer.Stop();
            isMonitoring = false;
            // Debug.WriteLine($"[FRAME TIMER] Stopped monitoring: {context}");
        }


        private void OnFrameTick(object? sender, EventArgs e)
        {
            if (!isMonitoring) return;

            var elapsed = frameStopwatch.ElapsedMilliseconds;
            if (elapsed >= THRESHOLD_MS)
            {
                // Debug.WriteLine($"[FRAME TIMER] ⚠️ UI Thread blocked for {elapsed}ms - potential frame drop!");
            }
            
            frameStopwatch.Restart();
        }


        // Monitors operation execution time and detects UI thread blocking
        public void MonitorOperation(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            // Debug.WriteLine($"[OPERATION MONITOR] Starting: {operationName}");
            
            StartMonitoring($"Operation: {operationName}");
            
            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                StopMonitoring($"Operation: {operationName}");
                // Debug.WriteLine($"[OPERATION MONITOR] Completed: {operationName} in {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}