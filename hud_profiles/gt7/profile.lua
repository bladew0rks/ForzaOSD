local assets = {
  background = "img/tach/background.png",
  line = "img/tach/line.png",
  dashes = "img/tach/dashes.png",
  brake_gauge = "img/gauge_left.png",
  throttle_gauge = "img/gauge_right.png",
  brake_icon = "img/icons/brakes_icon.png",
  throttle_icon = "img/icons/throttle_icon.png",
  handbrake = "img/icons/handbrake-hq.png",
  turbo_background = "img/turbo_background.png",
  tyre_center = "img/tyre-center.png",
  tyre_outline = "img/tyre-outline.png",
}
for i = 0, 80 do
  assets["rev" .. i] = string.format("img/tach/rev/rev_back_%02d.png", i)
end

local function hex(r, g, b)
  return string.format("#%02x%02x%02x", math.floor(r * 255 + 0.5), math.floor(g * 255 + 0.5), math.floor(b * 255 + 0.5))
end

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function mix(a, b, amount)
  return a + (b - a) * clamp(amount, 0, 1)
end

local function temperature_color(temperature)
  if temperature <= 0 then
    return "#666666"
  end

  if temperature < 160 then
    local amount = clamp((temperature - 80) / 80, 0, 1)
    return hex(mix(0.25, 1, amount), mix(0.55, 1, amount), 1)
  end

  if temperature <= 230 then
    return "#ffffff"
  end

  local amount = clamp((temperature - 230) / 80, 0, 1)
  return hex(1, mix(1, 0.12, amount), mix(1, 0.12, amount))
end

local function draw_tires(draw, tm, alpha, edit_mode)
  local x, y = -205, 12
  local temperatures = {
    tm.tire_temp_front_left,
    tm.tire_temp_front_right,
    tm.tire_temp_rear_left,
    tm.tire_temp_rear_right,
  }

  if edit_mode and not tm.available then
    temperatures = { 178, 184, 194, 188 }
  end

  draw.image {
    asset = "tyre_center",
    x = x,
    y = y,
    w = 201,
    h = 217,
    color = "#d8d8d8",
    alpha = 0.75 * alpha,
  }

  local tyre_positions = {
    { x = x + 4, y = y + 7 },
    { x = x + 172, y = y + 7 },
    { x = x + 4, y = y + 117 },
    { x = x + 172, y = y + 117 },
  }

  for i = 1, 4 do
    draw.image {
      asset = "tyre_outline",
      x = tyre_positions[i].x,
      y = tyre_positions[i].y,
      w = 46,
      h = 94,
      color = temperature_color(temperatures[i]),
      alpha = 0.92 * alpha,
    }
  end
end

local boost_car = nil
local peak_positive_boost = 0

local function update_boost_state(tm)
  if not tm.available then
    return
  end

  if boost_car ~= tm.car_ordinal then
    boost_car = tm.car_ordinal
    peak_positive_boost = 0
  end

  peak_positive_boost = math.max(peak_positive_boost, tm.boost)
end

local function draw_arc(draw, cx, cy, radius, start_angle, end_angle, alpha, thickness)
  local segments = 40
  local previous_x = cx + math.cos(start_angle) * radius
  local previous_y = cy + math.sin(start_angle) * radius

  for i = 1, segments do
    local angle = start_angle + (end_angle - start_angle) * i / segments
    local x = cx + math.cos(angle) * radius
    local y = cy + math.sin(angle) * radius
    draw.line {
      x1 = previous_x,
      y1 = previous_y,
      x2 = x,
      y2 = y,
      color = "#ffffff",
      alpha = alpha,
      thickness = thickness,
    }
    previous_x, previous_y = x, y
  end
end

local function gauge_number(value)
  if math.abs(value - math.floor(value + 0.5)) < 0.01 then
    return string.format("%.0f", value)
  end
  return string.format("%.1f", value)
end

