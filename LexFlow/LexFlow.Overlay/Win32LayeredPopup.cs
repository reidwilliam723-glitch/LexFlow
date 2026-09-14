using System.Runtime.InteropServices;

namespace LexFlow.Overlay;

/// <summary>
/// Shared layered, topmost, non-activating popup with its own message-loop thread.
/// Matches SuggestionOverlay's windowing approach.
/// </summary>
public abstract class Win32LayeredPopup : IDisposable
{
    protected IntPtr WindowHandle;
    private WndProcDelegate? _wndProcDelegate;
    private Thread? _messageThread;
    private readonly ManualResetEventSlim _windowReady = new(false);
    private readonly string _className;
    private readonly string _title;
    protected int LastX;
    protected int LastY;
    protected int LastWidth;
    protected int LastHeight;
    protected volatile bool IsShowing;
    private int _commandId;
    private uint _messageThreadId;

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    protected const uint SW_SHOWNA = 8;
    protected const uint SW_HIDE = 0;
    protected const uint SWP_NOACTIVATE = 0x0010;
    protected const uint SWP_SHOWWINDOW = 0x0040;
    protected const uint LWA_ALPHA = 0x0002;
    protected const uint WM_PAINT = 0x000F;
    protected const uint WM_MOUSEACTIVATE = 0x0021;
    protected const uint WM_LBUTTONDOWN = 0x0201;
    protected const uint WM_MOUSEMOVE = 0x0200;
    protected const uint WM_DESTROY = 0x0002;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;
    protected const uint WM_APP = 0x8000;
    protected const uint WM_LEXFLOW_SHOW = WM_APP + 1;
    protected const uint WM_LEXFLOW_HIDE = WM_APP + 2;
    protected const uint WM_LEXFLOW_REPAINT = WM_APP + 3;
    protected const uint MA_NOACTIVATE = 3;
    protected static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr DpiAwarenessContextPerMonitorV2 = new(-4);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    protected static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

    [DllImport("user32.dll")]
    protected static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    protected static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClass(ref WndClass lpWndClass);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    protected static extern IntPtr BeginPaint(IntPtr hWnd, out PaintStruct lpPaint);

    [DllImport("user32.dll")]
    protected static extern bool EndPaint(IntPtr hWnd, ref PaintStruct lpPaint);

