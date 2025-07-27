#pragma once

#include "interop-exception.h"

#include <rawaccel-io.hpp>
#include <rawaccel-validate.hpp>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace System::Reflection;
using namespace System::Runtime::Serialization;
using namespace Newtonsoft::Json;
using namespace Newtonsoft::Json::Linq;

namespace ra = rawaccel;

ra::modifier_settings default_modifier_settings;
ra::device_settings default_device_settings;

public ref struct VersionHelper
{
    literal String^ VersionString = RA_VER_STRING;

    static Version^ ValidOrThrow()
    {
        try {
            ra::version_t v = ra::valid_version_or_throw();
            return gcnew Version(v.major, v.minor, v.patch, 0);
        }
        catch (const ra::error& e) {
            throw gcnew InteropException(e);
        }
    }
};

[JsonConverter(Converters::StringEnumConverter::typeid)]
public enum class AccelMode
{
    classic, jump, natural, synchronous, power, lut, noaccel
};

[JsonConverter(Converters::StringEnumConverter::typeid)]
public enum class CapMode
{
    in_out, input, output
};

generic <typename T>
[StructLayout(LayoutKind::Sequential)]
public value struct Vec2
{
    T x;
    T y;
};

[StructLayout(LayoutKind::Sequential)]
public value struct AccelArgs
{
    literal int MaxLutPoints = ra::LUT_POINTS_CAPACITY;

    AccelMode mode;

    [JsonProperty("Gain / Velocity")]
    [MarshalAs(UnmanagedType::U1)]
    bool gain;

    double inputOffset;
    double outputOffset;
    double acceleration;
    double decayRate;
    double gamma;
    double motivity;
    double exponentClassic;
    double scale;
    double exponentPower;
    double limit;
    double syncSpeed;
    double smooth;

    [JsonProperty("Cap / Jump")]
    Vec2<double> cap;

    [JsonProperty("Cap mode")]
    CapMode capMode;

    [JsonIgnore]
    int length;

    [MarshalAs(UnmanagedType::ByValArray, SizeConst = ra::LUT_RAW_DATA_CAPACITY)]
    array<float>^ data;

    [OnDeserialized]
    void OnDeserializedMethod(StreamingContext context)
    {
        // data->Length must match SizeConst when marshalling
        length = data->Length;
        array<float>::Resize(data, ra::LUT_RAW_DATA_CAPACITY);
    }

    bool IsEquivalentTo(AccelArgs other)
    {
        if (mode != other.mode)
        {
            return false;
        }

        if (mode != AccelMode::noaccel && gain != other.gain)
        {
            return false;
        }

        bool isEquivalent = false;

        switch (mode)
        {
            case AccelMode::noaccel:
                isEquivalent = true;
            case AccelMode::synchronous:
                isEquivalent =
                    syncSpeed == other.syncSpeed &&
                    motivity == other.motivity &&
                    gamma == other.gamma &&
                    smooth == other.smooth;
                break;
            case AccelMode::classic:
                isEquivalent =
                    exponentClassic == other.exponentClassic &&
                    inputOffset == other.inputOffset &&
                    capMode == other.capMode &&
                    (capMode == CapMode::in_out ?
                        (cap.x == other.cap.x && cap.y == other.cap.y) :
                    capMode == CapMode::output ?
                        (acceleration == other.acceleration && cap.y == other.cap.y) :
                        (acceleration == other.acceleration && cap.x == other.cap.x));
                break;
            case AccelMode::power:
                isEquivalent =
                    exponentPower == other.exponentPower &&
                    outputOffset == other.outputOffset &&
                    capMode == other.capMode &&
                    (capMode == CapMode::in_out ?
                        (cap.x == other.cap.x && cap.y == other.cap.y) :
                    capMode == CapMode::output ?
                        (acceleration == other.scale && cap.y == other.cap.y) :
                        (acceleration == other.scale && cap.x == other.cap.x));
                break;
            case AccelMode::natural:
                isEquivalent =
                    decayRate == other.decayRate &&
                    inputOffset == other.inputOffset &&
                    limit == other.limit;
                break;
            case AccelMode::jump:
                isEquivalent =
                    cap.x == other.cap.x &&
                    cap.y == other.cap.y &&
                    smooth == other.smooth;
                break;
            case AccelMode::lut:
                isEquivalent = true;

                if (length != other.length)
                {
                    isEquivalent = false;
                    break;
                }

                for (int i = 0; i < length; i++)
                {
                    int index = i * 2;
                    if (data[index] != other.data[index] ||
                        data[index+1] != other.data[index+1])
                    {
                        isEquivalent = false;
                        break;
                    }
                }

                break;
            default:
                break;
        }

        return isEquivalent;
    }
};

