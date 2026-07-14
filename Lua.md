# Lua HUD API

ForzaOSD loads HUD profiles from `hud_profiles/<folder>/profile.lua`. Each script
runs in its own Lua 5.4 state and returns one profile table. The C# runtime owns
window placement, persistence, telemetry, audio capture, and rendering. Lua code
emits draw commands; it does not receive an ImGui object or a DX11 device.

API version 1 is the only supported version.

## Minimal profile

```lua
local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function render(ctx)
  local tm = ctx.telemetry
  local ratio = clamp(tm.rpm / math.max(1, tm.max_rpm), 0, 1)

  ctx.draw.rect {
    x = 20, y = 20, w = 400 * ratio, h = 12,
    color = "#36e6d1", alpha = ctx.opacity,
  }
  ctx.draw.text {
    text = string.format("%03d", ctx.metric and tm.speed_kph or tm.speed_mph),
    x = 220, y = 90, size = 72, align = "center",
    color = "#ffffff", alpha = ctx.opacity,
  }
end

return {
  api_version = 1,
  id = "example.simple",
  name = "Simple",
  author = "Example",
  version = "1.0.0",
  layout = { width = 440, height = 140, reference_height = 1080 },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.5, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.8, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.5, max = 2, order = 3 },
  },
  render = render,
}
```

New profile directories are discovered at startup. Restart ForzaOSD after adding
or removing a directory.

## Returned profile table

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `api_version` | integer | required | Must be `1`. |
| `id` | string | required | Stable persistence key. It must be non-empty and unique. |
| `name` | string | `id` | Name shown in the profile or module selector. |
| `author` | string | `""` | Profile metadata. |
| `version` | string | `""` | Profile metadata. The runtime does not parse this value. |
| `role` | string | `"hud"` | `"hud"` or `"module"`. |
| `visibility` | string | `"telemetry"` | `"telemetry"` or `"audio"`. |
| `asset_root` | string | `""` | Path relative to `profile.lua`. Asset and font paths are relative to this directory. |
| `layout` | table | see below | Local canvas dimensions and scale reference. |
| `assets` | table | `{}` | Map of asset keys to image paths. |
| `fonts` | table | `{}` | Map of font keys to font declarations. |
| `shaders` | table | `{}` | Map of shader keys to profile-local HLSL source paths. |
| `settings` | table | `{}` | Profile-owned settings schema. |
| `render` | function | required | Called as `render(ctx)` while the profile is visible. |

The default layout is:

```lua
layout = {
  width = 400,
  height = 200,
  reference_height = 1080,
}
```

Use positive layout dimensions. Coordinates are not clipped to the layout unless
a draw command supplies a clip rectangle.

### Roles and visibility

A `hud` is a primary profile. One primary profile is active at a time. A `module`
can be enabled alongside the selected primary profile; multiple modules can be
active.

Visibility is evaluated independently for every active profile:

| `visibility` | Render condition |
| --- | --- |
| `"telemetry"` | Edit mode, or a fresh packet with `race_on == true`. |
| `"audio"` | Edit mode, audio spectrum data available, or an active media session reporting playback. |

Telemetry becomes stale 500 ms after the last decoded packet. Edit mode bypasses
the visibility condition so the selected HUD and enabled modules can be positioned
without live data.

## Coordinates and placement

Draw commands use profile-local coordinates. The runtime computes one scale and
origin per profile:

```text
scale = viewport_height / max(1, reference_height) * settings.scale

origin_x = viewport_width  * settings.x - layout.width  * scale / 2
origin_y = viewport_height * settings.y - layout.height * scale / 2

screen_position = origin + local_position * scale
```

If `x`, `y`, or `scale` is absent from `ctx.settings`, the runtime uses `0.5`,
`0.5`, and `1`. Declare them in the settings schema if the user should be able to
move or resize the profile. `x` and `y` are normalized viewport anchors, not local
pixel offsets.

Coordinates, dimensions, font sizes, line thickness, corner radius, glow radius,
and clip rectangles are multiplied by `scale`. Lines and outlines have a minimum
rendered thickness of one screen pixel.

`space = "screen"` removes the profile origin and dynamic offset from one command.
Its coordinates are still multiplied by the profile scale:

```text
screen_position = command_position * scale
```

## Assets and fonts

Keep a profile's files below its own directory:

```text
hud_profiles/example/
  profile.lua
  assets/
    img/needle.png
    fonts/digits.ttf
```

