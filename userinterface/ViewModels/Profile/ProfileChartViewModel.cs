using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Interfaces;
using userinterface.Services;
using userspace_backend.Display;
using userspace_backend.Model.EditableSettings;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Profile
{
    public partial class ProfileChartViewModel : ViewModelBase, IAsyncInitializable
    {
        // Animation settings
        private const int AnimationMilliseconds = 200;

        // Data fitting and bounds
        private const double DataPaddingRatio = 0.1;


        // Default chart limits when no data or centering
        private const int DefaultAxisRange = 50;

        private const int DefaultYRange = 1;
        private const int DefaultMaxX = 100;
        private const int DefaultMaxY = 2;

        // Line and stroke thickness
        private const int MainStrokeThickness = 2;

        private const int StandardStrokeThickness = 1;
        private const float SubStrokeThickness = 0.5f;

        // Color transparency values
        private const byte SubSeparatorAlpha = 100;

        private const byte TooltipBackgroundAlpha = 180;

        // Theme color resource keys
        private static readonly string AxisTitleBrush = "PrimaryTextBrush";
        private static readonly string AxisLabelsBrush = "SecondaryTextBrush";
        private static readonly string AxisSeparatorsBrush = "BorderBrush";
        private static readonly string TooltipBackgroundBrush = "CardBackgroundBrush";

        // Axis labeling and text - will be set by localization service
        private const int AxisNameTextSize = 14;
        private const int AxisTextSize = 12;

        public static readonly TimeSpan AnimationsTime = new(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: AnimationMilliseconds);

        private readonly IThemeService themeService;
        private readonly LocalizationService localizationService;
        private readonly PreviewChartRenderer previewRenderer;
        private BE.ProfileModel currentProfileModel = null!;
        
        // Cached paint objects to avoid recreation
        private SolidColorPaint? cachedXStroke;
        private SolidColorPaint? cachedYStroke;
        
        // Sync object for thread safety - single allocation
        private readonly object syncObject = new object();

        public ProfileChartViewModel(IThemeService themeService, LocalizationService localizationService, PreviewChartRenderer previewRenderer)
        {
            this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.previewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));

            RecreateAxesCommand = new RelayCommand(() => 
            {
                EnsureInteractiveChartLoaded();
                RecreateAxes();
            });
            FitToDataCommand = new RelayCommand(() => 
            {
                EnsureInteractiveChartLoaded();
                FitToData();
            });
        }

        public bool IsInitialized { get; private set; }

        public bool IsInitializing { get; private set; }
        
        public bool IsInteractiveMode { get; private set; } = false;
        
        public bool IsLoadingChart { get; private set; } = false;
        
        public double ChartOpacity { get; private set; } = 0.0;
        
        private bool hasUserInteracted = false;

        public object Sync => syncObject;

        private ICurvePreview XCurvePreview { get; set; } = null!;

        private ICurvePreview YCurvePreview { get; set; } = null!;

        private EditableSetting<double> YXRatio { get; set; } = null!;

        public void Initialize(BE.ProfileModel profileModel)
        {
            if (currentProfileModel == profileModel)
                return;

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            YXRatio.PropertyChanged += OnYXRatioChanged;
        }


        // ================================================================================================
        // INITIALIZATION & SETUP
        // ================================================================================================

        public Task InitializeAsync()
        {
            if (IsInitializing || IsInitialized || currentProfileModel == null)
                return Task.CompletedTask;

            IsInitializing = true;

            try
            {
                // Show skeleton loader immediately
                IsLoadingChart = true;
                OnPropertyChanged(nameof(IsLoadingChart));
                
                // Initialize interactive chart
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Small delay to show skeleton loader
                        await Task.Delay(100);
                        
                        // Initialize chart components on background thread
                        await Task.Run(() =>
                        {
                            Series.Clear();
                            CreateSeries();
                        });
                        
                        // UI updates must happen on UI thread
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            try
                            {
                                XAxes = CreateXAxes();
                                YAxes = CreateYAxes();
                                TooltipTextPaint = new SolidColorPaint(themeService.GetCachedColor(AxisTitleBrush));
                                TooltipBackgroundPaint = new SolidColorPaint(themeService.GetCachedColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha));
                                
                                // Subscribe to events
                                this.themeService.ThemeChanged += OnThemeChanged;
                                this.localizationService.PropertyChanged += OnLocalizationChanged;

                                // Notify UI of changes
                                OnPropertyChanged(nameof(XAxes));
                                OnPropertyChanged(nameof(YAxes));
                                OnPropertyChanged(nameof(TooltipTextPaint));
                                OnPropertyChanged(nameof(TooltipBackgroundPaint));
                                OnPropertyChanged(nameof(Series));
                                
                                // Transition to interactive mode
                                TransitionToInteractiveMode();
                                
                                IsInitialized = true;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CHART INIT] Error in UI thread: {ex.Message}");
                                
                                // Hide skeleton loader on error
                                IsLoadingChart = false;
                                OnPropertyChanged(nameof(IsLoadingChart));
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CHART INIT] Error in background initialization: {ex.Message}");
                        
                        // Hide skeleton loader on error
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            IsLoadingChart = false;
                            OnPropertyChanged(nameof(IsLoadingChart));
                        });
                    }
                });
                
                // Mark as initialized immediately (background chart loading continues)
                IsInitialized = true;
            }
            finally
            {
                IsInitializing = false;
            }
            
            return Task.CompletedTask;
        }


        private void EnsureInteractiveChartLoaded()
        {
            if (!IsInteractiveMode && !IsLoadingChart && !hasUserInteracted)
            {
                hasUserInteracted = true;
                // Force immediate transition to interactive mode
                _ = ForceInteractiveMode();
            }
        }

        private async Task ForceInteractiveMode()
        {
            if (IsInteractiveMode)
                return;

            IsLoadingChart = true;
            OnPropertyChanged(nameof(IsLoadingChart));

            // Load full resolution data for interactive use
            await Task.Run(() =>
            {
                Series.Clear();
                CreateFullResolutionSeries();
            });

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(Series));
                TransitionToInteractiveMode();
            });
        }

        private void CreateFullResolutionSeries()
        {
            // Use full resolution data for interactive chart
            var xPoints = XCurvePreview?.Points?.ToArray() ?? Array.Empty<CurvePoint>();
            var yPoints = YCurvePreview?.Points?.ToArray() ?? Array.Empty<CurvePoint>();

            // Initialize cached stroke objects
            if (cachedXStroke == null)
                cachedXStroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = MainStrokeThickness };
            if (cachedYStroke == null)
                cachedYStroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = MainStrokeThickness };
            
            // Optimize array allocation based on YX ratio
            var hasYCurve = YXRatio.CurrentValidatedValue != 1.0;
            var seriesArray = hasYCurve ? new ISeries[2] : new ISeries[1];
            
            seriesArray[0] = CreateOptimizedLineSeries(xPoints, cachedXStroke, "X Curve Profile", "X Output");
            
            if (hasYCurve)
            {
                seriesArray[1] = CreateOptimizedLineSeries(yPoints, cachedYStroke, "Y Curve Profile", "Y Output");
            }

            foreach (var series in seriesArray)
            {
                Series.Add(series);
            }
        }

        private async void TransitionToInteractiveMode()
        {
            // Hide skeleton loader and show interactive chart at 0 opacity
            IsLoadingChart = false;
            IsInteractiveMode = true;
            ChartOpacity = 0.0;
            
            OnPropertyChanged(nameof(IsLoadingChart));
            OnPropertyChanged(nameof(IsInteractiveMode));
            OnPropertyChanged(nameof(ChartOpacity));
            
            // Small delay to ensure chart is rendered
            await Task.Delay(100);
            
            // Fade in interactive chart
            ChartOpacity = 1.0;
            OnPropertyChanged(nameof(ChartOpacity));
        }

        public Task SwitchToProfileAsync(BE.ProfileModel profileModel)
        {
            if (currentProfileModel == profileModel && IsInitialized)
                return Task.CompletedTask;

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            YXRatio.PropertyChanged += OnYXRatioChanged;

            // Update chart data synchronously for instant response
            CreateSeries();

            return Task.CompletedTask;
        }

        public ObservableCollection<ISeries> Series { get; set; } = new ObservableCollection<ISeries>();

        public Axis[] XAxes { get; set; } = new Axis[] { new Axis { Name = "Loading...", MinLimit = 0, MaxLimit = 1 } };

        public Axis[] YAxes { get; set; } = new Axis[] { new Axis { Name = "Loading...", MinLimit = 0, MaxLimit = 1 } };

        public SolidColorPaint TooltipTextPaint { get; set; } = new SolidColorPaint(SKColors.Black);

        public SolidColorPaint TooltipBackgroundPaint { get; set; } = new SolidColorPaint(SKColors.White);

        public ICommand RecreateAxesCommand { get; }

        public ICommand FitToDataCommand { get; }

        // ================================================================================================
        // PUBLIC METHODS
        // ================================================================================================

        public void FitToData()
        {
            var allPoints = XCurvePreview.Points.ToList();

            if (YXRatio.CurrentValidatedValue != 1.0)
            {
                allPoints.AddRange(YCurvePreview.Points);
            }

            if (allPoints.Count == 0)
            {
                SetDefaultLimits();
                return;
            }

            var (minX, maxX, minY, maxY) = CalculateDataBounds(allPoints);
            if (maxY == minY)
            {
                SetCenteredLimits(minX, maxX, minY, maxY);
            }
            else
            {
                SetPaddedLimits(minX, maxX, minY, maxY);
            }
        }

        public void RecreateAxes(double? xMinLimit = null, double? xMaxLimit = null, double? yMinLimit = null, double? yMaxLimit = null)
        {
            XAxes = CreateXAxes(xMinLimit, xMaxLimit);
            YAxes = CreateYAxes(yMinLimit, yMaxLimit);

            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }

        // ================================================================================================
        // CLEANUP & DISPOSAL
        // ================================================================================================

        public void Dispose()
        {
            themeService.ThemeChanged -= OnThemeChanged;
            localizationService.PropertyChanged -= OnLocalizationChanged;
            if (YXRatio != null)
                YXRatio.PropertyChanged -= OnYXRatioChanged;
            
            // Dispose cached paint objects
            if (cachedXStroke != null)
            {
                cachedXStroke.Dispose();
                cachedXStroke = null;
            }
            if (cachedYStroke != null)
            {
                cachedYStroke.Dispose();
                cachedYStroke = null;
            }
            
            // Clear preview renderer cache for memory cleanup
            previewRenderer.ClearCache();
        }

        // ================================================================================================
        // CHART DATA MANAGEMENT
        // ================================================================================================

        private ISeries[] CreateSeriesData()
        {
            // Pre-calculate and cache data points
            var xPoints = XCurvePreview?.Points?.ToArray() ?? Array.Empty<CurvePoint>();
            var yPoints = YCurvePreview?.Points?.ToArray() ?? Array.Empty<CurvePoint>();

            // Reduce points for better performance
            var reducedXPoints = ReducePointsForPreview(xPoints, 64);
            var reducedYPoints = ReducePointsForPreview(yPoints, 64);

            // Initialize cached stroke objects
            if (cachedXStroke == null)
                cachedXStroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = MainStrokeThickness };
            if (cachedYStroke == null)
                cachedYStroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = MainStrokeThickness };
            
            // Optimize array allocation based on YX ratio
            var hasYCurve = YXRatio.CurrentValidatedValue != 1.0;
            var seriesArray = hasYCurve ? new ISeries[2] : new ISeries[1];
            
            seriesArray[0] = CreateOptimizedLineSeries(reducedXPoints, cachedXStroke, "X Curve Profile", "X Output");
            
            if (hasYCurve)
            {
                seriesArray[1] = CreateOptimizedLineSeries(reducedYPoints, cachedYStroke, "Y Curve Profile", "Y Output");
            }

            return seriesArray;
        }

        private CurvePoint[] ReducePointsForPreview(CurvePoint[] points, int targetCount = 64)
        {
            if (points.Length <= targetCount)
                return points;

            // Use uniform sampling for consistent performance
            var step = (double)points.Length / targetCount;
            var reducedPoints = new CurvePoint[targetCount];
            
            for (int i = 0; i < targetCount; i++)
            {
                var sourceIndex = (int)(i * step);
                if (sourceIndex >= points.Length)
                    sourceIndex = points.Length - 1;
                
                reducedPoints[i] = points[sourceIndex];
            }
            
            return reducedPoints;
        }

        private LineSeries<CurvePoint> CreateOptimizedLineSeries(CurvePoint[] points, SolidColorPaint stroke, string name, string outputLabel)
        {
            return new LineSeries<CurvePoint>
            {
                Values = points,
                Fill = null,
                Stroke = stroke,
                Mapping = (curvePoint, index) => new LiveChartsCore.Kernel.Coordinate(x: curvePoint.MouseSpeed, y: curvePoint.Output),
                GeometrySize = 0,
                GeometryStroke = null,
                GeometryFill = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(100), // Reduced animations for performance
                Name = name,
                LineSmoothness = 0, // Disable smoothing for better performance
                XToolTipLabelFormatter = (chartPoint) => $"Speed: {chartPoint.Coordinate.SecondaryValue:F2}",
                YToolTipLabelFormatter = (chartPoint) => $"{outputLabel}: {chartPoint.Coordinate.PrimaryValue:F2}"
            };
        }

        private void CreateSeries()
        {
            var series = CreateSeriesData();
            Series.Clear();
            foreach (var s in series)
            {
                Series.Add(s);
            }
        }

        // ================================================================================================
        // EVENT HANDLERS
        // ================================================================================================

        private void OnYXRatioChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditableSetting<double>.CurrentValidatedValue))
            {
                CreateSeries();
                OnPropertyChanged(nameof(Series));
            }
        }

        // ================================================================================================
        // CHART AXES CREATION
        // ================================================================================================

        private Axis[] CreateXAxes(double? minLimit = null, double? maxLimit = null)
        {
            var axisName = localizationService?.GetText("ChartAxisMouseSpeed") ?? "Mouse Speed";
            var titleColor = themeService.GetCachedColor(AxisTitleBrush);
            var labelColor = themeService.GetCachedColor(AxisLabelsBrush);
            var separatorColor = themeService.GetCachedColor(AxisSeparatorsBrush);

            return new Axis[]
            {
                new Axis()
                {
                    Name = axisName,
                    NameTextSize = AxisNameTextSize,
                    NamePaint = new SolidColorPaint(titleColor),
                    LabelsPaint = new SolidColorPaint(labelColor),
                    TextSize = AxisTextSize,
                    SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = StandardStrokeThickness },
                    TicksPaint = new SolidColorPaint(titleColor) { StrokeThickness = StandardStrokeThickness },
                    SubseparatorsPaint = new SolidColorPaint(separatorColor.WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                    AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                    MinLimit = minLimit ?? 0,
                    MaxLimit = maxLimit
                }
            };
        }

        private Axis[] CreateYAxes(double? minLimit = null, double? maxLimit = null)
        {
            var axisName = localizationService?.GetText("ChartAxisOutput") ?? "Output";
            var titleColor = themeService.GetCachedColor(AxisTitleBrush);
            var labelColor = themeService.GetCachedColor(AxisLabelsBrush);
            var separatorColor = themeService.GetCachedColor(AxisSeparatorsBrush);

            return new Axis[]
            {
                new Axis()
                {
                    Name = axisName,
                    NameTextSize = AxisNameTextSize,
                    NamePaint = new SolidColorPaint(titleColor),
                    LabelsPaint = new SolidColorPaint(labelColor),
                    TextSize = AxisTextSize,
                    SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = StandardStrokeThickness },
                    TicksPaint = new SolidColorPaint(titleColor) { StrokeThickness = StandardStrokeThickness },
                    SubseparatorsPaint = new SolidColorPaint(separatorColor.WithAlpha(SubSeparatorAlpha)) { StrokeThickness = SubStrokeThickness },
                    AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                    MinLimit = minLimit ?? 0,
                    MaxLimit = maxLimit
                }
            };
        }


        // ================================================================================================
        // AXIS LIMITS MANAGEMENT
        // ================================================================================================

        private void SetDefaultLimits()
        {
            XAxes[0].MinLimit = 0;
            XAxes[0].MaxLimit = DefaultMaxX;
            YAxes[0].MinLimit = 0;
            YAxes[0].MaxLimit = DefaultMaxY;
        }

        private static (double minX, double maxX, double minY, double maxY) CalculateDataBounds(System.Collections.Generic.List<CurvePoint> points)
        {
            var minX = points.Min(p => p.MouseSpeed);
            var maxX = points.Max(p => p.MouseSpeed);
            var minY = points.Min(p => p.Output);
            var maxY = points.Max(p => p.Output);
            return (minX, maxX, minY, maxY);
        }

        private void SetCenteredLimits(double minX, double maxX, double minY, double maxY)
        {
            var centerY = (minY + maxY) / 2;
            var centerX = (minX + maxX) / 2;
            YAxes[0].MinLimit = Math.Max(0, centerY - DefaultYRange);
            YAxes[0].MaxLimit = centerY + DefaultYRange;
            XAxes[0].MinLimit = Math.Max(0, centerX - DefaultAxisRange);
            XAxes[0].MaxLimit = centerX + DefaultAxisRange;
        }

        private void SetPaddedLimits(double minX, double maxX, double minY, double maxY)
        {
            var xRange = maxX - minX;
            var yRange = maxY - minY;
            var xPadding = xRange * DataPaddingRatio;
            var yPadding = yRange * DataPaddingRatio;
            XAxes[0].MinLimit = Math.Max(0, minX - xPadding);
            XAxes[0].MaxLimit = maxX + xPadding;
            YAxes[0].MinLimit = Math.Max(0, minY - yPadding);
            YAxes[0].MaxLimit = maxY + yPadding;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            TooltipTextPaint.Color = themeService.GetCachedColor(AxisTitleBrush);
            TooltipBackgroundPaint.Color = themeService.GetCachedColor(TooltipBackgroundBrush).WithAlpha(TooltipBackgroundAlpha);

            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);

            // Notify tooltip property changes
            OnPropertyChanged(nameof(TooltipTextPaint));
            OnPropertyChanged(nameof(TooltipBackgroundPaint));
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Recreate axes with current limits but updated localized names
            var currentXMin = XAxes?[0]?.MinLimit;
            var currentXMax = XAxes?[0]?.MaxLimit;
            var currentYMin = YAxes?[0]?.MinLimit;
            var currentYMax = YAxes?[0]?.MaxLimit;

            RecreateAxes(currentXMin, currentXMax, currentYMin, currentYMax);
        }
    }
}