[StructLayout(LayoutKind::Sequential)]
public value struct SpeedArgs
{
    [JsonProperty("Whole/combined accel (set false for 'by component' mode)")]
    [MarshalAs(UnmanagedType::U1)]
    bool combineMagnitudes;

    double lpNorm;

    [JsonProperty("Time in ms after which an input is weighted at half its original value.")]
	double inputSmoothHalflife;

    [JsonProperty("Time in ms after which scale is weighted at half its original value.")]
	double scaleSmoothHalflife;

    [JsonProperty("Time in ms after which an output is weighted at half its original value.")]
	double outputSmoothHalflife;
};

[JsonObject(ItemRequired = Required::Always)]
[StructLayout(LayoutKind::Sequential, CharSet = CharSet::Unicode)]
public ref struct Profile
{
    [MarshalAs(UnmanagedType::ByValTStr, SizeConst = ra::MAX_NAME_LEN)]
    System::String^ name;

    [JsonProperty("Stretches domain for horizontal vs vertical inputs")]
    Vec2<double> domainXY;
    [JsonProperty("Stretches accel range for horizontal vs vertical inputs")]
    Vec2<double> rangeXY;

    [JsonProperty("Whole or horizontal accel parameters")]
    AccelArgs argsX;
    [JsonProperty("Vertical accel parameters")]
    AccelArgs argsY;
    [JsonProperty("Input speed calculation parameters")]
    SpeedArgs inputSpeedArgs;

    [JsonProperty("Output DPI")]
    double outputDPI;
    [JsonProperty("Y/X output DPI ratio (vertical sens multiplier)")]
    double yxOutputDPIRatio;
    [JsonProperty("L/R output DPI ratio (left sens multiplier)")]
    double lrOutputDPIRatio;
    [JsonProperty("U/D output DPI ratio (up sens multiplier)")]
    double udOutputDPIRatio;

    [JsonProperty("Degrees of rotation")]
    double rotation;

    [JsonProperty("Degrees of angle snapping")]
    double snap;

    [JsonIgnore]
    double minimumSpeed;
    [JsonProperty("Input Speed Cap")]
    double maximumSpeed;

    Profile(ra::profile& args)
    {
        Marshal::PtrToStructure(IntPtr(&args), this);
    }

    Profile() :
        Profile(default_modifier_settings.prof) {}
};

