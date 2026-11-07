#pragma once

#define _USE_MATH_DEFINES
#include <math.h>

template <typename T>
struct vec2 {
    T x;
    T y;
};

using vec2d = vec2<double>;

#pragma float_control(precise, on, push)
inline vec2d direction(double degrees)
{
    double radians = degrees * M_PI / 180;
    return { cos(radians), sin(radians) };
}
#pragma float_control(pop)

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
