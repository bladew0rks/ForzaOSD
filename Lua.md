# Lua HUD API

Each HUD lives in `hud_profiles/<folder>/profile.lua`. ForzaOSD discovers profiles
at startup and checks for edits every 250 ms. A bad hot reload is shown in the
Shift+Esc diagnostics while the last valid version keeps running.

## Profile table

The script returns one table:

```lua
return {
  api_version = 1,
  id = "author.profile",          -- stable and unique
  name = "Profile name",
  author = "Author",
  version = "1.0.0",
  role = "hud",                  -- "hud" or composable "module"
  visibility = "telemetry",      -- "telemetry" or "audio"
  asset_root = "assets",         -- relative to profile.lua
  layout = { width = 640, height = 240, reference_height = 1440 },
  assets = { needle = "img/needle.png" },
  fonts = { digits = { path = "fonts/digits.ttf", size = 96 } },
  settings = {
    x = { type="number", label="Horizontal", default=.5, min=0, max=1, order=1 },
    enabled = { type="boolean", label="Enabled", default=true, order=2 },
    style = { type="enum", label="Style", default="A", options={"A","B"}, order=3 },
    tint = { type="color", label="Tint", default={1, 1, 1, 1}, order=4 },
  },
  render = function(ctx) end,
}
```

Profile coordinates are local to `layout.width` and `layout.height`. They are
scaled from `reference_height`; conventional `x`, `y`, and `scale` settings place
the canvas. A `hud` is selected as the main speedometer. Any number of `module`
profiles can be enabled alongside it.

Declare every image and font in the returned table and keep them inside the
profile directory. Missing files reject the profile.

## Render context

`render(ctx)` receives:

- `ctx.telemetry`: `available`, `fresh`, `race_on`, `speed_mps`, `speed_kph`,
  `speed_mph`, `rpm`, `max_rpm`, `idle_rpm`, `car_ordinal`, `gear`, `gear_label`,
  `throttle`, `brake`, `handbrake`, `steering`, `fuel`, `boost`, four
  `tire_temp_*` values, `lap_number`, `race_position`, `lateral_g`, and
  `longitudinal_g`.
- `ctx.audio`: `enabled`, `available`, `playing`, `title`, `artist`, `source`,
  `rms`, `peak`, and `bands`. `bands` contains 28 normalized logarithmic bands
  from roughly 45 Hz to 18 kHz.
- `ctx.settings`: persisted values from the profile's settings schema.
- `ctx.metric`, `ctx.opacity`, `ctx.time`, and `ctx.edit_mode`.
- `ctx.draw`: the drawing functions below.

Tire temperatures are the raw Fahrenheit values sent by Forza. `lap_number` is
the number of completed laps. Optional telemetry can be zero when unavailable.

## Drawing

Every drawing function takes one table. Colors accept `#rgb`, `#rgba`, `#rrggbb`,
or `#rrggbbaa`. `alpha` multiplies the color alpha and should usually include
`ctx.opacity`.

```lua
draw.rect     { x, y, w, h, color, alpha, rounding, glow_radius, glow_intensity, glow_color }
draw.gradient { x, y, w, h, color, color2, color3, direction, alpha, rounding, glow_radius, glow_intensity, glow_color }
draw.outline  { x, y, w, h, color, alpha, rounding, thickness, glow_radius, glow_intensity, glow_color }
draw.line     { x1, y1, x2, y2, color, alpha, thickness, glow_radius, glow_intensity, glow_color }
draw.circle   { cx, cy, radius, color, alpha, glow_radius, glow_intensity, glow_color }
draw.text     { font, text, x, y, size, align, color, alpha, shadow, glow_radius, glow_intensity, glow_color }
draw.image    { asset, x, y, w, h, color, alpha, uv_x1, uv_y1, uv_x2, uv_y2, rotation, pivot_x, pivot_y, glow_radius, glow_intensity, glow_color }
draw.set_offset { x, y }
```

`align` is `left`, `center`, or `right`; gradient direction is `horizontal` or
`vertical`. Image rotation is clockwise in degrees around normalized `pivot_x`
and `pivot_y`. Any command can include `clip_x`, `clip_y`, `clip_w`, and `clip_h`.
Add `space = "screen"` for viewport-relative placement; omit it for the normal
profile canvas.

Glow uses layered falloff for primitives and the DX11 bloom pass for images. Keep
its radius and intensity modest: each glowing command costs extra draw work.

## Sandbox

Profiles get base Lua 5.4 operations plus the table, string, math, and UTF-8
libraries. Filesystem, process, network, package loading, debug APIs, and dynamic
script loading are unavailable. Loading and rendering also have instruction
budgets, so an infinite loop cannot freeze the overlay.
