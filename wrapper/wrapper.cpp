#pragma once

#include <type_traits>

#include <rawaccel.hpp>

#include "wrapper_io.hpp"

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace Newtonsoft::Json;

[JsonConverter(Converters::StringEnumConverter::typeid)]
public enum class AccelMode
{
    linear, classic, natural, naturalgain, power, motivity, noaccel
};

[JsonObject(ItemRequired = Required::Always)]
[StructLayout(LayoutKind::Sequential)]
public value struct AccelArgs
{
    double offset;
    [MarshalAs(UnmanagedType::U1)]
    bool legacyOffset;
    double acceleration;
    double scale;
    double limit;
    double exponent;
    double midpoint;
    double weight;
    [JsonProperty("legacyCap")]
    double scaleCap;
    double gainCap;
};

generic <typename T>
[JsonObject(ItemRequired = Required::Always)]
[StructLayout(LayoutKind::Sequential)]
public value struct Vec2
{
    T x;
    T y;
};

[JsonObject(ItemRequired = Required::Always)]
[StructLayout(LayoutKind::Sequential)]
public ref struct DriverSettings
{
    literal String^ Key = "Driver settings";

    [JsonProperty("Degrees of rotation")]
    double rotation;

    [JsonProperty("Use x as whole/combined accel")]
    [MarshalAs(UnmanagedType::U1)]
    bool combineMagnitudes;

    [JsonProperty("Accel modes")]
    Vec2<AccelMode> modes;

    [JsonProperty("Accel parameters")]
    Vec2<AccelArgs> args;

    [JsonProperty("Sensitivity")]
    Vec2<double> sensitivity;
    
    [JsonProperty(Required = Required::Default)]
    double minimumTime;

    bool ShouldSerializeminimumTime() 
    { 
        return minimumTime > 0 && minimumTime != DEFAULT_TIME_MIN;
    }
};


template <typename NativeSettingsFunc>
void as_native(DriverSettings^ managed_args, NativeSettingsFunc fn)
{
#ifndef NDEBUG
    if (Marshal::SizeOf(managed_args) != sizeof(settings))
        throw gcnew InvalidOperationException("setting sizes differ");
#endif
    settings args;
    Marshal::StructureToPtr(managed_args, (IntPtr)&args, false);
    fn(args);
    if constexpr (!std::is_invocable_v<NativeSettingsFunc, const settings&>) {
        Marshal::PtrToStructure((IntPtr)&args, managed_args);
    }
}

DriverSettings^ get_default()
{
    DriverSettings^ managed = gcnew DriverSettings();
    as_native(managed, [](settings& args) {
        args = {};
    });
    return managed;
}

void set_active(DriverSettings^ managed)
{
    as_native(managed, [](const settings& args) {
        wrapper_io::writeToDriver(args);
    });
}

DriverSettings^ get_active()
{
    DriverSettings^ managed = gcnew DriverSettings();
    as_native(managed, [](settings& args) {
        wrapper_io::readFromDriver(args);
    });
    return managed;
}

void update_modifier(mouse_modifier& mod, DriverSettings^ managed, vec2<si_pair*> luts = {})
{
    as_native(managed, [&](const settings& args) {
        mod = { args, luts };
    });
}

using error_list_t = Collections::Generic::List<String^>;

error_list_t^ get_accel_errors(AccelMode mode, AccelArgs^ args)
{
    accel_mode m = (accel_mode)mode;

    auto is_mode = [m](auto... modes) { return ((m == modes) || ...); };
    
    using am = accel_mode;

    auto error_list = gcnew error_list_t();
    
    if (args->acceleration > 10 && is_mode(am::natural, am::naturalgain))
        error_list->Add("acceleration can not be greater than 10");
    else if (args->acceleration < 0) {
        bool additive = m < am::power;
        if (additive) error_list->Add("acceleration can not be negative, use a negative weight to compensate");
        else error_list->Add("acceleration can not be negative");
    }
        
    if (args->scale <= 0)
        error_list->Add("scale must be positive");

    if (args->exponent <= 1 && is_mode(am::classic))
        error_list->Add("exponent must be greater than 1");
    else if (args->exponent <= 0)
        error_list->Add("exponent must be positive");

    if (args->limit <= 1)
        error_list->Add("limit must be greater than 1");

    if (args->midpoint <= 0)
        error_list->Add("midpoint must be positive");

    return error_list;
}

public ref class SettingsErrors
{
public:
    error_list_t^ x;
    error_list_t^ y;

    bool Empty()
    {
        return x->Count == 0 && y->Count == 0;
    }
};

public ref struct DriverInterop
{
    literal double WriteDelayMs = WRITE_DELAY;
    static initonly AccelArgs^ DefaultArgs = get_default()->args.x;

    static DriverSettings^ GetActiveSettings()
    {
        return get_active();
    }

    static void Write(DriverSettings^ args)
    {
        set_active(args);
    }

    static DriverSettings^ GetDefaultSettings()
    {
        return get_default();
    }

    static SettingsErrors^ GetSettingsErrors(DriverSettings^ args)
    {
        auto errors = gcnew SettingsErrors();

        errors->x = get_accel_errors(args->modes.x, args->args.x);

        if (args->combineMagnitudes) errors->y = gcnew error_list_t();
        else errors->y = get_accel_errors(args->modes.y, args->args.y);

        return errors;
    }

    static error_list_t^ GetAccelErrors(AccelMode mode, AccelArgs^ args)
    {
        return get_accel_errors(mode, args);
    }
};

public ref class ManagedAccel
{
    mouse_modifier* const modifier_instance = new mouse_modifier();
#ifdef RA_LOOKUP
    si_pair* const lut_x = new si_pair[LUT_SIZE];
    si_pair* const lut_y = new si_pair[LUT_SIZE];
#else
    si_pair* lut_x = nullptr;
    si_pair* lut_y = nullptr;
#endif

public:

    virtual ~ManagedAccel()
    {
        delete modifier_instance;
        delete[] lut_x;
        delete[] lut_y;
    }

    !ManagedAccel()
    {
        delete modifier_instance;
        delete[] lut_x;
        delete[] lut_y;
    }

    Tuple<double, double>^ Accelerate(int x, int y, double time)
    {
        vec2d in_out_vec = {
            (double)x,
            (double)y
        };
        modifier_instance->modify(in_out_vec, time);

        return gcnew Tuple<double, double>(in_out_vec.x, in_out_vec.y);
    }

    void UpdateFromSettings(DriverSettings^ args)
    {
        update_modifier(
            *modifier_instance, 
            args, 
            vec2<si_pair*>{ lut_x, lut_y }
        );
    }

    static ManagedAccel^ GetActiveAccel()
    {
        settings args;
        wrapper_io::readFromDriver(args);

        auto active = gcnew ManagedAccel();
        *active->modifier_instance = { 
            args
            , vec2<si_pair*> { active->lut_x, active->lut_y }
        };
        return active;
    }
};
