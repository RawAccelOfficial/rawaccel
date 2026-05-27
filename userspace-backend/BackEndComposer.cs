using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using DATA = userspace_backend.Data;
using userspace_backend.Display;
using userspace_backend.Driver;
using userspace_backend.Driver.Linux;
using userspace_backend.IO;
using userspace_backend.Model;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.AccelDefinitions.Formula;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using static userspace_backend.Data.Profiles.Accel.FormulaAccel;
using static userspace_backend.Data.Profiles.Accel.LookupTableAccel;
using static userspace_backend.Data.Profiles.Acceleration;
using static userspace_backend.Model.EditableSettings.EditableSettingsSelectorHelper;

namespace userspace_backend
{
    public static class BackEndComposer
    {
        public static IServiceProvider Compose(IServiceCollection services)
        {
            RegisterPlatformServices(services);
            services.TryAddSingleton<ISystemDevicesProvider, SystemDevicesProvider>();

            #region Parsers

            services.AddSingleton<IUserInputParser<string>, StringParser>();
            services.AddSingleton<IUserInputParser<int>, IntParser>();
            services.AddSingleton<IUserInputParser<double>, DoubleParser>();
            services.AddSingleton<IUserInputParser<bool>, BoolParser>();
            services.AddSingleton<IUserInputParser<AccelerationDefinitionType>, EnumParser<AccelerationDefinitionType>>();
            services.AddSingleton<IUserInputParser<AccelerationFormulaType>, EnumParser<AccelerationFormulaType>>();
            services.AddSingleton<IUserInputParser<LookupTableType>, EnumParser<LookupTableType>>();
            services.AddSingleton<IUserInputParser<LookupTableData>, LookupTableDataParser>();

            #endregion Parsers

            #region Validators

            services.AddSingleton<IModelValueValidator<int>, DefaultModelValueValidator<int>>();
            services.AddSingleton<IModelValueValidator<double>, DefaultModelValueValidator<double>>();
            services.AddSingleton<IModelValueValidator<string>, DefaultModelValueValidator<string>>();
            services.AddSingleton<IModelValueValidator<bool>, DefaultModelValueValidator<bool>>();
            services.AddSingleton<IModelValueValidator<AccelerationDefinitionType>, DefaultModelValueValidator<AccelerationDefinitionType>>();
            services.AddSingleton<IModelValueValidator<AccelerationFormulaType>, DefaultModelValueValidator<AccelerationFormulaType>>();
            services.AddSingleton<IModelValueValidator<LookupTableType>, DefaultModelValueValidator<LookupTableType>>();
            services.AddSingleton<IModelValueValidator<LookupTableData>, DefaultModelValueValidator<LookupTableData>>();

            services.AddKeyedSingleton<IModelValueValidator<string>, DefaultModelValueValidator<string>>(
                DefaultModelValueValidator<string>.AllChangeInvalidDIKey);

            services.AddKeyedSingleton<IModelValueValidator<string>, MaxNameLengthValidator>(
                ProfileModel.NameDIKey);

            #endregion Validators

            #region Hidden

            services.AddTransient<IHiddenModel, HiddenModel>();
            AddEditableSetting<double>(services, HiddenModel.RotationDegreesDIKey, "Rotation", 0);
            AddEditableSetting<double>(services, HiddenModel.AngleSnappingDegreesDIKey, "Angle Snapping", 0);
            AddEditableSetting<double>(services, HiddenModel.LeftRightRatioDIKey, "L/R Ratio", 1);
            AddEditableSetting<double>(services, HiddenModel.UpDownRatioDIKey, "U/D Ratio", 1);
            AddEditableSetting<double>(services, HiddenModel.SpeedCapDIKey, "Speed Cap", 0);
            AddEditableSetting<double>(services, HiddenModel.OutputSmoothingHalfLifeDIKey, "Output Smoothing Half-Life", 0,
                validatorFactory: sp => new RangeValidator<double>(min: 0));

            #endregion Hidden

            #region Coalescion

            services.AddTransient<ICoalescionModel, CoalescionModel>();
            AddEditableSetting<double>(services, CoalescionModel.InputSmoothingHalfLifeDIKey, "Input Smoothing Half-Life", 0,
                validatorFactory: sp => new RangeValidator<double>(min: 0));
            AddEditableSetting<double>(services, CoalescionModel.ScaleSmoothingHalfLifeDIKey, "Scale Smoothing Half-Life", 0,
                validatorFactory: sp => new RangeValidator<double>(min: 0));

            #endregion Coalescion

            #region Anisotropy

            services.AddTransient<IAnisotropyModel, AnisotropyModel>();
            AddEditableSetting<double>(services, AnisotropyModel.DomainXDIKey, "Domain X", 1);
            AddEditableSetting<double>(services, AnisotropyModel.DomainYDIKey, "Domain Y", 1);
            AddEditableSetting<double>(services, AnisotropyModel.RangeXDIKey, "Range X", 1);
            AddEditableSetting<double>(services, AnisotropyModel.RangeYDIKey, "Range Y", 1);
            AddEditableSetting<double>(services, AnisotropyModel.LPNormDIKey, "LP Norm", 2,
                validatorFactory: sp => new RangeValidator<double>(min: 0, minInclusive: false));
            AddEditableSetting<bool>(services, AnisotropyModel.CombineXYComponentsDIKey, "Combine X and Y Components", false,
                localizationKey: "AnisotropyCombineXY");

            #endregion Anisotropy

            #region Acceleration

            services.AddTransient<IAccelerationModel, AccelerationModel>();
            AddEditableSetting<AccelerationDefinitionType>(services, AccelerationModel.SelectionDIKey, "Definition Type", AccelerationDefinitionType.None);

            // Register selector options for AccelerationDefinitionType
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Acceleration>>(
                GetSelectionKey(AccelerationDefinitionType.None),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Acceleration>)sp.GetRequiredService<INoAccelDefinitionModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Acceleration>>(
                GetSelectionKey(AccelerationDefinitionType.Formula),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Acceleration>)sp.GetRequiredService<IFormulaAccelModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Acceleration>>(
                GetSelectionKey(AccelerationDefinitionType.LookupTable),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Acceleration>)sp.GetRequiredService<ILookupTableDefinitionModel>());

