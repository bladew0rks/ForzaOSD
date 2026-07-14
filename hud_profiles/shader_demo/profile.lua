local palettes = {
  Aqua = "#59f5e8",
  Amber = "#ffb84f",
  Violet = "#bd7cff",
}

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function render(ctx)
  local draw, settings = ctx.draw, ctx.settings
  local telemetry = ctx.telemetry
  local rpm_ratio = clamp(telemetry.rpm / math.max(1, telemetry.max_rpm), 0, 1)

  draw.shader {
    shader = "telemetry_grid",
    x = 0,
    y = 0,
    w = 520,
    h = 150,
    color = palettes[settings.palette] or palettes.Aqua,
    alpha = ctx.opacity,
    params = {
      settings.intensity,
      settings.speed,
      rpm_ratio,
      clamp(telemetry.throttle, 0, 1),
    },
  }

  draw.layer({
    shader = "glass_panel",
    x = 0,
    y = 175,
    w = 520,
    h = 110,
    margin = 14,
    sampler = "border",
    alpha = ctx.opacity,
    params = { rpm_ratio, clamp(telemetry.throttle, 0, 1) },
  }, function()
    draw.gradient {
      x = 0, y = 175, w = 520, h = 110,
      color = "#071719", color2 = "#020607", color3 = "#10262a",
      direction = "vertical", alpha = 0.90, rounding = 8,
    }
    draw.outline {
      x = 0, y = 175, w = 520, h = 110,
      color = palettes[settings.palette] or palettes.Aqua,
      alpha = 0.65, rounding = 8, thickness = 2,
    }
    draw.text {
      text = "EFFECT LAYER",
      x = 22, y = 205, size = 22,
      color = palettes[settings.palette] or palettes.Aqua,
      alpha = 0.85,
    }
    draw.text {
      text = string.format("%03.0f", ctx.metric and telemetry.speed_kph or telemetry.speed_mph),
      x = 492, y = 228, size = 52, align = "right",
      color = "#ffffff", alpha = 1,
    }
    draw.rect {
      x = 22, y = 250, w = 300, h = 8,
      color = "#173b3d", alpha = 0.8, rounding = 3,
    }
    draw.rect {
      x = 22, y = 250, w = 300 * rpm_ratio, h = 8,
      color = palettes[settings.palette] or palettes.Aqua,
      alpha = 1, rounding = 3,
    }
  end)
end

return {
  api_version = 1,
  id = "forzaosd.shader_demo",
  name = "Shader telemetry grid (demo)",
  author = "ForzaOSD",
  version = "1.0.0",
  role = "module",
  visibility = "telemetry",
  layout = { width = 520, height = 285, reference_height = 1080 },
  shaders = {
    telemetry_grid = "shaders/telemetry_grid.hlsl",
    glass_panel = "shaders/glass_panel.hlsl",
  },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.5, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.18, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.4, max = 2, order = 3 },
    intensity = { type = "number", label = "Intensity", default = 0.75, min = 0.1, max = 1.5, order = 4 },
    speed = { type = "number", label = "Animation speed", default = 1, min = 0, max = 4, order = 5 },
    palette = { type = "enum", label = "Palette", default = "Aqua", options = { "Aqua", "Amber", "Violet" }, order = 6 },
  },
  render = render,
}