local function draw_boost(draw, tm, alpha, edit_mode)
  local x, y, w, h = 1210, -4, 171, 186
  local boost_psi = edit_mode and not tm.available and 14.5 or tm.boost
  local boost_bar = boost_psi * 0.0689476
  local peak_bar = peak_positive_boost * 0.0689476
  local maximum_bar = math.max(2, math.ceil(math.max(boost_bar, peak_bar)))

  draw.image {
    asset = "turbo_background",
    x = x,
    y = y,
    w = w,
    h = h,
    color = "#ffffff",
    alpha = alpha,
  }

  local cx, cy = x + w * 0.5, y + h * 0.5
  local radius = w * 75.75 / 228
  if boost_bar >= 0 then
    local end_angle = math.pi + math.pi * clamp(boost_bar / maximum_bar, 0, 1)
    draw_arc(draw, cx, cy, radius, math.pi, end_angle, alpha, 6.75)
  else
    local start_angle = math.pi - math.pi * 0.5 * clamp(-boost_bar, 0, 1)
    draw_arc(draw, cx, cy, radius, start_angle, math.pi, alpha, 6.75)
  end

  draw.text {
    font = "text",
    text = gauge_number(maximum_bar / 2),
    x = cx,
    y = y + 13,
    size = 18,
    align = "center",
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  }
  draw.text {
    font = "text",
    text = "0",
    x = x + 15,
    y = cy - 8,
    size = 18,
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  }
  draw.text {
    font = "text",
    text = "-1",
    x = cx,
    y = y + h - 21,
    size = 18,
    align = "center",
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  }
  draw.text {
    font = "text",
    text = gauge_number(maximum_bar),
    x = x + w - 10,
    y = cy - 8,
    size = 18,
    align = "right",
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  }
end

local function screen(command)
  command.space = "screen"
  return command
end

local function draw_race_info(draw, tm, alpha, edit_mode)
  if not edit_mode and tm.race_position <= 0 then
    return
  end

  local position = edit_mode and not tm.available and 7 or math.max(1, tm.race_position)
  local current_lap = edit_mode and not tm.available and 1 or tm.lap_number + 1
  local x, y = 70, 80

  draw.outline(screen {
    x = x + 2.5,
    y = y + 2.5,
    w = 147,
    h = 147,
    color = "#ffffff",
    alpha = 0.6 * alpha,
    thickness = 5,
  })
  draw.rect(screen {
    x = x,
    y = y,
    w = 152,
    h = 152,
    color = "#000000",
    alpha = 0.6 * alpha,
  })
  draw.text(screen {
    font = "text",
    text = tostring(position),
    x = x + 76,
    y = y + 76,
    size = 77,
    align = "center",
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  })
  draw.text(screen {
    font = "bold",
    text = "POSITION",
    x = x + 185,
    y = y + 35,
    size = 38,
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  })
  draw.text(screen {
    font = "bold",
    text = "LAP",
    x = x + 515,
    y = y + 35,
    size = 38,
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  })
  draw.text(screen {
    font = "text",
    text = tostring(current_lap),
    x = x + 515,
    y = y + 104,
    size = 60,
    color = "#ffffff",
    alpha = alpha,
    shadow = true,
  })
end