[JsonObject(ItemRequired = Required::Always)]
[StructLayout(LayoutKind::Sequential)]
public value struct DeviceConfig {
    [MarshalAs(UnmanagedType::U1)]
    bool disable;

    [MarshalAs(UnmanagedType::U1)]
    [JsonProperty(Required = Required::Default)]
    bool setExtraInfo;

    [MarshalAs(UnmanagedType::U1)]
    [JsonProperty("Use constant time interval based on polling rate", Required = Required::Default)]
    bool pollTimeLock;

    [JsonProperty("DPI (normalizes input speed unit: counts/ms -> in/s)")]
    int dpi;

    [JsonProperty("Polling rate Hz (keep at 0 for automatic adjustment)")]
    int pollingRate;
    
    [ComponentModel::DefaultValue(ra::DEFAULT_TIME_MIN)]
    [JsonProperty(Required = Required::Default)]
    double minimumTime;

    [ComponentModel::DefaultValue(ra::DEFAULT_TIME_MAX)]
    [JsonProperty(Required = Required::Default)]
    double maximumTime;

    bool ShouldSerializesetExtraInfo()
    {
        return setExtraInfo == true;
    }

    bool ShouldSerializeminimumTime()
    {
        return minimumTime != ra::DEFAULT_TIME_MIN;
    }

    bool ShouldSerializemaximumTime()
    {
        return maximumTime != ra::DEFAULT_TIME_MAX;
    }

    void Init(const ra::device_config& cfg) 
    {
        disable = cfg.disable;
        setExtraInfo = cfg.set_extra_info;
        pollTimeLock = cfg.poll_time_lock;
        dpi = cfg.dpi;
        pollingRate = cfg.polling_rate;
        minimumTime = cfg.clamp.min;
        maximumTime = cfg.clamp.max;
    }
};

[JsonObject(ItemRequired = Required::Always)]
[StructLayout(LayoutKind::Sequential, CharSet = CharSet::Unicode)]
public ref struct DeviceSettings
{
    [MarshalAs(UnmanagedType::ByValTStr, SizeConst = ra::MAX_NAME_LEN)]
    String^ name;

    [MarshalAs(UnmanagedType::ByValTStr, SizeConst = ra::MAX_NAME_LEN)]
    String^ profile;

    [MarshalAs(UnmanagedType::ByValTStr, SizeConst = ra::MAX_DEV_ID_LEN)]
    String^ id;

    DeviceConfig config;

    DeviceSettings(ra::device_settings& args)
    {
        Marshal::PtrToStructure(IntPtr(&args), this);
    }

    DeviceSettings() :
        DeviceSettings(default_device_settings) {}
};


public ref class ProfileErrors
{
    List<String^>^ tmp;
    bool single;

    delegate void MsgHandler(const char*);

    void Add(const char* msg)
    {
        tmp->Add(gcnew String(msg));
    }

public:
    ref struct SingleProfileErrors
    {
        Profile^ prof;
        array<String^>^ messages;
        int lastX;
        int lastY;
    };

    List<SingleProfileErrors^>^ list;

    ProfileErrors(List<Profile^>^ profiles)
    {
        single = profiles->Count == 1;
        list = gcnew List<SingleProfileErrors^>();
        tmp = gcnew List<String^>();
        MsgHandler^ del = gcnew MsgHandler(this, &ProfileErrors::Add);
        GCHandle gch = GCHandle::Alloc(del);
        auto fp = static_cast<void (*)(const char*)>(
            Marshal::GetFunctionPointerForDelegate(del).ToPointer());
        ra::profile* native_ptr = new ra::profile();

        for each (auto prof in profiles) {
            Marshal::StructureToPtr(prof, IntPtr(native_ptr), false);
            auto [last_x, last_y, _] = ra::valid(*native_ptr, fp);

            if (tmp->Count != 0) {
                auto singleErrors = gcnew SingleProfileErrors();
                singleErrors->messages = tmp->ToArray();
                singleErrors->lastX = last_x;
                singleErrors->lastY = last_y;
                singleErrors->prof = prof;
                list->Add(singleErrors);
                tmp->Clear();
            }
        }

        tmp = nullptr;
        gch.Free();
        delete native_ptr;
    }

    bool Empty()
    {
        return list->Count == 0;
    }

    virtual String^ ToString() override
    {
        Text::StringBuilder^ sb = gcnew Text::StringBuilder();

        for each (auto elem in list) {
            if (!single) {
                sb->AppendFormat("profile: {0}\n", elem->prof->name);
            }

            auto msgs = elem->messages;
            if (elem->prof->inputSpeedArgs.combineMagnitudes) {
                for (int i = 0; i < elem->lastX; i++) {
                    sb->AppendFormat("\t{0}\n", msgs[i]);
                }
            }
            else {
                for (int i = 0; i < elem->lastX; i++) {
                    sb->AppendFormat("\tx: {0}\n", msgs[i]);
                }
                for (int i = elem->lastX; i < elem->lastY; i++) {
                    sb->AppendFormat("\ty: {0}\n", msgs[i]);
                }
            }

            for (int i = elem->lastY; i < msgs->Length; i++) {
                sb->AppendFormat("\t{0}\n", msgs[i]);
            }
        }

        return sb->ToString();
    }
};

