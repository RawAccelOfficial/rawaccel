#pragma once

#include "accel-union.hpp"

namespace rawaccel {

    struct time_clamp {
        milliseconds min = DEFAULT_TIME_MIN;
        milliseconds max = DEFAULT_TIME_MAX;
    };

    struct device_config {
        bool disable = false;
        bool set_extra_info = false;
        bool poll_time_lock = false;
        int dpi = 0;
        int polling_rate = 0;
        time_clamp clamp;
    };

    struct device_settings {
        wchar_t name[MAX_NAME_LEN] = {};
        wchar_t profile[MAX_NAME_LEN] = {};
        wchar_t id[MAX_DEV_ID_LEN] = {};
        device_config config;
    };

    enum class distance_mode : unsigned char {
        euclidean = 0,
        separate = 1,
        max = 2,
        Lp = 3,
    };

    struct modifier_flags {
        bool apply_rotate = 0;
        bool compute_ref_angle = 0;
        bool apply_snap = 0;
        bool clamp_speed = 0;
        bool apply_directional_weight = 0;
        bool apply_dir_mul_x = 0;
        bool apply_dir_mul_y = 0;
        bool apply_sector_sensitivity = 0;

        modifier_flags(const profile& args) 
        {
            clamp_speed = args.speed_max > 0 && args.speed_min <= args.speed_max;
            apply_rotate = args.degrees_rotation != 0;
            apply_snap = args.degrees_snap != 0;
            apply_directional_weight = args.speed_processor_args.whole && 
                args.range_weights.x != args.range_weights.y;
            compute_ref_angle = apply_snap || apply_directional_weight;
            apply_dir_mul_x = args.lr_output_dpi_ratio != 1;
            apply_dir_mul_y = args.ud_output_dpi_ratio != 1;
            apply_sector_sensitivity =
                (args.sector_right_half_width_degrees > 0 && args.sector_right_multiplier != 1) ||
                (args.sector_se_half_width_degrees > 0 && args.sector_se_multiplier != 1) ||
                (args.sector_down_half_width_degrees > 0 && args.sector_down_multiplier != 1) ||
                (args.sector_sw_half_width_degrees > 0 && args.sector_sw_multiplier != 1) ||
                (args.sector_left_half_width_degrees > 0 && args.sector_left_multiplier != 1) ||
                (args.sector_nw_half_width_degrees > 0 && args.sector_nw_multiplier != 1) ||
                (args.sector_up_half_width_degrees > 0 && args.sector_up_multiplier != 1) ||
                (args.sector_ne_half_width_degrees > 0 && args.sector_ne_multiplier != 1);
        }

        modifier_flags() = default;
    };

    /// <summary>
    /// Simple exponential moving average smoother.
    /// </summary>
    struct simple_ema_smoother {

        double windowCoefficient = 0;
        double cutoffCoefficient = 0;

        void init(const double halfLife)
        {
            windowTotal = 0;
            cutoffTotal = 0;

            windowCoefficient = halfLife > 0 ? pow(0.5, 1 / halfLife) : 0;
            cutoffCoefficient = 1.0 - sqrt(1.0 - windowCoefficient);
        }

        double windowTotal = 0;
        double cutoffTotal = 0;

        double smooth(const double speed, const milliseconds time)
        {
            // compute coefficients
            double timeAdjustedWindowCoefficient = 1 - pow(windowCoefficient, time);
            double timeAdjustedCutoffCoefficient = 1 - pow(cutoffCoefficient, time);

            // adjust total based on coefficient and difference between new value and total
            windowTotal += timeAdjustedWindowCoefficient * (speed - windowTotal);
            cutoffTotal += timeAdjustedCutoffCoefficient * (speed - cutoffTotal);

            return min(windowTotal, cutoffTotal);
        }
    };

    /// <summary>
    /// Linear exponential moving average smoother.
    /// </summary>
    struct linear_ema_smoother {

