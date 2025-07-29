using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Display;

namespace userinterface.Services
{
    public class PreviewChartRenderer
    {
        private readonly ConcurrentDictionary<string, byte[]> bitmapCache = new();
        private const int ChartWidth = 400;
        private const int ChartHeight = 300;
        private const int MainStrokeThickness = 2;
        private const double ToleranceThreshold = 0.001;

        public async Task<byte[]> GenerateChartPreviewAsync(CurvePoint[] xPoints, CurvePoint[] yPoints, double yxRatio, string? xAxisName = null, string? yAxisName = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Generate cache key
                    var cacheKey = GenerateChartHash(xPoints, yPoints, yxRatio);
                    
                    // Check cache first
                    if (bitmapCache.TryGetValue(cacheKey, out var cachedBitmap))
                    {
                        return cachedBitmap;
                    }

                    // Create simplified series
                    var hasYCurve = Math.Abs(yxRatio - 1.0) > ToleranceThreshold;
                    var seriesCount = hasYCurve ? 2 : 1;
                    var series = new ISeries[seriesCount];

                    // X series
                    series[0] = new LineSeries<CurvePoint>
                    {
                        Values = xPoints,
                        Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = MainStrokeThickness },
                        Fill = null,
                        GeometrySize = 0,
                        AnimationsSpeed = TimeSpan.Zero,
                        LineSmoothness = 0,
                        Name = "X Curve",
                        Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output)
                    };

                    // Y series if needed
                    if (hasYCurve)
                    {
                        series[1] = new LineSeries<CurvePoint>
                        {
                            Values = yPoints,
                            Stroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = MainStrokeThickness },
                            Fill = null,
                            GeometrySize = 0,
                            AnimationsSpeed = TimeSpan.Zero,
                            LineSmoothness = 0,
                            Name = "Y Curve",
                            Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output)
                        };
                    }

                    // Create headless chart
                    var chart = new SKCartesianChart
                    {
                        Width = ChartWidth,
                        Height = ChartHeight,
                        Series = series,
                        XAxes = new[]
                        {
                            new Axis
                            {
                                Name = xAxisName ?? "Mouse Speed",
                                NameTextSize = 12,
                                NamePaint = new SolidColorPaint(SKColors.Black),
                                LabelsPaint = new SolidColorPaint(SKColors.DarkGray),
                                TextSize = 10,
                                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                                AnimationsSpeed = TimeSpan.Zero
                            }
                        },
                        YAxes = new[]
                        {
                            new Axis
                            {
                                Name = yAxisName ?? "Output",
                                NameTextSize = 12,
                                NamePaint = new SolidColorPaint(SKColors.Black),
                                LabelsPaint = new SolidColorPaint(SKColors.DarkGray),
                                TextSize = 10,
                                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                                AnimationsSpeed = TimeSpan.Zero
                            }
                        },
                        Background = SKColors.White
                    };

                    // Render to bitmap
                    using var image = chart.GetImage();
                    using var data = image.Encode(SKEncodedImageFormat.Png, 80);
                    var bitmapBytes = data.ToArray();

                    // Cache the result
                    bitmapCache.TryAdd(cacheKey, bitmapBytes);

                    return bitmapBytes;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CHART PREVIEW] Error generating preview: {ex.Message}");
                    return Array.Empty<byte>();
                }
            });
        }

        public string GenerateChartHash(CurvePoint[] xPoints, CurvePoint[] yPoints, double yxRatio)
        {
            // Simple hash without crypto to avoid loading System.Security.Cryptography on UI thread
            var hashCode = yxRatio.GetHashCode();
            
            // Sample a few key points for hash
            if (xPoints.Length > 0)
            {
                hashCode = HashCode.Combine(hashCode, xPoints[0].MouseSpeed, xPoints[0].Output);
                if (xPoints.Length > 1)
                {
                    var midIndex = xPoints.Length / 2;
                    hashCode = HashCode.Combine(hashCode, xPoints[midIndex].MouseSpeed, xPoints[midIndex].Output);
                    hashCode = HashCode.Combine(hashCode, xPoints[^1].MouseSpeed, xPoints[^1].Output);
                }
            }

            return hashCode.ToString("X8");
        }

        public void ClearCache()
        {
            bitmapCache.Clear();
        }

        public void InvalidateCache(string cacheKey)
        {
            bitmapCache.TryRemove(cacheKey, out _);
        }
    }
}