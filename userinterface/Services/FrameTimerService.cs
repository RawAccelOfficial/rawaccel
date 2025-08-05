using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace userinterface.Services
{
    public class FrameTimerService
    {
        private readonly Stopwatch frameStopwatch = new();
        private readonly DispatcherTimer frameTimer;
        private readonly ILogger<FrameTimerService> logger;
        private const double THRESHOLD_MS = 8.33;
        private bool isMonitoring = false;

        public FrameTimerService(ILogger<FrameTimerService> logger)
        {
            this.logger = logger;
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
            logger.LogDebug("Started monitoring: {Context}", context);
        }

        public void StopMonitoring(string context = "")
        {
            if (!isMonitoring) return;
            
            frameTimer.Stop();
            isMonitoring = false;
            logger.LogDebug("Stopped monitoring: {Context}", context);
        }


        private void OnFrameTick(object? sender, EventArgs e)
        {
            if (!isMonitoring) return;

            var elapsed = frameStopwatch.ElapsedMilliseconds;
            if (elapsed >= THRESHOLD_MS)
            {
                logger.LogWarning("UI Thread blocked for {ElapsedMs}ms - potential frame drop!", elapsed);
            }
            
            frameStopwatch.Restart();
        }


        public void MonitorOperation(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogDebug("Starting operation: {OperationName}", operationName);
            
            StartMonitoring($"Operation: {operationName}");
            
            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                StopMonitoring($"Operation: {operationName}");
                logger.LogDebug("Completed operation: {OperationName} in {ElapsedMs}ms", operationName, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}