Declare images by key:

```lua
asset_root = "assets",
assets = {
  needle = "img/needle.png",
  face = "img/face.png",
},
```

`draw.image` refers to the key, not the path:

```lua
draw.image { asset = "face", x = 0, y = 0, w = 300, h = 300 }
```

The runtime resolves declared image paths during profile loading. A missing image
or a path outside the ForzaOSD application directory rejects the profile. Images
are decoded through Windows Imaging Component; shipped profiles use PNG.

Fonts are separate declarations:

```lua
fonts = {
  digits = { path = "fonts/digits.ttf", size = 96 },
  labels = { path = "fonts/labels.ttf", size = 32 },
},
```

`size` is the font's atlas size. A text command can render the font at another
size. An empty or unknown font key uses ImGui's default font.

Image textures are cached by path. Editing an image in place does not invalidate
the cache; restart ForzaOSD to reload it. An unknown key in `draw.image` is skipped
without a runtime error.

## Custom DX11 shaders

Profiles can declare HLSL pixel effects stored below their own directory:

```lua
shaders = {
  scanlines = "shaders/scanlines.hlsl",
},
```

Shader files implement one function. ForzaOSD injects the DX11 resource bindings,
constant buffer, pixel input, and entry point:

```hlsl
float4 effect(ForzaOSDInput input)
{
    float4 sampled = source_texture.Sample(source_sampler, input.uv);
    return sampled * input.color;
}
```

`ForzaOSDInput` contains:

| Field | Meaning |
| --- | --- |
| `float2 uv` | Interpolated command UV coordinates. |
| `float4 color` | Interpolated `color` and `alpha` tint. |
| `float2 screen_position` | Current pixel position in viewport pixels. |
| `float2 local_position` | Position relative to the command's unrotated bounds, normally `0..1`. |

The injected globals are:

| Name | Meaning |
| --- | --- |
| `source_texture`, `source_sampler` | Optional declared asset at `t0` and sampler at `s0`; the texture is solid white when no asset is supplied. |
| `viewport` | Width, height, inverse width, inverse height. |
| `bounds` | Command screen X, Y, width, and height. |
| `frame` | Elapsed time and frame delta in `.x` and `.y`; `.zw` is the screen-origin offset used while rendering an effect layer. |
| `params0` ... `params3` | Up to 16 Lua parameters, packed four per vector. Missing values are zero. |

Render the effect with `draw.shader`:

```lua
draw.shader {
  shader = "scanlines",
  asset = "face", -- optional
  x = 0, y = 0, w = 400, h = 200,
  color = "#5ffff0",
  alpha = ctx.opacity,
  sampler = "clamp",
  params = { ctx.telemetry.rpm, ctx.settings.intensity },
}
```

`shader` must name a declared shader. `asset` is optional but, when present, must
name a declared image. `sampler` is `"clamp"` by default and also accepts `"wrap"`
and transparent `"border"` sampling.
`params` accepts at most 16 finite numbers. Telemetry and settings are deliberately
passed through Lua rather than exposed as a second implicit shader interface.

Shader commands support the image UV, rotation, pivot, color, alpha, coordinate
space, and clipping fields. They use the normal straight-alpha blend state. Glow
fields, extra textures, compute shaders, UAVs, arbitrary constant buffers, and
multi-pass effects are not supported.

Sources compile as `ps_4_0` when the profile loads. They must use the `.hlsl`
extension, cannot escape their profile directory, cannot use `#include`, and are
limited to 256 KiB. Compiler diagnostics include the shader filename and source
line. The bundled `Shader telemetry grid (demo)` module is disabled by default and
shows a procedural, telemetry-driven example.

### Effect layers

`draw.layer` renders ordinary draw commands into a pooled offscreen texture, runs
that texture through a declared shader, and composites the result at the same
point in the HUD's draw order:

```lua
draw.layer({
  shader = "glass",
  x = 20, y = 40, w = 400, h = 160,
  margin = 12,
  sampler = "border",
  alpha = ctx.opacity,
  params = { ctx.telemetry.rpm, ctx.telemetry.throttle },
}, function()
  draw.rect { x = 20, y = 40, w = 400, h = 160, color = "#071315" }
  draw.text { x = 220, y = 100, align = "center", text = "LAYERED" }
  draw.image { asset = "needle", x = 200, y = 60, w = 40, h = 120 }
end)
```

