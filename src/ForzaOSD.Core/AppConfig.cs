using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ForzaOSD.Core;

public sealed class AppConfig
{
    public const int CurrentVersion = 11;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("bind_address")]
    public string BindAddress { get; set; } = "127.0.0.1";

    [JsonPropertyName("udp_port")]
    public ushort UdpPort { get; set; } = 5300;

    [JsonPropertyName("process_name")]
    public string GameProcessName { get; set; } = "forzahorizon6";

    [JsonPropertyName("hotkey_vk")]
    public int HotkeyVk { get; set; } = 0x1B;

    [JsonPropertyName("hotkey_modifiers")]
    public int HotkeyModifiers { get; set; } = 0x04;

    [JsonPropertyName("metric")]
    public bool Metric { get; set; } = true;

    [JsonPropertyName("show_only_when_foreground")]
    public bool ShowOnlyWhenForeground { get; set; } = true;

    [JsonPropertyName("max_fps")]
    public int MaxFps { get; set; } = 60;

    [JsonPropertyName("capture_unknown_packets")]
    public bool CaptureUnknownPackets { get; set; }

    [JsonPropertyName("hud_profile")]
    public string HudProfile { get; set; } = "forzaosd.vfd";

    [JsonPropertyName("hud_modules")]
    public List<string> HudModules { get; set; } = [];

    [JsonPropertyName("layout")]
    public LayoutConfig Layout { get; set; } = new();

    [JsonPropertyName("theme")]
    public ThemeConfig Theme { get; set; } = new();

    [JsonPropertyName("widgets")]
    public WidgetConfig Widgets { get; set; } = new();

    [JsonPropertyName("profile_settings")]
    public JsonObject ProfileSettings { get; set; } = [];

    [JsonPropertyName("audio")]
    public AudioConfig Audio { get; set; } = new();

    public static (AppConfig Config, string Warning) Load(string path)
    {
        if (!File.Exists(path))
            return (new(), "Using defaults; config.json does not exist yet");
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? [];
            var config = root.Deserialize<AppConfig>(JsonOptions) ?? new();
            if (config.Version < 5)
            {
                config.HotkeyVk = 0x1B;
                config.HotkeyModifiers = 0x04;
            }
            if (config.ProfileSettings["native"] is null)
            {
                config.ProfileSettings["native"] = new JsonObject
                {
                    ["x"] = config.Layout.X,
                    ["y"] = config.Layout.Y,
                    ["scale"] = config.Layout.Scale,
                    ["redline"] = config.Theme.RedlineRatio,
                };
            }
            if (root["profiles"] is JsonObject legacyProfiles)
            {
                if (
                    legacyProfiles["gt7hud"] is JsonObject gt7
                    && config.ProfileSettings["csp.gt7hud"] is null
                )
                {
                    config.ProfileSettings["csp.gt7hud"] = new JsonObject
                    {
                        ["x"] = gt7["x"]?.GetValue<double>() ?? .5,
                        ["y"] = (gt7["y"]?.GetValue<double>() ?? 1) - 120d / 2160d,
                        ["scale"] = gt7["scale"]?.GetValue<double>() ?? 1,
                        ["show_extras"] = gt7["show_extras"]?.GetValue<bool>() ?? true,
                    };
                }
                if (
                    legacyProfiles["shift_tacho"] is JsonObject shift
                    && config.ProfileSettings["beamng.ghostsdigitaltacho"] is null
                )
                    config.ProfileSettings["beamng.ghostsdigitaltacho"] = shift.DeepClone();
            }
            config.Version = CurrentVersion;
            config.Normalize();
            return (config, "");
        }
        catch (Exception e)
        {
            return (new(), $"Invalid config.json; using defaults: {e.Message}");
        }
    }

    public void Save(string path)
    {
        Normalize();
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, JsonOptions) + Environment.NewLine);
        File.Move(temp, path, true);
    }

    private void Normalize()
    {
        Version = CurrentVersion;
        if (UdpPort == 0)
            UdpPort = 1;
        var processName = Path.GetFileNameWithoutExtension(GameProcessName?.Trim()) ?? "";
        GameProcessName = string.IsNullOrEmpty(processName) ? "forzahorizon6" : processName;
        Layout.X = Math.Clamp(Layout.X, 0, 1);
        Layout.Y = Math.Clamp(Layout.Y, 0, 1);
        Layout.Scale = Math.Clamp(Layout.Scale, .5f, 3);
        Layout.Opacity = Math.Clamp(Layout.Opacity, .1f, 1);
        MaxFps = Math.Clamp(MaxFps, 30, 240);
        Theme.RedlineRatio = Math.Clamp(Theme.RedlineRatio, .5f, 1);
        HudModules ??= [];
        HudModules = HudModules
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Audio ??= new();
        Audio.CaptureMode = Audio.CaptureMode == "application" ? "application" : "output";
        Audio.OutputDeviceId ??= "";
        Audio.ApplicationName ??= "";
        Audio.GamepadIndex = Math.Clamp(Audio.GamepadIndex, -1, 3);
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.Strict,
    };
}

public sealed class AudioConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("capture_mode")]
    public string CaptureMode { get; set; } = "output";

    [JsonPropertyName("output_device_id")]
    public string OutputDeviceId { get; set; } = "";

    [JsonPropertyName("application_name")]
    public string ApplicationName { get; set; } = "";

    [JsonPropertyName("media_controls_enabled")]
    public bool MediaControlsEnabled { get; set; } = true;

    [JsonPropertyName("gamepad_index")]
    public int GamepadIndex { get; set; } = -1;
}

public sealed class LayoutConfig
{
    [JsonPropertyName("x")]
    public float X { get; set; } = .5f;

    [JsonPropertyName("y")]
    public float Y { get; set; } = .82f;

    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1;

    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 1;
}

public sealed class ThemeConfig
{
    [JsonPropertyName("accent")]
    public float[] Accent { get; set; } = [.1f, .78f, 1, 1];

    [JsonPropertyName("redline_ratio")]
    public float RedlineRatio { get; set; } = .9f;
}

public sealed class WidgetConfig
{
    [JsonPropertyName("speed")]
    public bool Speed { get; set; } = true;

    [JsonPropertyName("rpm")]
    public bool Rpm { get; set; } = true;

    [JsonPropertyName("gear")]
    public bool Gear { get; set; } = true;

    [JsonPropertyName("pedals")]
    public bool Pedals { get; set; } = true;
}