        // This constant found via experimentation.
        // Allowing user to specify may confuse parameterization without much gain.
        static constexpr double trendDampening = 0.75;

        double windowCoefficient = 0;
        double cutoffCoefficient = 0;
        double windowTrendCoefficient = 0;
        double cutoffTrendCoefficient = 0;
        bool is_init = false;

        void init(const double halfLife, const double trendHalfLife)
        {
            windowTotal = 0;
            cutoffTotal = 0;
            windowTrendTotal = 0;
            cutoffTrendTotal = 0;

            windowCoefficient = halfLife > 0 ? pow(0.5, 1 / halfLife) : 0;
            windowTrendCoefficient = trendHalfLife > 0 ? pow(0.5, 1 / trendHalfLife) : 0;
            cutoffCoefficient = 1.0 - sqrt(1.0 - windowCoefficient);
            cutoffTrendCoefficient = 1.0 - sqrt(1.0 - windowTrendCoefficient);
        }

        double windowTotal = 0;
        double cutoffTotal = 0;
        double windowTrendTotal = 0;
        double cutoffTrendTotal = 0;

        double smooth(const double speed, const milliseconds time)
        {
            // compute coefficients
            double timeAdjustedWindowCoefficient = 1 - pow(windowCoefficient, time);
            double timeAdjustedCutoffCoefficient = 1 - pow(cutoffCoefficient, time);

            double timeAdjustedWindowTrendCoefficient = 1 - pow(windowTrendCoefficient, time);
            double timeAdjustedCutoffTrendCoefficient = 1 - pow(cutoffTrendCoefficient, time);

            // save old totals for trend adjustment
            double oldWindowTotal = windowTotal;
            double oldCutoffTotal = cutoffTotal;

            // dampen trends
            windowTrendTotal *= trendDampening;
            cutoffTrendTotal *= trendDampening;

            // add dampened trend
            windowTotal += windowTrendTotal * time;
            cutoffTotal += cutoffTrendTotal * time;

            // adjust total based on coefficient and difference between new value and total
            windowTotal += timeAdjustedWindowCoefficient * (speed - windowTotal);
            cutoffTotal += timeAdjustedCutoffCoefficient * (speed - cutoffTotal);

            // don't let trend carry us below 0
            windowTotal = max(windowTotal, 0.0);
            cutoffTotal = max(cutoffTotal, 0.0);

            // adjust trend based on coefficient and difference between new value and total
            double newWindowTrend = time > 0 ? (windowTotal - oldWindowTotal) / time : 0;
            double newCutoffTrend = time > 0 ? (cutoffTotal - oldCutoffTotal) / time : 0;
            windowTrendTotal += timeAdjustedWindowTrendCoefficient * (newWindowTrend - windowTrendTotal);
            cutoffTrendTotal += timeAdjustedCutoffTrendCoefficient * (newCutoffTrend - cutoffTrendTotal);

            return min(windowTotal, cutoffTotal);
        }
    };

    struct speed_processor_flags
    {
        bool should_smooth_input = false;
        bool should_smooth_scale = false;
        bool should_smooth_output = false;
        distance_mode dist_mode = {};
    };

    struct smoother {
        linear_ema_smoother input_speed_smoother = {};
        simple_ema_smoother scale_smoother = {};
        linear_ema_smoother output_speed_smoother = {};
    };

    /// <summary>
    /// Processes input and output speed. Can also smooth scaling.
    /// Stateful (smoothers can be used and require previous state.)
    /// </summary>
    struct speed_processor {

        speed_args args = {};
        speed_processor_flags speed_flags = {};
        smoother smoother_x = {};
        smoother smoother_y = {};

        static constexpr double input_trend_halflife = 1.25;
        static constexpr double output_trend_halflife = 0.7;

        speed_processor() = default;

