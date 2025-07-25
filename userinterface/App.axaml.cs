using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Settings;
using userinterface.Views;
using userspace_backend;
using DATA = userspace_backend.Data;

namespace userinterface;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IModalService, ModalService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<LocalizationService>();

        // Register backend services
        services.AddSingleton<Bootstrapper>(provider => BootstrapBackEnd());
        services.AddSingleton<BackEnd>(provider =>
        {
            var bootstrapper = provider.GetRequiredService<Bootstrapper>();
            var backEnd = new BackEnd(bootstrapper);
            backEnd.Load();
            return backEnd;
        });

        RegisterViewModels(services);

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            var mainWindow = new MainWindow()
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };

            // Set up the toast control (was already created in MainWindow.axaml)
            var toastView = mainWindow.FindControl<Views.Controls.ToastView>("ToastView");
            if (toastView != null)
            {
                toastView.DataContext = Services.GetRequiredService<ToastViewModel>();
            }

            desktop.MainWindow = mainWindow;

            // Show alpha build warning modal
            _ = ShowAlphaBuildWarningAsync();
            
            // Warm up GPU pipeline for animations
            _ = WarmUpAnimationPipelineAsync();

#if DEBUG
            desktop.MainWindow.AttachDevTools();
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterViewModels(IServiceCollection services)
    {
        // Main ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ToastViewModel>();

        // Device ViewModels
        services.AddTransient<ViewModels.Device.DevicesPageViewModel>();
        services.AddTransient<ViewModels.Device.DevicesListViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupsViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupSelectorViewModel>();
        services.AddTransient<ViewModels.Device.DeviceViewModel>();

        // Profile ViewModels
        services.AddTransient<ViewModels.Profile.ProfilesPageViewModel>();
        services.AddSingleton<ViewModels.Profile.ProfileListViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileChartViewModel>();
        services.AddTransient<ViewModels.Profile.AccelerationFormulaSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.AccelerationLUTSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.AccelerationProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.AnisotropyProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.CoalescionProfileSettingsViewModel>();
        services.AddTransient<ViewModels.Profile.HiddenProfileSettingsViewModel>();

        // Mapping ViewModels
        services.AddTransient<ViewModels.Mapping.MappingsPageViewModel>();
        services.AddTransient<ViewModels.Mapping.MappingViewModel>();
        services.AddTransient<ViewModels.Mapping.MappingListElementViewModel>();

        // Settings ViewModels
        services.AddTransient<SettingsPageViewModel>();

        // Control ViewModels
        services.AddTransient<ViewModels.Controls.DualColumnLabelFieldViewModel>();
        services.AddTransient<ViewModels.Controls.EditableBoolViewModel>();
        services.AddTransient<ViewModels.Controls.EditableFieldViewModel>();
        services.AddTransient<ViewModels.Controls.NamedEditableFieldViewModel>();
    }

    protected static Bootstrapper BootstrapBackEnd()
    {
        return new Bootstrapper()
        {
            BackEndLoader = new BackEndLoader(System.AppDomain.CurrentDomain.BaseDirectory),
            DevicesToLoad =
            [
                new DATA.Device() { Name = "Superlight 2", DPI = 32000, HWID = @"HID\VID_046D&PID_C54D&MI_00", PollingRate = 1000, DeviceGroup = "Logitech Mice" },
                new DATA.Device() { Name = "Outset AX", DPI = 1200, HWID = @"HID\VID_3057&PID_0001", PollingRate = 1000, DeviceGroup = "Testing" },
                new DATA.Device() { Name = "Razer Viper 8K", DPI = 1200, HWID = @"HID\VID_31E3&PID_1310", PollingRate = 1000, DeviceGroup = "Testing" },
            ],
            ProfilesToLoad =
            [
                new DATA.Profile()
                {
                    Name = "Favorite", OutputDPI = 1600,
                    YXRatio = 1.333,
                    Acceleration = new DATA.Profiles.Accel.Formula.SynchronousAccel()
                    {
                        SyncSpeed = 25.85,
                        Motivity = 1.1333,
                        Gamma = 0.063,
                        Smoothness = 0.5,
                        Anisotropy = new DATA.Profiles.Anisotropy()
                        {
                            CombineXYComponents = false,
                            Domain = new DATA.Profiles.Vector2() { X = 1, Y = 4 },
                            Range = new DATA.Profiles.Vector2() { X = 1, Y = 1 },
                            LPNorm = 2,
                        },
                        Coalescion = new DATA.Profiles.Coalescion()
                        {
                            InputSmoothingHalfLife = 10,
                            ScaleSmoothingHalfLife = 0,
                        },
                    },
                    Hidden = new DATA.Profiles.Hidden() { RotationDegrees = 8, },
                },
                new DATA.Profile() { Name = "Test", OutputDPI = 1200, YXRatio = 1.0 },
                new DATA.Profile() { Name = "SpecificGame", OutputDPI = 3200, YXRatio = 1.333 },
            ],
            MappingsToLoad = new DATA.MappingSet()
            {
                Mappings =
                [
                    new DATA.Mapping() {
                        Name = "Usual",
                        GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping()
                        {
                            { "Logitech Mice", "Favorite" },
                            { "Testing", "Default" },
                            { "Default", "Default" },
                        },
                    },
                    new DATA.Mapping() {
                        Name = "ForSpecificGame",
                        GroupsToProfiles = new DATA.Mapping.GroupsToProfilesMapping()
                        {
                            { "Logitech Mice", "SpecificGame" },
                            { "Testing", "SpecificGame" },
                        },
                    },
                ],
            },
        };
    }

    private async Task ShowAlphaBuildWarningAsync()
    {
        var modalService = Services?.GetService<IModalService>();
        if (modalService != null)
        {
            var warningView = new Views.Controls.AlphaBuildWarningView();
            await modalService.ShowDialogAsync<bool>(warningView);
        }
    }

    public static void OpenBugReportUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/RawAccelOfficial/rawaccel/issues",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open bug report URL: {ex.Message}");
        }
    }

    public static void OpenDiscordUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/7pQh8zH",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open Discord URL: {ex.Message}");
        }
    }

    private async Task WarmUpAnimationPipelineAsync()
    {
        try
        {
            Debug.WriteLine("[GPU-WARMUP] Starting warmup...");
            
            // Wait longer for the UI to fully initialize and load profile elements
            await Task.Delay(500);
            
            var desktop = Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var mainWindow = desktop?.MainWindow;
            
            if (mainWindow == null)
            {
                Debug.WriteLine("[GPU-WARMUP] No main window found");
                return;
            }

            // Find the ProfileListView and wait for it to be fully loaded
            var profileListView = mainWindow.FindControl<userinterface.Views.Profile.ProfileListView>("ProfileListView");
            if (profileListView == null)
            {
                Debug.WriteLine("[GPU-WARMUP] ProfileListView not found");
                return;
            }

            // Wait for the view to be fully loaded with elements
            int retries = 0;
            while (retries < 10)
            {
                if (profileListView.IsLoaded && !profileListView.AreAnimationsActive)
                {
                    break;
                }
                await Task.Delay(100);
                retries++;
            }

            Debug.WriteLine($"[GPU-WARMUP] Found ProfileListView after {retries} retries, running invisible animation warmup");
            
            // Make the entire view invisible during warmup
            var originalOpacity = profileListView.Opacity;
            profileListView.Opacity = 0;
            
            // Run a quick collapse then expand to warm up the GPU pipeline
            Debug.WriteLine("[GPU-WARMUP] Running collapse warmup");
            await profileListView.CollapseElements();
            
            Debug.WriteLine("[GPU-WARMUP] Running expand warmup"); 
            await profileListView.ExpandElements();
            
            // Return to collapsed state (default)
            Debug.WriteLine("[GPU-WARMUP] Returning to collapsed state");
            await profileListView.CollapseElements();
            
            // Restore visibility
            profileListView.Opacity = originalOpacity;
            
            // Pre-load SkiaSharp and LiveCharts libraries that cause first-frame stutter
            Debug.WriteLine("[GPU-WARMUP] Pre-loading graphics libraries");
            try
            {
                _ = typeof(LiveChartsCore.SkiaSharpView.Avalonia.CartesianChart).Assembly;
                _ = typeof(SkiaSharp.SKCanvas).Assembly;
                
                // Force load the specific library that causes the 83ms stutter
                try
                {
                    _ = typeof(SkiaSharp.HarfBuzz.SKShaper).Assembly;
                    Debug.WriteLine("[GPU-WARMUP] SkiaSharp.HarfBuzz loaded");
                }
                catch 
                {
                    // Try alternative way to load HarfBuzz
                    System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(
                        new System.Reflection.AssemblyName("SkiaSharp.HarfBuzz"));
                    Debug.WriteLine("[GPU-WARMUP] SkiaSharp.HarfBuzz loaded via AssemblyLoadContext");
                }
                
                Debug.WriteLine("[GPU-WARMUP] All graphics libraries loaded");
            }
            catch (Exception libEx)
            {
                Debug.WriteLine($"[GPU-WARMUP] Library preload failed: {libEx.Message}");
            }
            
            Debug.WriteLine("[GPU-WARMUP] ProfileListView animation pipeline warmed up successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GPU-WARMUP] Failed to warm up: {ex.Message}");
        }
    }
}