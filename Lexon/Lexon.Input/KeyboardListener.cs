using Lexon.Input.Interfaces;
using System.Runtime.InteropServices;

namespace Lexon.Input;

/// <summary>
/// Global keyboard listener using a low-level hook so overlay keys (Tab/Esc)
/// can be swallowed instead of going into the target application.
/// </summary>
public class KeyboardListener : IKeyboardListener
{
    private bool _isRunning;
    private Thread? _messageThread;
    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;
    private uint _messageThreadId;
    private readonly ManualResetEventSlim _hookReady = new(false);
    private int _swallowKeyUpVk = -1;

    public event EventHandler<KeyboardEventArgs>? KeyPressed;
    public event EventHandler<KeyboardEventArgs>? KeyReleased;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const uint WM_QUIT = 0x0012;
    private const int HC_ACTION = 0;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

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
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
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
        if (_isRunning) return;

        _hookReady.Reset();
        _messageThread = new Thread(MessageLoopThread)
        {
            IsBackground = true,
            Name = "Lexon Keyboard Listener"
        };
        _messageThread.Start();
        if (!_hookReady.Wait(3000))
        {
            DiagnosticLog.Write("KeyboardListener: hook thread did not become ready in time");
        }

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

    private void MessageLoopThread()
    {
        _messageThreadId = GetCurrentThreadId();
        _hookProc = HookCallback;

        var module = GetModuleHandle(null);
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, module, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, IntPtr.Zero, 0);
        }

        if (_hookHandle == IntPtr.Zero)
        {
            DiagnosticLog.Write($"KeyboardListener: SetWindowsHookEx failed Win32Error={Marshal.GetLastWin32Error()}");
            _hookReady.Set();
            return;
        }

        DiagnosticLog.Write($"KeyboardListener: hook installed hwndModule=0x{module:X} hook=0x{_hookHandle:X} thread={_messageThreadId}");
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
        if (nCode == HC_ACTION)
        {
            var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (info.dwExtraInfo.ToUInt64() == InputMarkers.LexonInjectedMarker)
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            var message = wParam.ToInt32();
            var isDown = message is WM_KEYDOWN or WM_SYSKEYDOWN;
            var isUp = message is WM_KEYUP or WM_SYSKEYUP;

            if (isDown || isUp)
            {
                var args = new KeyboardEventArgs
                {
                    VirtualKey = (int)info.vkCode,
                    IsShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0,
                    IsControlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0,
                    IsAltPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0
                };

                try
                {
                    if (isDown)
                    {
                        KeyPressed?.Invoke(this, args);
                        if (args.Handled)
                        {
                            _swallowKeyUpVk = args.VirtualKey;
                        }
                    }
                    else
                    {
                        if (_swallowKeyUpVk == args.VirtualKey)
                        {
                            args.Handled = true;
                            _swallowKeyUpVk = -1;
                        }

                        KeyReleased?.Invoke(this, args);
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticLog.Write($"KeyboardListener handler exception: {ex.Message}");
                }

                if (args.Handled)
                {
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}
