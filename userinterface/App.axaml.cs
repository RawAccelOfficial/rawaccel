using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.ViewModels.Controls;
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
        // Configure services
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ModalService>();
        services.AddSingleton<SettingsService>();

        // Register backend services
        services.AddSingleton<Bootstrapper>(provider => BootstrapBackEnd());
        services.AddSingleton<BackEnd>(provider =>
        {
            var bootstrapper = provider.GetRequiredService<Bootstrapper>();
            var backEnd = new BackEnd(bootstrapper);
            backEnd.Load();
            return backEnd;
        });

        // Register ViewModels
        RegisterViewModels(services);

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            // Create everything through DI
            var mainWindow = new MainWindow()
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };

            // Set up the toast control using DI
            var toastView = mainWindow.FindControl<userinterface.Views.Controls.ToastView>("ToastView");
            if (toastView != null)
            {
                toastView.DataContext = Services.GetRequiredService<ToastViewModel>();
            }

            desktop.MainWindow = mainWindow;

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
        services.AddTransient<ViewModels.Device.DeviceViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupViewModel>();
        services.AddTransient<ViewModels.Device.DeviceGroupSelectorViewModel>();

        // Profile ViewModels
        services.AddTransient<ViewModels.Profile.ProfilesPageViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileListViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileListElementViewModel>();
        services.AddTransient<ViewModels.Profile.ProfileChartViewModel>();

        // Mapping ViewModels
        services.AddTransient<ViewModels.Mapping.MappingsPageViewModel>();

        // Control ViewModels
        services.AddTransient<ViewModels.Controls.DualColumnLabelFieldViewModel>();
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
}