        void init(speed_args in_args)
        {
            args = in_args;

            if (!in_args.whole) {
                speed_flags.dist_mode = distance_mode::separate;
            }
            else if (in_args.lp_norm >= MAX_NORM || args.lp_norm <= 0) {
                speed_flags.dist_mode = distance_mode::max;
            }
            else if (in_args.lp_norm != 2) {
                speed_flags.dist_mode = distance_mode::Lp;
            }
            else {
                speed_flags.dist_mode = distance_mode::euclidean;
            }

            speed_flags.should_smooth_input = in_args.input_speed_smooth_halflife > 0;
            speed_flags.should_smooth_scale = in_args.scale_smooth_halflife > 0;
            speed_flags.should_smooth_output = in_args.output_speed_smooth_halflife > 0;

            if (speed_flags.should_smooth_input)
            {
                smoother_x.input_speed_smoother.init(in_args.input_speed_smooth_halflife, input_trend_halflife);
                smoother_y.input_speed_smoother.init(in_args.input_speed_smooth_halflife, input_trend_halflife);
            }

            if (speed_flags.should_smooth_scale)
            {
                smoother_x.scale_smoother.init(in_args.scale_smooth_halflife);
                smoother_y.scale_smoother.init(in_args.scale_smooth_halflife);
            }

            if (speed_flags.should_smooth_output)
            {
                smoother_x.output_speed_smoother.init(in_args.output_speed_smooth_halflife, output_trend_halflife);
                smoother_y.output_speed_smoother.init(in_args.output_speed_smooth_halflife, output_trend_halflife);
            }
        }

        double calc_speed_whole(vec2d in, milliseconds time)
        {
            double speed;

            if (speed_flags.dist_mode == distance_mode::max) {
                speed = maxsd(in.x, in.y);
            }
            else if (speed_flags.dist_mode == distance_mode::Lp) {
                speed = lp_distance(in, args.lp_norm);
            }
            else {
                speed = magnitude(in);
            }

            if (speed_flags.should_smooth_input)
            {
                speed = smoother_x.input_speed_smoother.smooth(speed, time);
            }

            return speed;
        }

        void calc_speed_separate(vec2d& in, milliseconds time)
        {
            double speed_x = in.x;
            double speed_y = in.y;

            if (speed_flags.should_smooth_input)
            {
                speed_x = smoother_x.input_speed_smoother.smooth(speed_x, time);
                speed_y = smoother_y.input_speed_smoother.smooth(speed_y, time);
            }

            in.x = speed_x;
            in.y = speed_y;
        }
    };

    struct modifier_settings {
        profile prof;

        struct data_t {
            modifier_flags flags;
            vec2d rot_direction;
            accel_union accel_x;
            accel_union accel_y;
        } data = {};
    };

    inline void init_data(modifier_settings& settings)
    {
        auto set_accel = [](accel_union& u, const accel_args& args) {
            u.visit([&](auto& impl) {
                impl = { args };
            }, args);
        };

        set_accel(settings.data.accel_x, settings.prof.accel_x);
        set_accel(settings.data.accel_y, settings.prof.accel_y);

        settings.data.rot_direction = direction(settings.prof.degrees_rotation);

        settings.data.flags = modifier_flags(settings.prof);
    }

    struct io_base {
        device_config default_dev_cfg;
        unsigned modifier_data_size = 0;
        unsigned device_data_size = 0;
    };

    static_assert(alignof(io_base) == alignof(modifier_settings) && alignof(modifier_settings) == alignof(device_settings));

    inline double sector_angle_difference(double a, double b)
    {
        double diff = fabs(a - b);
        return diff > 180 ? 360 - diff : diff;
    }

    inline void sector_consider(
        double multiplier,
        double half_width,
        double min_speed,
        double feather,
        double center,
        double angle,
        double input_speed,
        double& best_diff,
        double& best_scale)
    {
        if (half_width <= 0 || multiplier <= 0 || input_speed < min_speed) {
            return;
        }

        half_width = minsd(half_width, 180.0);
        double diff = sector_angle_difference(angle, center);

        // Overlapping sectors use the closest center.
        if (diff > half_width || diff >= best_diff) {
            return;
        }

        feather = clampsd(feather, 0.0, half_width);
        double blend = 1;

        if (feather > 0) {
            const double feather_start = half_width - feather;
            if (diff > feather_start) {
                blend = clampsd((half_width - diff) / feather, 0.0, 1.0);
            }
        }

        best_diff = diff;
        best_scale = 1 + (multiplier - 1) * blend;
    }

