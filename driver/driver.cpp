#include "driver.h"

#include <rawaccel.hpp>
#include <rawaccel-io-def.h>
#include <rawaccel-version.h>
#include <rawaccel-validate.hpp>


#ifdef ALLOC_PRAGMA
#pragma alloc_text (INIT, DriverEntry)
#pragma alloc_text (INIT, RawaccelInit)
#pragma alloc_text (INIT, CreateControlDevice)
#pragma alloc_text (PAGE, EvtDeviceAdd)
#pragma alloc_text (PAGE, EvtIoInternalDeviceControl)
#pragma alloc_text (PAGE, RawaccelControl)
#pragma alloc_text (PAGE, DeviceCleanup)
#pragma alloc_text (PAGE, DeviceSetup)
#pragma alloc_text (PAGE, WriteDelay)
#endif

using milliseconds = double;

// shows as 'Racl' in poolmon
static constexpr ULONG POOL_TAG = 0x6c636152u;

static constexpr size_t MAX_SETTINGS_POOL_BYTES = 64 * 1024 * 1024;
static constexpr unsigned MAX_SETTINGS_COUNT = 8192;

struct {
    bool initialized;
    WDFCOLLECTION device_collection;
    WDFWAITLOCK collection_lock;
    ra::io_base base_data;
    ra::modifier_settings* modifier_data;
    ra::device_settings* device_data;
    milliseconds tick_interval;
} global = {};

extern "C" PULONG InitSafeBootMode;

static bool ValidateAndInitSettings(
    const ra::io_base& base,
    ra::modifier_settings* mods,
    ra::device_settings* devs)
{
    if (ra::valid(base.default_dev_cfg).count != 0) {
        return false;
    }

    for (auto i = 0u; i < base.modifier_data_size; i++) {
        if (ra::valid(mods[i].prof).count != 0) {
            return false;
        }
        ra::init_data(mods[i]);
    }

    for (auto i = 0u; i < base.device_data_size; i++) {
        if (ra::valid(devs[i]).count != 0) {
            return false;
        }
    }

    return true;
}

// precise behavior required to handle NaN (UB with /fp:fast)
#pragma float_control(precise, on, push)

static __forceinline
bool 
ValidCarry(double x, double y)
{
    return fabs(x) < 1 && fabs(y) < 1;
}

#pragma float_control(pop)