local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity
  local cx = 630
  -- Match the source GT7HUD's limiter compensation. Forza's current RPM
  -- bounces below EngineMaxRpm at the limiter, so an unbiased ratio never
  -- reaches the final rev frames on many cars.
  local limiter_bias = 1.05
  local rpm_ratio = clamp(tm.rpm * limiter_bias / math.max(1, tm.max_rpm), 0, 1)

  update_boost_state(tm)
  if settings.show_race_info then
    draw_race_info(draw, tm, alpha, ctx.edit_mode)
  end
  if settings.show_tires then
    draw_tires(draw, tm, alpha, ctx.edit_mode)
  end
  if settings.show_boost and (ctx.edit_mode or peak_positive_boost > 0.5) then
    draw_boost(draw, tm, alpha, ctx.edit_mode)
  end

  draw.image { asset = "background", x = 230, y = 20, w = 800, h = 196, color = "#ffffff", alpha = alpha }
  draw.image { asset = "line", x = 230, y = 20, w = 800, h = 61, color = "#ffffff", alpha = 0.15 * alpha }

  local progress = clamp((rpm_ratio - 0.8) / 0.2, 0, 1)
  local frame = math.floor(progress * 80 + 0.5)
  local blink = rpm_ratio >= 0.995 and math.floor(ctx.time * 10) % 2 == 1
  if not blink then
    draw.image {
      asset = "rev" .. frame, x = 230, y = 20, w = 800, h = 60,
      color = hex(1 - 0.2 * progress, 0.6 * progress, 0.9 * progress),
      alpha = alpha,
    }
  end

  draw.image { asset = "dashes", x = 230, y = 20, w = 800, h = 62, color = "#000000", alpha = 0.35 * alpha }
  draw.line { x1 = cx, y1 = 72, x2 = cx, y2 = 197, color = "#ffffff", alpha = alpha, thickness = 1 }

  draw.text {
    font = "digits", text = string.format("%.0f", ctx.metric and tm.speed_kph or tm.speed_mph),
    x = 480, y = 128, size = 68, align = "center",
    color = "#ffffff", alpha = alpha, shadow = true,
  }
  draw.text {
    font = "text", text = ctx.metric and "km/h" or "mph",
    x = 525, y = 178, size = 27, align = "center",
    color = "#ffffff", alpha = 0.82 * alpha, shadow = false,
  }
  draw.text {
    font = "digits", text = tm.gear_label,
    x = 714, y = 131, size = 116, align = "center",
    color = "#ffffff", alpha = alpha, shadow = true,
  }

  local steer = tm.steering
  draw.circle { cx = cx, cy = 11, radius = 3, color = "#ffffff", alpha = 0.3 * alpha }
  draw.circle {
    cx = cx + 390 * steer, cy = 11 + 25 * math.abs(steer) ^ 2, radius = 4,
    color = "#ff1a1a", alpha = 0.8 * alpha,
  }

  draw.image { asset = "brake_gauge", x = 102, y = 37, w = 62, h = 190, color = "#ffffff", alpha = alpha }
  draw.rect { x = 125, y = 48 + 168 * (1 - tm.brake), w = 24, h = 168 * tm.brake, color = "#ffffff", alpha = alpha, rounding = 2 }
  draw.image { asset = "brake_icon", x = 60, y = 112, w = 39, h = 39, color = "#ffffff", alpha = alpha }

  draw.image { asset = "throttle_gauge", x = 1096, y = 37, w = 62, h = 190, color = "#ffffff", alpha = alpha }
  draw.rect { x = 1111, y = 48 + 168 * (1 - tm.throttle), w = 24, h = 168 * tm.throttle, color = "#ffffff", alpha = alpha, rounding = 2 }
  draw.image { asset = "throttle_icon", x = 1160, y = 112, w = 39, h = 39, color = "#ffffff", alpha = alpha }

  if settings.show_extras then
    draw.text {
      font = "text", text = string.format("FUEL %.0f%%", 100 * math.max(0, math.min(1, tm.fuel))),
      x = 1350, y = 95, size = 22, align = "right",
      color = "#ffffff", alpha = alpha, shadow = false,
    }
    if tm.handbrake then
      draw.image { asset = "handbrake", x = 1165, y = 145, w = 40, h = 40, color = "#ff3333", alpha = alpha }
    end
  end
end

return {
  api_version = 1,
  id = "csp.gt7hud",
  name = "gt7",
  author = "Inori / ForzaOSD adapter",
  version = "1.3.0",
  asset_root = "assets",
  layout = { width = 1260, height = 240, reference_height = 2160 },
  assets = assets,
  fonts = {
    digits = { path = "fonts/arkitech_medium.ttf", size = 116 },
    text = { path = "fonts/gt7-MyFont Regular.ttf", size = 48 },
    bold = { path = "fonts/gt7-MyFont Bold.ttf", size = 48 },
  },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.5, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.944, min = 0.2, max = 1.1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.3, max = 2, order = 3 },
    show_extras = { type = "boolean", label = "Fuel and handbrake", default = true, order = 4 },
    show_tires = { type = "boolean", label = "Tire temperatures", default = true, order = 5 },
    show_boost = { type = "boolean", label = "Boost gauge", default = true, order = 6 },
    show_race_info = { type = "boolean", label = "Race position and lap", default = true, order = 7 },
  },
  render = render,
}
