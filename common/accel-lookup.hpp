#pragma once

#include "rawaccel-base.hpp"
#include "utility.hpp"

#include <math.h>

namespace rawaccel {

	// represents the range [2^start, 2^stop], with num - 1
	// elements linearly spaced between each exponential step
	struct fp_rep_range {
		int start;
		int stop;
		int num;

		template <typename Func>
		void for_each(Func fn) const
		{
			for (int e = 0; e < stop - start; e++) {
				double exp_scale = scalbn(1, e + start) / num;

				for (int i = 0; i < num; i++) {
					fn((i + num) * exp_scale);
				}
			}

			fn(scalbn(1, stop));
		}

		int size() const
		{
			return (stop - start) * num + 1;
		}
	};

	struct si_pair {
		float slope = 0;
		float intercept = 0;
	};

	struct arbitrary_lut_point {
		float applicable_speed = 0;
		si_pair slope_intercept = {};
	};

	struct lookup {
		enum { capacity = LUT_POINTS_CAPACITY };

		fp_rep_range range;
		bool velocity_points;
		arbitrary_lut_point data[capacity] = {};
		int log_lookup[capacity] = {};
		double first_point_speed;
		double last_point_speed;
		int last_arbitrary_index;
		int last_log_lookup_index;
		double last_log_lookup_speed;
		double first_log_lookup_speed;

		double operator()(double speed, const accel_args&) const
		{
			int index = 0;
			int last_arb_index = last_arbitrary_index;
			int last_log_index = last_log_lookup_index;

			if (speed <= 0) return 1;

			if (unsigned(last_arb_index) < capacity &&
				unsigned(last_log_index) < capacity &&
				speed > first_point_speed)
			{
				if (speed > last_point_speed)
				{
					index = last_arb_index;
				}
				else if (speed > last_log_lookup_speed)
				{
					int last_log = log_lookup[last_log_index];
					if (unsigned(last_log) >= capacity) return 1;
					index = search_from(last_log, last_arb_index, speed);
				}
				else if (speed < first_log_lookup_speed)
				{
					index = search_from(0, last_arb_index, speed);
				}
				else
				{
					int log_index = get_log_index(speed);
					if (unsigned(log_index) >= capacity) return 1;
					int arbitrary_index = log_lookup[log_index];
					if (arbitrary_index < 0) return 1;
					index = search_from(arbitrary_index, last_arb_index, speed);
				}

			}

			return apply(index, speed);
		}

		int inline get_log_index(double speed) const
		{
			double speed_log = log(speed) - range.start;
			int index = (int)floor(speed_log * range.num);
			return index;
		}

		int inline search_from(int index, int last, double speed) const
		{
			do
			{
				index++;
			} 			
			while (index <= last && data[index].applicable_speed < speed);

			return index - 1;
		}

		double inline apply(int index, double speed) const
		{
			auto [slope, intercept] = data[index].slope_intercept;

			if (velocity_points)
			{
				return slope + intercept / speed;
			}
			else
			{
				return slope * speed + intercept;
			}
		}

		void fill(const float* raw_data, int raw_length)
		{
			auto* points = reinterpret_cast<const vec2<float>*>(raw_data);
			int length = raw_length / 2;

			first_point_speed = points[0].x;
			last_arbitrary_index = length - 1;
			// -2 because the last index in the arbitrary array is used for slope-intercept only
			last_point_speed = points[length-2].x;

			int start = static_cast<int>(floor(log(first_point_speed)));
			first_log_lookup_speed = exp(start*1.0);
			int end = static_cast<int>(floor(log(last_point_speed)));
			last_log_lookup_speed = exp(end*1.0);
			int num = end > start ? static_cast<int>(capacity / (end - start)) : 1;
			range = fp_rep_range{ start, end, num };
			last_log_lookup_index = end > start ? num * (end - start) - 1 : 0;

			vec2<float> current = {0, velocity_points ? 0.0f : 1.0f };
			vec2<float> next;
			int log_index = 0;
			double log_inner_iterator = range.start;
			double log_inner_slice = 1.0 / (range.num * 1.0);
			double log_value = exp(log_inner_iterator);

			for (int i = 0; i < length; i++)
			{
				next = points[i];
				double slope = (next.y - current.y) / (next.x - current.x);
				double intercept = next.y - slope * next.x;
				si_pair current_si = { 
					static_cast<float>(slope), 
					static_cast<float>(intercept)
				};
				arbitrary_lut_point current_lut_point = { 
					static_cast<float>(current.x), 
					current_si 
				};

				this->data[i] = current_lut_point;

				while (log_value < next.x && log_inner_iterator < end)
				{
					this->log_lookup[log_index] = i;
					log_index++;
					log_inner_iterator += log_inner_slice;
					log_value = exp(log_inner_iterator);
				}

				current = next;
			}
		}

		lookup(const accel_args& args)
		{
			velocity_points = args.gain;
			fill(args.data, args.length);
		}
	};

}
