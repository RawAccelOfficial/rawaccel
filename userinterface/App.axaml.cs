using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
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
        services.AddTransient<ViewModels.Profile.ProfileListElementViewModel>();
        services.AddTransient<ViewModels.Profile.ActiveProfilesListViewModel>();
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
            var message = "This is an alpha build of RawAccel. You may encounter bugs or unexpected behavior.\n\n" +
                          "You can report issues at: https://github.com/RawAccelOfficial/rawaccel/issues\n\n" +
                          "There's also a bug report button in the Settings page for easy access.";

            await modalService.ShowMessageAsync(
                "Alpha Build Warning",
                message,
                "I Understand");
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
            System.Diagnostics.Debug.WriteLine($"Failed to open bug report URL: {ex.Message}");
        }
    }
}