__declspec(guard(ignore))
VOID
RawaccelCallback(
    IN PDEVICE_OBJECT DeviceObject,
    IN PMOUSE_INPUT_DATA InputDataStart,
    IN PMOUSE_INPUT_DATA InputDataEnd,
    IN OUT PULONG InputDataConsumed
)
/*++

Routine Description:

    Called when there are mouse packets to report to the RIT.

Arguments:

    DeviceObject - Context passed during the connect IOCTL

    InputDataStart - First packet to be reported

    InputDataEnd - One past the last packet to be reported.  Total number of
                   packets is equal to InputDataEnd - InputDataStart

    InputDataConsumed - Set to the total number of packets consumed by the RIT
                        (via the function pointer we replaced in the connect
                        IOCTL)

--*/
{
    WDFDEVICE hDevice = WdfWdmDeviceGetWdfDeviceHandle(DeviceObject);
    PDEVICE_EXTENSION devExt = FilterGetData(hDevice);

    auto num_packets = InputDataEnd - InputDataStart;

    // DeviceSetup rewrites these fields from another CPU
    KIRQL oldIrql;
    KeAcquireSpinLock(&devExt->state_lock, &oldIrql);

    if (num_packets > 0 &&
        !(InputDataStart->Flags & MOUSE_MOVE_ABSOLUTE) &&
        devExt->enable) {
        
        milliseconds time;
        if (devExt->keep_time) {
            counter_t now = KeQueryPerformanceCounter(NULL).QuadPart;
            counter_t ticks = now - devExt->counter;
            devExt->counter = now;
            milliseconds raw = ticks * global.tick_interval / num_packets;
            time = ra::clampsd(raw, devExt->clamp.min, devExt->clamp.max);
        }
        else {
            time = devExt->clamp.min;
        }

        auto it = InputDataStart;
        do {
            if (devExt->set_extra_info) {
                union {
                    short input[2];
                    ULONG data;
                } u = { short(it->LastX), short(it->LastY) };

                it->ExtraInformation = u.data;
            }

            if (it->LastX || it->LastY) {
                vec2d input = {
                    static_cast<double>(it->LastX),
                    static_cast<double>(it->LastY)
                };

                devExt->mod.modify(input, devExt->speed_processor, devExt->mod_settings, devExt->input_dpi_normalization_factor, time);

                double carried_result_x = input.x + devExt->carry.x;
                double carried_result_y = input.y + devExt->carry.y;

                LONG out_x = static_cast<LONG>(carried_result_x);
                LONG out_y = static_cast<LONG>(carried_result_y);

                double carry_x = carried_result_x - out_x;
                double carry_y = carried_result_y - out_y;

                if (ValidCarry(carry_x, carry_y)) {
                    devExt->carry.x = carry_x;
                    devExt->carry.y = carry_y;
                    it->LastX = out_x;
                    it->LastY = out_y;
                }
            }
        } while (++it != InputDataEnd);

    }

    KeReleaseSpinLock(&devExt->state_lock, oldIrql);

    (*(PSERVICE_CALLBACK_ROUTINE)devExt->UpperConnectData.ClassService)(
        devExt->UpperConnectData.ClassDeviceObject,
        InputDataStart,
        InputDataEnd,
        InputDataConsumed
        );
}


