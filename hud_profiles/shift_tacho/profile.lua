local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity

  if settings.dynamic_movement then
    draw.set_offset {
      x = math.max(-75, math.min(75, tm.lateral_g * 18)),
      y = math.max(-100, math.min(100, -tm.longitudinal_g * 22)),
    }
  end

  local rpm_ratio = math.max(0, math.min(1, tm.rpm / math.max(1, tm.max_rpm)))
  local at_redline = rpm_ratio >= 0.85

  if at_redline then
    -- CSS equivalent of the outer 10 px red box-shadow.
    draw.rect { x = 54, y = 99, w = 102, h = 82, color = "#ff0000", alpha = 0.05 * alpha, rounding = 14 }
    draw.rect { x = 57, y = 102, w = 96, h = 76, color = "#ff0000", alpha = 0.1 * alpha, rounding = 11 }
    draw.rect { x = 60, y = 105, w = 90, h = 70, color = "#ff0808", alpha = 0.2 * alpha, rounding = 8 }
  end

  -- Original .GDT_inset-display CSS: a black 50% -> 25% -> 50%
  -- horizontal gradient, bright lower bevel, and dark inset shadow.
  draw.gradient {
    x = 65, y = 110, w = 80, h = 60,
    color = "#00000080", color2 = "#00000040", color3 = "#00000080",
    alpha = alpha, rounding = 5,
  }
  draw.outline { x = 65, y = 110, w = 80, h = 60, color = "#000000", alpha = 0.8 * alpha, rounding = 5, thickness = 1 }
  draw.outline { x = 67, y = 112, w = 76, h = 56, color = "#000000", alpha = 0.22 * alpha, rounding = 3, thickness = 3 }
  if at_redline then
    -- The original combines a deep red inset bloom with a thin amber-hot edge.
    draw.outline { x = 68, y = 113, w = 74, h = 54, color = "#ff0808", alpha = 0.42 * alpha, rounding = 2, thickness = 10 }
    draw.outline { x = 66, y = 111, w = 78, h = 58, color = "#ffd438", alpha = 0.9 * alpha, rounding = 4, thickness = 3 }
  else
    draw.line { x1 = 68, y1 = 172, x2 = 142, y2 = 172, color = "#ffffff", alpha = alpha, thickness = 1 }
  end

  draw.text {
    font = "digits", text = tm.gear_label,
    x = 105, y = 140, size = 40, align = "center",
    color = "#ffffff", alpha = alpha, shadow = true,
  }
  draw.text {
    font = "digits", text = string.format("%.0f", ctx.metric and tm.speed_kph or tm.speed_mph),
    x = 258, y = 60, size = 86, align = "center",
    color = "#ffffff", alpha = alpha, shadow = true,
  }
  draw.text {
    font = "labels", text = ctx.metric and "KM/H" or "MPH",
    x = 324, y = 46, size = 18, align = "left",
    color = "#ffffff", alpha = alpha, shadow = false,
  }

  if settings.show_lights then
    for i = 0, 6 do
      local active = rpm_ratio >= 0.65 + i * 0.05
      local glow = "#1a1a1a"
      if active then
        if i < 2 then
          glow = "#00ff00"
        elseif i < 4 then
          glow = "#ffff00"
        else
          glow = "#ff4500"
        end
      end
      local cx = 174 + i * 20
      if active then
        -- Layered falloff recreates the 10 px CSS bloom without a blur shader.
        draw.circle { cx = cx, cy = 127, radius = 17, color = glow, alpha = 0.04 * alpha }
        draw.circle { cx = cx, cy = 127, radius = 14, color = glow, alpha = 0.09 * alpha }
        draw.circle { cx = cx, cy = 127, radius = 11, color = glow, alpha = 0.2 * alpha }
        draw.circle { cx = cx, cy = 127, radius = 8, color = glow, alpha = alpha }
        draw.circle { cx = cx, cy = 127, radius = 5, color = "#ffff66", alpha = 0.9 * alpha }
      else
        draw.circle { cx = cx, cy = 128, radius = 8, color = "#ffffff", alpha = 0.22 * alpha }
        draw.circle { cx = cx, cy = 126, radius = 8, color = "#000000", alpha = 0.5 * alpha }
      end
      draw.image {
        asset = "revlight", x = 167 + i * 20, y = 120, w = 14, h = 14,
        color = "#ffffff", alpha = (active and 1 or 0.25) * alpha,
      }
    end
  end

  local indicators = {
    { asset = "ebrake", active = tm.handbrake },
    { asset = "abs", active = false },
    { asset = "traction", active = false },
  }
  for i, indicator in ipairs(indicators) do
    local cx = 185 + (i - 1) * 50
    draw.circle { cx = cx, cy = 174, radius = 20, color = "#000000", alpha = 0.5 * alpha }
    draw.gradient {
      x = cx - 20, y = 151, w = 40, h = 40,
      color = "#141414", color2 = "#0a0a0a", color3 = "#000000",
      alpha = alpha, rounding = 20, direction = "vertical",
    }
    draw.image {
      asset = indicator.asset, x = cx - 16, y = 155, w = 32, h = 32,
      color = indicator.active and "#ff2626" or "#ffffff",
      alpha = indicator.active and 1 or 0.5,
    }
  end
end

return {
  api_version = 1,
  id = "beamng.ghostsdigitaltacho",
  name = "nfs shift",
  author = "GhostInTheLeague / ForzaOSD adapter",
  version = "0.2",
  asset_root = "assets",
  layout = { width = 440, height = 280, reference_height = 1080 },
  assets = {
    revlight = "img/revlight.png",
    ebrake = "img/ebrake.png",
    abs = "img/abs.png",
    traction = "img/traction.png",
  },
  fonts = {
    digits = { path = "font/RobotoMono-Medium.ttf", size = 90 },
    labels = { path = "font/BarlowSemiCondensed-Bold.ttf", size = 34 },
  },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.83, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.78, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.3, max = 2, order = 3 },
    show_lights = { type = "boolean", label = "RPM shift lights", default = true, order = 4 },
    dynamic_movement = { type = "boolean", label = "Dynamic G-force movement", default = false, order = 5 },
  },
  render = render,
}
