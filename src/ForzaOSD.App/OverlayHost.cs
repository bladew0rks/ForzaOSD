using System.Diagnostics;
using System.Windows.Forms;
using ForzaOSD.Core;
using ImGuiNET;

namespace ForzaOSD.App;

internal sealed class OverlayHost : IDisposable
{
    private const string WindowClass = "ForzaOSDOverlayWindow.Managed";
    private const long WindowSearchIntervalMs = 1000;
    private readonly AppConfig config;
    private readonly string configPath;
    private readonly TelemetryService telemetry;
    private readonly AudioSpectrumService audio;
    private readonly NativeMethods.WndProc wndProc;
    private nint hwnd;
    private nint gameWindow;
    private uint gameProcessId;
    private long nextGameWindowSearch;
    private bool running = true,
        editMode,
        overlayVisible;
    private NativeMethods.Rect overlayBounds;
    private string gameProcessName = "";
    private string status;
    private D3D11Host? graphics;
    private HudRuntime? hud;
    private NotifyIcon? trayIcon;

    internal OverlayHost(
        AppConfig config,
        string configPath,
        TelemetryService telemetry,
        AudioSpectrumService audio,
        string status
    )
    {
        this.config = config;
        this.configPath = configPath;
        this.telemetry = telemetry;
        this.audio = audio;
        this.status = status;
        wndProc = WindowProc;
    }