    [DllImport("user32.dll")]
    protected static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    protected static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Point pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct PaintStruct
    {
        public IntPtr hdc;
        [MarshalAs(UnmanagedType.Bool)] public bool fErase;
        public Rect rcPaint;
        [MarshalAs(UnmanagedType.Bool)] public bool fRestore;
        [MarshalAs(UnmanagedType.Bool)] public bool fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct Rect
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int x, y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    protected readonly OverlayChrome Chrome;
    private readonly OverlayThemeHost _themeHost;
    private readonly EventHandler _paletteChangedHandler;

    protected Win32LayeredPopup(string className, string title, OverlayThemeHost themeHost)
    {
        _className = className;
        _title = title;
        _themeHost = themeHost ?? throw new ArgumentNullException(nameof(themeHost));
        Chrome = new OverlayChrome(_themeHost.Palette);
        _paletteChangedHandler = (_, _) => Repaint();
        _themeHost.Palette.Changed += _paletteChangedHandler;
        InitializeWindow();
    }

    private void InitializeWindow()
    {
        _wndProcDelegate = WindowProc;
        var wndClass = new WndClass
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = GetModuleHandle(string.Empty),
            lpszClassName = _className,
            lpszMenuName = null!
        };
        RegisterClass(ref wndClass);

        _messageThread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = _className
        };
        _messageThread.SetApartmentState(ApartmentState.STA);
        _messageThread.Start();
        if (!_windowReady.Wait(2000) || WindowHandle == IntPtr.Zero)
        {
            throw new TimeoutException($"{_className} window creation timed out");
        }
    }

    private void MessageLoop()
    {
        SetThreadDpiAwarenessContext(DpiAwarenessContextPerMonitorV2);
        _messageThreadId = GetCurrentThreadId();
        WindowHandle = CreateWindowEx(
            WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
            _className,
            _title,
            WS_POPUP,
            0, 0, 420, 280,
            IntPtr.Zero, IntPtr.Zero, GetModuleHandle(string.Empty), IntPtr.Zero);

        if (WindowHandle != IntPtr.Zero)
        {
            SetLayeredWindowAttributes(WindowHandle, 0, 245, LWA_ALPHA);
        }

        _windowReady.Set();
        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    protected virtual IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        switch (uMsg)
        {
            case WM_PAINT:
                var hdc = BeginPaint(hWnd, out var ps);
                try
                {
                    using var g = System.Drawing.Graphics.FromHdc(hdc);
                    Paint(g);
                }
                finally
                {
                    EndPaint(hWnd, ref ps);
                }
                return IntPtr.Zero;
            case WM_MOUSEACTIVATE:
                return (IntPtr)MA_NOACTIVATE;
            case WM_KEYDOWN:
            case WM_KEYUP:
            case WM_CHAR:
            case WM_SYSKEYDOWN:
            case WM_SYSKEYUP:
                return IntPtr.Zero;
            case WM_LBUTTONDOWN:
                OnClick(lParam);
                return IntPtr.Zero;
            case WM_LEXFLOW_SHOW:
                ApplyShow();
                return IntPtr.Zero;
            case WM_LEXFLOW_HIDE:
                ApplyHide();
                return IntPtr.Zero;
            case WM_LEXFLOW_REPAINT:
                if (WindowHandle != IntPtr.Zero)
                {
                    Invalidate(WindowHandle);
                }
                return IntPtr.Zero;
            case WM_DESTROY:
                PostQuitMessage(0);
                return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }

    [DllImport("user32.dll")]
    private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    protected void Invalidate(IntPtr handle) => InvalidateRect(handle, IntPtr.Zero, true);

    protected void Repaint()
    {
        if (WindowHandle != IntPtr.Zero)
        {
            PostMessage(WindowHandle, WM_LEXFLOW_REPAINT, IntPtr.Zero, IntPtr.Zero);
        }
    }

    protected abstract void Paint(System.Drawing.Graphics g);
    protected virtual void OnClick(IntPtr lParam) { }

    protected void RequestShow(int x, int y, int width, int height)
    {
        LastX = x;
        LastY = y;
        LastWidth = width;
        LastHeight = height;
        IsShowing = true;
        Interlocked.Increment(ref _commandId);
        if (WindowHandle != IntPtr.Zero)
        {
            PostMessage(WindowHandle, WM_LEXFLOW_SHOW, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public virtual void Hide()
    {
        IsShowing = false;
        if (WindowHandle == IntPtr.Zero)
        {
            return;
        }

        Interlocked.Increment(ref _commandId);
        if (GetCurrentThreadId() == _messageThreadId)
        {
            ApplyHide();
            return;
        }

        SendMessage(WindowHandle, WM_LEXFLOW_HIDE, IntPtr.Zero, IntPtr.Zero);
    }

    private void ApplyShow()
    {
        if (!IsShowing || WindowHandle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(WindowHandle, HwndTopMost, LastX, LastY, LastWidth, LastHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        ShowWindow(WindowHandle, SW_SHOWNA);
        Invalidate(WindowHandle);
    }

    private void ApplyHide()
    {
        if (WindowHandle != IntPtr.Zero)
        {
            ShowWindow(WindowHandle, SW_HIDE);
        }
    }

    protected static (int X, int Y) ClampToWorkArea(int x, int y, int width, int height)
    {
        var work = GetWorkArea(x, y);
        if (x + width > work.right)
        {
            x = work.right - width;
        }

        if (y + height > work.bottom)
        {
            y = work.bottom - height;
        }

        x = Math.Max(work.left, x);
        y = Math.Max(work.top, y);
        return (x, y);
    }

    private static Rect GetWorkArea(int x, int y)
    {
        var point = new Point { x = x, y = y };
        var monitor = MonitorFromPoint(point, 2);
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
        {
            return info.rcWork;
        }

        return new Rect { left = 0, top = 0, right = GetSystemMetrics(0), bottom = GetSystemMetrics(1) };
    }

    protected static (int X, int Y) UnpackPoint(IntPtr lParam)
    {
        var packed = lParam.ToInt64();
        return ((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF));
    }

    public bool Visible => IsShowing;

    public virtual void Dispose()
    {
        _themeHost.Palette.Changed -= _paletteChangedHandler;
        Hide();
        if (WindowHandle != IntPtr.Zero)
        {
            DestroyWindow(WindowHandle);
            WindowHandle = IntPtr.Zero;
        }
    }
}
