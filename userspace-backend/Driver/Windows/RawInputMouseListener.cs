using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RawAccel.Contracts;

namespace userspace_backend.Driver.Windows
{
    // Captures live mouse speed via Win32 raw input on its own message-only
    // window + thread (Avalonia exposes no WndProc to hook). Reports in chart
    // units: counts/ms normalized to 1000 DPI.
    //
    // Raw input keys devices by HANDLE; config keys DPI by hardware-id. We map
    // handle -> id via wrapper.dll's MultiHandleDevice; unmapped handles use the
    // default factor.
    internal sealed class RawInputMouseListener : IDisposable
    {
        // No movement for this long => Zero (line eases back to rest).
        private const double FreshnessMs = 150.0;

        // Clamp inter-event interval so bursts/stalls don't spike or flatline.
        // 0.1 ms = 10 kHz ceiling, above any real polling rate.
        private const double MinIntervalMs = 0.1;
        private const double MaxIntervalMs = 100.0;

        private const int LifecycleTimeoutMs = 2000;

        // Win32 constants.
        private const uint WM_DESTROY = 0x0002;
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_QUIT = 0x0012;
        private const uint WM_INPUT = 0x00FF;
        private const uint WM_INPUT_DEVICE_CHANGE = 0x00FE;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const uint RIDEV_DEVNOTIFY = 0x00002000;
        private const uint RIM_TYPEMOUSE = 0;
        private const ushort MOUSE_MOVE_ABSOLUTE = 0x01;
        private const uint RAWINPUT_ERROR = unchecked((uint)-1);
        private static readonly IntPtr HWND_MESSAGE = new(-3);

        // Distinct class name per instance: a re-created listener can't collide
        // with a not-yet-freed class.
        private static int instanceCounter;

        private readonly ILogger logger;
        private readonly string className;
        private readonly object gate = new();
        private readonly object lifecycleGate = new();
        private readonly ManualResetEventSlim ready = new(false);

        private Thread? thread;
        private uint nativeThreadId;
        private IntPtr hwnd;
        private WndProcDelegate? wndProc; // kept alive against GC for RegisterClass
        private volatile bool running;
        private volatile bool disposed;

        // Latest speed (under gate); lastEventTimestamp is the freshness +
        // inter-event clock (Interlocked).
        private double lastX, lastY, lastCombined;
        private long lastEventTimestamp;

        // handle -> NormalizedDpi/dpi; defaultFactor for the rest. Volatile +
        // build-once-publish keeps HandleRawInput lock-free on the hot path.
        private volatile Dictionary<IntPtr, double> handleFactors = new();
        private double defaultFactor = 1.0;

        // Applied config's DPI-by-id, used to rebuild handleFactors.
        private Dictionary<string, int> dpiById = new(StringComparer.OrdinalIgnoreCase);
        private int defaultDpi = (int)RawAccelConstants.NormalizedDpi;

        public RawInputMouseListener(ILogger? logger = null)
        {
            this.logger = logger ?? NullLogger.Instance;
            int id = Interlocked.Increment(ref instanceCounter);
            className = $"RawAccelRawInputSink_{Environment.ProcessId}_{id}";
        }

        // Starts the capture thread. Idempotent. Blocks briefly until ready.
        public void Start()
        {
            lock (lifecycleGate)
            {
                if (disposed || thread != null) return;
                thread = new Thread(ThreadMain)
                {
                    IsBackground = true,
                    Name = "RawAccelRawInput",
                };
                thread.Start();
            }
            ready.Wait(LifecycleTimeoutMs);
        }

        // Feeds per-device DPI from the applied config. Safe before Start.
        public void UpdateDevices(RawAccelConfig config)
        {
            int defDpi = config.defaultDeviceConfig?.dpi ?? (int)RawAccelConstants.NormalizedDpi;
            if (defDpi <= 0) defDpi = (int)RawAccelConstants.NormalizedDpi;

            var byId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (config.devices != null)
            {
                foreach (var dev in config.devices)
                {
                    if (string.IsNullOrEmpty(dev.id)) continue;
                    byId[dev.id] = dev.config?.dpi ?? defDpi;
                }
            }

            lock (gate)
            {
                dpiById = byId;
                defaultDpi = defDpi;
            }
            RebuildHandleMap();
        }