public ref class DeviceErrors
{
    List<String^>^ tmp;
    bool single;

    delegate void MsgHandler(const char*);

    void Add(const char* msg)
    {
        tmp->Add(gcnew String(msg));
    }

public:
    ref struct SingleDeviceErrors
    {
        DeviceSettings^ settings;
        array<String^>^ messages;
    };

    List<SingleDeviceErrors^>^ list;

    DeviceErrors(List<DeviceSettings^>^ devSettings)
    {
        single = devSettings->Count == 1;
        list = gcnew List<SingleDeviceErrors^>();
        tmp = gcnew List<String^>();
        MsgHandler^ del = gcnew MsgHandler(this, &DeviceErrors::Add);
        GCHandle gch = GCHandle::Alloc(del);
        auto fp = static_cast<void (*)(const char*)>(
            Marshal::GetFunctionPointerForDelegate(del).ToPointer());
        ra::device_settings* native_ptr = new ra::device_settings();

        for each (auto dev in devSettings) {
            Marshal::StructureToPtr(dev, IntPtr(native_ptr), false);
            ra::valid(*native_ptr, fp);

            if (tmp->Count != 0) {
                auto singleErrors = gcnew SingleDeviceErrors();
                singleErrors->messages = tmp->ToArray();
                singleErrors->settings = dev;
                list->Add(singleErrors);
                tmp->Clear();
            }
        }

        tmp = nullptr;
        gch.Free();
        delete native_ptr;
    }

    bool Empty()
    {
        return list->Count == 0;
    }

    virtual String^ ToString() override
    {
        Text::StringBuilder^ sb = gcnew Text::StringBuilder();

        for each (auto elem in list) {
            if (!single) {
                sb->AppendFormat("device: {0}\n", elem->settings->id);
                if (!String::IsNullOrWhiteSpace(elem->settings->name)) {
                    sb->AppendFormat("  name: {0}\n", elem->settings->name);
                }
            }

            for each (auto msg in elem->messages) {
                sb->AppendFormat("\tx: {0}\n", msg);
            }
        }

        return sb->ToString();
    }
};

struct accel_instance_t {
    ra::modifier mod;
    ra::modifier_settings settings;

    accel_instance_t() = default;

    accel_instance_t(ra::modifier_settings& args) :
        settings(args),
        mod(args) {}

    void init(Profile^ args)
    {
        Marshal::StructureToPtr(args, IntPtr(&settings.prof), false);
        ra::init_data(settings);
        mod = { settings };
    }
};

struct speed_calc_instance_t
{
    ra::speed_processor speed_calculator = {};
};

