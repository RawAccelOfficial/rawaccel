#pragma once

#include "rawaccel-io-def.h"
#include "rawaccel-version.h"
#include "rawaccel-error.hpp"
#include "rawaccel.hpp"

#include <memory>

namespace rawaccel {

    inline void io_control(DWORD code, void* in, DWORD in_size, void* out, DWORD out_size) 
    {
        HANDLE ra_handle = INVALID_HANDLE_VALUE;

        // open with the access this IOCTL requires (CTL_CODE bits 14-15)
        const DWORD required = (code >> 14) & 3u;
        DWORD access = 0;
        if (required & FILE_READ_ACCESS) access |= GENERIC_READ;
        if (required & FILE_WRITE_ACCESS) access |= GENERIC_WRITE;
        if (access == 0) access = GENERIC_READ;

        ra_handle = CreateFileW(L"\\\\.\\rawaccel", access,
            FILE_SHARE_READ | FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, 0);

        if (ra_handle == INVALID_HANDLE_VALUE) {
            const DWORD error = GetLastError();
            if (error == ERROR_FILE_NOT_FOUND) throw install_error();
            throw sys_error("CreateFileW failed", error);
        }

        DWORD dummy;

        BOOL success = DeviceIoControl(
            ra_handle,
            code,
            in,
            in_size,
            out,
            out_size,
            &dummy,  // bytes returned
            NULL     // overlapped structure
        );

        CloseHandle(ra_handle);

        if (!success) {
            throw sys_error("DeviceIoControl failed");
        }
    }

    inline std::unique_ptr<std::byte[]> read()
    {
        io_base base_data;

        io_control(READ, NULL, 0, &base_data, sizeof(io_base));

        size_t size = sizeof(base_data);

        if (base_data.modifier_data_size == 0) {
            // driver has no data, but it's more useful to return something, 
            // so return a default modifier_settings object along with base data
             
            size += sizeof(modifier_settings);
            base_data.modifier_data_size = 1;
            auto bytes = std::make_unique<std::byte[]>(size);
            *reinterpret_cast<io_base*>(bytes.get()) = base_data;
            *reinterpret_cast<modifier_settings*>(bytes.get() + sizeof(io_base)) = {};
            return bytes;
        }
        else {
            size += sizeof(modifier_settings) * base_data.modifier_data_size;
            size += sizeof(device_settings) * base_data.device_data_size;
            auto bytes = std::make_unique<std::byte[]>(size);
            io_control(READ, NULL, 0, bytes.get(), DWORD(size));
            return bytes;
        }
    }

    // buffer must point to at least sizeof(io_base) bytes
    inline void write(const void* buffer)
    {
        if (buffer == nullptr) throw io_error("write buffer is null");

        auto* base_ptr = static_cast<const io_base*>(buffer);
        auto size = sizeof(io_base);
        size += base_ptr->modifier_data_size * sizeof(modifier_settings);
        size += base_ptr->device_data_size * sizeof(device_settings);

        if (size > DWORD(-1)) throw io_error("write buffer is too large");

        io_control(WRITE, const_cast<void*>(buffer), DWORD(size), NULL, 0);
    }

    inline void reset()
    {
        io_base base_data{};
        // all modifier/device data is cleared when a default io_base is passed
        io_control(WRITE, &base_data, sizeof(io_base), NULL, 0);
    }

    inline version_t get_version() 
    {
        version_t v;

        try {
            io_control(GET_VERSION, NULL, 0, &v, sizeof(version_t));
        }
        catch (const sys_error&) {
            // assume request is not implemented (< 1.3)
            v = { 0 }; 
        }

        return v;
    }

    inline version_t valid_version_or_throw()
    {
        auto v = get_version();

        if (v < min_driver_version) {
            throw error("reinstallation required");
        }

        if (version < v) {
            throw error("newer driver is installed");
        }

        return v;
    }

}