Commands inside the function retain their normal profile or screen coordinates;
they are clipped to the layer's expanded capture area. `margin` expands every side
of that area so blur, bloom, distortion, and chromatic offsets have room beyond
the nominal bounds. It defaults to `0` and is limited to `512` profile units.

The layer options accept the same `shader`, `sampler`, `params`, `color`, `alpha`,
and coordinate-space fields as `draw.shader`. The color and alpha apply when the
finished layer is composited, so avoid applying `ctx.opacity` both to every inner
command and to the layer unless the multiplied result is intentional.

Offscreen drawing uses alpha blending, so `source_texture` contains premultiplied
RGB. Effects that need straight RGB should unpremultiply each sample:

```hlsl
float4 sample = source_texture.Sample(source_sampler, input.uv);
sample.rgb /= max(sample.a, 0.0001);
```

Layers cannot be nested. A profile may emit at most 16 layers per frame. Render
targets are reused by pixel dimensions and capped at 4096 pixels per axis; larger
logical layers render at reduced resolution. Each layer adds an offscreen draw
pass plus its shader pass, so tightly bound the layer and avoid wrapping an entire
4K viewport when only one instrument needs an effect.

## Settings schema

The settings editor implements three types:

| Type | Required fields | Optional fields |
| --- | --- | --- |
| `"boolean"` | `default` as a boolean | `label`, `order` |
| `"number"` | `default` as a number | `label`, `min`, `max`, `order` |
| `"enum"` | `default` as a string, `options` as a sequence of strings | `label`, `order` |

`label` defaults to the setting key. `order` defaults to `100`. Number limits
default to `0` and `1`. Ctrl-click a number slider to enter a value as text.

```lua
settings = {
  x = {
    type = "number",
    label = "Horizontal",
    default = 0.5,
    min = 0,
    max = 1,
    order = 1,
  },
  show_units = {
    type = "boolean",
    label = "Speed units",
    default = true,
    order = 2,
  },
  palette = {
    type = "enum",
    label = "Palette",
    default = "Aqua",
    options = { "Aqua", "Amber" },
    order = 3,
  },
}
```

Values are available at `ctx.settings.<key>`. They are stored under
`profile_settings.<profile-id>` in `config.json` when the user presses `Save
settings`. Adding a new schema entry inserts its default. Changing a default does
not replace a value already present in `config.json`.

There is no `color` setting editor in API version 1. Use an enum for a fixed
palette or expose numeric channels separately.

## Render context

`render(ctx)` receives these fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `ctx.telemetry` | table | Latest decoded Forza packet fields. |
| `ctx.audio` | table | Audio spectrum and Windows media-session metadata. |
| `ctx.settings` | table | Persisted settings for this profile. |
| `ctx.draw` | table | Draw-command functions. |
| `ctx.metric` | boolean | Global unit setting. The profile chooses which values or labels to use. |
| `ctx.opacity` | number | Global opacity in the range `0.1` to `1`. It is not applied automatically. |
| `ctx.time` | number | Monotonic timer value in seconds. Its origin is unspecified; it is not wall-clock time. |
| `ctx.edit_mode` | boolean | `true` while the Shift+Esc settings window is open. |

The Lua state persists between frames. Module-level variables can hold animation
state, observed peak values, or the previous gear. A successful hot reload creates
a new Lua state and resets those variables.

The draw-command list is cleared before each call. Commands render in call order;
later commands can cover earlier commands.

### Telemetry fields

