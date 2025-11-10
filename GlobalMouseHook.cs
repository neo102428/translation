using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

public class GlobalMouseHook : IDisposable
{
    public delegate void MouseHookCallback(Point point);
    public delegate void MouseWheelCallback(int delta);

    public event MouseHookCallback MiddleButtonDown;
    public event MouseHookCallback MiddleButtonUp;
    public event MouseWheelCallback MouseWheelScroll;
    public event MouseHookCallback MouseMove;

    private const int WH_MOUSE_LL = 14;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEMOVE = 0x0200;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private IntPtr _hookID = IntPtr.Zero;
    private readonly LowLevelMouseProc _proc;

    public GlobalMouseHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookID != IntPtr.Zero) return;
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public void Uninstall()
    {
        if (_hookID == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookID);
        _hookID = IntPtr.Zero;
    }

    public void Dispose()
    {
        Uninstall();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            // --- 核心修正：在这里添加了对 null 值的检查 ---
            object? marshalledMouseStruct = Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            if (marshalledMouseStruct != null)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)marshalledMouseStruct;
                Point currentPoint = new Point(hookStruct.pt.x, hookStruct.pt.y);

                switch ((int)wParam)
                {
                    case WM_MBUTTONDOWN:
                        MiddleButtonDown?.Invoke(currentPoint);
                        break;

                    case WM_MBUTTONUP:
                        MiddleButtonUp?.Invoke(currentPoint);
                        break;

                    case WM_MOUSEWHEEL:
                        int delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                        MouseWheelScroll?.Invoke(delta);
                        break;

                    case WM_MOUSEMOVE:
                        MouseMove?.Invoke(currentPoint);
                        break;
                }
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}