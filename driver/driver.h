#pragma once

#include "rawaccel.hpp"
#include "rawaccel-io-def.h"

#include <kbdmou.h>
#include <wdf.h>

#if DBG
#define DebugPrint(_x_) DbgPrint _x_
#else
#define DebugPrint(_x_)
#endif

#define NTDEVICE_NAME         L"\\Device\\rawaccel"
#define SYMBOLIC_NAME_STRING  L"\\DosDevices\\rawaccel"

using counter_t = long long;
namespace ra = rawaccel;

typedef struct _DEVICE_EXTENSION {
    KSPIN_LOCK state_lock;
    bool enable;
    bool keep_time;
    bool set_extra_info;
    double input_dpi_normalization_factor;
    counter_t counter;
    ra::time_clamp clamp;
    ra::modifier mod;
    vec2d carry;
    CONNECT_DATA UpperConnectData;
    ra::modifier_settings mod_settings;
    ra::speed_processor speed_processor;
    WCHAR dev_id[ra::MAX_DEV_ID_LEN];
} DEVICE_EXTENSION, *PDEVICE_EXTENSION;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_EXTENSION, FilterGetData)

EXTERN_C_START

DRIVER_INITIALIZE DriverEntry;

EVT_WDF_DRIVER_DEVICE_ADD EvtDeviceAdd;
EVT_WDF_IO_QUEUE_IO_INTERNAL_DEVICE_CONTROL EvtIoInternalDeviceControl;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL RawaccelControl;
EVT_WDF_OBJECT_CONTEXT_CLEANUP DeviceCleanup;

VOID DeviceSetup(WDFOBJECT);
VOID WriteDelay(VOID);
VOID RawaccelInit(WDFDRIVER);
NTSTATUS CreateControlDevice(WDFDRIVER);

EXTERN_C_END

VOID RawaccelCallback(
    IN PDEVICE_OBJECT DeviceObject,
    IN PMOUSE_INPUT_DATA InputDataStart,
    IN PMOUSE_INPUT_DATA InputDataEnd,
    IN OUT PULONG InputDataConsumed
);

VOID DispatchPassThrough(
    _In_ WDFREQUEST Request,
    _In_ WDFIOTARGET Target
);
