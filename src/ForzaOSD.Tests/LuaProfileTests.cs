using KeraLua;

namespace ForzaOSD.Tests;

public sealed class LuaProfileTests
{
    private static readonly string[] BlockedGlobals =
    [
        "dofile",
        "loadfile",
        "load",
        "require",
        "package",
        "io",
        "os",
        "debug",
        "coroutine",
    ];

    [Fact]
    public void ShippedProfilesHaveValidUniqueIdsAndSandbox()
    {
        var root = FindRepositoryRoot();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (
            var script in Directory.EnumerateFiles(
                Path.Combine(root, "hud_profiles"),
                "profile.lua",
                SearchOption.AllDirectories
            )
        )
        {
            using var lua = CreateSandbox();
            Assert.Equal(LuaStatus.OK, lua.LoadFile(script));
            Assert.Equal(LuaStatus.OK, lua.PCall(0, 1, 0));
            Assert.True(lua.IsTable(-1));
            lua.GetField(-1, "api_version");
            Assert.Equal(1, lua.ToInteger(-1));
            lua.Pop(1);
            lua.GetField(-1, "id");
            Assert.True(lua.IsString(-1));
            Assert.True(ids.Add(lua.ToString(-1)), "Duplicate profile id");
            lua.Pop(1);
            lua.GetField(-1, "role");
            if (!lua.IsNil(-1))
            {
                Assert.True(lua.IsString(-1));
                Assert.Contains(lua.ToString(-1), new[] { "hud", "module" });
            }
            lua.Pop(1);
            foreach (var blocked in BlockedGlobals)
            {
                lua.GetGlobal(blocked);
                Assert.True(lua.IsNil(-1), $"{blocked} must not be exposed");
                lua.Pop(1);
            }
        }
        Assert.Contains("native", ids);
        Assert.Contains("forzaosd.vfd", ids);
        Assert.Contains("forzaosd.vfd_radio", ids);
        Assert.Contains("forzaosd.tire_telemetry", ids);
    }

    [Fact]
    public void InfiniteScriptHitsInstructionBudget()
    {
        using var lua = CreateSandbox();
        LuaHookFunction hook = (state, _) =>
            Lua.FromIntPtr(state).Error("HUD script exceeded its instruction budget");
        Assert.Equal(LuaStatus.OK, lua.LoadString("while true do end"));
        lua.SetHook(hook, LuaHookMask.Count, 100_000);
        var result = lua.PCall(0, 0, 0);
        lua.SetHook(hook, (LuaHookMask)0, 0);
        Assert.NotEqual(LuaStatus.OK, result);
        Assert.Contains("instruction budget", lua.ToString(-1));
    }

    [Fact]
    public void ShippedProfilesRenderWithPreviewContext()
    {
        var root = FindRepositoryRoot();
        foreach (
            var script in Directory.EnumerateFiles(
                Path.Combine(root, "hud_profiles"),
                "profile.lua",
                SearchOption.AllDirectories
            )
        )
        {
            using var lua = CreateSandbox();
            Assert.Equal(LuaStatus.OK, lua.LoadFile(script));
            Assert.Equal(LuaStatus.OK, lua.PCall(0, 1, 0));
            lua.SetGlobal("profile");

            const string preview = """
                local function noop(_) end
                local draw = {
                  rect = noop,
                  gradient = noop,
                  outline = noop,
                  line = noop,
                  circle = noop,
                  text = noop,
                  image = noop,
                  set_offset = noop,
                }
                local settings = {}
                for key, schema in pairs(profile.settings or {}) do
                  settings[key] = schema.default
                end
                local telemetry = setmetatable({
                  available = false,
                  fresh = false,
                  race_on = false,
                  gear_label = "N",
                }, { __index = function() return 0 end })
                local bands = {}
                for index = 1, 28 do
                  bands[index] = index / 28
                end
                local audio = setmetatable({
                  enabled = true,
                  available = false,
                  playing = true,
                  title = "Preview Track",
                  artist = "Preview Artist",
                  source = "ForzaOSD.Tests",
                  rms = 0.25,
                  peak = 0.7,
                  bands = bands,
                }, { __index = function() return 0 end })
                profile.render({
                  draw = draw,
                  settings = settings,
                  telemetry = telemetry,
                  audio = audio,
                  metric = true,
                  opacity = 1,
                  time = 12.5,
                  edit_mode = true,
                })
                """;
            Assert.Equal(LuaStatus.OK, lua.LoadString(preview));
            var result = lua.PCall(0, 0, 0);
            Assert.True(
                result == LuaStatus.OK,
                $"{script} preview failed: {lua.ToString(-1)}"
            );
        }
    }

    [Fact]
    public void TireTelemetryRendersEveryModeAndDetailState()
    {
        var script = Path.Combine(
            FindRepositoryRoot(),
            "hud_profiles",
            "tire_telemetry",
            "profile.lua"
        );
        using var lua = CreateSandbox();
        Assert.Equal(LuaStatus.OK, lua.LoadFile(script));
        Assert.Equal(LuaStatus.OK, lua.PCall(0, 1, 0));
        lua.SetGlobal("profile");

        const string preview = """
            local function noop(_) end
            local draw = {
              rect = noop, gradient = noop, outline = noop, line = noop,
              circle = noop, text = noop, image = noop, set_offset = noop,
            }
            local telemetry = setmetatable({
              available = true, fresh = true, race_on = true,
              tire_temp_front_left = 130, tire_temp_front_right = 180,
              tire_temp_rear_left = 225, tire_temp_rear_right = 260,
              tire_combined_slip_front_left = 0.5,
              tire_combined_slip_front_right = 0.8,
              tire_combined_slip_rear_left = 1.0,
              tire_combined_slip_rear_right = 1.4,
              wheel_in_puddle_front_left = true,
              wheel_on_rumble_strip_rear_right = true,
            }, { __index = function() return 0 end })
            local modes = { "Combined", "Ratio", "Angle" }
            local time = 1
            for _, mode in ipairs(modes) do
              for _, metric in ipairs({ true, false }) do
                for _, details in ipairs({ true, false }) do
                  profile.render({
                    draw = draw,
                    settings = {
                      x = 0.16, y = 0.72, scale = 1,
                      slip_mode = mode, show_details = details, show_contacts = true,
                    },
                    telemetry = telemetry,
                    metric = metric,
                    opacity = 1,
                    time = time,
                    edit_mode = false,
                  })
                  time = time + 0.016
                end
              end
            end
            telemetry.fresh = false
            profile.render({
              draw = draw,
              settings = {
                x = 0.16, y = 0.72, scale = 1,
                slip_mode = "Combined", show_details = false, show_contacts = false,
              },
              telemetry = telemetry,
              metric = true,
              opacity = 0.5,
              time = time + 1,
              edit_mode = false,
            })
            """;
        Assert.Equal(LuaStatus.OK, lua.LoadString(preview));
        var result = lua.PCall(0, 0, 0);
        Assert.True(result == LuaStatus.OK, lua.ToString(-1));
    }

    private static Lua CreateSandbox()
    {
        var lua = new Lua(true);
        foreach (var blocked in BlockedGlobals)
        {
            lua.PushNil();
            lua.SetGlobal(blocked);
        }
        return lua;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "hud_profiles")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository hud_profiles directory");
    }
}