    inline double sector_sensitivity_multiplier(
        const vec2d& raw_input,
        double input_speed,
        const profile& args)
    {
        if (raw_input.x == 0 && raw_input.y == 0) {
            return 1;
        }

        double angle = atan2(raw_input.y, raw_input.x) * 180.0 / M_PI;
        if (angle < 0) {
            angle += 360;
        }

        double best_diff = 1e300;
        double best_scale = 1;

        sector_consider(args.sector_right_multiplier, args.sector_right_half_width_degrees,
            args.sector_right_min_speed, args.sector_right_feather_degrees,
            0, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_se_multiplier, args.sector_se_half_width_degrees,
            args.sector_se_min_speed, args.sector_se_feather_degrees,
            45, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_down_multiplier, args.sector_down_half_width_degrees,
            args.sector_down_min_speed, args.sector_down_feather_degrees,
            90, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_sw_multiplier, args.sector_sw_half_width_degrees,
            args.sector_sw_min_speed, args.sector_sw_feather_degrees,
            135, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_left_multiplier, args.sector_left_half_width_degrees,
            args.sector_left_min_speed, args.sector_left_feather_degrees,
            180, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_nw_multiplier, args.sector_nw_half_width_degrees,
            args.sector_nw_min_speed, args.sector_nw_feather_degrees,
            225, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_up_multiplier, args.sector_up_half_width_degrees,
            args.sector_up_min_speed, args.sector_up_feather_degrees,
            270, angle, input_speed, best_diff, best_scale);
        sector_consider(args.sector_ne_multiplier, args.sector_ne_half_width_degrees,
            args.sector_ne_min_speed, args.sector_ne_feather_degrees,
            315, angle, input_speed, best_diff, best_scale);

        return best_scale;
    }

