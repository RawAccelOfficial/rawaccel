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

    struct driver_settings {
        profile prof;

        struct data_t {
            vec2d rot_direction;
            accel_union accel_x;
            accel_union accel_y;
        } data = {};
    };

    inline void init_data(driver_settings& settings)
    {
        auto set_accel = [](accel_union& u, const accel_args& args) {
            u.visit([&](auto& impl) {
                impl = { args };
            }, args);
        };

        set_accel(settings.data.accel_x, settings.prof.accel_x);
        set_accel(settings.data.accel_y, settings.prof.accel_y);

        settings.data.rot_direction = direction(settings.prof.degrees_rotation);
    }

    inline constexpr unsigned DRIVER_CAPACITY = POOL_SIZE / sizeof(driver_settings);
    inline constexpr unsigned DEVICE_CAPACITY = POOL_SIZE / sizeof(device_settings);

    struct io_t {
        device_config default_dev_cfg;
        unsigned driver_data_size;
        unsigned device_data_size;
        driver_settings driver_data[DRIVER_CAPACITY];
        device_settings device_data[DEVICE_CAPACITY];
    };

    class modifier {
    public:
#ifdef _KERNEL_MODE
        __forceinline
#endif
        void modify(vec2d& in, const driver_settings& settings, double dpi_factor, milliseconds time) const
        {
            auto& args = settings.prof;
            auto& data = settings.data;

            double reference_angle = 0;
            double ips_factor = dpi_factor / time;

            if (apply_rotate) in = rotate(in, data.rot_direction);

            if (compute_ref_angle && in.y != 0) {
                if (in.x == 0) {
                    reference_angle = M_PI / 2;
                }
                else {
                    reference_angle = atan(fabs(in.y / in.x));

                    if (apply_snap) {
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

            if (clamp_speed) {
                double speed = magnitude(in) * ips_factor;
                double ratio = clampsd(speed, args.speed_min, args.speed_max) / speed;
                in.x *= ratio;
                in.y *= ratio;
            }

            vec2d abs_weighted_vel = {
                fabs(in.x * ips_factor * args.domain_weights.x),
                fabs(in.y * ips_factor * args.domain_weights.y)
            };

            if (distance_mode == separate) {
                in.x *= (*cb_x)(data.accel_x, args.accel_x, abs_weighted_vel.x, args.range_weights.x);
                in.y *= (*cb_y)(data.accel_y, args.accel_y, abs_weighted_vel.y, args.range_weights.y);
            }
            else {
                double speed;

                if (distance_mode == max) {
                    speed = maxsd(abs_weighted_vel.x, abs_weighted_vel.y);
                }
                else if (distance_mode == Lp) {
                    speed = lp_distance(abs_weighted_vel, args.lp_norm);
                }
                else {
                    speed = magnitude(abs_weighted_vel);
                }

                double weight = args.range_weights.x;

                if (apply_directional_weight) {
                    double diff = args.range_weights.y - args.range_weights.x;
                    weight += 2 / M_PI * reference_angle * diff;
                }

                double scale = (*cb_x)(data.accel_x, args.accel_x, speed, weight);
                in.x *= scale;
                in.y *= scale;
            }

            double dpi_adjusted_sens = args.sensitivity * dpi_factor;
            in.x *= dpi_adjusted_sens;
            in.y *= dpi_adjusted_sens * args.yx_sens_ratio;

            if (apply_dir_mul_x && in.x < 0) {
                in.x *= args.dir_multipliers.x;
            }

            if (apply_dir_mul_y && in.y < 0) {
                in.y *= args.dir_multipliers.y;
            }
        }

        modifier(driver_settings& settings)
        {
            auto& args = settings.prof;

            clamp_speed = args.speed_max > 0 && args.speed_min <= args.speed_max;
            apply_rotate = args.degrees_rotation != 0;
            apply_snap = args.degrees_snap != 0;
            apply_directional_weight = args.range_weights.x != args.range_weights.y;
            compute_ref_angle = apply_snap || apply_directional_weight;
            apply_dir_mul_x = args.dir_multipliers.x != 1;
            apply_dir_mul_y = args.dir_multipliers.y != 1;

            if (!args.whole) {
                distance_mode = distance_mode::separate;
            }
            else if (args.lp_norm >= MAX_NORM || args.lp_norm <= 0) {
                distance_mode = distance_mode::max;
            }
            else if (args.lp_norm != 2) {
                distance_mode = distance_mode::Lp;
            }
            else {
                distance_mode = distance_mode::euclidean;
            }

            set_callback(cb_x, settings.data.accel_x, args.accel_x);
            set_callback(cb_y, settings.data.accel_y, args.accel_y);
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

        bool apply_rotate = 0;
        bool compute_ref_angle = 0;
        bool apply_snap = 0;
        bool clamp_speed = 0;
        enum distance_mode : unsigned char {
            separate,
            max,
            Lp,
            euclidean,
        } distance_mode = {};
        bool apply_directional_weight = 0;
        bool apply_dir_mul_x = 0;
        bool apply_dir_mul_y = 0;

        callback_t cb_x = &callback_template<accel_noaccel>;
        callback_t cb_y = &callback_template<accel_noaccel>;
    };

} // rawaccel
