using System.Runtime.InteropServices;

namespace ForzaOSD.App;

internal static partial class NativeMethods
{
    internal const uint WM_DESTROY = 0x0002,
        WM_SIZE = 0x0005,
        WM_CLOSE = 0x0010,
        WM_QUIT = 0x0012,
        WM_MOUSEACTIVATE = 0x0021,
        WM_NCHITTEST = 0x0084,
        WM_HOTKEY = 0x0312,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MOUSEWHEEL = 0x020A,
        WM_MOUSEHWHEEL = 0x020E,
        WM_MOUSEMOVE = 0x0200,
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102;
    internal const int MA_NOACTIVATE = 3,
        HTTRANSPARENT = -1,
        GWL_EXSTYLE = -20;
    internal const long WS_EX_TOPMOST = 0x8,
        WS_EX_TRANSPARENT = 0x20,
        WS_EX_TOOLWINDOW = 0x80,
        WS_EX_LAYERED = 0x80000,
        WS_EX_NOACTIVATE = 0x08000000;
    internal const uint WS_POPUP = 0x80000000,
        SW_HIDE = 0,
        SW_SHOWNOACTIVATE = 4;
    internal const uint SWP_NOSIZE = 1,
        SWP_NOMOVE = 2,
        SWP_NOACTIVATE = 0x10,
        SWP_FRAMECHANGED = 0x20,
        SWP_SHOWWINDOW = 0x40;
    internal static readonly nint HWND_TOPMOST = -1;
    internal const uint PM_REMOVE = 1,
        MOD_NOREPEAT = 0x4000,
        LWA_ALPHA = 2;
    internal const uint SPI_GETWORKAREA = 0x0030;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        internal int X,
            Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        internal int Left,
            Top,
            Right,
            Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Msg
    {
        internal nint Hwnd;
        internal uint Message;
        internal nuint WParam;
        internal nint LParam;
        internal uint Time;
        internal Point Pt;
        internal uint Private;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WndClassEx
    {
        internal uint Size,
            Style;
        internal WndProc WndProc;
        internal int ClsExtra,
            WndExtra;
        internal nint Instance,
            Icon,
            Cursor,
            Background;
        internal string? MenuName,
            ClassName;
        internal nint IconSmall;
    }

    internal delegate nint WndProc(nint hwnd, uint message, nuint wParam, nint lParam);
    internal delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Margins
    {
        internal int Left;
        internal int Right;
        internal int Top;
        internal int Bottom;
    }

    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode)]
    internal static extern ushort RegisterClassEx(ref WndClassEx value);

    [LibraryImport(
        "user32.dll",
        EntryPoint = "UnregisterClassW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterClass(string name, nint instance);

    [LibraryImport(
        "user32.dll",
        EntryPoint = "CreateWindowExW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    internal static partial nint CreateWindowEx(
        long ex,
        string cls,
        string title,
        uint style,
        int x,
        int y,
        int w,
        int h,
        nint parent,
        nint menu,
        nint instance,
        nint param
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    internal static partial nint DefWindowProc(nint hwnd, uint msg, nuint wp, nint lp);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PeekMessage(
        out Msg msg,
        nint hwnd,
        uint min,
        uint max,
        uint remove
    );

    [LibraryImport("user32.dll")]
    internal static partial nint DispatchMessage(ref Msg msg);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TranslateMessage(ref Msg msg);

    [LibraryImport("user32.dll")]
    internal static partial void PostQuitMessage(int code);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hwnd, uint command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(
        nint hwnd,
        nint after,
        int x,
        int y,
        int w,
        int h,
        uint flags
    );

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial nint GetWindowLongPtr(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial nint SetWindowLongPtr(nint hwnd, int index, nint value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(nint hwnd, out Rect rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ClientToScreen(nint hwnd, ref Point point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumWindows(EnumWindowsProc callback, nint param);

    [LibraryImport(
        "user32.dll",
        EntryPoint = "GetWindowTextW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    internal static partial int GetWindowText(nint hwnd, [Out] char[] text, int count);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(nint hwnd);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(nint hwnd, int id);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetLayeredWindowAttributes(
        nint hwnd,
        uint key,
        byte alpha,
        uint flags
    );

    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "GetModuleHandleW",
        StringMarshalling = StringMarshalling.Utf16
    )]
    internal static partial nint GetModuleHandle(string? module);

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out Point point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ScreenToClient(nint hwnd, ref Point point);

    [LibraryImport("user32.dll")]
    internal static partial short GetAsyncKeyState(int key);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SystemParametersInfo(
        uint action,
        uint parameter,
        out Rect value,
        uint update
    );
}
