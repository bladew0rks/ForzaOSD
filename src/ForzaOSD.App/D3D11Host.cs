using System.Numerics;
using ImGuiNET;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace ForzaOSD.App;

internal sealed class D3D11Host : IDisposable
{
    private static readonly (int VirtualKey, ImGuiKey Key)[] KeyboardMap =
    [
        (0x08, ImGuiKey.Backspace),
        (0x09, ImGuiKey.Tab),
        (0x0D, ImGuiKey.Enter),
        (0x1B, ImGuiKey.Escape),
        (0x20, ImGuiKey.Space),
        (0x23, ImGuiKey.End),
        (0x24, ImGuiKey.Home),
        (0x25, ImGuiKey.LeftArrow),
        (0x26, ImGuiKey.UpArrow),
        (0x27, ImGuiKey.RightArrow),
        (0x28, ImGuiKey.DownArrow),
        (0x2D, ImGuiKey.Insert),
        (0x2E, ImGuiKey.Delete),
        (0x30, ImGuiKey._0),
        (0x31, ImGuiKey._1),
        (0x32, ImGuiKey._2),
        (0x33, ImGuiKey._3),
        (0x34, ImGuiKey._4),
        (0x35, ImGuiKey._5),
        (0x36, ImGuiKey._6),
        (0x37, ImGuiKey._7),
        (0x38, ImGuiKey._8),
        (0x39, ImGuiKey._9),
        (0x41, ImGuiKey.A),
        (0x43, ImGuiKey.C),
        (0x45, ImGuiKey.E),
        (0x56, ImGuiKey.V),
        (0x58, ImGuiKey.X),
        (0x59, ImGuiKey.Y),
        (0x5A, ImGuiKey.Z),
        (0x60, ImGuiKey.Keypad0),
        (0x61, ImGuiKey.Keypad1),
        (0x62, ImGuiKey.Keypad2),
        (0x63, ImGuiKey.Keypad3),
        (0x64, ImGuiKey.Keypad4),
        (0x65, ImGuiKey.Keypad5),
        (0x66, ImGuiKey.Keypad6),
        (0x67, ImGuiKey.Keypad7),
        (0x68, ImGuiKey.Keypad8),
        (0x69, ImGuiKey.Keypad9),
        (0x6A, ImGuiKey.KeypadMultiply),
        (0x6B, ImGuiKey.KeypadAdd),
        (0x6D, ImGuiKey.KeypadSubtract),
        (0x6E, ImGuiKey.KeypadDecimal),
        (0x6F, ImGuiKey.KeypadDivide),
        (0xA0, ImGuiKey.LeftShift),
        (0xA1, ImGuiKey.RightShift),
        (0xA2, ImGuiKey.LeftCtrl),
        (0xA3, ImGuiKey.RightCtrl),
        (0xA4, ImGuiKey.LeftAlt),
        (0xA5, ImGuiKey.RightAlt),
        (0xBB, ImGuiKey.Equal),
        (0xBC, ImGuiKey.Comma),
        (0xBD, ImGuiKey.Minus),
        (0xBE, ImGuiKey.Period),
    ];

    private readonly nint hwnd;
    private readonly ID3D11Device device;
    private readonly ID3D11DeviceContext context;
    private readonly IDXGISwapChain1 swapChain;
    private ID3D11RenderTargetView? target;
    private readonly ImGuiRenderer renderer;
    private readonly bool[] keyboardState = new bool[256];
    private bool ctrlDown,
        shiftDown,
        altDown,
        capturingInput;
    internal int Width { get; private set; }
    internal int Height { get; private set; }
    internal ID3D11Device Device => device;
    internal ImGuiRenderer Renderer => renderer;