            #endregion Acceleration

            #region FormulaAccel

            services.AddTransient<IFormulaAccelModel, FormulaAccelModel>();
            AddEditableSetting<AccelerationFormulaType>(services, FormulaAccelModel.SelectionDIKey, "Formula Type", AccelerationFormulaType.Synchronous,
                autoUpdateFromInterface: true);
            AddEditableSetting<bool>(services, FormulaAccelModel.GainDIKey, "Apply to Gain", false);

            // Register selector options for AccelerationFormulaType
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>>(
                GetSelectionKey(AccelerationFormulaType.Synchronous),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>)sp.GetRequiredService<ISynchronousAccelerationDefinitionModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>>(
                GetSelectionKey(AccelerationFormulaType.Linear),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>)sp.GetRequiredService<ILinearAccelerationDefinitionModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>>(
                GetSelectionKey(AccelerationFormulaType.Classic),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>)sp.GetRequiredService<IClassicAccelerationDefinitionModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>>(
                GetSelectionKey(AccelerationFormulaType.Power),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>)sp.GetRequiredService<IPowerAccelerationDefinitionModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>>(
                GetSelectionKey(AccelerationFormulaType.Natural),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>)sp.GetRequiredService<INaturalAccelerationDefinitionModel>());
            services.AddKeyedTransient<IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>>(
                GetSelectionKey(AccelerationFormulaType.Jump),
                (sp, key) => (IEditableSettingsCollectionSpecific<userspace_backend.Data.Profiles.Accel.FormulaAccel>)sp.GetRequiredService<IJumpAccelerationDefinitionModel>());

