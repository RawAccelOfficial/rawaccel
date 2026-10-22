#pragma once

#define _USE_MATH_DEFINES
#include <math.h>

template <typename T>
struct vec2 {
    T x;
    T y;
};

using vec2d = vec2<double>;

inline vec2d direction(double degrees)
{
    double radians = degrees * M_PI / 180;
    return { cos(radians), sin(radians) };
}

// inverse of direction, result in [0, 360]
inline double angle_degrees(const vec2d& v)
{
    double degrees = atan2(v.y, v.x) * 180 / M_PI;
    return (degrees < 0) ? degrees + 360 : degrees;
}

constexpr vec2d rotate(const vec2d& v, const vec2d& direction)
{
    return {
        v.x * direction.x - v.y * direction.y,
        v.x * direction.y + v.y * direction.x
    };
}

inline double magnitude(const vec2d& v)
{
    return sqrt(v.x * v.x + v.y * v.y);
}


inline double lp_distance(const vec2d& v, double p)
{
    return pow(pow(fabs(v.x), p) + pow(fabs(v.y), p), 1 / p);
}
