using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
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
using userspace_backend.Driver;
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

        // Speed-line smoothing: a UI-thread timer eases the displayed line toward
        // the latest ~30 Hz poll target. TimeConstant = glide speed (larger is
        // smoother/laggier); SettleEpsilon = chart-unit "arrived" threshold.
        private const int TweenIntervalMs = 16;            // ~60 Hz
        private const double TweenTimeConstantMs = 60.0;
        private const double SpeedSettleEpsilon = 0.05;

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
        private readonly MouseSpeedPollingService speedPoller;
        private BE.IProfileModel currentProfileModel = null!;

        // Tween state: target* is the latest poll sample, disp* the eased position
        // rendered. tweenTimer pumps disp -> target and self-stops once settled.
        private DispatcherTimer? tweenTimer;
        private DateTime lastTweenTick;
        private double targetSpeedX, targetSpeedY, targetSpeedCombined;
        private double dispSpeedX, dispSpeedY, dispSpeedCombined;

        // Cached paint objects to avoid recreation
        private SolidColorPaint? cachedXStroke;
        private SolidColorPaint? cachedYStroke;

        // Active profile's "combine X and Y" flag: one (combined) or two (per-axis)
        // current-speed lines.
        private IEditableSettingSpecific<bool> CombineXY { get; set; } = null!;

        // Sync object for thread safety - single allocation
        private readonly object syncObject = new object();

        public ProfileChartViewModel(IThemeService themeService, LocalizationService localizationService, PreviewChartRenderer previewRenderer, MouseSpeedPollingService speedPoller)
        {
            this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            this.localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            this.previewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));
            this.speedPoller = speedPoller ?? throw new ArgumentNullException(nameof(speedPoller));

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
            ToggleSpeedLinesCommand = new RelayCommand(() => ShowSpeedLines = !ShowSpeedLines);
        }

        private bool showSpeedLines = true;

        // Whether the live current-speed line(s) are shown; toggled from the button bar.
        public bool ShowSpeedLines
        {
            get => showSpeedLines;
            set
            {
                if (showSpeedLines == value) return;
                showSpeedLines = value;
                OnPropertyChanged(nameof(ShowSpeedLines));
                OnPropertyChanged(nameof(SpeedLinesIconOpacity));
                if (showSpeedLines)
                {
                    // Reflect current positions immediately, then ease toward target.
                    RebuildSpeedSections();
                    EnsureTweenRunning();
                }
                else
                {
                    StopTween();
                    Sections = Array.Empty<RectangularSection>();
                    OnPropertyChanged(nameof(Sections));
                }
            }
        }

        // Dims the toggle button's icon when the lines are hidden.
        public double SpeedLinesIconOpacity => ShowSpeedLines ? 1.0 : 0.35;

        public bool IsInitialized { get; private set; }

        public bool IsInitializing { get; private set; }
        
        public bool IsInteractiveMode { get; private set; } = false;
        
        public bool IsLoadingChart { get; private set; } = false;
        
        public double ChartOpacity { get; private set; } = 0.0;
        
        private bool hasUserInteracted = false;

        public object Sync => syncObject;

        private ICurvePreview XCurvePreview { get; set; } = null!;

        private ICurvePreview YCurvePreview { get; set; } = null!;

        private IEditableSettingSpecific<double> YXRatio { get; set; } = null!;

        public void Initialize(BE.IProfileModel profileModel)
        {
            if (currentProfileModel == profileModel)
                return;

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            YXRatio.PropertyChanged += OnYXRatioChanged;

            CombineXY = profileModel.Acceleration.Anisotropy.CombineXYComponents;
            CombineXY.PropertyChanged += OnCombineXYChanged;
            RebuildSpeedSections();
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

            // Begin polling live mouse speed now that the chart is on screen.
            StartSpeedPollingIfPossible();
        }

        public Task SwitchToProfileAsync(BE.IProfileModel profileModel)
        {
            if (currentProfileModel == profileModel && IsInitialized)
                return Task.CompletedTask;

            if (YXRatio != null)
                YXRatio.PropertyChanged -= OnYXRatioChanged;
            if (CombineXY != null)
                CombineXY.PropertyChanged -= OnCombineXYChanged;

            currentProfileModel = profileModel;
            XCurvePreview = profileModel.XCurvePreview;
            YCurvePreview = profileModel.YCurvePreview;
            YXRatio = profileModel.YXRatio;

            YXRatio.PropertyChanged += OnYXRatioChanged;

            CombineXY = profileModel.Acceleration.Anisotropy.CombineXYComponents;
            CombineXY.PropertyChanged += OnCombineXYChanged;
            RebuildSpeedSections();

            // Update chart data synchronously for instant response
            CreateSeries();

            return Task.CompletedTask;
        }

        public ObservableCollection<ISeries> Series { get; set; } = new ObservableCollection<ISeries>();

        // Vertical current-speed line(s) bound to CartesianChart.Sections: one in
        // combined mode, two (X/Y) in separate. Reassigned fresh each update because
        // LiveCharts won't redraw a section mutated in place.
        public IEnumerable<RectangularSection> Sections { get; private set; } = Array.Empty<RectangularSection>();

        public Axis[] XAxes { get; set; } = new Axis[] { new Axis { Name = "Loading...", MinLimit = 0, MaxLimit = 1 } };

        public Axis[] YAxes { get; set; } = new Axis[] { new Axis { Name = "Loading...", MinLimit = 0, MaxLimit = 1 } };

        public SolidColorPaint TooltipTextPaint { get; set; } = new SolidColorPaint(SKColors.Black);

        public SolidColorPaint TooltipBackgroundPaint { get; set; } = new SolidColorPaint(SKColors.White);

        public ICommand RecreateAxesCommand { get; }

        public ICommand FitToDataCommand { get; }

        public ICommand ToggleSpeedLinesCommand { get; }

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
            if (CombineXY != null)
                CombineXY.PropertyChanged -= OnCombineXYChanged;

            // Stop and release the live-speed poller and its tween pump.
            speedPoller.Dispose();
            if (tweenTimer != null)
            {
                tweenTimer.Stop();
                tweenTimer.Tick -= OnTweenTick;
                tweenTimer = null;
            }

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
        // LIVE CURRENT-SPEED INDICATOR LINES
        // ================================================================================================

        // Vertical zero-width line (Xi == Xj) at the given speed; non-positive
        // speed -> NaN bounds, which render nothing. Fresh paint per call: a shared
        // one gets disposed by LiveCharts when its section is removed.
        private static RectangularSection MakeSpeedLine(double speed, SKColor color)
        {
            double x = speed > 0 ? speed : double.NaN;
            return new RectangularSection
            {
                Xi = x,
                Xj = x,
                Fill = null,
                Stroke = new SolidColorPaint(color) { StrokeThickness = MainStrokeThickness },
            };
        }

        // Builds the indicator line(s) for the sample and reassigns Sections.
        private void PublishSpeedSections(MouseSpeedSample sample)
        {
            if (!ShowSpeedLines)
            {
                Sections = Array.Empty<RectangularSection>();
                OnPropertyChanged(nameof(Sections));
                return;
            }

            bool combined = CombineXY?.CurrentValidatedValue ?? true;

            // X/Y lines match the curve colors; combined uses a neutral theme color.
            Sections = combined
                ? new[] { MakeSpeedLine(sample.Combined, themeService.GetCachedColor(AxisLabelsBrush)) }
                : new[]
                {
                    MakeSpeedLine(sample.X, SKColors.CornflowerBlue),
                    MakeSpeedLine(sample.Y, SKColors.OrangeRed),
                };
            OnPropertyChanged(nameof(Sections));
        }

        // Republishes section(s) at the current eased positions. Called on init and
        // when the combine-X/Y mode or show toggle changes.
        private void RebuildSpeedSections() =>
            PublishSpeedSections(new MouseSpeedSample(dispSpeedX, dispSpeedY, dispSpeedCombined));

        // Poller callback (UI thread): record the new target; the tween eases toward it.
        private void ApplySpeedSample(MouseSpeedSample sample)
        {
            targetSpeedX = sample.X;
            targetSpeedY = sample.Y;
            targetSpeedCombined = sample.Combined;
            EnsureTweenRunning();
        }

        // Starts the tween pump if there's anything to animate; no-op once settled.
        private void EnsureTweenRunning()
        {
            if (!IsInteractiveMode || !ShowSpeedLines) return;
            if (IsSpeedSettled()) return;

            if (tweenTimer == null)
            {
                tweenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TweenIntervalMs) };
                tweenTimer.Tick += OnTweenTick;
            }
            if (!tweenTimer.IsEnabled)
            {
                lastTweenTick = DateTime.UtcNow;
                tweenTimer.Start();
            }
        }

        private void StopTween() => tweenTimer?.Stop();

        // True when every axis' displayed position has effectively reached its target.
        private bool IsSpeedSettled() =>
            SpeedAxisSettled(dispSpeedX, targetSpeedX) &&
            SpeedAxisSettled(dispSpeedY, targetSpeedY) &&
            SpeedAxisSettled(dispSpeedCombined, targetSpeedCombined);

        private static bool SpeedAxisSettled(double disp, double target) =>
            Math.Abs(disp - target) < SpeedSettleEpsilon;

        // Frame-rate-independent exponential ease toward target; a fading line
        // (target <= 0) snaps to 0 so MakeSpeedLine hides it.
        private static double EaseSpeedAxis(double disp, double target, double alpha)
        {
            double next = disp + (target - disp) * alpha;
            if (target <= 0 && next < SpeedSettleEpsilon) next = 0;
            return next;
        }

        private void OnTweenTick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            double dtMs = (now - lastTweenTick).TotalMilliseconds;
            lastTweenTick = now;
            if (dtMs <= 0) dtMs = TweenIntervalMs;

            double alpha = 1.0 - Math.Exp(-dtMs / TweenTimeConstantMs);
            if (alpha < 0) alpha = 0;
            else if (alpha > 1) alpha = 1;

            dispSpeedX = EaseSpeedAxis(dispSpeedX, targetSpeedX, alpha);
            dispSpeedY = EaseSpeedAxis(dispSpeedY, targetSpeedY, alpha);
            dispSpeedCombined = EaseSpeedAxis(dispSpeedCombined, targetSpeedCombined, alpha);

            PublishSpeedSections(new MouseSpeedSample(dispSpeedX, dispSpeedY, dispSpeedCombined));

            if (IsSpeedSettled())
            {
                // Snap off residual sub-epsilon error, then idle until the next target.
                dispSpeedX = targetSpeedX;
                dispSpeedY = targetSpeedY;
                dispSpeedCombined = targetSpeedCombined;
                StopTween();
            }
        }

        private void StartSpeedPollingIfPossible()
        {
            if (IsInteractiveMode)
            {
                speedPoller.Start(ApplySpeedSample);
            }
        }

        // ================================================================================================
        // EVENT HANDLERS
        // ================================================================================================

        private void OnYXRatioChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEditableSettingSpecific<double>.CurrentValidatedValue))
            {
                CreateSeries();
                OnPropertyChanged(nameof(Series));
            }
        }

        private void OnCombineXYChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ModelValue is the observable property that raises change events;
            // CurrentValidatedValue is a plain getter that never notifies.
            if (e.PropertyName != nameof(IEditableSettingSpecific<bool>.ModelValue))
                return;

            Avalonia.Threading.Dispatcher.UIThread.Post(RebuildSpeedSections);
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
            // The combined-speed line reads its theme color fresh on each poll
            // (see PublishSpeedSections), so no paint update is needed here.

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