        // Current normalized input speed; Zero if idle/unavailable.
        public MouseSpeedSample CurrentSample()
        {
            if (!running) return MouseSpeedSample.Zero;

            long last = Interlocked.Read(ref lastEventTimestamp);
            if (last == 0) return MouseSpeedSample.Zero;

            double sinceMs = (Stopwatch.GetTimestamp() - last) * 1000.0 / Stopwatch.Frequency;
            if (sinceMs > FreshnessMs) return MouseSpeedSample.Zero;

            lock (gate)
            {
                return new MouseSpeedSample(lastX, lastY, lastCombined);
            }
        }

        public void Dispose()
        {
            lock (lifecycleGate)
            {
                if (disposed) return;
                disposed = true;
                running = false;
            }

            // Paired with ThreadMain's Volatile.Write outside any lock.
            uint tid = Volatile.Read(ref nativeThreadId);
            if (tid != 0)
            {
                // Wake the loop; the thread tears down its own window/class.
                PostThreadMessageW(tid, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            }
            thread?.Join(LifecycleTimeoutMs);
            ready.Dispose();
        }

        // ----------------------------------------------------------------------------
        // Capture thread
        // ----------------------------------------------------------------------------

        private void ThreadMain()
        {
            Volatile.Write(ref nativeThreadId, GetCurrentThreadId());

            IntPtr hInstance = GetModuleHandleW(null);
            wndProc = WindowProc;

            var wc = new WNDCLASS
            {
                lpfnWndProc = wndProc,
                hInstance = hInstance,
                lpszClassName = className,
            };

            bool classRegistered = false;

            try
            {
                if (RegisterClassW(ref wc) == 0)
                {
                    logger.LogWarning("RegisterClass failed (err {Err}); speed line disabled",
                        Marshal.GetLastWin32Error());
                    ready.Set();
                    return;
                }
                classRegistered = true;

                hwnd = CreateWindowExW(0, className, string.Empty, 0, 0, 0, 0, 0,
                    HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);
                if (hwnd == IntPtr.Zero)
                {
                    logger.LogWarning("CreateWindowEx failed (err {Err}); speed line disabled",
                        Marshal.GetLastWin32Error());
                    ready.Set();
                    return;
                }

                var devices = new[]
                {
                    new RAWINPUTDEVICE
                    {
                        UsagePage = 0x01, // generic desktop
                        Usage = 0x02,     // mouse
                        Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY,
                        hwndTarget = hwnd,
                    },
                };
                if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
                {
                    logger.LogWarning("RegisterRawInputDevices failed (err {Err}); speed line disabled",
                        Marshal.GetLastWin32Error());
                    ready.Set();
                    return;
                }

                RebuildHandleMap();
                running = true;
                ready.Set();

                logger.LogDebug(
                    "raw input listener active: hwnd=0x{Hwnd:x}, mapped handles={Count}",
                    hwnd.ToInt64(), handleFactors.Count);

                while (GetMessageW(out MSG msg, IntPtr.Zero, 0, 0) > 0)
                {
                    DispatchMessageW(ref msg);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "raw input listener thread failed");
                ready.Set();
            }
            finally
            {
                running = false;
                if (hwnd != IntPtr.Zero)
                {
                    DestroyWindow(hwnd);
                    hwnd = IntPtr.Zero;
                }
                if (classRegistered) UnregisterClassW(className, hInstance);
            }
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_INPUT:
                    try { HandleRawInput(lParam); }
                    catch (Exception ex) { logger.LogTrace(ex, "WM_INPUT handling failed"); }
                    return DefWindowProcW(hWnd, msg, wParam, lParam);

                case WM_INPUT_DEVICE_CHANGE:
                    try { RebuildHandleMap(); }
                    catch (Exception ex) { logger.LogTrace(ex, "device map rebuild failed"); }
                    return IntPtr.Zero;

                case WM_CLOSE:
                    DestroyWindow(hWnd);
                    return IntPtr.Zero;

                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;

                default:
                    return DefWindowProcW(hWnd, msg, wParam, lParam);
            }
        }