            #endregion FormulaAccel

            #region LookupTable

            services.AddTransient<ILookupTableDefinitionModel, LookupTableDefinitionModel>();
            AddEditableSetting<LookupTableType>(services, LookupTableDefinitionModel.ApplyAsDIKey, "Apply as", LookupTableType.Velocity);
            AddEditableSetting<LookupTableData>(services, LookupTableDefinitionModel.DataDIKey, "Data", new LookupTableData());

            #endregion LookupTable

            #region NoAccel

            services.AddTransient<INoAccelDefinitionModel, NoAccelDefinitionModel>();

            #endregion NoAccel

            #region SynchronousAccel

            services.AddTransient<ISynchronousAccelerationDefinitionModel, SynchronousAccelerationDefinitionModel>();
            AddEditableSetting<double>(services, SynchronousAccelerationDefinitionModel.SyncSpeedDIKey, "Sync Speed", 15);
            AddEditableSetting<double>(services, SynchronousAccelerationDefinitionModel.MotivityDIKey, "Motivity", 1.4);
            AddEditableSetting<double>(services, SynchronousAccelerationDefinitionModel.GammaDIKey, "Gamma", 1);
            AddEditableSetting<double>(services, SynchronousAccelerationDefinitionModel.SmoothnessDIKey, "Smoothness", 0.5);

            #endregion SynchronousAccel

            #region LinearAccel

            services.AddTransient<ILinearAccelerationDefinitionModel, LinearAccelerationDefinitionModel>();
            AddEditableSetting<double>(services, LinearAccelerationDefinitionModel.AccelerationDIKey, "Acceleration", 0.01);
            AddEditableSetting<double>(services, LinearAccelerationDefinitionModel.OffsetDIKey, "Offset", 0);
            AddEditableSetting<double>(services, LinearAccelerationDefinitionModel.CapDIKey, "Cap", 0);

            #endregion LinearAccel

            #region ClassicAccel

            services.AddTransient<IClassicAccelerationDefinitionModel, ClassicAccelerationDefinitionModel>();
            AddEditableSetting<double>(services, ClassicAccelerationDefinitionModel.AccelerationDIKey, "Acceleration", 0.01);
            AddEditableSetting<double>(services, ClassicAccelerationDefinitionModel.ExponentDIKey, "Exponent", 2);
            AddEditableSetting<double>(services, ClassicAccelerationDefinitionModel.OffsetDIKey, "Offset", 0);
            AddEditableSetting<double>(services, ClassicAccelerationDefinitionModel.CapDIKey, "Cap", 0);

            #endregion ClassicAccel

            #region PowerAccel

            services.AddTransient<IPowerAccelerationDefinitionModel, PowerAccelerationDefinitionModel>();
            AddEditableSetting<double>(services, PowerAccelerationDefinitionModel.ScaleDIKey, "Scale", 1);
            AddEditableSetting<double>(services, PowerAccelerationDefinitionModel.ExponentDIKey, "Exponent", 0.05);
            AddEditableSetting<double>(services, PowerAccelerationDefinitionModel.OutputOffsetDIKey, "Output Offset", 0);
            AddEditableSetting<double>(services, PowerAccelerationDefinitionModel.CapDIKey, "Cap", 0);

            #endregion PowerAccel

            #region JumpAccel

            services.AddTransient<IJumpAccelerationDefinitionModel, JumpAccelerationDefinitionModel>();
            AddEditableSetting<double>(services, JumpAccelerationDefinitionModel.SmoothDIKey, "Smooth", 0.5);
            AddEditableSetting<double>(services, JumpAccelerationDefinitionModel.InputDIKey, "Input", 15);
            AddEditableSetting<double>(services, JumpAccelerationDefinitionModel.OutputDIKey, "Output", 1.5);

            #endregion JumpAccel

            #region NaturalAccel