| Field | Type | Unit or range | Meaning |
| --- | --- | --- | --- |
| `available` | boolean | | At least one valid packet has been received. |
| `fresh` | boolean | | The last valid packet is no more than 500 ms old. |
| `race_on` | boolean | | Forza's race-active flag from the last valid packet. |
| `speed_mps` | number | m/s | Current speed from the packet. |
| `speed_kph` | number | km/h | `speed_mps * 3.6`. |
| `speed_mph` | number | mph | `speed_mps * 2.2369363`. |
| `rpm` | number | RPM | Current engine speed. |
| `max_rpm` | number | RPM | Engine maximum supplied by Forza. |
| `idle_rpm` | number | RPM | Engine idle speed supplied by Forza. |
| `acceleration_x`, `acceleration_y`, `acceleration_z` | number | m/s² | Local acceleration; X is right, Y is up, and Z is forward. |
| `velocity_x`, `velocity_y`, `velocity_z` | number | m/s | Local velocity on the right, up, and forward axes. |
| `angular_velocity_x`, `angular_velocity_y`, `angular_velocity_z` | number | rad/s | Local pitch, yaw, and roll rates. |
| `yaw`, `pitch`, `roll` | number | radians | Car orientation in world space. |
| `car_ordinal` | number | | Forza car make/model identifier. |
| `car_class` | number | `0..7` | Raw performance-class identifier. |
| `car_performance_index` | number | normally `100..999` | Performance index supplied by Forza. |
| `drivetrain_type` | number | `0..2` | `0` is FWD, `1` is RWD, and `2` is AWD. |
| `num_cylinders` | number | | Engine cylinder count. |
| `car_group` | number | | FH6 car-group identifier; zero for Motorsport packets. |
| `smashable_velocity_difference` | number | m/s | Velocity lost in the latest smashable-object collision; zero for Motorsport packets. |
| `smashable_mass` | number | kg | Mass of the recently hit smashable object; zero for Motorsport packets. |
| `position_x`, `position_y`, `position_z` | number | meters | Car position in world space. |
| `power_watts` | number | watts | Instantaneous engine power. Negative values are valid. |
| `torque_nm` | number | N·m | Instantaneous engine torque. |
| `gear` | number | | Raw gear. `0` is reverse, `11` is neutral, and forward gears use their displayed number. |
| `gear_label` | string | | `"R"`, `"N"`, or the decimal raw gear value. |
| `throttle` | number | `0..1` | Accelerator input. |
| `brake` | number | `0..1` | Brake input. |
| `clutch` | number | `0..1` | Clutch input. |
| `handbrake` | boolean | | Handbrake input. |
| `steering` | number | `-1..1` | Normalized steering input. |
| `steering_raw` | number | `-128..127` | Signed steering byte before normalization. |
| `driving_line` | number | `-1..1` | Normalized driving-line position. |
| `driving_line_raw` | number | `-128..127` | Signed driving-line byte before normalization. |
| `ai_brake_difference` | number | `-1..1` | Normalized AI braking difference. |
| `ai_brake_difference_raw` | number | `-128..127` | Signed AI-braking byte before normalization. |
| `fuel` | number | normally `0..1` | Fuel level. |
| `boost` | number | PSI | Pressure relative to atmosphere. Negative values are manifold vacuum and can fall below `-10`. |
| `tire_temp_front_left` | number | °F | Raw tire temperature. |
| `tire_temp_front_right` | number | °F | Raw tire temperature. |
| `tire_temp_rear_left` | number | °F | Raw tire temperature. |
| `tire_temp_rear_right` | number | °F | Raw tire temperature. |
| `distance_traveled_m` | number | meters | Total distance traveled. |
| `best_lap_seconds` | number | seconds | Game-supplied best lap, or zero when unavailable. |
| `last_lap_seconds` | number | seconds | Game-supplied last lap, or zero when unavailable. |
| `current_lap_seconds` | number | seconds | Current lap clock, or zero when not timing a lap. |
| `race_time_seconds` | number | seconds | Total race time reported by the game. |
| `lap_number` | number | completed laps | Raw unsigned lap counter. |
| `race_position` | number | | Raw race-position byte. |
| `lateral_g` | number | g | Local X acceleration divided by standard gravity; positive is right. |
| `longitudinal_g` | number | g | Local Z acceleration divided by standard gravity; positive is forward. |

The following four-corner fields use the suffixes `front_left`, `front_right`,
`rear_left`, and `rear_right`:

| Field prefix | Type | Unit or range | Meaning |
| --- | --- | --- | --- |
| `normalized_suspension_travel_` | number | normally `0..1` | `0` is maximum stretch and `1` is maximum compression. |
| `tire_slip_ratio_` | number | normalized | Longitudinal slip; an absolute value above `1` means loss of grip. |
| `wheel_rotation_speed_` | number | rad/s | Signed wheel rotation speed. |
| `wheel_on_rumble_strip_` | boolean | | Whether the wheel is on a rumble strip. |
| `wheel_in_puddle_` | boolean | | Whether the wheel is in a puddle. |
| `surface_rumble_` | number | non-dimensional | Surface feedback value used by controller force feedback. |
| `tire_slip_angle_` | number | normalized | Lateral slip; an absolute value above `1` means loss of grip. |
| `tire_combined_slip_` | number | normalized | Combined tire slip; an absolute value above `1` means loss of grip. |
| `suspension_travel_meters_` | number | meters | Actual suspension travel. |

