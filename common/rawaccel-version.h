#pragma once

#define RA_VER_MAJOR 1
#define RA_VER_MINOR 7
#define RA_VER_PATCH 2

#define RA_OS "Win10+"

#define RA_M_STR_HELPER(x) #x
#define RA_M_STR(x) RA_M_STR_HELPER(x)

#define RA_VER_STRING RA_M_STR(RA_VER_MAJOR) "." RA_M_STR(RA_VER_MINOR) "." RA_M_STR(RA_VER_PATCH)

namespace rawaccel {

    struct version_t {
        int major;
        int minor;
        int patch;
    };

    constexpr bool operator<(const version_t& lhs, const version_t& rhs)
    {
        return (lhs.major != rhs.major) ?
               (lhs.major < rhs.major)  :
               (lhs.minor != rhs.minor) ?
               (lhs.minor < rhs.minor)  :
               (lhs.patch < rhs.patch)  ;
    }

    inline constexpr version_t version = { RA_VER_MAJOR, RA_VER_MINOR, RA_VER_PATCH };
#ifndef _KERNEL_MODE
    inline constexpr version_t min_driver_version = { 1, 7, 2 };
#endif

}
