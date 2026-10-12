#pragma once

#include "rawaccel-base.hpp"
#include "utility.hpp"

namespace rawaccel {

	struct valid_profile_ret_t {
		int last_x = 0;
		int last_y = 0;
		int count = 0;

		explicit operator bool() const
		{
			return count == 0;
		}
	};

	struct valid_device_ret_t {
		int count = 0;

		explicit operator bool() const
		{
			return count == 0;
		}
	};

	template <typename MsgHandler = noop>
	valid_profile_ret_t valid(const profile& args, MsgHandler fn = {})
	{
		valid_profile_ret_t ret;

		auto error = [&](auto msg) {
			++ret.count;
			fn(msg);
		};

		auto check_accel = [&error](const accel_args& args) {
			static_assert(LUT_POINTS_CAPACITY == 257, "update error msg");

			if (args.mode == accel_mode::lookup) {
				if (args.length < 4) {
					error("lookup mode requires at least 2 points");
				}
				else if (args.length > ra::LUT_RAW_DATA_CAPACITY) {
					error("too many data points (max=257)");
				}
			}
			else if (args.length > ra::LUT_RAW_DATA_CAPACITY) {
				error("data size > max");
			}

			if (args.input_offset < 0) {
				error("offset can not be negative");
			}

			if (args.output_offset < 0) {
				error("offset can not be negative");
			}

			bool jump_or_io_cap = 
				(args.mode == accel_mode::jump || 
					((args.mode == accel_mode::classic || args.mode == accel_mode::power) &&
						args.cap_mode == cap_mode::io));

			if (args.cap.x < 0) {
				error("cap (input) can not be negative");
			}
			else if (args.cap.x == 0 && jump_or_io_cap) {
				error("cap (input) can not be 0");
			}

			if (args.cap.y < 0) {
				error("cap (output) can not be negative");
			}
			else if (args.cap.y == 0 && jump_or_io_cap) {
				error("cap (output) can not be 0");
			}

			if ((args.mode == accel_mode::classic && 
					args.cap.x > 0 && 
					args.cap.x < args.input_offset && 
					args.cap_mode != cap_mode::out) ||
				(args.mode == accel_mode::power &&
					args.cap.y > 0 &&
					args.cap.y < args.output_offset &&
					args.cap_mode != cap_mode::in)) {
				error("cap < offset");
			}

			if (args.acceleration <= 0) {
				error("acceleration"" must be positive");
			}

			if (args.scale <= 0) {
				error("scale"" must be positive");
			}

			if (args.gamma <= 0) {
				error("gamma"" must be positive");
			}

			if (args.decay_rate <= 0) {
				error("decay rate"" must be positive");
			}

			if (args.motivity <= 1) {
				error("motivity must be greater than 1");
			}

			if (args.exponent_classic <= 1) {
				error("exponent must be greater than 1");
			}

			if (args.exponent_power <= 0) {
				error("exponent"" must be positive");
			}

			if (args.limit <= 0) {
				error("limit"" must be positive");
			}

			if (args.sync_speed <= 0) {
				error("synchronous speed"" must be positive");
			}

			if (args.smooth < 0 || args.smooth > 1) {
				error("smooth must be between 0 and 1");
			}

		};

		auto check_sector = [&error](
			double multiplier,
			double half_width,
			double min_speed,
			double feather)
		{
			if (multiplier <= 0) {
				error("sector sensitivity multiplier must be positive");
			}
			if (half_width < 0 || half_width > 180) {
				error("sector half width must be between 0 and 180 degrees");
			}
			if (min_speed < 0) {
				error("sector minimum speed can not be negative");
			}
			if (feather < 0 || feather > half_width) {
				error("sector feather must be between 0 and half width");
			}
		};

		check_accel(args.accel_x);

		if (!args.speed_processor_args.whole) {
			ret.last_x = ret.count;
			check_accel(args.accel_y);
			ret.last_y = ret.count;
		}

		if (args.name[0] == L'\0') {
			error("profile name can not be empty");
		}

		if (args.speed_max < 0) {
			error("speed cap is negative");
		}
		else if (args.speed_max < args.speed_min) {
			error("max speed is less than min speed");
		}

		if (args.degrees_snap < 0 || args.degrees_snap > 45) {
			error("snap angle must be between 0 and 45 degrees");
		}

		if (args.output_dpi == 0) {
			error("output DPI is 0");
		}
	
		if (args.yx_output_dpi_ratio == 0) {
			error("Y/X output DPI ratio is 0");
		}

		if (args.domain_weights.x <= 0 ||
			args.domain_weights.y <= 0) {
			error("domain weights"" must be positive");
		}

		if (args.lr_output_dpi_ratio <= 0 || args.ud_output_dpi_ratio <= 0) {
			error("output DPI ratio must be positive");
		}

		check_sector(args.sector_right_multiplier, args.sector_right_half_width_degrees,
			args.sector_right_min_speed, args.sector_right_feather_degrees);
		check_sector(args.sector_se_multiplier, args.sector_se_half_width_degrees,
			args.sector_se_min_speed, args.sector_se_feather_degrees);
		check_sector(args.sector_down_multiplier, args.sector_down_half_width_degrees,
			args.sector_down_min_speed, args.sector_down_feather_degrees);
		check_sector(args.sector_sw_multiplier, args.sector_sw_half_width_degrees,
			args.sector_sw_min_speed, args.sector_sw_feather_degrees);
		check_sector(args.sector_left_multiplier, args.sector_left_half_width_degrees,
			args.sector_left_min_speed, args.sector_left_feather_degrees);
		check_sector(args.sector_nw_multiplier, args.sector_nw_half_width_degrees,
			args.sector_nw_min_speed, args.sector_nw_feather_degrees);
		check_sector(args.sector_up_multiplier, args.sector_up_half_width_degrees,
			args.sector_up_min_speed, args.sector_up_feather_degrees);
		check_sector(args.sector_ne_multiplier, args.sector_ne_half_width_degrees,
			args.sector_ne_min_speed, args.sector_ne_feather_degrees);

		if (args.speed_processor_args.lp_norm <= 0) {
			error("Lp norm must be positive (default=2)");
		}

		if (args.range_weights.x < 0 || args.range_weights.y < 0) {
			error("range weights"" must be positive");
		}

		return ret;
	}

	template <typename MsgHandler = noop>
	valid_device_ret_t valid(const device_settings& args, MsgHandler fn = {})
	{
		valid_device_ret_t ret;

		auto error = [&](auto msg) {
			++ret.count;
			fn(msg);
		};


		if (args.config.dpi < 0) {
			error("dpi"" can not be negative");
		}

		if (args.config.polling_rate < 0) {
			error("polling rate"" can not be negative");
		}

		if (args.config.clamp.min <= 0) {
			error("minimum time"" must be positive");
		}

		if (args.config.clamp.max < args.config.clamp.min) {
			error("max time is less than min time");
		}

		return ret;
	}
}
