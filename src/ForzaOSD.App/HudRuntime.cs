using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using ForzaOSD.Core;
using ImGuiNET;
using KeraLua;

namespace ForzaOSD.App;

internal sealed unsafe class HudRuntime : IDisposable
{
    private static readonly CommandType[] CommandTypes = Enum.GetValues<CommandType>();
    private readonly string appDirectory;
    private readonly D3D11Host graphics;
    private readonly List<Profile> profiles = [];
    private string diagnostic = "";
    private long lastPoll;

    internal HudRuntime(string appDirectory, D3D11Host graphics)
    {
        this.appDirectory = Path.GetFullPath(appDirectory);
        this.graphics = graphics;
        Discover();
    }

    private void Discover()
    {
        var root = Path.Combine(appDirectory, "hud_profiles");
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException("hud_profiles directory is missing");
        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            var script = Path.Combine(folder, "profile.lua");
            if (!File.Exists(script))
                continue;
            try
            {
                var profile = Load(script);
                if (profiles.Any(p => p.Id == profile.Id))
                    throw new InvalidDataException("Duplicate Lua profile id: " + profile.Id);
                profiles.Add(profile);
            }
            catch (Exception e)
            {
                diagnostic = $"Invalid profile {Path.GetFileName(folder)}: {e.Message}";
            }
        }
        if (!profiles.Any(p => p.Id == "native"))
            throw new InvalidDataException("Native Lua profile failed to load");
        var report = Environment.GetEnvironmentVariable("FORZAOSD_PROFILE_REPORT")
            ?? Environment.GetEnvironmentVariable("FHUD_PROFILE_REPORT");
        if (report is { Length: > 0 })
            File.WriteAllLines(
                report,
                profiles.Select(p => $"{p.Id}|{p.Name}|{p.AssetRoot}").Prepend(diagnostic)
            );
        graphics.Renderer.RecreateFontsTexture();
    }

    private Profile Load(string script)
    {
        var lua = new Lua(true);
        foreach (
            var blocked in new[]
            {
                "dofile",
                "loadfile",
                "load",
                "require",
                "package",
                "io",
                "os",
                "debug",
                "coroutine",
            }
        )
        {
            lua.PushNil();
            lua.SetGlobal(blocked);
        }
        if (lua.LoadFile(script) != LuaStatus.OK)
            throw new InvalidDataException(lua.ToString(-1));
        LuaHookFunction hook = (state, _) =>
            Lua.FromIntPtr(state).Error("HUD script exceeded its instruction budget");
        lua.SetHook(hook, LuaHookMask.Count, 100000);
        var result = lua.PCall(0, 1, 0);
        lua.SetHook(hook, (LuaHookMask)0, 0);
        if (result != LuaStatus.OK)
            throw new InvalidDataException(lua.ToString(-1));
        if (!lua.IsTable(-1))
            throw new InvalidDataException("profile.lua must return a table");
        var profile = new Profile(lua, script, hook)
        {
            Api = (int)Number(lua, -1, "api_version", 0),
            Id = String(lua, -1, "id", ""),
        };
        if (profile.Api != 1)
            throw new InvalidDataException("Unsupported Lua HUD API version: " + profile.Api);
        if (string.IsNullOrEmpty(profile.Id))
            throw new InvalidDataException("Profile id is required");
        profile.Name = String(lua, -1, "name", profile.Id);
        profile.Author = String(lua, -1, "author", "");
        profile.Version = String(lua, -1, "version", "");
        profile.Role = String(lua, -1, "role", "hud");
        if (profile.Role is not ("hud" or "module"))
            throw new InvalidDataException("Profile role must be 'hud' or 'module'");
        profile.Visibility = String(lua, -1, "visibility", "telemetry");
        lua.GetField(-1, "layout");
        if (lua.IsTable(-1))
        {
            profile.Width = (float)Number(lua, -1, "width", 400);
            profile.Height = (float)Number(lua, -1, "height", 200);
            profile.ReferenceHeight = (float)Number(lua, -1, "reference_height", 1080);
        }
        lua.Pop(1);
        var declared = String(lua, -1, "asset_root", "");
        var profileRoot = Path.GetDirectoryName(script)!;
        var assetRoot = Path.GetFullPath(Path.Combine(profileRoot, declared));
        if (!IsWithin(assetRoot, appDirectory))
            throw new InvalidDataException("Asset root escapes the ForzaOSD application directory");
        profile.AssetRoot = assetRoot;
        ReadStringMap(
            lua,
            -1,
            "assets",
            (key, value) =>
            {
                var path = Path.GetFullPath(Path.Combine(assetRoot, value));
                if (!IsWithin(path, appDirectory))
                    throw new InvalidDataException("Asset path escapes application directory");
                if (!File.Exists(path))
                    throw new FileNotFoundException("Missing declared asset", path);
                profile.Assets[key] = path;
            }
        );
        lua.GetField(-1, "fonts");
        if (lua.IsTable(-1))
        {
            lua.PushNil();
            while (lua.Next(-2))
            {
                if (lua.IsString(-2) && lua.IsTable(-1))
                {
                    var key = lua.ToString(-2);
                    var path = Path.Combine(assetRoot, String(lua, -1, "path", ""));
                    var size = (float)Number(lua, -1, "size", 32);
                    var font = ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size);
                    if (font.NativePtr == null)
                        throw new InvalidDataException("Could not load font: " + path);
                    profile.Fonts[key] = font;
                }
                lua.Pop(1);
            }
        }
        lua.Pop(1);
        lua.GetField(-1, "settings");
        if (lua.IsTable(-1))
        {
            lua.PushNil();
            while (lua.Next(-2))
            {
                if (lua.IsString(-2) && lua.IsTable(-1))
                {
                    var s = new Setting
                    {
                        Key = lua.ToString(-2),
                        Label = String(lua, -1, "label", lua.ToString(-2)),
                        Type = String(lua, -1, "type", "number"),
                        Min = (float)Number(lua, -1, "min", 0),
                        Max = (float)Number(lua, -1, "max", 1),
                        Order = (int)Number(lua, -1, "order", 100),
                    };
                    lua.GetField(-1, "default");
                    s.Default = ReadJson(lua, -1);
                    lua.Pop(1);
                    lua.GetField(-1, "options");
                    if (lua.IsTable(-1))
                    {
                        for (long i = 1; ; i++)
                        {
                            lua.RawGetInteger(-1, i);
                            if (!lua.IsString(-1))
                            {
                                lua.Pop(1);
                                break;
                            }
                            s.Options.Add(lua.ToString(-1));
                            lua.Pop(1);
                        }
                    }
                    lua.Pop(1);
                    profile.Settings.Add(s);
                }
                lua.Pop(1);
            }
        }
        lua.Pop(1);
        profile.Settings.Sort((a, b) => a.Order.CompareTo(b.Order));
        lua.PushCopy(-1);
        lua.SetGlobal("__profile");
        lua.Pop(1);
        foreach (var kind in CommandTypes)
            profile.Callbacks.Add(state => DrawCallback(profile, kind, state));
        profile.Modified = File.GetLastWriteTimeUtc(script);
        return profile;
    }

    internal HudActions Render(
        AppConfig config,
        TelemetrySnapshot telemetry,
        AudioSpectrumService audio,
        bool edit,
        string status,
        int width,
        int height
    )
    {
        Profile? configured = null,
            native = null,
            firstHud = null;
        foreach (var profile in profiles)
        {
            if (!profile.Valid)
                continue;
            if (profile.Id == config.HudProfile)
                configured = profile;
            if (profile.Role != "hud")
                continue;
            firstHud ??= profile;
            if (profile.Id == "native")
                native = profile;
        }
        var primary = configured is { Role: "hud" }
            ? configured
            : native ?? firstHud ?? throw new InvalidOperationException("No valid HUD profile");

        if (configured is { Role: "module" })
        {
            EnableModule(config, configured.Id, true);
            config.HudProfile = primary.Id;
        }
        else if (configured is null)
        {
            config.HudProfile = primary.Id;
        }

        var audioSnapshot = audio.Snapshot;
        RenderProfile(primary, config, telemetry, audioSnapshot, edit, width, height);
        foreach (var profile in profiles)
            if (
                profile.Valid
                && profile.Role == "module"
                && IsModuleEnabled(config, profile.Id)
            )
                RenderProfile(profile, config, telemetry, audioSnapshot, edit, width, height);
        if (!edit)
            return new();
        bool save = false,
            quit = false,
            restart = false,
            restartAudio = false;
        ImGui.SetNextWindowPos(new(24, 24), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new(410, 0), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(1);
        ImGui.Begin("ForzaOSD settings");
        ImGui.TextUnformatted("Shift+Esc: close edit mode");
        var availableModules = profiles.Where(p => p.Valid && p.Role == "module").ToArray();
        if (ImGui.BeginTabBar("##SettingsTabs"))
        {
            if (ImGui.BeginTabItem("HUD"))
            {
                DrawHudSettings(config, primary, availableModules);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Connection"))
            {
                DrawConnectionSettings(config, telemetry, status, ref restart);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Audio"))
            {
                DrawAudioSettings(config, audio, ref restartAudio);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        ImGui.Separator();
        if (ImGui.Button("Save settings"))
            save = true;
        ImGui.SameLine();
        if (ImGui.Button("Quit"))
            quit = true;
        ImGui.End();
        return new(save, quit, restart, restartAudio);
    }

    private void DrawHudSettings(
        AppConfig config,
        Profile primary,
        IReadOnlyList<Profile> availableModules
    )
    {
        ImGui.SeparatorText("Profiles");
        if (ImGui.BeginCombo("Speedometer", primary.Name))
        {
            foreach (var profile in profiles.Where(p => p.Valid && p.Role == "hud"))
            {
                var chosen = config.HudProfile == profile.Id;
                if (ImGui.Selectable(profile.Name, chosen))
                    config.HudProfile = profile.Id;
                if (chosen)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        foreach (var module in availableModules)
        {
            var enabled = IsModuleEnabled(config, module.Id);
            ImGui.PushID(module.Id);
            if (ImGui.Checkbox(module.Name, ref enabled))
                EnableModule(config, module.Id, enabled);
            ImGui.PopID();
        }

        ImGui.SeparatorText("Display");
        var metric = config.Metric;
        if (ImGui.Checkbox("Metric units", ref metric))
            config.Metric = metric;
        var opacity = config.Layout.Opacity;
        if (ImGui.SliderFloat("Opacity", ref opacity, .1f, 1))
            config.Layout.Opacity = opacity;
        var maxFps = config.MaxFps;
        if (ImGui.SliderInt("Frame rate limit", ref maxFps, 30, 240))
            config.MaxFps = maxFps;

        ImGui.SeparatorText(primary.Name);
        DrawSettings(config, primary);
        foreach (var module in availableModules)
            if (IsModuleEnabled(config, module.Id))
            {
                ImGui.SeparatorText(module.Name);
                DrawSettings(config, module);
            }
    }

    private void DrawConnectionSettings(
        AppConfig config,
        TelemetrySnapshot telemetry,
        string status,
        ref bool restart
    )
    {
        ImGui.SeparatorText("Status");
        ImGui.Text($"Telemetry: {telemetry.Format}");
        ImGui.Text($"Packets: {telemetry.PacketsPerSecond:F1}/s  size: {telemetry.LastPacketSize}");
        if (!string.IsNullOrEmpty(telemetry.Detail))
            ImGui.TextWrapped(telemetry.Detail);
        if (!string.IsNullOrEmpty(diagnostic))
            ImGui.TextWrapped(diagnostic);
        if (!string.IsNullOrEmpty(status))
            ImGui.TextWrapped(status);

        ImGui.SeparatorText("Listener");
        var bind = config.BindAddress;
        if (ImGui.InputText("Bind address", ref bind, 64))
            config.BindAddress = bind;
        var port = (int)config.UdpPort;
        if (ImGui.InputInt("UDP port", ref port))
            config.UdpPort = (ushort)Math.Clamp(port, 1, 65535);
        if (ImGui.Button("Restart listener"))
            restart = true;

        ImGui.SeparatorText("Overlay target");
        var processName = config.GameProcessName;
        if (ImGui.InputText("Game process", ref processName, 128))
            config.GameProcessName = processName;
        var foreground = config.ShowOnlyWhenForeground;
        if (ImGui.Checkbox("Only show over foreground game", ref foreground))
            config.ShowOnlyWhenForeground = foreground;
    }

    private void RenderProfile(
        Profile profile,
        AppConfig config,
        TelemetrySnapshot telemetry,
        AudioSnapshot audio,
        bool edit,
        int width,
        int height
    )
    {
        EnsureSettings(config, profile);
        var visible = profile.Visibility == "audio"
            ? edit || audio.Playing || audio.Available
            : telemetry.ShouldShow(edit);
        if (visible && CallRender(profile, config, telemetry, audio, edit))
            Execute(profile, config, width, height, edit);
    }

    private bool CallRender(
        Profile p,
        AppConfig config,
        TelemetrySnapshot t,
        AudioSnapshot audio,
        bool edit
    )
    {
        p.Commands.Clear();
        var l = p.Lua;
        l.GetGlobal("__profile");
        l.GetField(-1, "render");
        if (!l.IsFunction(-1))
        {
            l.Pop(2);
            diagnostic = "Lua profile error: render(ctx) is missing";
            return false;
        }
        l.NewTable();
        PushTelemetry(l, t);
        l.SetField(-2, "telemetry");
        l.NewTable();
        Set(l, "enabled", audio.Enabled);
        Set(l, "available", audio.Available);
        Set(l, "playing", audio.Playing);
        Set(l, "title", audio.Title);
        Set(l, "artist", audio.Artist);
        Set(l, "source", audio.Source);
        Set(l, "rms", audio.Rms);
        Set(l, "peak", audio.Peak);
        l.NewTable();
        for (var i = 0; i < audio.Bands.Length; i++)
        {
            l.PushNumber(audio.Bands[i]);
            l.RawSetInteger(-2, i + 1);
        }
        l.SetField(-2, "bands");
        l.SetField(-2, "audio");
        l.NewTable();
        foreach (var pair in config.ProfileSettings[p.Id]!.AsObject())
            PushJsonField(l, pair.Key, pair.Value);
        l.SetField(-2, "settings");
        l.NewTable();
        for (var i = 0; i < CommandTypes.Length; i++)
        {
            l.PushCFunction(p.Callbacks[i]);
            l.SetField(-2, CommandName(CommandTypes[i]));
        }
        l.SetField(-2, "draw");
        Set(l, "time", Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
        Set(l, "edit_mode", edit);
        Set(l, "metric", config.Metric);
        Set(l, "opacity", config.Layout.Opacity);
        l.SetHook(p.Hook, LuaHookMask.Count, 100000);
        var result = l.PCall(1, 0, 0);
        l.SetHook(p.Hook, (LuaHookMask)0, 0);
        if (result != LuaStatus.OK)
        {
            diagnostic = "Lua profile error: " + l.ToString(-1);
            l.SetTop(0);
            return false;
        }
        l.Pop(1);
        return true;
    }

    internal static void PushTelemetry(Lua l, TelemetrySnapshot t)
    {
        l.NewTable();
        var f = t.Frame;
        Set(l, "available", t.HasFrame);
        Set(l, "fresh", !t.Stale);
        Set(l, "race_on", f.RaceOn);
        Set(l, "rpm", f.EngineRpm);
        Set(l, "max_rpm", f.EngineMaxRpm);
        Set(l, "idle_rpm", f.EngineIdleRpm);
        Set(l, "acceleration_x", f.AccelerationX);
        Set(l, "acceleration_y", f.AccelerationY);
        Set(l, "acceleration_z", f.AccelerationZ);
        Set(l, "velocity_x", f.VelocityX);
        Set(l, "velocity_y", f.VelocityY);
        Set(l, "velocity_z", f.VelocityZ);
        Set(l, "angular_velocity_x", f.AngularVelocityX);
        Set(l, "angular_velocity_y", f.AngularVelocityY);
        Set(l, "angular_velocity_z", f.AngularVelocityZ);
        Set(l, "yaw", f.Yaw);
        Set(l, "pitch", f.Pitch);
        Set(l, "roll", f.Roll);
        Set(
            l,
            "normalized_suspension_travel_front_left",
            f.NormalizedSuspensionTravelFrontLeft
        );
        Set(
            l,
            "normalized_suspension_travel_front_right",
            f.NormalizedSuspensionTravelFrontRight
        );
        Set(
            l,
            "normalized_suspension_travel_rear_left",
            f.NormalizedSuspensionTravelRearLeft
        );
        Set(
            l,
            "normalized_suspension_travel_rear_right",
            f.NormalizedSuspensionTravelRearRight
        );
        Set(l, "tire_slip_ratio_front_left", f.TireSlipRatioFrontLeft);
        Set(l, "tire_slip_ratio_front_right", f.TireSlipRatioFrontRight);
        Set(l, "tire_slip_ratio_rear_left", f.TireSlipRatioRearLeft);
        Set(l, "tire_slip_ratio_rear_right", f.TireSlipRatioRearRight);
        Set(l, "wheel_rotation_speed_front_left", f.WheelRotationSpeedFrontLeft);
        Set(l, "wheel_rotation_speed_front_right", f.WheelRotationSpeedFrontRight);
        Set(l, "wheel_rotation_speed_rear_left", f.WheelRotationSpeedRearLeft);
        Set(l, "wheel_rotation_speed_rear_right", f.WheelRotationSpeedRearRight);
        Set(l, "wheel_on_rumble_strip_front_left", f.WheelOnRumbleStripFrontLeft);
        Set(l, "wheel_on_rumble_strip_front_right", f.WheelOnRumbleStripFrontRight);
        Set(l, "wheel_on_rumble_strip_rear_left", f.WheelOnRumbleStripRearLeft);
        Set(l, "wheel_on_rumble_strip_rear_right", f.WheelOnRumbleStripRearRight);
        Set(l, "wheel_in_puddle_front_left", f.WheelInPuddleFrontLeft);
        Set(l, "wheel_in_puddle_front_right", f.WheelInPuddleFrontRight);
        Set(l, "wheel_in_puddle_rear_left", f.WheelInPuddleRearLeft);
        Set(l, "wheel_in_puddle_rear_right", f.WheelInPuddleRearRight);
        Set(l, "surface_rumble_front_left", f.SurfaceRumbleFrontLeft);
        Set(l, "surface_rumble_front_right", f.SurfaceRumbleFrontRight);
        Set(l, "surface_rumble_rear_left", f.SurfaceRumbleRearLeft);
        Set(l, "surface_rumble_rear_right", f.SurfaceRumbleRearRight);
        Set(l, "tire_slip_angle_front_left", f.TireSlipAngleFrontLeft);
        Set(l, "tire_slip_angle_front_right", f.TireSlipAngleFrontRight);
        Set(l, "tire_slip_angle_rear_left", f.TireSlipAngleRearLeft);
        Set(l, "tire_slip_angle_rear_right", f.TireSlipAngleRearRight);
        Set(l, "tire_combined_slip_front_left", f.TireCombinedSlipFrontLeft);
        Set(l, "tire_combined_slip_front_right", f.TireCombinedSlipFrontRight);
        Set(l, "tire_combined_slip_rear_left", f.TireCombinedSlipRearLeft);
        Set(l, "tire_combined_slip_rear_right", f.TireCombinedSlipRearRight);
        Set(l, "suspension_travel_meters_front_left", f.SuspensionTravelMetersFrontLeft);
        Set(l, "suspension_travel_meters_front_right", f.SuspensionTravelMetersFrontRight);
        Set(l, "suspension_travel_meters_rear_left", f.SuspensionTravelMetersRearLeft);
        Set(l, "suspension_travel_meters_rear_right", f.SuspensionTravelMetersRearRight);
        Set(l, "car_ordinal", f.CarOrdinal);
        Set(l, "car_class", f.CarClass);
        Set(l, "car_performance_index", f.CarPerformanceIndex);
        Set(l, "drivetrain_type", f.DrivetrainType);
        Set(l, "num_cylinders", f.NumCylinders);
        Set(l, "car_group", f.CarGroup);
        Set(l, "smashable_velocity_difference", f.SmashableVelocityDifference);
        Set(l, "smashable_mass", f.SmashableMass);
        Set(l, "position_x", f.PositionX);
        Set(l, "position_y", f.PositionY);
        Set(l, "position_z", f.PositionZ);
        Set(l, "speed_mps", f.SpeedMps);
        Set(l, "speed_kph", f.SpeedMps * 3.6);
        Set(l, "speed_mph", f.SpeedMps * 2.2369363);
        Set(l, "power_watts", f.PowerWatts);
        Set(l, "torque_nm", f.TorqueNm);
        Set(l, "tire_temp_front_left", f.TireTempFrontLeft);
        Set(l, "tire_temp_front_right", f.TireTempFrontRight);
        Set(l, "tire_temp_rear_left", f.TireTempRearLeft);
        Set(l, "tire_temp_rear_right", f.TireTempRearRight);
        Set(l, "boost", f.Boost);
        Set(l, "fuel", f.Fuel);
        Set(l, "distance_traveled_m", f.DistanceTraveledMeters);
        Set(l, "best_lap_seconds", f.BestLapSeconds);
        Set(l, "last_lap_seconds", f.LastLapSeconds);
        Set(l, "current_lap_seconds", f.CurrentLapSeconds);
        Set(l, "race_time_seconds", f.RaceTimeSeconds);
        Set(l, "lap_number", f.LapNumber);
        Set(l, "race_position", f.RacePosition);
        Set(l, "gear", t.HasFrame ? f.Gear : 11);
        Set(l, "gear_label", GearFormatter.Format(t.HasFrame ? f.Gear : (byte)11));
        Set(l, "throttle", f.Throttle);
        Set(l, "brake", f.Brake);
        Set(l, "clutch", f.Clutch);
        Set(l, "handbrake", f.Handbrake);
        Set(l, "steering_raw", f.SteeringRaw);
        Set(l, "steering", f.Steering);
        Set(l, "driving_line_raw", f.DrivingLineRaw);
        Set(l, "driving_line", f.DrivingLine);
        Set(l, "ai_brake_difference_raw", f.AiBrakeDifferenceRaw);
        Set(l, "ai_brake_difference", f.AiBrakeDifference);
        Set(l, "lateral_g", f.LateralG);
        Set(l, "longitudinal_g", f.LongitudinalG);
    }

    private static int DrawCallback(Profile p, CommandType type, nint state)
    {
        var l = Lua.FromIntPtr(state);
        var c = new Command
        {
            Type = type,
            X = (float)Number(
                l,
                1,
                type == CommandType.Circle ? "cx"
                    : type == CommandType.Line ? "x1"
                    : "x",
                0
            ),
            Y = (float)Number(
                l,
                1,
                type == CommandType.Circle ? "cy"
                    : type == CommandType.Line ? "y1"
                    : "y",
                0
            ),
            W = (float)Number(l, 1, type == CommandType.Line ? "x2" : "w", 0),
            H = (float)Number(l, 1, type == CommandType.Line ? "y2" : "h", 0),
            Radius = (float)Number(
                l,
                1,
                type == CommandType.Circle ? "radius" : "rounding",
                0
            ),
            Thickness = (float)Number(l, 1, "thickness", 1),
            GlowRadius = (float)Number(l, 1, "glow_radius", 0),
            GlowIntensity = (float)Number(l, 1, "glow_intensity", 0),
            Rotation = (float)Number(l, 1, "rotation", 0),
            PivotX = (float)Number(l, 1, "pivot_x", 0.5),
            PivotY = (float)Number(l, 1, "pivot_y", 0.5),
            UvX1 = (float)Number(l, 1, "uv_x1", 0),
            UvY1 = (float)Number(l, 1, "uv_y1", 0),
            UvX2 = (float)Number(l, 1, "uv_x2", 1),
            UvY2 = (float)Number(l, 1, "uv_y2", 1),
            Size = (float)Number(l, 1, "size", 24),
            Text = String(l, 1, "text", ""),
            Asset = String(l, 1, "asset", ""),
            Font = String(l, 1, "font", ""),
            Align = String(l, 1, "align", "left"),
            Direction = String(l, 1, "direction", "horizontal"),
            Space = String(l, 1, "space", "profile"),
            Shadow = Bool(l, 1, "shadow", false),
            Color = Color(l, 1),
            Color2 = Color(l, 1, "color2", "#ffffff"),
            Color3 = Color(l, 1, "color3", "#ffffff"),
            GlowColor = Color(l, 1, "glow_color", "#ffffff"),
            ClipX = (float)Number(l, 1, "clip_x", 0),
            ClipY = (float)Number(l, 1, "clip_y", 0),
            ClipW = (float)Number(l, 1, "clip_w", 0),
            ClipH = (float)Number(l, 1, "clip_h", 0),
        };
        if (type == CommandType.Offset)
        {
            p.OffsetX = c.X;
            p.OffsetY = c.Y;
        }
        else
            p.Commands.Add(c);
        return 0;
    }

    private void Execute(Profile p, AppConfig config, int width, int height, bool edit)
    {
        var values = EnsureSettings(config, p);
        var scale = height / Math.Max(1, p.ReferenceHeight) * NodeFloat(values["scale"], 1);
        var anchorX = NodeFloat(values["x"], .5f);
        var anchorY = NodeFloat(values["y"], .5f);
        var origin = new Vector2(
            width * anchorX - p.Width * scale * .5f + p.OffsetX * scale,
            height * anchorY - p.Height * scale * .5f + p.OffsetY * scale
        );
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(new(width, height));
        ImGui.SetNextWindowBgAlpha(0);
        var flags =
            ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoBringToFrontOnFocus;
        if (!edit)
            flags |= ImGuiWindowFlags.NoInputs;
        ImGui.Begin("##LuaHudCanvas_" + p.Id, flags);
        var d = ImGui.GetWindowDrawList();
        foreach (ref readonly var c in CollectionsMarshal.AsSpan(p.Commands))
        {
            var commandOrigin = c.Space == "screen" ? Vector2.Zero : origin;
            var a = commandOrigin + new Vector2(c.X, c.Y) * scale;
            var col = c.Color;
            var clipped = c.ClipW > 0 && c.ClipH > 0;
            if (clipped)
                d.PushClipRect(
                    commandOrigin + new Vector2(c.ClipX, c.ClipY) * scale,
                    commandOrigin
                        + new Vector2(c.ClipX + c.ClipW, c.ClipY + c.ClipH) * scale,
                    true
                );
            switch (c.Type)
            {
                case CommandType.Rect:
                    if (HasGlow(c))
                        DrawRectGlow(
                            d,
                            a,
                            a + new Vector2(c.W, c.H) * scale,
                            c.GlowColor,
                            c.GlowRadius * scale,
                            c.GlowIntensity,
                            c.Radius * scale,
                            false,
                            0
                        );
                    d.AddRectFilled(a, a + new Vector2(c.W, c.H) * scale, col, c.Radius * scale);
                    break;
                case CommandType.Gradient:
                    if (HasGlow(c))
                        DrawRectGlow(
                            d,
                            a,
                            a + new Vector2(c.W, c.H) * scale,
                            c.GlowColor,
                            c.GlowRadius * scale,
                            c.GlowIntensity,
                            c.Radius * scale,
                            false,
                            0
                        );
                    DrawGradient(
                        d,
                        a,
                        a + new Vector2(c.W, c.H) * scale,
                        c.Color,
                        c.Color2,
                        c.Color3,
                        c.Radius * scale,
                        c.Direction == "vertical"
                    );
                    break;
                case CommandType.Outline:
                    if (HasGlow(c))
                        DrawRectGlow(
                            d,
                            a,
                            a + new Vector2(c.W, c.H) * scale,
                            c.GlowColor,
                            c.GlowRadius * scale,
                            c.GlowIntensity,
                            c.Radius * scale,
                            true,
                            Math.Max(1, c.Thickness * scale)
                        );
                    d.AddRect(
                        a,
                        a + new Vector2(c.W, c.H) * scale,
                        col,
                        c.Radius * scale,
                        ImDrawFlags.None,
                        Math.Max(1, c.Thickness * scale)
                    );
                    break;
                case CommandType.Line:
                    var lineEnd = commandOrigin + new Vector2(c.W, c.H) * scale;
                    if (HasGlow(c))
                        DrawLineGlow(
                            d,
                            a,
                            lineEnd,
                            c.GlowColor,
                            c.GlowRadius * scale,
                            c.GlowIntensity,
                            Math.Max(1, c.Thickness * scale)
                        );
                    d.AddLine(
                        a,
                        lineEnd,
                        col,
                        Math.Max(1, c.Thickness * scale)
                    );
                    break;
                case CommandType.Circle:
                    if (HasGlow(c))
                        DrawCircleGlow(
                            d,
                            a,
                            c.Radius * scale,
                            c.GlowColor,
                            c.GlowRadius * scale,
                            c.GlowIntensity
                        );
                    d.AddCircleFilled(a, c.Radius * scale, col);
                    break;
                case CommandType.Text:
                    var font = p.Fonts.GetValueOrDefault(c.Font, ImGui.GetFont());
                    var size = c.Size * scale;
                    var ext = font.CalcTextSizeA(size, float.MaxValue, 0, c.Text);
                    var pos = a;
                    if (c.Align == "center")
                        pos.X -= ext.X * .5f;
                    else if (c.Align == "right")
                        pos.X -= ext.X;
                    pos.Y -= ext.Y * .5f;
                    if (c.Shadow)
                        d.AddText(font, size, pos + new Vector2(2, 3) * scale, 0xA0000000, c.Text);
                    if (HasGlow(c))
                        DrawTextGlow(
                            d,
                            font,
                            size,
                            pos,
                            c.Text,
                            c.GlowColor,
                            c.GlowRadius * scale,
                            c.GlowIntensity
                        );
                    d.AddText(font, size, pos, col, c.Text);
                    break;
                case CommandType.Image:
                    if (p.Assets.TryGetValue(c.Asset, out var path))
                    {
                        var texture = graphics.Renderer.GetOrLoadTexture(path, graphics.Device);
                        var imageSize = new Vector2(c.W, c.H) * scale;
                        if (
                            c.GlowRadius > 0
                            && c.GlowIntensity > 0
                            && c.W > 0
                            && c.H > 0
                            && Math.Abs(c.Rotation) < 0.001f
                        )
                        {
                            var uvMargin = new Vector2(c.GlowRadius / c.W, c.GlowRadius / c.H);
                            var bloomTexture = graphics.Renderer.GetOrLoadBloomTexture(
                                path,
                                graphics.Device,
                                uvMargin,
                                c.GlowIntensity,
                                c.GlowColor
                            );
                            var margin = new Vector2(c.GlowRadius * scale);
                            d.AddImage(
                                bloomTexture,
                                a - margin,
                                a + imageSize + margin,
                                -uvMargin,
                                Vector2.One + uvMargin
                            );
                        }
                        var uvTopLeft = new Vector2(c.UvX1, c.UvY1);
                        var uvBottomRight = new Vector2(c.UvX2, c.UvY2);
                        if (Math.Abs(c.Rotation) < 0.001f)
                            d.AddImage(
                                texture,
                                a,
                                a + imageSize,
                                uvTopLeft,
                                uvBottomRight,
                                col
                            );
                        else
                        {
                            var pivot = a + imageSize * new Vector2(c.PivotX, c.PivotY);
                            var radians = c.Rotation * MathF.PI / 180;
                            var topLeft = RotatePoint(a, pivot, radians);
                            var topRight = RotatePoint(
                                a + new Vector2(imageSize.X, 0),
                                pivot,
                                radians
                            );
                            var bottomRight = RotatePoint(a + imageSize, pivot, radians);
                            var bottomLeft = RotatePoint(
                                a + new Vector2(0, imageSize.Y),
                                pivot,
                                radians
                            );
                            d.AddImageQuad(
                                texture,
                                topLeft,
                                topRight,
                                bottomRight,
                                bottomLeft,
                                uvTopLeft,
                                new Vector2(c.UvX2, c.UvY1),
                                uvBottomRight,
                                new Vector2(c.UvX1, c.UvY2),
                                col
                            );
                        }
                    }
                    break;
            }
            if (clipped)
                d.PopClipRect();
        }
        if (edit)
            d.AddRect(
                origin,
                origin + new Vector2(p.Width, p.Height) * scale,
                0xB4FFBE23,
                8 * scale,
                ImDrawFlags.None,
                Math.Max(1, 2 * scale)
            );
        ImGui.End();
    }

    private static bool HasGlow(in Command command) =>
        command.GlowRadius > 0 && command.GlowIntensity > 0;

    private static uint ScaleAlpha(uint color, float amount)
    {
        var alpha = (color >> 24) & 255;
        var scaled = (uint)Math.Clamp(Math.Round(alpha * Math.Max(0, amount)), 0, 255);
        return (color & 0x00FFFFFF) | (scaled << 24);
    }

    private static float GlowLayerAlpha(float intensity, int layer, int layers)
    {
        var distance = layer / (float)(layers + 1);
        var falloff = 1 - distance;
        return Math.Clamp(intensity, 0, 4) * 0.12f * falloff * falloff;
    }

    private static void DrawRectGlow(
        ImDrawListPtr draw,
        Vector2 min,
        Vector2 max,
        uint color,
        float radius,
        float intensity,
        float rounding,
        bool outline,
        float thickness
    )
    {
        const int layers = 6;
        for (var layer = layers; layer >= 1; layer--)
        {
            var expansion = radius * layer / (layers + 1);
            var glowColor = ScaleAlpha(color, GlowLayerAlpha(intensity, layer, layers));
            var glowMin = min - new Vector2(expansion);
            var glowMax = max + new Vector2(expansion);
            if (outline)
                draw.AddRect(
                    glowMin,
                    glowMax,
                    glowColor,
                    rounding + expansion,
                    ImDrawFlags.None,
                    thickness + expansion * 0.65f
                );
            else
                draw.AddRectFilled(
                    glowMin,
                    glowMax,
                    glowColor,
                    rounding + expansion
                );
        }
    }

    private static void DrawLineGlow(
        ImDrawListPtr draw,
        Vector2 start,
        Vector2 end,
        uint color,
        float radius,
        float intensity,
        float thickness
    )
    {
        const int layers = 6;
        for (var layer = layers; layer >= 1; layer--)
        {
            var expansion = radius * layer / (layers + 1);
            draw.AddLine(
                start,
                end,
                ScaleAlpha(color, GlowLayerAlpha(intensity, layer, layers)),
                thickness + expansion * 2
            );
        }
    }

    private static void DrawCircleGlow(
        ImDrawListPtr draw,
        Vector2 center,
        float circleRadius,
        uint color,
        float glowRadius,
        float intensity
    )
    {
        const int layers = 6;
        for (var layer = layers; layer >= 1; layer--)
        {
            var expansion = glowRadius * layer / (layers + 1);
            draw.AddCircleFilled(
                center,
                circleRadius + expansion,
                ScaleAlpha(color, GlowLayerAlpha(intensity, layer, layers))
            );
        }
    }

    private static void DrawTextGlow(
        ImDrawListPtr draw,
        ImFontPtr font,
        float size,
        Vector2 position,
        string text,
        uint color,
        float radius,
        float intensity
    )
    {
        draw.AddText(font, size, position, ScaleAlpha(color, intensity * 0.28f), text);
        const int layers = 5;
        const int samples = 12;
        for (var layer = layers; layer >= 1; layer--)
        {
            var distance = radius * layer / (layers + 1);
            var layerAlpha = GlowLayerAlpha(intensity, layer, layers) * 0.32f;
            var glowColor = ScaleAlpha(color, layerAlpha);
            for (var sample = 0; sample < samples; sample++)
            {
                var angle = MathF.Tau * sample / samples;
                var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
                draw.AddText(font, size, position + offset, glowColor, text);
            }
        }
    }

    private static Vector2 RotatePoint(Vector2 point, Vector2 pivot, float radians)
    {
        var offset = point - pivot;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        return pivot
            + new Vector2(
                offset.X * cosine - offset.Y * sine,
                offset.X * sine + offset.Y * cosine
            );
    }

    private static void DrawGradient(
        ImDrawListPtr draw,
        Vector2 min,
        Vector2 max,
        uint left,
        uint middle,
        uint right,
        float radius,
        bool vertical
    )
    {
        var length = vertical ? max.Y - min.Y : max.X - min.X;
        var steps = Math.Max(1, (int)Math.Ceiling(length));
        var stepLength = length / steps;
        radius = Math.Clamp(radius, 0, Math.Min(max.X - min.X, max.Y - min.Y) * 0.5f);

        for (var i = 0; i < steps; i++)
        {
            var t = (i + 0.5f) / steps;
            var color =
                t <= 0.5f
                    ? LerpColor(left, middle, t * 2)
                    : LerpColor(middle, right, (t - 0.5f) * 2);
            var center =
                (vertical ? min.Y : min.X) + (i + 0.5f) * stepLength;
            var cornerDistance = Math.Min(
                center - (vertical ? min.Y : min.X),
                (vertical ? max.Y : max.X) - center
            );
            var inset = 0f;
            if (cornerDistance < radius)
            {
                var offset = radius - cornerDistance;
                inset = radius - MathF.Sqrt(Math.Max(0, radius * radius - offset * offset));
            }
            var start = i * stepLength;
            var end = (i + 1) * stepLength;
            if (vertical)
                draw.AddRectFilled(
                    new Vector2(min.X + inset, min.Y + start),
                    new Vector2(max.X - inset, min.Y + end),
                    color
                );
            else
                draw.AddRectFilled(
                    new Vector2(min.X + start, min.Y + inset),
                    new Vector2(min.X + end, max.Y - inset),
                    color
                );
        }
    }

    private static uint LerpColor(uint from, uint to, float amount)
    {
        uint result = 0;
        for (var shift = 0; shift <= 24; shift += 8)
        {
            var a = (int)((from >> shift) & 255);
            var b = (int)((to >> shift) & 255);
            result |= (uint)Math.Round(a + (b - a) * amount) << shift;
        }
        return result;
    }

    private static JsonObject EnsureSettings(AppConfig config, Profile p)
    {
        if (config.ProfileSettings[p.Id] is not JsonObject values)
        {
            values = [];
            config.ProfileSettings[p.Id] = values;
        }
        foreach (var s in p.Settings)
            if (!values.ContainsKey(s.Key))
                values[s.Key] = s.Default?.DeepClone();
        return values;
    }

    private void DrawSettings(AppConfig config, Profile p)
    {
        var v = EnsureSettings(config, p);
        ImGui.PushID(p.Id);
        foreach (var s in p.Settings)
        {
            if (s.Type == "boolean")
            {
                var x = v[s.Key]?.GetValue<bool>() ?? false;
                if (ImGui.Checkbox(s.Label, ref x))
                    v[s.Key] = x;
            }
            else if (s.Type == "number")
            {
                var x = NodeFloat(v[s.Key], 0);
                if (ImGui.SliderFloat(s.Label, ref x, s.Min, s.Max))
                    v[s.Key] = (double)x;
            }
            else if (s.Type == "enum")
            {
                var x = v[s.Key]?.GetValue<string>() ?? "";
                if (ImGui.BeginCombo(s.Label, x))
                {
                    foreach (var option in s.Options)
                        if (ImGui.Selectable(option, option == x))
                        {
                            x = option;
                            v[s.Key] = x;
                        }
                    ImGui.EndCombo();
                }
            }
        }
        ImGui.PopID();
    }

    private static bool IsModuleEnabled(AppConfig config, string id) =>
        config.HudModules.Contains(id);

    private static void EnableModule(AppConfig config, string id, bool enabled)
    {
        config.HudModules.RemoveAll(module => module.Equals(id, StringComparison.Ordinal));
        if (enabled)
            config.HudModules.Add(id);
    }

    private static void DrawAudioSettings(
        AppConfig config,
        AudioSpectrumService audio,
        ref bool restart
    )
    {
        ImGui.SeparatorText("Capture");
        var enabled = config.Audio.Enabled;
        if (ImGui.Checkbox("Enable spectrum capture", ref enabled))
        {
            config.Audio.Enabled = enabled;
            restart = true;
        }

        var applicationMode = config.Audio.CaptureMode == "application";
        var modeLabel = applicationMode ? "Application only" : "Output mix";
        if (ImGui.BeginCombo("Capture", modeLabel))
        {
            if (ImGui.Selectable("Output mix", !applicationMode))
            {
                config.Audio.CaptureMode = "output";
                applicationMode = false;
                restart = true;
            }
            if (ImGui.Selectable("Application only", applicationMode))
            {
                config.Audio.CaptureMode = "application";
                applicationMode = true;
                restart = true;
            }
            ImGui.EndCombo();
        }

        if (applicationMode)
        {
            var applications = audio.GetApplicationOptions();
            var current = applications.FirstOrDefault(option =>
                option.Id.Equals(
                    config.Audio.ApplicationName,
                    StringComparison.OrdinalIgnoreCase
                )
            );
            var currentLabel = string.IsNullOrEmpty(current.Name)
                ? string.IsNullOrEmpty(config.Audio.ApplicationName)
                    ? "Select a running application"
                    : config.Audio.ApplicationName
                : current.Name;
            if (ImGui.BeginCombo("Application", currentLabel))
            {
                foreach (var option in applications)
                {
                    var selected = option.Id.Equals(
                        config.Audio.ApplicationName,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (ImGui.Selectable(option.Name, selected))
                    {
                        config.Audio.ApplicationName = option.Id;
                        restart = true;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.TextWrapped(
                "Application capture follows the selected process tree and leaves normal playback untouched."
            );
        }
        else
        {
            var outputs = audio.GetOutputOptions();
            var current = outputs.FirstOrDefault(option => option.Id == config.Audio.OutputDeviceId);
            var currentLabel = string.IsNullOrEmpty(current.Name)
                ? "Default Windows output"
                : current.Name;
            if (ImGui.BeginCombo("Analyze output", currentLabel))
            {
                foreach (var option in outputs)
                {
                    var selected = option.Id == config.Audio.OutputDeviceId;
                    if (ImGui.Selectable(option.Name, selected))
                    {
                        config.Audio.OutputDeviceId = option.Id;
                        restart = true;
                    }
                }
                ImGui.EndCombo();
            }
        }

        if (ImGui.Button("Restart audio capture"))
            restart = true;
        var audioStatus = audio.Snapshot.Status;
        if (!string.IsNullOrEmpty(audioStatus))
            ImGui.TextWrapped(audioStatus);
        var audioSnapshot = audio.Snapshot;
        var strongestBand = audioSnapshot.Bands.Length == 0
            ? 0
            : audioSnapshot.Bands.Max();
        ImGui.Text(
            $"Signal: RMS {audioSnapshot.Rms:F3}  peak {audioSnapshot.Peak:F3}  band {strongestBand:F2}"
        );

        ImGui.SeparatorText("Gamepad media controls");
        var mediaControls = config.Audio.MediaControlsEnabled;
        if (ImGui.Checkbox("Enable D-pad controls", ref mediaControls))
            config.Audio.MediaControlsEnabled = mediaControls;

        var controllerLabel = config.Audio.GamepadIndex < 0
            ? "Auto (first connected)"
            : $"Controller {config.Audio.GamepadIndex + 1}";
        if (ImGui.BeginCombo("Gamepad", controllerLabel))
        {
            if (ImGui.Selectable("Auto (first connected)", config.Audio.GamepadIndex < 0))
                config.Audio.GamepadIndex = -1;
            for (var index = 0; index < 4; index++)
            {
                var selected = config.Audio.GamepadIndex == index;
                if (ImGui.Selectable($"Controller {index + 1}", selected))
                    config.Audio.GamepadIndex = index;
            }
            ImGui.EndCombo();
        }
        ImGui.TextWrapped(
            "D-pad left: previous  |  D-pad right: next  |  left + right: play/pause"
        );
        ImGui.TextWrapped(audio.MediaControlStatus);
    }

    private static float NodeFloat(JsonNode? node, float fallback)
    {
        if (node is not JsonValue value)
            return fallback;
        if (value.TryGetValue<float>(out var single))
            return single;
        if (value.TryGetValue<double>(out var number))
            return (float)number;
        if (value.TryGetValue<int>(out var integer))
            return integer;
        return fallback;
    }

    internal void ReloadChangedProfiles()
    {
        var now = Environment.TickCount64;
        if (now - lastPoll < 250)
            return;
        lastPoll = now;
        foreach (var p in profiles.ToArray())
        {
            var stamp = File.GetLastWriteTimeUtc(p.Script);
            if (stamp == p.Modified)
                continue;
            try
            {
                var replacement = Load(p.Script);
                var i = profiles.IndexOf(p);
                profiles[i] = replacement;
                p.Dispose();
                graphics.Renderer.RecreateFontsTexture();
                diagnostic = "Reloaded Lua HUD: " + replacement.Name;
            }
            catch (Exception e)
            {
                diagnostic = "Hot reload failed for " + p.Name + ": " + e.Message;
                p.Modified = stamp;
            }
        }
    }

    private static bool IsWithin(string path, string root) =>
        path.StartsWith(
            Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase
        ) || path.Equals(root, StringComparison.OrdinalIgnoreCase);

    private static string String(Lua l, int i, string k, string fallback)
    {
        l.GetField(i, k);
        var x = l.IsString(-1) ? l.ToString(-1) : fallback;
        l.Pop(1);
        return x;
    }

    private static double Number(Lua l, int i, string k, double fallback)
    {
        l.GetField(i, k);
        var x = l.IsNumber(-1) ? l.ToNumber(-1) : fallback;
        l.Pop(1);
        return x;
    }

    private static bool Bool(Lua l, int i, string k, bool fallback)
    {
        l.GetField(i, k);
        var x = l.IsBoolean(-1) ? l.ToBoolean(-1) : fallback;
        l.Pop(1);
        return x;
    }

    private static void Set(Lua l, string k, double v)
    {
        l.PushNumber(v);
        l.SetField(-2, k);
    }

    private static void Set(Lua l, string k, bool v)
    {
        l.PushBoolean(v);
        l.SetField(-2, k);
    }

    private static void Set(Lua l, string k, string v)
    {
        l.PushString(v);
        l.SetField(-2, k);
    }

    private static void ReadStringMap(Lua l, int table, string field, Action<string, string> add)
    {
        l.GetField(table, field);
        if (l.IsTable(-1))
        {
            l.PushNil();
            while (l.Next(-2))
            {
                if (l.IsString(-2) && l.IsString(-1))
                    add(l.ToString(-2), l.ToString(-1));
                l.Pop(1);
            }
        }
        l.Pop(1);
    }

    private static JsonNode? ReadJson(Lua l, int i)
    {
        if (l.IsBoolean(i))
            return JsonValue.Create(l.ToBoolean(i));
        if (l.IsNumber(i))
            return JsonValue.Create(l.ToNumber(i));
        if (l.IsString(i))
            return JsonValue.Create(l.ToString(i));
        if (l.IsTable(i))
        {
            var a = new JsonArray();
            for (long n = 1; ; n++)
            {
                l.RawGetInteger(i, n);
                if (!l.IsNumber(-1))
                {
                    l.Pop(1);
                    break;
                }
                a.Add(l.ToNumber(-1));
                l.Pop(1);
            }
            return a;
        }
        return null;
    }

    private static void PushJsonField(Lua l, string key, JsonNode? n)
    {
        if (n is JsonValue j && j.TryGetValue<bool>(out var b))
            l.PushBoolean(b);
        else if (n is JsonValue jn && jn.TryGetValue<double>(out var d))
            l.PushNumber(d);
        else if (n is JsonValue jf && jf.TryGetValue<float>(out var f))
            l.PushNumber(f);
        else if (n is JsonValue ji && ji.TryGetValue<int>(out var integer))
            l.PushNumber(integer);
        else if (n is JsonValue js && js.TryGetValue<string>(out var s))
            l.PushString(s);
        else if (n is JsonArray a)
        {
            l.NewTable();
            for (var i = 0; i < a.Count; i++)
            {
                l.PushNumber(a[i]!.GetValue<double>());
                l.RawSetInteger(-2, i + 1);
            }
        }
        else
            return;
        l.SetField(-2, key);
    }

    private static uint Color(Lua l, int i) => Color(l, i, "color", "#ffffff");

    private static uint Color(Lua l, int i, string field, string fallback)
    {
        var text = String(l, i, field, fallback).TrimStart('#');
        uint rgba = text.Length >= 6 ? Convert.ToUInt32(text[..6], 16) : 0xffffff;
        var r = (rgba >> 16) & 255;
        var g = (rgba >> 8) & 255;
        var b = rgba & 255;
        var alpha = text.Length >= 8 ? Convert.ToUInt32(text[6..8], 16) : 255;
        l.GetField(i, "alpha");
        if (l.IsNumber(-1))
            alpha = (uint)Math.Round(alpha * Math.Clamp(l.ToNumber(-1), 0, 1));
        l.Pop(1);
        return r | (g << 8) | (b << 16) | (alpha << 24);
    }

    private static string CommandName(CommandType t) =>
        t switch
        {
            CommandType.Rect => "rect",
            CommandType.Gradient => "gradient",
            CommandType.Outline => "outline",
            CommandType.Line => "line",
            CommandType.Circle => "circle",
            CommandType.Text => "text",
            CommandType.Image => "image",
            _ => "set_offset",
        };

    public void Dispose()
    {
        foreach (var p in profiles)
            p.Dispose();
        profiles.Clear();
    }

    private sealed class Profile(Lua lua, string script, LuaHookFunction hook) : IDisposable
    {
        internal Lua Lua = lua;
        internal string Script = script,
            Id = "",
            Name = "",
            Author = "",
            Version = "",
            Role = "hud",
            Visibility = "telemetry",
            AssetRoot = "";
        internal int Api;
        internal bool Valid = true;
        internal float Width = 400,
            Height = 200,
            ReferenceHeight = 1080,
            OffsetX,
            OffsetY;
        internal DateTime Modified;
        internal LuaHookFunction Hook = hook;
        internal List<LuaFunction> Callbacks = [];
        internal Dictionary<string, string> Assets = [];
        internal Dictionary<string, ImFontPtr> Fonts = [];
        internal List<Setting> Settings = [];
        internal List<Command> Commands = [];

        public void Dispose() => Lua.Dispose();
    }

    private sealed class Setting
    {
        internal string Key = "",
            Label = "",
            Type = "number";
        internal float Min,
            Max = 1;
        internal int Order;
        internal JsonNode? Default;
        internal List<string> Options = [];
    }

    private enum CommandType
    {
        Rect,
        Gradient,
        Outline,
        Line,
        Circle,
        Text,
        Image,
        Offset,
    }

    private struct Command
    {
        public Command() { }

        internal CommandType Type;
        internal float X,
            Y,
            W,
            H,
            Radius,
            Thickness = 1,
            GlowRadius,
            GlowIntensity,
            Rotation,
            PivotX = 0.5f,
            PivotY = 0.5f,
            UvX1,
            UvY1,
            UvX2 = 1,
            UvY2 = 1,
            ClipX,
            ClipY,
            ClipW,
            ClipH,
            Size;
        internal string Text = "",
            Asset = "",
            Font = "",
            Align = "left",
            Direction = "horizontal",
            Space = "profile";
        internal bool Shadow;
        internal uint Color,
            Color2,
            Color3,
            GlowColor;
    }
}