When no packet is available, numeric fields are zero except `gear`, which is `11`,
and `gear_label`, which is `"N"`. A stale snapshot retains the last decoded frame;
check `fresh` when a profile needs to distinguish live data from retained data.

ForzaOSD rejects malformed core fields and replaces non-finite optional values
with zero. Optional values are not otherwise clamped. Clamp presentation values
before using them as dimensions, ratios, or array indices.

All telemetry fields are the latest packet values. The runtime does not smooth
or interpolate them. The game can retain lap values after an event, so use
`race_on`, `fresh`, and `current_lap_seconds` when detecting an actively timed
lap. FH6 does not provide tire wear or an ABS-active flag in this format.

### Audio fields

| Field | Type | Meaning |
| --- | --- | --- |
| `enabled` | boolean | Audio capture is enabled in the global settings. |
| `available` | boolean | The analyzer has produced a spectrum frame. |
| `playing` | boolean | The selected Windows media session reports that it is playing. |
| `title` | string | Media-session title, or an empty string. |
| `artist` | string | Media-session artist or album artist, or an empty string. |
| `source` | string | Media-session source application identifier, or an empty string. |
| `rms` | number | RMS amplitude of the latest 2,048-sample analysis block. |
| `peak` | number | Peak absolute amplitude of the latest analysis block. |
| `bands` | table | 28 one-based, low-to-high spectrum values. |

`bands[1]` starts near 45 Hz. `bands[28]` ends at the lower of 18 kHz or 48% of
the capture sample rate. Bands are logarithmically spaced, mapped to `0..1`, and
smoothed with faster attack than release. Treat them as display levels, not
calibrated sound-pressure measurements.

Spectrum availability and media metadata are independent. A player can expose
title and playback state while capture is disabled or silent.

## Draw commands

Every draw function takes one table and returns no value.

```lua
draw.rect {
  x = 10, y = 10, w = 160, h = 40,
  color = "#20d9c4", alpha = ctx.opacity, rounding = 4,
}
```

All drawable commands accept these common fields:

| Field | Default | Meaning |
| --- | --- | --- |
| `color` | `"#ffffff"` | Fill, stroke, text, or image-tint color. |
| `alpha` | `1` | Multiplier for every color in the command. |
| `space` | `"profile"` | `"profile"` uses the profile origin; `"screen"` does not. |
| `glow_radius` | `0` | Glow extent in profile units. |
| `glow_intensity` | `0` | Glow strength. |
| `glow_color` | `"#ffffff"` | Glow color. |
| `clip_x`, `clip_y` | `0`, `0` | Clip-rectangle origin. |
| `clip_w`, `clip_h` | `0`, `0` | Clip-rectangle size. Non-positive values disable clipping. |

### Command fields

| Function | Position and size | Other fields |
| --- | --- | --- |
| `draw.rect` | `x`, `y`, `w`, `h` default to `0` | `rounding` defaults to `0`. |
| `draw.gradient` | `x`, `y`, `w`, `h` default to `0` | `color2` and `color3` default to white; `direction` defaults to `"horizontal"`; `rounding` defaults to `0`. |
| `draw.outline` | `x`, `y`, `w`, `h` default to `0` | `rounding` defaults to `0`; `thickness` defaults to `1`. |
| `draw.line` | `x1`, `y1`, `x2`, `y2` default to `0` | `thickness` defaults to `1`. |
| `draw.circle` | `cx`, `cy`, `radius` default to `0` | No command-specific fields. |
| `draw.text` | `x`, `y` default to `0`; `size` defaults to `24` | `font` and `text` default to `""`; `align` defaults to `"left"`; `shadow` defaults to `false`. |
| `draw.image` | `x`, `y`, `w`, `h` default to `0` | `asset` defaults to `""`; `rotation` defaults to `0`; see the UV and pivot fields below. |
| `draw.shader` | `x`, `y`, `w`, `h` default to `0` | Requires `shader`; optional `asset`, `params`, and `sampler`; supports image UV and pivot fields. |
| `draw.layer` | `x`, `y`, `w`, `h` must form positive bounds | Requires `shader` and a second render-function argument; optional `margin`, `params`, and `sampler`. |
| `draw.set_offset` | `x`, `y` default to `0` | This is profile state, not a queued drawable command. |