public ref class ManagedAccel
{
    accel_instance_t* const instance = new accel_instance_t();
    speed_calc_instance_t* const speed_instance = new speed_calc_instance_t();

public:

    ManagedAccel() {}

    ManagedAccel(ra::modifier_settings& settings) : 
        instance(new accel_instance_t(settings)) {}

    ManagedAccel(Profile^ settings)
    {
        Settings = settings;
    }

    virtual ~ManagedAccel()
    {
        delete instance;
    }

    !ManagedAccel()
    {
        delete instance;
    }

    /// <summary>
    /// The modifier inside of ManagedAccel is now stateful, which can result in strange graphs when used for graphing purposes.
    /// This method creates a copy of its instance with any stateful elements turned off.
    /// </summary>
    /// <returns></returns>
    ManagedAccel^ CreateStatelessCopy()
    {
        Profile^ profile = Settings;

        profile->inputSpeedArgs.inputSmoothHalflife = 0;
        profile->inputSpeedArgs.scaleSmoothHalflife = 0;
        profile->inputSpeedArgs.outputSmoothHalflife = 0;

        return gcnew ManagedAccel(profile);
    }

    Tuple<double, double>^ Accelerate(int x, int y, double dpi_factor, double time)
    {
        vec2d in_out_vec = {
            (double)x,
            (double)y
        };

        instance->mod.modify(in_out_vec, speed_instance->speed_calculator, instance->settings, dpi_factor, time);

        return gcnew Tuple<double, double>(in_out_vec.x, in_out_vec.y);
    }

    property Profile^ Settings
    {
        Profile^ get()
        {
            return gcnew Profile(instance->settings.prof);
        }

        void set(Profile^ val)
        {
            instance->init(val);
            ra::speed_args speedArgs;
            Marshal::StructureToPtr(val->inputSpeedArgs, IntPtr(&speedArgs), false);
            speed_instance->speed_calculator.init(speedArgs);
        }
    }

    ra::modifier_settings NativeSettings()
    {
        return instance->settings;
    }
};

public ref class SpeedCalculatorArgs
{
public:
    SpeedCalculatorArgs() {}
    SpeedCalculatorArgs(
        double lp_norm,
        double input_speed_smooth_halflife,
        double scale_smooth_halflife,
        double output_speed_smooth_halflife)
    {
        speed_args->lp_norm = lp_norm;
        speed_args->input_speed_smooth_halflife = input_speed_smooth_halflife;
        speed_args->scale_smooth_halflife = scale_smooth_halflife;
        speed_args->output_speed_smooth_halflife = output_speed_smooth_halflife;
    }

    ra::speed_args* const speed_args = new ra::speed_args();
};

public ref class SpeedCalculator
{
    speed_calc_instance_t* const instance = new speed_calc_instance_t();

public:
    SpeedCalculator() {}

    void Init(SpeedCalculatorArgs^ args)
    {
        instance->speed_calculator.init(*args->speed_args);
    }

    double CalculateSpeed(double x, double y, double time)
    {
        vec2d in = { x ,y };
        return instance->speed_calculator.calc_speed_whole(in, time);
    }

    double SmoothScale(double scale, double time)
    {
        return instance->speed_calculator.smoother_x.scale_smoother.smooth(scale, time);
    }

    double SmoothOutput(double outputSpeed, double time)
    {
        return instance->speed_calculator.smoother_x.output_speed_smoother.smooth(outputSpeed, time);
    }
};