    class modifier {
    public:
#ifdef _KERNEL_MODE
        __forceinline
#endif
        void modify(vec2d& in, speed_processor& speed_processor, const modifier_settings& settings, double dpi_factor, milliseconds time) const
        {
            auto& args = settings.prof;
            auto& data = settings.data;
            auto& flags = settings.data.flags;

            double reference_angle = 0;
            double ips_factor = dpi_factor / time;
            const vec2d sector_input = in;

            if (flags.apply_rotate) in = rotate(in, data.rot_direction);

            if (flags.compute_ref_angle && in.y != 0) {
                if (in.x == 0) {
                    reference_angle = M_PI / 2;
                }
                else {
                    reference_angle = atan(fabs(in.y / in.x));

                    if (flags.apply_snap) {
                        double snap = args.degrees_snap * M_PI / 180;

                        if (reference_angle > M_PI / 2 - snap) {
                            reference_angle = M_PI / 2;
                            in = { 0, _copysign(magnitude(in), in.y) };
                        }
                        else if (reference_angle < snap) {
                            reference_angle = 0;
                            in = { _copysign(magnitude(in), in.x), 0 };
                        }
                    }
                }
            }

            if (flags.clamp_speed) {
                double speed = magnitude(in) * ips_factor;
                double ratio = clampsd(speed, args.speed_min, args.speed_max) / speed;
                in.x *= ratio;
                in.y *= ratio;
            }

            vec2d abs_weighted_vel = {
                fabs(in.x * ips_factor * args.domain_weights.x),
                fabs(in.y * ips_factor * args.domain_weights.y)
            };

            if (speed_processor.speed_flags.dist_mode == distance_mode::separate) {

                speed_processor.calc_speed_separate(abs_weighted_vel, time);

                double scale_x = (*cb_x)(data.accel_x, args.accel_x, abs_weighted_vel.x, args.range_weights.x);
                double scale_y = (*cb_y)(data.accel_y, args.accel_y, abs_weighted_vel.y, args.range_weights.y);

                if (speed_processor.speed_flags.should_smooth_scale)
                {
                    scale_x = speed_processor.smoother_x.scale_smoother.smooth(scale_x, time);
                    scale_y = speed_processor.smoother_y.scale_smoother.smooth(scale_y, time);
                }
                
                in.x *= scale_x;
                in.y *= scale_y;

                if (speed_processor.speed_flags.should_smooth_output)
                {
                    in.x = _copysign(speed_processor.smoother_x.output_speed_smoother.smooth(fabs(in.x), time), in.x);
                    in.y = _copysign(speed_processor.smoother_y.output_speed_smoother.smooth(fabs(in.y), time), in.y);
                }
            }
            else {
                double speed = speed_processor.calc_speed_whole(abs_weighted_vel, time);

                double weight = args.range_weights.x;

                if (flags.apply_directional_weight) {
                    double diff = args.range_weights.y - args.range_weights.x;
                    weight += 2 / M_PI * reference_angle * diff;
                }

                double scale = (*cb_x)(data.accel_x, args.accel_x, speed, weight);

                if (speed_processor.speed_flags.should_smooth_scale)
                {
                    scale = speed_processor.smoother_x.scale_smoother.smooth(scale, time);
                }

                in.x *= scale;
                in.y *= scale;

                if (speed_processor.speed_flags.should_smooth_output)
                {
                    double mag = magnitude(in);
                    if (mag > 0)
                    {
                        double smoothedMag = speed_processor.smoother_x.output_speed_smoother.smooth(mag, time);
                        in.x *= (smoothedMag / mag);
                        in.y *= (smoothedMag / mag);
                    }
                }
            }

            double dpi_adjustment = output_dpi_adjustment_factor * dpi_factor;
            in.x *= dpi_adjustment;
            in.y *= dpi_adjustment * args.yx_output_dpi_ratio;

            if (flags.apply_dir_mul_x && in.x < 0) {
                in.x *= args.lr_output_dpi_ratio;
            }

            if (flags.apply_dir_mul_y && in.y < 0) {
                in.y *= args.ud_output_dpi_ratio;
            }

            if (flags.apply_sector_sensitivity &&
                (sector_input.x != 0 || sector_input.y != 0)) {
                const double sector_input_speed = magnitude(sector_input) * ips_factor;
                const double sector_scale = sector_sensitivity_multiplier(
                    sector_input, sector_input_speed, args);

                // One scalar on both output components preserves direction.
                in.x *= sector_scale;
                in.y *= sector_scale;
            }
        }

        modifier(modifier_settings& settings)
        {
            set_callback(cb_x, settings.data.accel_x, settings.prof.accel_x);
            set_callback(cb_y, settings.data.accel_y, settings.prof.accel_y);
            output_dpi_adjustment_factor = settings.prof.output_dpi / NORMALIZED_DPI;
        }

        modifier() = default;

    private:
        using callback_t = double (*)(const accel_union&, const accel_args&, double, double);

        void set_callback(callback_t& cb, accel_union& u, const accel_args& args)
        {
            u.visit([&](auto& impl) {
                cb = &callback_template<remove_ref_t<decltype(impl)>>;
            }, args);
        }

        template <typename AccelFunc>
        static double callback_template(const accel_union& u, 
                                        const accel_args& args, 
                                        double x, 
                                        double range_weight)
        {
            auto& accel_fn = reinterpret_cast<const AccelFunc&>(u);
            return 1 + (accel_fn(x, args) - 1) * range_weight;
        }

        callback_t cb_x = &callback_template<accel_noaccel>;
        callback_t cb_y = &callback_template<accel_noaccel>;

        double output_dpi_adjustment_factor = 1;
    };

} // rawaccel
