#pragma once

#include "math-vec2.hpp"

namespace rawaccel {
    using milliseconds = double;

    inline constexpr int POLL_RATE_MIN = 125;
    inline constexpr int POLL_RATE_MAX = 8000;

    inline constexpr milliseconds DEFAULT_TIME_MIN = 1000.0 / POLL_RATE_MAX / 2;
    inline constexpr milliseconds DEFAULT_TIME_MAX = 100;

    inline constexpr milliseconds WRITE_DELAY = 1000;

    inline constexpr size_t MAX_DEV_ID_LEN = 200;
    inline constexpr size_t MAX_NAME_LEN = 256;

    inline constexpr size_t LUT_RAW_DATA_CAPACITY = 514;
    inline constexpr size_t LUT_POINTS_CAPACITY = LUT_RAW_DATA_CAPACITY / 2;

    inline constexpr size_t DIRECTIONAL_MULTIPLIER_RAW_DATA_CAPACITY = 256;
    inline constexpr size_t DIRECTIONAL_MULTIPLIER_POINTS_CAPACITY = DIRECTIONAL_MULTIPLIER_RAW_DATA_CAPACITY / 2;

    inline constexpr double DIRECTIONAL_MULTIPLIER_MIN = 0.5;
    inline constexpr double DIRECTIONAL_MULTIPLIER_MAX = 2;

    inline constexpr double MAX_NORM = 16;

    // At this DPI, one count per ms equals one inch per second.
    inline constexpr double NORMALIZED_DPI = 1000;

    inline constexpr bool LEGACY = 0;
    inline constexpr bool GAIN = 1;
    
    enum class accel_mode {
        classic,
        jump,
        natural,
        synchronous,
        power,
        lookup,
        noaccel
    };

    enum class cap_mode {
        io, in, out
    };

    struct accel_args {
        accel_mode mode = accel_mode::noaccel;
        bool gain = 1;

        double input_offset = 0;
        double output_offset = 0;
        double acceleration = 0.005;
        double decay_rate = 0.1;
        double gamma = 1;
        double motivity = 1.5;
        double exponent_classic = 2;
        double scale = 1;
        double exponent_power = 0.05;
        double limit = 1.5;
        double sync_speed = 5;
        double smooth = 0.5;

        vec2d cap = { 15, 1.5 };
        cap_mode cap_mode = cap_mode::out;

        int length = 0;
        mutable float data[LUT_RAW_DATA_CAPACITY] = {};
    };

    struct speed_args
    {
        bool whole = true;
        double lp_norm = 2;
        double input_speed_smooth_halflife = 0;
        double scale_smooth_halflife = 0;
        double output_speed_smooth_halflife = 0;
    };

    struct profile {
        wchar_t name[MAX_NAME_LEN] = L"default";

        vec2d domain_weights = { 1, 1 };
        vec2d range_weights = { 1, 1 };

        accel_args accel_x;
        accel_args accel_y;
        speed_args speed_processor_args;

        double output_dpi = NORMALIZED_DPI;
        double yx_output_dpi_ratio = 1;
        double lr_output_dpi_ratio = 1;
        double ud_output_dpi_ratio = 1;

        // (angle, multiplier) pairs, angle in degrees: 0 = right, 90 = down, 180 = left, 270 = up
        int directional_multiplier_length = 0;
        float directional_multiplier_data[DIRECTIONAL_MULTIPLIER_RAW_DATA_CAPACITY] = {};

        double degrees_rotation = 0;

        double degrees_snap = 0;

        double speed_min = 0;
        double speed_max = 0;
    };
}