    internal D3D11Host(nint hwnd, int width, int height)
    {
        this.hwnd = hwnd;
        Width = width;
        Height = height;
        D3D11CreateDevice(
                IntPtr.Zero,
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                [FeatureLevel.Level_11_0, FeatureLevel.Level_10_0],
                out device,
                out _,
                out context
            )
            .CheckError();
        using var factory = CreateDXGIFactory1<IDXGIFactory2>();
        var desc = new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.R8G8B8A8_UNorm,
            BufferCount = 2,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = SampleDescription.Default,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.Discard,
            AlphaMode = AlphaMode.Ignore,
        };
        swapChain = factory.CreateSwapChainForHwnd(
            device,
            hwnd,
            desc,
            new SwapChainFullscreenDescription { Windowed = true }
        );
        factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter);
        CreateTarget();
        renderer = new(device, context);
    }

    internal void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || width == Width && height == Height)
            return;
        target?.Dispose();
        target = null;
        Width = width;
        Height = height;
        swapChain.ResizeBuffers(0, (uint)width, (uint)height, Format.Unknown, SwapChainFlags.None);
        CreateTarget();
    }

    private void CreateTarget()
    {
        using var buffer = swapChain.GetBuffer<ID3D11Texture2D>(0);
        target = device.CreateRenderTargetView(buffer);
    }

    internal void NewFrame(float delta, bool captureInput)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(Width, Height);
        io.DeltaTime = delta;
        if (captureInput)
        {
            PollKeyboard();
            if (NativeMethods.GetCursorPos(out var p) && NativeMethods.ScreenToClient(hwnd, ref p))
                io.AddMousePosEvent(p.X, p.Y);
        }
        else if (capturingInput)
        {
            ReleaseInput(io);
        }
        capturingInput = captureInput;
        ImGui.NewFrame();
    }

    private void ReleaseInput(ImGuiIOPtr io)
    {
        foreach (var (virtualKey, key) in KeyboardMap)
        {
            if (!keyboardState[virtualKey])
                continue;
            keyboardState[virtualKey] = false;
            io.AddKeyEvent(key, false);
        }
        SetModifier(io, ImGuiKey.ModCtrl, false, ref ctrlDown);
        SetModifier(io, ImGuiKey.ModShift, false, ref shiftDown);
        SetModifier(io, ImGuiKey.ModAlt, false, ref altDown);
        io.AddMouseButtonEvent(0, false);
        io.AddMouseButtonEvent(1, false);
        io.AddMouseButtonEvent(2, false);
        io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
    }

    internal bool ProcessMessage(uint message, nuint wp, nint lp)
    {
        if (ImGui.GetCurrentContext() == 0)
            return false;
        var io = ImGui.GetIO();
        switch (message)
        {
            case NativeMethods.WM_LBUTTONDOWN:
                PollKeyboard();
                io.AddMouseButtonEvent(0, true);
                return true;
            case NativeMethods.WM_LBUTTONUP:
                io.AddMouseButtonEvent(0, false);
                return true;
            case NativeMethods.WM_RBUTTONDOWN:
                io.AddMouseButtonEvent(1, true);
                return true;
            case NativeMethods.WM_RBUTTONUP:
                io.AddMouseButtonEvent(1, false);
                return true;
            case NativeMethods.WM_MBUTTONDOWN:
                io.AddMouseButtonEvent(2, true);
                return true;
            case NativeMethods.WM_MBUTTONUP:
                io.AddMouseButtonEvent(2, false);
                return true;
            case NativeMethods.WM_MOUSEWHEEL:
                io.AddMouseWheelEvent(0, (short)(wp >> 16) / 120f);
                return true;
            case NativeMethods.WM_MOUSEHWHEEL:
                io.AddMouseWheelEvent((short)(wp >> 16) / 120f, 0);
                return true;
            case NativeMethods.WM_CHAR:
                io.AddInputCharacter((uint)wp);
                return true;
        }
        return false;
    }

    private void PollKeyboard()
    {
        var io = ImGui.GetIO();
        var ctrl = IsKeyDown(0xA2) || IsKeyDown(0xA3);
        var shift = IsKeyDown(0xA0) || IsKeyDown(0xA1);
        var alt = IsKeyDown(0xA4) || IsKeyDown(0xA5);

        SetModifier(io, ImGuiKey.ModCtrl, ctrl, ref ctrlDown);
        SetModifier(io, ImGuiKey.ModShift, shift, ref shiftDown);
        SetModifier(io, ImGuiKey.ModAlt, alt, ref altDown);

        foreach (var (virtualKey, key) in KeyboardMap)
        {
            var down = IsKeyDown(virtualKey);
            if (keyboardState[virtualKey] == down)
                continue;
            keyboardState[virtualKey] = down;
            io.AddKeyEvent(key, down);
            if (down && !ctrl && !alt)
                AddNumericCharacter(io, virtualKey, shift);
        }
    }

    private static bool IsKeyDown(int virtualKey) =>
        (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private static void SetModifier(
        ImGuiIOPtr io,
        ImGuiKey key,
        bool down,
        ref bool previous
    )
    {
        if (down == previous)
            return;
        previous = down;
        io.AddKeyEvent(key, down);
    }

    private static void AddNumericCharacter(ImGuiIOPtr io, int virtualKey, bool shift)
    {
        if (virtualKey is >= 0x30 and <= 0x39)
            io.AddInputCharacter((uint)('0' + virtualKey - 0x30));
        else if (virtualKey is >= 0x60 and <= 0x69)
            io.AddInputCharacter((uint)('0' + virtualKey - 0x60));
        else if (virtualKey is 0x6E or 0xBC or 0xBE)
            io.AddInputCharacter('.');
        else if (virtualKey is 0x6D or 0xBD)
            io.AddInputCharacter('-');
        else if (virtualKey == 0x6B || virtualKey == 0xBB && shift)
            io.AddInputCharacter('+');
        else if (virtualKey == 0x45)
            io.AddInputCharacter(shift ? 'E' : 'e');
    }

    internal void Render()
    {
        ImGui.Render();
        context.OMSetRenderTargets(target!);
        context.ClearRenderTargetView(target!, new Vortice.Mathematics.Color4(0, 0, 0, 0));
        renderer.Render(ImGui.GetDrawData());
        swapChain.Present(0, PresentFlags.None);
    }

    public void Dispose()
    {
        renderer.Dispose();
        target?.Dispose();
        swapChain.Dispose();
        context.Dispose();
        device.Dispose();
    }
}