`draw.gradient` uses `color` at the start, `color2` at the midpoint, and `color3`
at the end. `direction = "vertical"` selects a top-to-bottom gradient; every other
value is treated as horizontal.

For text, `x` is the left, center, or right anchor selected by `align`. `y` is the
vertical center of the text. Valid alignment values are `"left"`, `"center"`, and
`"right"`; unknown values behave as left alignment.

Image UV fields are `uv_x1`, `uv_y1`, `uv_x2`, and `uv_y2`. Rotation is clockwise
in degrees around the normalized `pivot_x`, `pivot_y` point.

`draw.set_offset` sets a profile-wide local offset. It is not a transform stack and
its position in the command sequence does not matter. The value applies to all
profile-space commands and the edit-mode boundary. It persists until the profile
sets another value, so code using conditional movement should explicitly reset it:

```lua
if ctx.settings.dynamic_movement then
  draw.set_offset { x = ctx.telemetry.lateral_g * 12, y = 0 }
else
  draw.set_offset { x = 0, y = 0 }
end
```

### Color and opacity

Colors are strings in `#RRGGBB` or `#RRGGBBAA` form. Hex digits are
case-insensitive. Three- and four-digit shorthand is not supported.

`alpha` is optional and clamped to `0..1`. It multiplies the alpha channel of the
command's colors, including gradient and glow colors. The runtime does not apply
`ctx.opacity` implicitly. Profiles should pass it on commands that follow the
global opacity setting:

```lua
draw.image {
  asset = "needle",
  x = 100, y = 20, w = 24, h = 180,
  color = "#ffffffcc",
  alpha = 0.75 * ctx.opacity,
}
```

### Glow

All drawable commands accept:

```lua
glow_radius = 8
glow_intensity = 1
glow_color = "#38ffe8"
```

Glow is disabled unless both radius and intensity are greater than zero.
`glow_color` defaults to white, not to `color`. Rectangles, outlines, lines,
circles, and text use layered ImGui primitives. Unrotated images use the external
custom shader in `Shaders/bloom.hlsl`; this follows the same compiler and renderer
path as profile shaders. Rotated images do not render an image bloom pass.

Intensity is clamped internally for primitive falloff. Each glowing command emits
additional geometry or shader work; avoid applying glow to large numbers of
overlapping elements.

### Clipping

Every drawable command accepts:

```lua
clip_x = 0
clip_y = 0
clip_w = 200
clip_h = 80
```

Clipping is active only when `clip_w > 0` and `clip_h > 0`. The clip rectangle uses
the same coordinate space as the command and is intersected with the current ImGui
clip rectangle.

## Reloading and errors

ForzaOSD checks the modification time of each loaded `profile.lua` and its declared
shader sources every 250 ms. A successful reload replaces the Lua state and shader
programs, then rebuilds the font atlas. A syntax, asset, font, shader compilation,
or other load-time error leaves the previous profile instance active and prints
the error in the Shift+Esc diagnostics.

These changes require a restart:

- adding or removing a profile directory;
- replacing an image at the same path;
- changing font or other files that are not watched shader sources.

A runtime error in `render(ctx)` discards that frame's command list and reports a
diagnostic. Fixing and saving `profile.lua` reloads the profile.

## Sandbox and execution limits

Each profile gets Lua base functions and the `table`, `string`, `math`, and `utf8`
libraries. The runtime removes these globals:

```text
dofile  loadfile  load  require  package  io  os  debug  coroutine
```

Profiles cannot load another script, access the filesystem, start a process, open
a network connection, or use the debug library through the supported API.

Custom HLSL is trusted local GPU code, not part of the Lua security sandbox. The
runtime restricts paths and only supplies the documented bindings, but a shader
with excessive or unbounded work can still stall or reset the graphics driver.
Only install shader profiles from sources you trust.

Profile loading and each `render(ctx)` call have a 100,000-instruction hook limit.
Exceeding it raises `HUD script exceeded its instruction budget`. Keep expensive
asset-name generation at load time and avoid unbounded loops in `render`.

## Validation

Run the Lua profile tests from the repository root:

```powershell
.\.dotnet\dotnet test src\ForzaOSD.Tests\ForzaOSD.Tests.csproj -c Release --filter FullyQualifiedName~LuaProfileTests
```

The tests check syntax, API version, unique IDs, roles, blocked globals, the
instruction limit, and a preview render using default settings. They do not replace
an edit-mode visual check with the intended assets and telemetry values.