        // Cached: Marshal reflection per WM_INPUT would burn the hot path.
        private static readonly uint RawInputMouseSize = (uint)Marshal.SizeOf<RAWINPUTMOUSE>();
        private static readonly uint RawInputHeaderSize =
            (uint)Marshal.OffsetOf<RAWINPUTMOUSE>(nameof(RAWINPUTMOUSE.MouseFlags));

        private void HandleRawInput(IntPtr hRawInput)
        {
            uint size = RawInputMouseSize;
            if (GetRawInputData(hRawInput, RID_INPUT, out RAWINPUTMOUSE data, ref size, RawInputHeaderSize)
                == RAWINPUT_ERROR)
            {
                return;
            }

            if (data.Type != RIM_TYPEMOUSE) return;
            if ((data.MouseFlags & MOUSE_MOVE_ABSOLUTE) != 0) return; // only relative motion
            if (data.LastX == 0 && data.LastY == 0) return;

            double factor = FactorForHandle(data.Device);

            long now = Stopwatch.GetTimestamp();
            long prev = Interlocked.Exchange(ref lastEventTimestamp, now);
            double dtMs = prev == 0
                ? MaxIntervalMs
                : (now - prev) * 1000.0 / Stopwatch.Frequency;
            if (dtMs < MinIntervalMs) dtMs = MinIntervalMs;
            else if (dtMs > MaxIntervalMs) dtMs = MaxIntervalMs;

            double dx = data.LastX;
            double dy = data.LastY;
            double speedX = Math.Abs(dx) * factor / dtMs;
            double speedY = Math.Abs(dy) * factor / dtMs;
            double speedCombined = Math.Sqrt(dx * dx + dy * dy) * factor / dtMs;

            lock (gate)
            {
                lastX = speedX;
                lastY = speedY;
                lastCombined = speedCombined;
            }
        }

        private static double FactorFor(int dpi) =>
            dpi > 0 ? RawAccelConstants.NormalizedDpi / dpi : 1.0;

        private double FactorForHandle(IntPtr handle)
        {
            var map = handleFactors;
            return map.TryGetValue(handle, out double f) ? f : Volatile.Read(ref defaultFactor);
        }

        // Rebuilds handle -> factor from the live device list + config.
        private void RebuildHandleMap()
        {
            var map = new Dictionary<IntPtr, double>();
            Dictionary<string, int> byId;
            int defDpi;
            lock (gate)
            {
                byId = dpiById;
                defDpi = defaultDpi;
            }
            double defFactor = FactorFor(defDpi);

            try
            {
                foreach (MultiHandleDevice device in MultiHandleDevice.GetList())
                {
                    int dpi = (device.id != null && byId.TryGetValue(device.id, out int d) && d > 0)
                        ? d
                        : defDpi;
                    double factor = FactorFor(dpi);
                    foreach (IntPtr handle in device.handles)
                    {
                        map[handle] = factor;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "enumerating raw input devices failed");
            }

            Volatile.Write(ref defaultFactor, defFactor);
            handleFactors = map; // volatile store publishes the new map
        }

        // ----------------------------------------------------------------------------
        // P/Invoke
        // ----------------------------------------------------------------------------

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASS
        {
            public uint style;
            public WndProcDelegate lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr hwndTarget;
        }

        // Flattened RAWINPUTHEADER + RAWMOUSE; Padding mirrors the native
        // union's 4-byte alignment after MouseFlags.
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTMOUSE
        {
            // RAWINPUTHEADER
            public uint Type;
            public uint Size;
            public IntPtr Device;
            public IntPtr wParam;
            // RAWMOUSE
            public ushort MouseFlags;
            public ushort Padding;
            public ushort ButtonFlags;
            public ushort ButtonData;
            public uint RawButtons;
            public int LastX;
            public int LastY;
            public uint ExtraInformation;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool UnregisterClassW(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName,
            string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd,
            uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DispatchMessageW(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostThreadMessageW(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            [In] RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand,
            out RAWINPUTMOUSE pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string? lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
