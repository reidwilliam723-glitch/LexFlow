using System.Runtime.InteropServices;
using LexFlow.Input.Interfaces;

namespace LexFlow.Input;

public sealed class MouseListener : IDisposable
{
    public event EventHandler<MouseButtonEventArgs>? RightButtonDown;
    public event EventHandler<MouseButtonEventArgs>? LeftButtonUp;

    private bool _isRunning;
    private Thread? _messageThread;
    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelMouseProc? _hookProc;
    private uint _messageThreadId;
    private readonly ManualResetEventSlim _hookReady = new(false);

    private const int WH_MOUSE_LL = 14;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_LBUTTONUP = 0x0202;
    private const uint WM_QUIT = 0x0012;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public int X;
        public int Y;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
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

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _hookReady.Reset();
        _messageThread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "LexFlow Mouse Listener"
        };
        _messageThread.Start();
        _hookReady.Wait(3000);
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
        if (_messageThreadId != 0)
        {
            PostThreadMessage(_messageThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        _messageThread?.Join(1000);
    }

    private void MessageLoop()
    {
        _messageThreadId = GetCurrentThreadId();
        _hookProc = HookCallback;
        var module = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, module, 0);
        _hookReady.Set();
        try
        {
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == 0)
        {
            var message = wParam.ToInt32();
            if (message is WM_RBUTTONDOWN or WM_LBUTTONUP)
            {
                var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                if (info.dwExtraInfo.ToUInt64() != InputMarkers.LexFlowInjectedMarker)
                {
                    var args = new MouseButtonEventArgs { X = info.X, Y = info.Y };
                    if (message == WM_RBUTTONDOWN)
                    {
                        RightButtonDown?.Invoke(this, args);
                    }
                    else
                    {
                        LeftButtonUp?.Invoke(this, args);
                    }
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}

public sealed class MouseButtonEventArgs : EventArgs
{
    public int X { get; set; }
    public int Y { get; set; }
}