            services.AddTransient<INaturalAccelerationDefinitionModel, NaturalAccelerationDefinitionModel>();
            AddEditableSetting<double>(services, NaturalAccelerationDefinitionModel.DecayRateDIKey, "Decay Rate", 0.1);
            AddEditableSetting<double>(services, NaturalAccelerationDefinitionModel.InputOffsetDIKey, "Input Offset", 0);
            AddEditableSetting<double>(services, NaturalAccelerationDefinitionModel.LimitDIKey, "Limit", 1.5);

            #endregion NaturalAccel

            #region Profile

            services.AddTransient<IProfileModel, ProfileModel>();
            AddEditableSetting<string>(services, ProfileModel.NameDIKey, "Name", "Empty",
                validatorFactory: sp => sp.GetRequiredKeyedService<IModelValueValidator<string>>(ProfileModel.NameDIKey));
            AddEditableSetting<int>(services, ProfileModel.OutputDPIDIKey, "Output DPI", 1000);
            AddEditableSetting<double>(services, ProfileModel.YXRatioDIKey, "Y/X Ratio", 1.0);

            #endregion Profile

            #region Display

            services.AddTransient<ICurvePreview, CurvePreview>();

            #endregion Display

            #region DeviceGroup

            services.AddSingleton<DeviceGroups>(sp =>
            {
                // Initialize with empty collection - will be populated during BackEnd.Load()
                return new DeviceGroups([]);
            });

            services.AddSingleton<DeviceGroupValidator>(sp =>
            {
                var deviceGroups = sp.GetRequiredService<DeviceGroups>();
                return new DeviceGroupValidator(deviceGroups);
            });

            services.AddSingleton<DevicesModel>(sp =>
            {
                var systemDevicesProvider = sp.GetRequiredService<ISystemDevicesProvider>();
                var deviceGroups = sp.GetRequiredService<DeviceGroups>();
                return new DevicesModel(sp, systemDevicesProvider, deviceGroups);
            });

            // TODO: HWID should never be exposed to user.
            services.AddTransient<IDeviceModel, DeviceModel>();
            AddEditableSetting<string>(services, DeviceModel.NameDIKey, "Name", "name");
            AddEditableSetting<string>(services, DeviceModel.HardwareIDDIKey, "Hardware ID", "hwid");
            AddEditableSetting<int>(services, DeviceModel.DPIDIKey, "DPI", 1000,
                validatorFactory: sp => new RangeValidator<int>(min: 1));
            AddEditableSetting<int>(services, DeviceModel.PollRateDIKey, "Polling Rate", 1000,
                validatorFactory: sp => new RangeValidator<int>(min: 1));
            AddEditableSetting<bool>(services, DeviceModel.IgnoreDIKey, "Ignore", false);
            AddEditableSetting<string>(services, DeviceModel.DeviceGroupDIKey, "Device Group", "default");

            #endregion DeviceGroup

            #region Mapping

            services.AddSingleton<MappingsModel>(sp =>
            {
                // Initialize with empty MappingSet - will be populated during BackEnd.Load()
                var deviceGroups = sp.GetRequiredService<DeviceGroups>();
                var profiles = sp.GetRequiredService<IProfilesModel>();
                var emptyMappingSet = new DATA.MappingSet { Mappings = [] };
                return new MappingsModel(emptyMappingSet, deviceGroups, profiles, sp);
            });

            services.AddSingleton<MappingNameValidator>(sp =>
            {
                var mappings = sp.GetRequiredService<MappingsModel>();
                return new MappingNameValidator(mappings);
            });

            services.AddTransient<IMappingModel, MappingModel>();
            AddEditableSetting<string>(services, MappingModel.NameDIKey, "Name", "name",
                validatorFactory: sp => sp.GetRequiredService<MappingNameValidator>());

            #endregion Mapping

            #region IO Layer

            services.AddSingleton<DevicesReaderWriter>();
            services.AddSingleton<MappingsReaderWriter>();
            services.AddSingleton<ProfileReaderWriter>();
            services.AddSingleton<SettingsReaderWriter>();

