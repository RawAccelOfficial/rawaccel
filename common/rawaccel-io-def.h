#pragma once

#define NOMINMAX

#ifdef _KERNEL_MODE
#include <ntddk.h>
#else
#include <Windows.h>
#endif

namespace rawaccel {
    // the I/O manager checks these against the handle, so the device ACL controls access
    constexpr ULONG READ = (ULONG)CTL_CODE(0x8888u, 0x888, METHOD_BUFFERED, FILE_READ_ACCESS);
    constexpr ULONG WRITE = (ULONG)CTL_CODE(0x8888u, 0x889, METHOD_BUFFERED, FILE_WRITE_ACCESS);
    constexpr ULONG GET_VERSION = (ULONG)CTL_CODE(0x8888u, 0x88a, METHOD_BUFFERED, FILE_READ_ACCESS);
}