[JsonObject(ItemRequired = Required::Always)]
public ref class DriverConfig {
public:
    literal double WriteDelayMs = ra::WRITE_DELAY;
    literal String^ Key = "Driver settings";

    String^ version = RA_VER_STRING;

    DeviceConfig defaultDeviceConfig;

    List<Profile^>^ profiles;

    [NonSerialized]
    List<ManagedAccel^>^ accels;

    List<DeviceSettings^>^ devices;

    void Activate()
    {
        if (accels->Count != profiles->Count) {
            throw gcnew Exception("Profile count does not match ManagedAccel");
        }

        std::byte* buffer;

        auto modifier_data_bytes = accels->Count * sizeof(ra::modifier_settings);
        auto device_data_bytes = devices->Count * sizeof(ra::device_settings);

        try {
            buffer = new std::byte[sizeof(ra::io_base) + modifier_data_bytes + device_data_bytes];
        }
        catch (const std::exception& e) {
            throw gcnew InteropException(e);
        }

        auto* byte_ptr = buffer;

        auto* base_data = reinterpret_cast<ra::io_base*>(byte_ptr);
        base_data->default_dev_cfg.disable = defaultDeviceConfig.disable;
        base_data->default_dev_cfg.set_extra_info = defaultDeviceConfig.setExtraInfo;
        base_data->default_dev_cfg.poll_time_lock = defaultDeviceConfig.pollTimeLock;
        base_data->default_dev_cfg.dpi = defaultDeviceConfig.dpi;
        base_data->default_dev_cfg.polling_rate = defaultDeviceConfig.pollingRate;
        base_data->default_dev_cfg.clamp.min = defaultDeviceConfig.minimumTime;
        base_data->default_dev_cfg.clamp.max = defaultDeviceConfig.maximumTime;
        base_data->modifier_data_size = accels->Count;
        base_data->device_data_size = devices->Count;

        byte_ptr += sizeof(ra::io_base);

        auto* modifier_data = reinterpret_cast<ra::modifier_settings*>(byte_ptr);
        for (auto i = 0; i < accels->Count; i++) {
            auto& mod_settings = modifier_data[i];
            mod_settings = accels[i]->NativeSettings();
        }

        byte_ptr += modifier_data_bytes;

        auto* device_data = reinterpret_cast<ra::device_settings*>(byte_ptr);
        for (auto i = 0; i < devices->Count; i++) {
            auto& dev_settings = device_data[i];
            Marshal::StructureToPtr(devices[i], IntPtr(&dev_settings), false);
        }

        try {
            ra::write(buffer);
            delete[] buffer;
        }
        catch (const std::exception& e) {
            delete[] buffer;
            throw gcnew InteropException(e);
        }
    }
    
    void SetProfileAt(int index, Profile^ val)
    {
        profiles[index] = val;
        accels[index]->Settings = val;
    }

    // returns null or a joined list of error messages
    String^ Errors()
    {
        Text::StringBuilder^ sb = gcnew Text::StringBuilder();

        ProfileErrors^ profErrors = gcnew ProfileErrors(profiles);
        if (!profErrors->Empty()) {
            sb->Append(profErrors->ToString());
        }

        DeviceSettings^ defaultDev = gcnew DeviceSettings();
        defaultDev->config = defaultDeviceConfig;
        defaultDev->id = "Default";
        devices->Add(defaultDev);

        DeviceErrors^ devErrors = gcnew DeviceErrors(devices);
        if (!devErrors->Empty()) {
            sb->Append(profErrors->ToString());
        }

        devices->RemoveAt(devices->Count - 1);

        if (sb->Length == 0) {
            return nullptr;
        }
        else {
            return sb->ToString();
        }
    }

    JObject^ ToJObject()
    {
        auto dataQueue = gcnew Queue<array<float>^>();
        auto empty = Array::Empty<float>();

        for each (auto prof in profiles) {
            if (prof->argsX.mode == AccelMode::lut) {
                // data->Length is fixed for interop, 
                // temporary resize avoids serializing a bunch of zeros
                Array::Resize(prof->argsX.data, prof->argsX.length);
            }
            else {
                // table data may be used internally in any mode, 
                // so hide it when it's not needed for deserialization 
                dataQueue->Enqueue(prof->argsX.data);
                prof->argsX.data = empty;
            }

            if (prof->argsY.mode == AccelMode::lut) {
                Array::Resize(prof->argsY.data, prof->argsY.length);
            }
            else {
                dataQueue->Enqueue(prof->argsY.data);
                prof->argsY.data = empty;
            }
        }

        JObject^ jObject = JObject::FromObject(this);
        String^ capModes = String::Join(" | ", Enum::GetNames(CapMode::typeid));
        String^ accelModes = String::Join(" | ", Enum::GetNames(AccelMode::typeid));
        jObject->AddFirst(gcnew JProperty("### Cap modes ###", capModes));
        jObject->AddFirst(gcnew JProperty("### Accel modes ###", accelModes));

        for each (auto prof in profiles) {
            if (prof->argsX.mode == AccelMode::lut) {
                Array::Resize(prof->argsX.data, ra::LUT_RAW_DATA_CAPACITY);
            }
            else {
                prof->argsX.data = dataQueue->Dequeue();
            }

            if (prof->argsY.mode == AccelMode::lut) {
                Array::Resize(prof->argsY.data, ra::LUT_RAW_DATA_CAPACITY);
            }
            else {
                prof->argsY.data = dataQueue->Dequeue();
            }
        }

        return jObject;
    }

    String^ ToJSON()
    {
        return ToJObject()->ToString();
    }

    // returns (config, null) or (null, error message)
    static Tuple<DriverConfig^, String^>^ Convert(String^ json)
    {
        auto jss = gcnew JsonSerializerSettings();
        jss->DefaultValueHandling = DefaultValueHandling::Populate;
        auto cfg = JsonConvert::DeserializeObject<DriverConfig^>(json, jss);
        if (cfg == nullptr) throw gcnew JsonException("invalid JSON");

        auto message = cfg->Errors();
        if (message != nullptr) {
            return gcnew Tuple<DriverConfig^, String^>(nullptr, message);
        }
        else {
            cfg->accels = gcnew List<ManagedAccel^>();

            if (cfg->profiles->Count == 0) {
                cfg->profiles->Add(gcnew Profile());
            }

            for each (auto prof in cfg->profiles) {
                cfg->accels->Add(gcnew ManagedAccel(prof));
            }
            return gcnew Tuple<DriverConfig^, String^>(cfg, nullptr);
        }
    }

    static DriverConfig^ GetActive()
    {
        std::unique_ptr<std::byte[]> bytes;
        try {
            bytes = ra::read();
        }
        catch (const std::exception& e) {
            throw gcnew InteropException(e);
        } 

        auto cfg = gcnew DriverConfig();
        cfg->profiles = gcnew List<Profile^>();
        cfg->accels = gcnew List<ManagedAccel^>();
        cfg->devices = gcnew List<DeviceSettings^>();

        auto* byte_ptr = bytes.get();
        ra::io_base* base_data = reinterpret_cast<ra::io_base*>(byte_ptr);
        cfg->defaultDeviceConfig.Init(base_data->default_dev_cfg);

        byte_ptr += sizeof(ra::io_base);

        ra::modifier_settings* modifier_data = reinterpret_cast<ra::modifier_settings*>(byte_ptr);
        for (auto i = 0u; i < base_data->modifier_data_size; i++) {
            auto& mod_settings = modifier_data[i];
            cfg->profiles->Add(gcnew Profile(mod_settings.prof));
            cfg->accels->Add(gcnew ManagedAccel(mod_settings));
        }

        byte_ptr += base_data->modifier_data_size * sizeof(ra::modifier_settings);

        ra::device_settings* device_data = reinterpret_cast<ra::device_settings*>(byte_ptr);
        for (auto i = 0u; i < base_data->device_data_size; i++) {
            auto& dev_settings = device_data[i];
            cfg->devices->Add(gcnew DeviceSettings(dev_settings));
        }

        return cfg;
    }

    static DriverConfig^ FromProfile(Profile^ prof)
    {
        auto cfg = gcnew DriverConfig();
        cfg->profiles = gcnew List<Profile^>();
        cfg->accels = gcnew List<ManagedAccel^>();
        cfg->devices = gcnew List<DeviceSettings^>();

        cfg->profiles->Add(prof);
        cfg->accels->Add(gcnew ManagedAccel(prof));
        cfg->defaultDeviceConfig.Init(default_device_settings.config);
        return cfg;
    }

    static DriverConfig^ GetDefault()
    {
        return FromProfile(gcnew Profile());
    }

    static void Deactivate() 
    {
        try {
            ra::reset();
        }
        catch (const std::exception& e) {
            throw gcnew InteropException(e);
        }
    }

private: 
    DriverConfig() {}
};

