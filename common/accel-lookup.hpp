#pragma once

#include "rawaccel-base.hpp"
#include "utility.hpp"

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

	__forceinline
	constexpr double lerp(double a, double b, double t)
	{
		double x = a + t * (b - a);
		if ((t > 1) == (a < b)) {
			return maxsd(x, b);
		}
		return minsd(x, b);
	}

	struct lookup {
		enum { capacity = LUT_POINTS_CAPACITY };

		int size;
		bool velocity;

		lookup(const accel_args& args) :
			size(args.length / 2),
			velocity(args.gain) {}

		double operator()(double x, const accel_args& args) const
		{
			auto* points = reinterpret_cast<const vec2<float>*>(args.data);

			int lo = 0;
			int n = size;
			if (n < 0) n = 0;
			else if (n > capacity) n = capacity;
			int hi = n - 2;

			if (x <= 0) return 0;

			if (hi < capacity - 1) {

				while (lo <= hi) {
					int mid = (lo + hi) / 2;
					auto p = points[mid];

					if (x < p.x) {
						hi = mid - 1;
					}
					else if (x > p.x) {
						lo = mid + 1;
					}
					else {
						double y = p.y;
						if (velocity) y /= x;
						return y;
					}
				}

				if (lo > 0) {
					auto& a = points[lo - 1];
					auto& b = points[lo];
					double t = (x - a.x) / (b.x - a.x);
					double y = lerp(a.y, b.y, t);
					if (velocity) y /= x;
					return y;
				}

			}

			double y = points[0].y;
			if (velocity) y /= points[0].x;
			return y;
		}
	};


}