#pragma warning(push)
#pragma warning(disable:28118) // this callback will run at IRQL=PASSIVE_LEVEL
_Use_decl_annotations_
VOID
RawaccelControl(
    WDFQUEUE         Queue,
    WDFREQUEST       Request,
    size_t           OutputBufferLength,
    size_t           InputBufferLength,
    ULONG            IoControlCode
)
/*++
Routine Description:
    This event is called when the framework receives IRP_MJ_DEVICE_CONTROL
    requests from the system.
Arguments:
    Queue - Handle to the framework queue object that is associated
            with the I/O request.
    Request - Handle to a framework request object.
    OutputBufferLength - length of the request's output buffer,
                        if an output buffer is available.
    InputBufferLength - length of the request's input buffer,
                        if an input buffer is available.
    IoControlCode - the driver-defined or system-defined I/O control code
                    (IOCTL) that is associated with the request.
Return Value:
   VOID
--*/
{
    NTSTATUS status;
    void* buffer;
    size_t buffer_length;
    size_t bytes_out = 0;

    UNREFERENCED_PARAMETER(Queue);
    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);
    PAGED_CODE();

    DebugPrint(("Ioctl received into filter control object.\n"));

    if (!global.initialized) {
        WdfRequestCompleteWithInformation(Request, STATUS_CANCELLED, 0);
        return;
    }

    const auto SIZEOF_BASE = sizeof(ra::io_base);

    switch (IoControlCode) {
    case ra::READ:
        status = WdfRequestRetrieveOutputBuffer(
            Request,
            SIZEOF_BASE,
            &buffer,
            &buffer_length
        );
        if (!NT_SUCCESS(status)) {
            DebugPrint(("RetrieveOutputBuffer failed: 0x%x\n", status));
        }
        else {
            *static_cast<ra::io_base*>(buffer) = global.base_data;

            size_t modifier_bytes = global.base_data.modifier_data_size * sizeof(ra::modifier_settings);
            size_t device_bytes = global.base_data.device_data_size * sizeof(ra::device_settings);
            size_t total_bytes = SIZEOF_BASE + modifier_bytes + device_bytes;

            if (buffer_length < total_bytes) {
                bytes_out = SIZEOF_BASE;
            }
            else {
                BYTE* output_ptr = static_cast<BYTE*>(buffer) + SIZEOF_BASE;

                if (global.modifier_data) RtlCopyMemory(output_ptr, global.modifier_data, modifier_bytes);
                output_ptr += modifier_bytes;
                if (global.device_data) RtlCopyMemory(output_ptr, global.device_data, device_bytes);
                bytes_out = total_bytes;
            }
        }
        break;
    case ra::WRITE:
        status = WdfRequestRetrieveInputBuffer(
            Request,
            SIZEOF_BASE,
            &buffer,
            &buffer_length
        );
        if (!NT_SUCCESS(status)) {
            DebugPrint(("RetrieveInputBuffer failed: 0x%x\n", status));
        }
        else {
            ra::io_base& input = *static_cast<ra::io_base*>(buffer);

            auto modifier_bytes = size_t(input.modifier_data_size) * sizeof(ra::modifier_settings);
            auto device_bytes = size_t(input.device_data_size) * sizeof(ra::device_settings);
            auto alloc_size = modifier_bytes + device_bytes;
            auto total_size = alloc_size + SIZEOF_BASE;

            auto max_u32 = unsigned(-1);
            if (modifier_bytes > max_u32 || device_bytes > max_u32 || total_size > max_u32 ||
                alloc_size > MAX_SETTINGS_POOL_BYTES ||
                input.modifier_data_size > MAX_SETTINGS_COUNT ||
                input.device_data_size > MAX_SETTINGS_COUNT) {
                status = STATUS_INVALID_PARAMETER;
                break;
            }

            if (input.modifier_data_size == 0) {
                // clear data and disable all devices
                WriteDelay();

                WdfWaitLockAcquire(global.collection_lock, NULL);

                global.base_data = {};

                if (global.modifier_data) {
                    ExFreePoolWithTag(global.modifier_data, POOL_TAG);
                    global.modifier_data = NULL;
                    global.device_data = NULL;
                }

                auto count = WdfCollectionGetCount(global.device_collection);

                for (auto i = 0u; i < count; i++) {
                    DeviceSetup(WdfCollectionGetItem(global.device_collection, i));
                }

                WdfWaitLockRelease(global.collection_lock);
            }
            else if (buffer_length == total_size) {
                void* pool = ExAllocatePoolWithTag(PagedPool, alloc_size, POOL_TAG);
                if (!pool) {
                    DebugPrint(("ExAllocatePoolWithTag (PagedPool) failed"));
                    status = STATUS_UNSUCCESSFUL;
                    break;
                }
                RtlCopyMemory(pool, static_cast<BYTE*>(buffer) + SIZEOF_BASE, alloc_size);

                auto* new_mods = static_cast<ra::modifier_settings*>(pool);
                auto* new_devs = static_cast<ra::device_settings*>(
                    static_cast<BYTE*>(pool) + modifier_bytes);

                if (!ValidateAndInitSettings(input, new_mods, new_devs)) {
                    ExFreePoolWithTag(pool, POOL_TAG);
                    status = STATUS_INVALID_PARAMETER;
                    break;
                }

                WriteDelay();

                WdfWaitLockAcquire(global.collection_lock, NULL);

                if (global.modifier_data) {
                    ExFreePoolWithTag(global.modifier_data, POOL_TAG);
                }

                global.device_data = input.device_data_size > 0 ? new_devs : NULL;
                global.modifier_data = new_mods;
                global.base_data = input;

                auto count = WdfCollectionGetCount(global.device_collection);

                for (auto i = 0u; i < count; i++) {
                    DeviceSetup(WdfCollectionGetItem(global.device_collection, i));
                }

                WdfWaitLockRelease(global.collection_lock);
            }
            else {
                status = STATUS_INVALID_PARAMETER;
            }
        }
        break;
    case ra::GET_VERSION:
        status = WdfRequestRetrieveOutputBuffer(
            Request,
            sizeof(ra::version_t),
            &buffer,
            NULL
        );
        if (!NT_SUCCESS(status)) {
            DebugPrint(("RetrieveOutputBuffer failed: 0x%x\n", status));
        }
        else {
            *static_cast<ra::version_t*>(buffer) = ra::version;
            bytes_out = sizeof(ra::version_t);
        }
        break;
    default:
        status = STATUS_INVALID_DEVICE_REQUEST;
        break;
    }

    WdfRequestCompleteWithInformation(Request, status, bytes_out);

}
#pragma warning(pop) // enable 28118 again