    internal void Run()
    {
        var instance = NativeMethods.GetModuleHandle(null);
        var wc = new NativeMethods.WndClassEx
        {
            Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.WndClassEx>(),
            WndProc = wndProc,
            Instance = instance,
            ClassName = WindowClass,
        };
        if (NativeMethods.RegisterClassEx(ref wc) == 0)
            throw new InvalidOperationException("Could not register overlay window class");
        var ex =
            NativeMethods.WS_EX_TOPMOST
            | NativeMethods.WS_EX_TRANSPARENT
            | NativeMethods.WS_EX_LAYERED
            | NativeMethods.WS_EX_TOOLWINDOW
            | NativeMethods.WS_EX_NOACTIVATE;
        hwnd = NativeMethods.CreateWindowEx(
            ex,
            WindowClass,
            "ForzaOSD",
            NativeMethods.WS_POPUP,
            0,
            0,
            1280,
            720,
            0,
            0,
            instance,
            0
        );
        if (hwnd == 0)
            throw new InvalidOperationException("Could not create overlay window");
        var margins = new NativeMethods.Margins { Left = -1 };
        NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);
        ImGui.CreateContext();
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.Colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(.06f, .06f, .07f, 1);
        style.Colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(.06f, .06f, .07f, 1);
        graphics = new(hwnd, 1280, 720);
        hud = new(AppContext.BaseDirectory, graphics);
        CreateTrayIcon();
        NativeMethods.RegisterHotKey(
            hwnd,
            1,
            (uint)config.HotkeyModifiers | NativeMethods.MOD_NOREPEAT,
            (uint)config.HotkeyVk
        );
        var timer = Stopwatch.StartNew();
        var framePacer = new FramePacer();
        double previous = timer.Elapsed.TotalSeconds;
        while (running)
        {
            Application.DoEvents();
            var game = FindGameWindow();
            if (!UpdateWindow(game))
            {
                previous = timer.Elapsed.TotalSeconds;
                framePacer.Reset();
                Thread.Sleep(50);
                continue;
            }
            framePacer.Wait(config.MaxFps);
            var telemetrySnapshot = telemetry.Snapshot;
            audio.PollMediaControls(config.Audio, telemetrySnapshot.IsDriving);
            var now = timer.Elapsed.TotalSeconds;
            hud.ReloadChangedProfiles();
            graphics.NewFrame((float)Math.Clamp(now - previous, .001, .1), editMode);
            previous = now;
            var actions = hud.Render(
                config,
                telemetrySnapshot,
                audio,
                editMode,
                status,
                graphics.Width,
                graphics.Height
            );
            if (actions.Save)
            {
                try
                {
                    config.Save(configPath);
                    status = "Settings saved";
                }
                catch (Exception e)
                {
                    status = "Save failed: " + e.Message;
                }
            }
            if (actions.RestartTelemetry)
            {
                try
                {
                    telemetry.StartAsync(config).GetAwaiter().GetResult();
                    status = "UDP listener restarted";
                }
                catch (Exception e)
                {
                    status = "Listener error: " + e.Message;
                }
            }
            if (actions.RestartAudio)
            {
                audio.Start(config.Audio);
                status = "Audio capture restarted";
            }
            if (actions.Quit)
                running = false;
            graphics.Render();
        }
        trayIcon?.Dispose();
        trayIcon = null;
        NativeMethods.UnregisterHotKey(hwnd, 1);
        NativeMethods.DestroyWindow(hwnd);
        NativeMethods.UnregisterClass(WindowClass, instance);
        hwnd = 0;
    }

    private nint WindowProc(nint window, uint message, nuint wp, nint lp)
    {
        if (editMode && message == NativeMethods.WM_MOUSEACTIVATE)
            return NativeMethods.MA_NOACTIVATE;
        if (editMode && graphics?.ProcessMessage(message, wp, lp) == true)
            return 1;
        switch (message)
        {
            case NativeMethods.WM_HOTKEY when wp == 1:
                SetEditMode(!editMode);
                return 0;
            case NativeMethods.WM_NCHITTEST when !editMode:
                return NativeMethods.HTTRANSPARENT;
            case NativeMethods.WM_SIZE when wp != 1:
                graphics?.Resize((int)(lp & 0xffff), (int)((lp >> 16) & 0xffff));
                return 0;
            case NativeMethods.WM_CLOSE:
                running = false;
                return 0;
            case NativeMethods.WM_DESTROY:
                NativeMethods.PostQuitMessage(0);
                return 0;
        }
        return NativeMethods.DefWindowProc(window, message, wp, lp);
    }

    private void SetEditMode(bool enabled)
    {
        editMode = enabled;
        var ex = (long)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        ex = enabled
            ? (ex & ~NativeMethods.WS_EX_TRANSPARENT) | NativeMethods.WS_EX_NOACTIVATE
            : ex | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (nint)ex);
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNOACTIVATE);
        overlayVisible = true;
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE
                | NativeMethods.SWP_NOSIZE
                | NativeMethods.SWP_NOACTIVATE
                | NativeMethods.SWP_FRAMECHANGED
        );
    }

    private nint FindGameWindow()
    {
        var configuredProcessName =
            Path.GetFileNameWithoutExtension(config.GameProcessName.Trim()) ?? "";
        if (!gameProcessName.Equals(configuredProcessName, StringComparison.OrdinalIgnoreCase))
        {
            gameWindow = 0;
            gameProcessId = 0;
            gameProcessName = configuredProcessName;
            nextGameWindowSearch = 0;
        }
        if (
            gameWindow != 0
            && NativeMethods.IsWindow(gameWindow)
            && NativeMethods.IsWindowVisible(gameWindow)
            && NativeMethods.GetWindowThreadProcessId(gameWindow, out var ownerProcessId) != 0
            && ownerProcessId == gameProcessId
        )
            return gameWindow;

        gameWindow = 0;
        gameProcessId = 0;
        var now = Environment.TickCount64;
        if (now < nextGameWindowSearch)
            return 0;
        nextGameWindowSearch = now + WindowSearchIntervalMs;
        if (configuredProcessName.Length == 0)
            return 0;

        var processIds = new HashSet<uint>();
        foreach (var process in Process.GetProcessesByName(configuredProcessName))
        {
            using (process)
                processIds.Add((uint)process.Id);
        }
        if (processIds.Count == 0)
            return 0;

        nint found = 0;
        uint foundProcessId = 0;
        NativeMethods.EnumWindows(
            (candidate, _) =>
            {
                if (!NativeMethods.IsWindowVisible(candidate))
                    return true;
                if (
                    NativeMethods.GetWindowThreadProcessId(candidate, out var processId) == 0
                    || !processIds.Contains(processId)
                )
                    return true;
                found = candidate;
                foundProcessId = processId;
                return false;
            },
            0
        );
        gameWindow = found;
        gameProcessId = foundProcessId;
        return gameWindow;
    }

    private bool UpdateWindow(nint game)
    {
        NativeMethods.Rect r;
        if (game == 0 || NativeMethods.IsIconic(game))
        {
            if (!editMode)
            {
                HideOverlay();
                return false;
            }
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, out r, 0);
        }
        else
        {
            NativeMethods.GetClientRect(game, out r);
            var topLeft = new NativeMethods.Point { X = r.Left, Y = r.Top };
            var bottomRight = new NativeMethods.Point { X = r.Right, Y = r.Bottom };
            NativeMethods.ClientToScreen(game, ref topLeft);
            NativeMethods.ClientToScreen(game, ref bottomRight);
            r = new NativeMethods.Rect
            {
                Left = topLeft.X,
                Top = topLeft.Y,
                Right = bottomRight.X,
                Bottom = bottomRight.Y,
            };
        }
        if (
            config.ShowOnlyWhenForeground
            && game != 0
            && NativeMethods.GetForegroundWindow() != game
            && !editMode
        )
        {
            HideOverlay();
            return false;
        }
        if (!overlayVisible || !SameBounds(r, overlayBounds))
        {
            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                r.Left,
                r.Top,
                r.Right - r.Left,
                r.Bottom - r.Top,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW
            );
            overlayBounds = r;
            overlayVisible = true;
        }
        return true;
    }

    private void HideOverlay()
    {
        if (!overlayVisible)
            return;
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
        overlayVisible = false;
    }

    private static bool SameBounds(NativeMethods.Rect left, NativeMethods.Rect right) =>
        left.Left == right.Left
        && left.Top == right.Top
        && left.Right == right.Right
        && left.Bottom == right.Bottom;

    private void CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open settings (Shift+Esc)", null, (_, _) => SetEditMode(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit ForzaOSD", null, (_, _) => running = false);
        trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "ForzaOSD telemetry overlay",
            Visible = true,
            ContextMenuStrip = menu,
        };
        trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                SetEditMode(true);
        };
    }

    public void Dispose()
    {
        hud?.Dispose();
        graphics?.Dispose();
        if (ImGui.GetCurrentContext() != 0)
            ImGui.DestroyContext();
    }
}

internal readonly record struct HudActions(
    bool Save = false,
    bool Quit = false,
    bool RestartTelemetry = false,
    bool RestartAudio = false
);
