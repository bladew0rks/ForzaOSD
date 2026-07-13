local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity

  draw.rect {
    x = 0, y = 0, w = 520, h = 190,
    color = "#080d14", alpha = 0.88 * alpha, rounding = 18,
  }
  draw.outline {
    x = 0, y = 0, w = 520, h = 190,
    color = "#ffffff", alpha = 0.12 * alpha, rounding = 18, thickness = 1,
  }

  local rpm_ratio = math.max(0, math.min(1, tm.rpm / math.max(1, tm.max_rpm)))
  draw.rect { x = 28, y = 25, w = 464, h = 18, color = "#ffffff", alpha = 0.09 * alpha, rounding = 8 }
  local at_redline = rpm_ratio >= settings.redline
  draw.rect {
    x = 28, y = 25, w = 464 * rpm_ratio, h = 18,
    color = at_redline and "#ff334d" or "#1ac7ff",
    alpha = alpha, rounding = 8,
  }

  draw.text {
    font = "default", text = string.format("%.0f RPM", tm.rpm),
    x = 28, y = 58, size = 15, align = "left",
    color = "#b3c2d1", alpha = alpha, shadow = false,
  }

  local speed = ctx.metric and tm.speed_kph or tm.speed_mph
  draw.text {
    font = "default", text = string.format("%03.0f", math.max(0, speed)),
    x = 155, y = 118, size = 64, align = "center",
    color = "#f5faff", alpha = alpha, shadow = true,
  }
  draw.text {
    font = "default", text = ctx.metric and "KM/H" or "MPH",
    x = 155, y = 166, size = 14, align = "center",
    color = "#b3c2d1", alpha = alpha, shadow = false,
  }

  draw.text {
    font = "default", text = tm.gear_label,
    x = 300, y = 119, size = 78, align = "center",
    color = "#1ac7ff", alpha = alpha, shadow = true,
  }
  draw.text {
    font = "default", text = "GEAR",
    x = 300, y = 171, size = 12, align = "center",
    color = "#b3c2d1", alpha = alpha, shadow = false,
  }

  draw.text {
    font = "default", text = "THROTTLE",
    x = 390, y = 88, size = 12, align = "left",
    color = "#b3c2d1", alpha = alpha, shadow = false,
  }
  draw.rect { x = 390, y = 100, w = 95, h = 15, color = "#ffffff", alpha = 0.09 * alpha, rounding = 4 }
  draw.rect { x = 390, y = 100, w = 95 * tm.throttle, h = 15, color = "#1ac7ff", alpha = alpha, rounding = 4 }

  draw.text {
    font = "default", text = "BRAKE",
    x = 390, y = 132, size = 12, align = "left",
    color = "#b3c2d1", alpha = alpha, shadow = false,
  }
  draw.rect { x = 390, y = 144, w = 95, h = 15, color = "#ffffff", alpha = 0.09 * alpha, rounding = 4 }
  draw.rect { x = 390, y = 144, w = 95 * tm.brake, h = 15, color = "#ff4545", alpha = alpha, rounding = 4 }

  if tm.handbrake then
    draw.text {
      font = "default", text = "HANDBRAKE",
      x = 390, y = 174, size = 12, align = "left",
      color = "#ff4040", alpha = alpha, shadow = false,
    }
  end
end

return {
  api_version = 1,
  id = "native",
  name = "debug",
  author = "ForzaOSD",
  version = "0.5.0",
  layout = { width = 520, height = 190, reference_height = 1080 },
  fonts = {},
  assets = {},
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.5, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.82, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.5, max = 2, order = 3 },
    redline = { type = "number", label = "Redline", default = 0.9, min = 0.5, max = 1, order = 4 },
  },
  render = render,
}