VOID
RawaccelInit(WDFDRIVER driver)
{
    NTSTATUS status;

    status = CreateControlDevice(driver);

    if (!NT_SUCCESS(status)) {
        DebugPrint(("CreateControlDevice failed with status 0x%x\n", status));
        return;
    }

    status = WdfCollectionCreate(
        WDF_NO_OBJECT_ATTRIBUTES,
        &global.device_collection
    );

    if (!NT_SUCCESS(status)) {
        DebugPrint(("WdfCollectionCreate failed with status 0x%x\n", status));
        return;
    }

    status = WdfWaitLockCreate(
        WDF_NO_OBJECT_ATTRIBUTES,
        &global.collection_lock
    );

    if (!NT_SUCCESS(status)) {
        DebugPrint(("WdfWaitLockCreate failed with status 0x%x\n", status));
        return;
    }

    LARGE_INTEGER freq;
    KeQueryPerformanceCounter(&freq);
    global.tick_interval = 1e3 / freq.QuadPart;

    global.initialized = true;
}

VOID
DeviceSetup(WDFOBJECT hDevice) 
{
    auto* devExt = FilterGetData(hDevice);

    KIRQL oldIrql;
    KeAcquireSpinLock(&devExt->state_lock, &oldIrql);

    auto set_ext_from_cfg = [devExt](const ra::device_config& cfg) {
        devExt->enable = !cfg.disable;
        devExt->set_extra_info = cfg.set_extra_info;
        devExt->input_dpi_normalization_factor = (cfg.dpi > 0) ? (ra::NORMALIZED_DPI / cfg.dpi) : 1;

        bool rate_given = cfg.polling_rate > 0;

        devExt->keep_time = !(cfg.poll_time_lock && rate_given);

        if (devExt->keep_time) {
            devExt->clamp = cfg.clamp;

            if (rate_given) {
                devExt->clamp.min = 1000.0 / cfg.polling_rate;
            }
        }
        else {
            milliseconds interval = 1000.0 / cfg.polling_rate;
            devExt->clamp = { interval, interval };
        }
    };

    auto set_mod_if_found = [devExt](auto* prof_name) {
        for (auto i = 0u; i < global.base_data.modifier_data_size; i++) {
            auto& profile = global.modifier_data[i].prof;

            if (wcsncmp(prof_name, profile.name, ra::MAX_NAME_LEN) == 0) {
                devExt->mod_settings = global.modifier_data[i];
                devExt->mod = { devExt->mod_settings };
                devExt->speed_processor.init(devExt->mod_settings.prof.speed_processor_args);
                return;
            }
        }
    };

    if (!global.modifier_data) {
        devExt->enable = false;
        devExt->mod = {};
        KeReleaseSpinLock(&devExt->state_lock, oldIrql);
        return;
    }
    
    set_ext_from_cfg(global.base_data.default_dev_cfg);
    devExt->mod_settings = *global.modifier_data;
    devExt->mod = { devExt->mod_settings };
    devExt->speed_processor.init(devExt->mod_settings.prof.speed_processor_args);
    
    for (auto i = 0u; i < global.base_data.device_data_size; i++) {
        auto& dev_settings = global.device_data[i];

        if (wcsncmp(devExt->dev_id, dev_settings.id, ra::MAX_DEV_ID_LEN) == 0) {
            set_ext_from_cfg(dev_settings.config);

            if (dev_settings.profile[0] != L'\0') {
                set_mod_if_found(dev_settings.profile);
            }

            break;
        }
    }

    KeReleaseSpinLock(&devExt->state_lock, oldIrql);
}