            #endregion IO Layer

            #region BackEnd

            services.AddSingleton<IProfilesModel, ProfilesModel>();

            services.AddSingleton<IBackEnd, BackEnd>();

            #endregion BackEnd

            return services.BuildServiceProvider();
        }

        // Registers a keyed EditableSettingV2<T> built from the DI-provided parser and
        // validator. Collapses the dozens of otherwise-identical registration blocks.
        // Pass validatorFactory to override the default (type-keyed) validator.
        private static void AddEditableSetting<T>(
            IServiceCollection services,
            object diKey,
            string displayName,
            T initialValue,
            bool autoUpdateFromInterface = false,
            string? localizationKey = null,
            Func<IServiceProvider, IModelValueValidator<T>>? validatorFactory = null)
            where T : IComparable
        {
            services.AddKeyedTransient<IEditableSettingSpecific<T>>(
                diKey,
                (sp, key) => new EditableSettingV2<T>(
                    displayName: displayName,
                    initialValue: initialValue,
                    parser: sp.GetRequiredService<IUserInputParser<T>>(),
                    validator: validatorFactory is null
                        ? sp.GetRequiredService<IModelValueValidator<T>>()
                        : validatorFactory(sp),
                    autoUpdateFromInterface: autoUpdateFromInterface,
                    localizationKey: localizationKey!,
                    logger: sp.GetService<ILoggerFactory>()
                        ?.CreateLogger(EditableSettingV2<T>.LoggerCategoryName)));
        }

        // TODO: This reflection-based registration exists only because wrapper.dll
        // is .NET Framework 4.7.2 mixed-mode C++/CLI (cannot be loaded
        // in process by net8.0), and Driver/Windows/*.cs is Compile-Removed on
        // non-Windows. Once wrapper is migrated to net8.0-windows
        // (<CLRSupport>NetCore</CLRSupport>), replace this with
        // compile safe registration and delete RegisterWindowsServicesByReflection
        private static void RegisterPlatformServices(IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows-side impls (WindowsRawAccelDriver, ManagedAccelEvaluator,
                // WindowsSystemDevicesRetriever) live under Driver/Windows/ and
                // are excluded from non-Windows builds via csproj. They depend
                // on wrapper.dll (C++/CLI). Registered via reflection so this
                // method can compile on Linux where those types do not exist.
                RegisterWindowsServicesByReflection(services);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                services.TryAddSingleton<IRawAccelDriver, LinuxAgentDriver>();
                services.TryAddSingleton<IAccelEvaluator, LinuxAccelEvaluator>();
                services.TryAddSingleton<ISystemDevicesRetriever, LinuxSystemDevicesRetriever>();
            }
            else
            {
                throw new PlatformNotSupportedException(
                    $"Raw Accel backend supports Windows and Linux only; current platform: {RuntimeInformation.OSDescription}");
            }
        }

        private static void RegisterWindowsServicesByReflection(IServiceCollection services)
        {
            var asm = typeof(BackEndComposer).Assembly;

            var driverType = asm.GetType("userspace_backend.Driver.Windows.WindowsRawAccelDriver");
            var evaluatorType = asm.GetType("userspace_backend.Driver.Windows.ManagedAccelEvaluator");
            var devicesType = asm.GetType("userspace_backend.Driver.Windows.WindowsSystemDevicesRetriever");

            if (driverType is null || evaluatorType is null || devicesType is null)
            {
                throw new InvalidOperationException(
                    "Windows driver/evaluator/devices types missing from this build; " +
                    "ensure userspace-backend was built on Windows so wrapper.dll is referenced.");
            }

            services.TryAddSingleton(typeof(IRawAccelDriver), driverType);
            services.TryAddSingleton(typeof(IAccelEvaluator), evaluatorType);
            services.TryAddSingleton(typeof(ISystemDevicesRetriever), devicesType);
        }
    }
}