VOID
DeviceCleanup(WDFOBJECT hDevice)
{
    PAGED_CODE();
    DebugPrint(("Removing device from collection\n"));

    WdfWaitLockAcquire(global.collection_lock, NULL);
    WdfCollectionRemove(global.device_collection, hDevice);
    WdfWaitLockRelease(global.collection_lock);
}

VOID 
WriteDelay()
{
    LARGE_INTEGER interval;
    interval.QuadPart = static_cast<LONGLONG>(ra::WRITE_DELAY) * -10000;
    KeDelayExecutionThread(KernelMode, FALSE, &interval);
}

NTSTATUS
DriverEntry(
    IN  PDRIVER_OBJECT  DriverObject,
    IN  PUNICODE_STRING RegistryPath
)
/*++
Routine Description:

     Installable driver initialization entry point.
    This entry point is called directly by the I/O system.

--*/
{

    WDF_DRIVER_CONFIG config;
    NTSTATUS status;
    WDFDRIVER driver;

    DebugPrint(("km accel filter.\n"));
    DebugPrint(("Built %s %s\n", __DATE__, __TIME__));

    // Initialize driver config to control the attributes that
    // are global to the driver. Note that framework by default
    // provides a driver unload routine. If you create any resources
    // in the DriverEntry and want to be cleaned in driver unload,
    // you can override that by manually setting the EvtDriverUnload in the
    // config structure. In general xxx_CONFIG_INIT macros are provided to
    // initialize most commonly used members.

    WDF_DRIVER_CONFIG_INIT(
        &config,
        EvtDeviceAdd
    );

    //
    // Create a framework driver object to represent our driver.
    //
    status = WdfDriverCreate(DriverObject,
        RegistryPath,
        WDF_NO_OBJECT_ATTRIBUTES,
        &config,
        &driver);


    if (NT_SUCCESS(status)) {
        if (*InitSafeBootMode == 0) RawaccelInit(driver);
    }
    else {
        DebugPrint(("WdfDriverCreate failed with status 0x%x\n", status));
    }

    return status;
}


NTSTATUS
CreateControlDevice(WDFDRIVER Driver)
/*++
Routine Description:
    This routine is called to create a control device object so that application
    can talk to the filter driver directly instead of going through the entire
    device stack. This kind of control device object is useful if the filter
    driver is underneath another driver which prevents ioctls not known to it
    or if the driver's dispatch routine is owned by some other (port/class)
    driver and it doesn't allow any custom ioctls.
Arguments:
    Driver - Handle to wdf driver object.
Return Value:
    WDF status code
--*/
{
    PWDFDEVICE_INIT             pInit = NULL;
    WDFDEVICE                   controlDevice = NULL;
    WDF_IO_QUEUE_CONFIG         ioQueueConfig;
    NTSTATUS                    status;
    WDFQUEUE                    queue;
    DECLARE_CONST_UNICODE_STRING(ntDeviceName, NTDEVICE_NAME);
    DECLARE_CONST_UNICODE_STRING(symbolicLinkName, SYMBOLIC_NAME_STRING);
    // interactive users can write so settings apply without elevation, others are read-only
    DECLARE_CONST_UNICODE_STRING(controlDeviceSddl,
        L"D:P(A;;GA;;;SY)(A;;GRGWGX;;;BA)(A;;GRGW;;;IU)(A;;GR;;;WD)(A;;GR;;;RC)");

    DebugPrint(("Creating Control Device\n"));

    //
    //
    // In order to create a control device, we first need to allocate a
    // WDFDEVICE_INIT structure and set all properties.
    //
    pInit = WdfControlDeviceInitAllocate(
        Driver,
        &controlDeviceSddl
    );

    if (pInit == NULL) {
        status = STATUS_INSUFFICIENT_RESOURCES;
        goto Error;
    }

    //
    // Set exclusive to false so that more than one app can talk to the
    // control device simultaneously.
    //
    WdfDeviceInitSetExclusive(pInit, FALSE);

    status = WdfDeviceInitAssignName(pInit, &ntDeviceName);

    if (!NT_SUCCESS(status)) {
        goto Error;
    }

    status = WdfDeviceCreate(&pInit,
        WDF_NO_OBJECT_ATTRIBUTES,
        &controlDevice);

    if (!NT_SUCCESS(status)) {
        goto Error;
    }

    //
    // Create a symbolic link for the control object so that usermode can open
    // the device.
    //

    status = WdfDeviceCreateSymbolicLink(controlDevice, &symbolicLinkName);

    if (!NT_SUCCESS(status)) {
        goto Error;
    }

    //
    // Configure the default queue associated with the control device object
    // to be Serial so that request passed to RawaccelControl are serialized.
    //

    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&ioQueueConfig,
        WdfIoQueueDispatchSequential);

    ioQueueConfig.EvtIoDeviceControl = RawaccelControl;

    //
    // Framework by default creates non-power managed queues for
    // filter drivers.
    //
    status = WdfIoQueueCreate(controlDevice,
        &ioQueueConfig,
        WDF_NO_OBJECT_ATTRIBUTES,
        &queue // pointer to default queue
    );
    if (!NT_SUCCESS(status)) {
        goto Error;
    }

    //
    // Control devices must notify WDF when they are done initializing.   I/O is
    // rejected until this call is made.
    //
    WdfControlFinishInitializing(controlDevice);

    return STATUS_SUCCESS;

Error:

    if (pInit != NULL) WdfDeviceInitFree(pInit);

    if (controlDevice != NULL) {
        //
        // Release the reference on the newly created object, since
        // we couldn't initialize it.
        //
        WdfObjectDelete(controlDevice);
    }

    DebugPrint(("CreateControlDevice failed with status code 0x%x\n", status));

    return status;
}

NTSTATUS
EvtDeviceAdd(
    IN WDFDRIVER        Driver,
    IN PWDFDEVICE_INIT  DeviceInit
    )
/*++
Routine Description:

    EvtDeviceAdd is called by the framework in response to AddDevice
    call from the PnP manager. Here you can query the device properties
    using WdfFdoInitWdmGetPhysicalDevice/IoGetDeviceProperty and based
    on that, decide to create a filter device object and attach to the
    function stack.

    If you are not interested in filtering this particular instance of the
    device, you can just return STATUS_SUCCESS without creating a framework
    device.

Arguments:

    Driver - Handle to a framework driver object created in DriverEntry

    DeviceInit - Pointer to a framework-allocated WDFDEVICE_INIT structure.

Return Value:

    NTSTATUS

--*/   
{
    WDF_OBJECT_ATTRIBUTES deviceAttributes;
    NTSTATUS status;
    WDFDEVICE hDevice;
    WDF_IO_QUEUE_CONFIG ioQueueConfig;

    UNREFERENCED_PARAMETER(Driver);

    PAGED_CODE();

    DebugPrint(("Enter FilterEvtDeviceAdd \n"));

    if (!global.initialized) {
        return STATUS_SUCCESS;
    }

    //
    // Tell the framework that you are filter driver. Framework
    // takes care of inherting all the device flags & characterstics
    // from the lower device you are attaching to.
    //
    WdfFdoInitSetFilter(DeviceInit);

    WdfDeviceInitSetDeviceType(DeviceInit, FILE_DEVICE_MOUSE);

    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&deviceAttributes,
        DEVICE_EXTENSION);

    deviceAttributes.EvtCleanupCallback = DeviceCleanup;

    //
    // Create a framework device object.  This call will in turn create
    // a WDM deviceobject, attach to the lower stack and set the
    // appropriate flags and attributes.
    //
    status = WdfDeviceCreate(&DeviceInit, &deviceAttributes, &hDevice);
    if (!NT_SUCCESS(status)) {
        DebugPrint(("WdfDeviceCreate failed with status code 0x%x\n", status));
        return status;
    }

    auto* devExt = FilterGetData(hDevice);
    KeInitializeSpinLock(&devExt->state_lock);
    devExt->dev_id[0] = L'\0';

    //
    // get device id from bus driver
    //
    DEVICE_OBJECT* pdo = WdfDeviceWdmGetPhysicalDevice(hDevice);

    IO_STATUS_BLOCK iosb = {};
    NTSTATUS tmp = STATUS_UNSUCCESSFUL;

    if (pdo != NULL) {
        KEVENT ke;
        KeInitializeEvent(&ke, NotificationEvent, FALSE);

        PIRP Irp = IoBuildSynchronousFsdRequest(IRP_MJ_PNP,
            pdo, NULL, 0, NULL, &ke, &iosb);

        if (Irp != NULL) {
            Irp->IoStatus.Status = STATUS_NOT_SUPPORTED;
            PIO_STACK_LOCATION stack = IoGetNextIrpStackLocation(Irp);
            stack->MinorFunction = IRP_MN_QUERY_ID;
            stack->Parameters.QueryId.IdType = BusQueryDeviceID;

            tmp = IoCallDriver(pdo, Irp);

            if (tmp == STATUS_PENDING) {
                KeWaitForSingleObject(&ke, Executive, KernelMode, FALSE, NULL);
                tmp = iosb.Status;
            }
        }
    }

    if (NT_SUCCESS(tmp) && iosb.Information != 0) {
        auto* id_ptr = reinterpret_cast<WCHAR*>(iosb.Information);
        wcsncpy(devExt->dev_id, id_ptr, ra::MAX_DEV_ID_LEN - 1);
        devExt->dev_id[ra::MAX_DEV_ID_LEN - 1] = L'\0';
        ExFreePool(id_ptr);
    }
    else {
        DebugPrint(("IoCallDriver failed with status 0x%x\n", tmp));
    }

    WdfWaitLockAcquire(global.collection_lock, NULL);

    DeviceSetup(hDevice);

    tmp = WdfCollectionAdd(global.device_collection, hDevice);

    if (!NT_SUCCESS(tmp)) {
        DebugPrint(("WdfCollectionAdd failed with status 0x%x\n", tmp));
    }

    WdfWaitLockRelease(global.collection_lock);

    //
    // Configure the default queue to be Parallel. Do not use sequential queue
    // if this driver is going to be filtering PS2 ports because it can lead to
    // deadlock. The PS2 port driver sends a request to the top of the stack when it
    // receives an ioctl request and waits for it to be completed. If you use a
    // a sequential queue, this request will be stuck in the queue because of the 
    // outstanding ioctl request sent earlier to the port driver.
    //
    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&ioQueueConfig,
                             WdfIoQueueDispatchParallel);

    //
    // Framework by default creates non-power managed queues for
    // filter drivers.
    //
    ioQueueConfig.EvtIoInternalDeviceControl = EvtIoInternalDeviceControl;

    status = WdfIoQueueCreate(hDevice,
                            &ioQueueConfig,
                            WDF_NO_OBJECT_ATTRIBUTES,
                            WDF_NO_HANDLE // pointer to default queue
                            );
    if (!NT_SUCCESS(status)) {
        DebugPrint( ("WdfIoQueueCreate failed 0x%x\n", status));
        return status;
    }

    return status;
}


VOID
EvtIoInternalDeviceControl(
    IN WDFQUEUE      Queue,
    IN WDFREQUEST    Request,
    IN size_t        OutputBufferLength,
    IN size_t        InputBufferLength,
    IN ULONG         IoControlCode
    )
/*++

Routine Description:

    This routine is the dispatch routine for internal device control requests.
    There are two specific control codes that are of interest:
    
    IOCTL_INTERNAL_MOUSE_CONNECT:
        Store the old context and function pointer and replace it with our own.
        This makes life much simpler than intercepting IRPs sent by the RIT and
        modifying them on the way back up.
                                      
    IOCTL_INTERNAL_I8042_HOOK_MOUSE:
        Add in the necessary function pointers and context values so that we can
        alter how the ps/2 mouse is initialized.
                                            
    NOTE:  Handling IOCTL_INTERNAL_I8042_HOOK_MOUSE is *NOT* necessary if 
           all you want to do is filter MOUSE_INPUT_DATAs.  You can remove
           the handling code and all related device extension fields and 
           functions to conserve space.
                                         

--*/
{
    
    PDEVICE_EXTENSION devExt;
    PCONNECT_DATA connectData;
    NTSTATUS status = STATUS_SUCCESS;
    WDFDEVICE hDevice;
    size_t length;

    UNREFERENCED_PARAMETER(OutputBufferLength);
    UNREFERENCED_PARAMETER(InputBufferLength);

    PAGED_CODE();

    hDevice = WdfIoQueueGetDevice(Queue);
    devExt = FilterGetData(hDevice);

    switch (IoControlCode) {

    //
    // Connect a mouse class device driver to the port driver.
    //
    case IOCTL_INTERNAL_MOUSE_CONNECT: {
        //
        // Only allow one connection.
        //
        if (devExt->UpperConnectData.ClassService != NULL) {
            status = STATUS_SHARING_VIOLATION;
            break;
        }

        //
        // Copy the connection parameters to the device extension.
        //
        status = WdfRequestRetrieveInputBuffer(Request,
            sizeof(CONNECT_DATA),
            reinterpret_cast<PVOID*>(&connectData),
            &length);
        if (!NT_SUCCESS(status)) {
            DebugPrint(("WdfRequestRetrieveInputBuffer failed %x\n", status));
            break;
        }

        devExt->UpperConnectData = *connectData;

        //
        // Hook into the report chain.  Everytime a mouse packet is reported to
        // the system, RawaccelCallback will be called
        //

        connectData->ClassDeviceObject = WdfDeviceWdmGetDeviceObject(hDevice);
        connectData->ClassService = RawaccelCallback;

        break;
    }
    //
    // Disconnect a mouse class device driver from the port driver.
    //
    case IOCTL_INTERNAL_MOUSE_DISCONNECT: 
        //
        // Clear the connection parameters in the device extension.
        //
        // devExt->UpperConnectData.ClassDeviceObject = NULL;
        // devExt->UpperConnectData.ClassService = NULL;
        status = STATUS_NOT_IMPLEMENTED;
        break;
    case IOCTL_MOUSE_QUERY_ATTRIBUTES:
    default:
        break;
    }

    if (!NT_SUCCESS(status)) {
        WdfRequestComplete(Request, status);
        return;
    }

    DispatchPassThrough(Request, WdfDeviceGetIoTarget(hDevice));
}


VOID
DispatchPassThrough(
    _In_ WDFREQUEST Request,
    _In_ WDFIOTARGET Target
)
/*++
Routine Description:

    Passes a request on to the lower driver.


--*/
{
    //
    // Pass the IRP to the target
    //

    WDF_REQUEST_SEND_OPTIONS options;
    BOOLEAN ret;
    NTSTATUS status = STATUS_SUCCESS;

    //
    // We are not interested in post processing the IRP so 
    // fire and forget.
    //
    WDF_REQUEST_SEND_OPTIONS_INIT(&options,
        WDF_REQUEST_SEND_OPTION_SEND_AND_FORGET);

    ret = WdfRequestSend(Request, Target, &options);

    if (ret == FALSE) {
        status = WdfRequestGetStatus(Request);
        DebugPrint(("WdfRequestSend failed: 0x%x\n", status));
        WdfRequestComplete(Request, status);
    }

    return;
}
