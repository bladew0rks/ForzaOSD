local clamp = function(value, low, high)
  return math.max(low, math.min(high, value))
end

local corners = {
  { suffix = "front_left", label = "FL", x = 58, y = 78, shock_x = 146 },
  { suffix = "front_right", label = "FR", x = 404, y = 78, shock_x = 350 },
  { suffix = "rear_left", label = "RL", x = 58, y = 250, shock_x = 146 },
  { suffix = "rear_right", label = "RR", x = 404, y = 250, shock_x = 350 },
}

local smoothed_suspension = { 0.5, 0.5, 0.5, 0.5 }
local last_time = nil

local function temperature_color(temp_f)
  if temp_f < 140 then return "#3a7cff" end
  if temp_f < 175 then return "#32c5ff" end
  if temp_f <= 220 then return "#42e38b" end
  if temp_f <= 245 then return "#f6c445" end
  return "#ff445f"
end

local function slip_color(value)
  local magnitude = math.abs(value)
  if magnitude < 0.75 then return "#52e5a5" end
  if magnitude < 1 then return "#f6c445" end
  return "#ff445f"
end

local function telemetry_value(tm, prefix, suffix)
  return tm[prefix .. suffix] or 0
end

local function selected_slip(tm, mode, suffix)
  if mode == "Ratio" then
    return telemetry_value(tm, "tire_slip_ratio_", suffix)
  end
  if mode == "Angle" then
    return telemetry_value(tm, "tire_slip_angle_", suffix)
  end
  return telemetry_value(tm, "tire_combined_slip_", suffix)
end

local function draw_coilover(draw, x, y, compression, alpha)
  local top = y + 8
  local bottom = y + 90
  local center = x + 9
  local spring_top = top + 7 + compression * 14
  local spring_bottom = bottom - 7 - compression * 14
  local segments = 8

  draw.line {
    x1 = center, y1 = top, x2 = center, y2 = bottom,
    color = "#8290a3", alpha = 0.45 * alpha, thickness = 3,
  }
  draw.rect {
    x = x + 5, y = top + 5, w = 8, h = 29,
    color = "#c7d2df", alpha = 0.7 * alpha, rounding = 3,
  }
  draw.rect {
    x = x + 7, y = top + 28, w = 4, h = 45,
    color = "#f4f8fc", alpha = 0.88 * alpha, rounding = 2,
  }

  local previous_x = center
  local previous_y = spring_top
  for index = 1, segments do
    local ratio = index / segments
    local next_x = index == segments and center or center + (index % 2 == 0 and -9 or 9)
    local next_y = spring_top + (spring_bottom - spring_top) * ratio
    draw.line {
      x1 = previous_x, y1 = previous_y, x2 = next_x, y2 = next_y,
      color = "#f6c445", alpha = 0.9 * alpha, thickness = 2,
      glow_color = "#f6c445", glow_radius = 2, glow_intensity = 0.25,
    }
    previous_x, previous_y = next_x, next_y
  end

  local marker_y = bottom - 8 - compression * 66
  draw.line {
    x1 = x - 2, y1 = marker_y, x2 = x + 20, y2 = marker_y,
    color = "#ffffff", alpha = alpha, thickness = 2,
  }
  draw.circle { cx = center, cy = top, radius = 4, color = "#e8eef5", alpha = alpha }
  draw.circle { cx = center, cy = bottom, radius = 4, color = "#e8eef5", alpha = alpha }
end

local function draw_tire(draw, ctx, corner, compression, alpha)
  local tm, settings = ctx.telemetry, ctx.settings
  local temp_f = telemetry_value(tm, "tire_temp_", corner.suffix)
  local temp = ctx.metric and (temp_f - 32) * 5 / 9 or temp_f
  local temp_color = temperature_color(temp_f)
  local slip = selected_slip(tm, settings.slip_mode, corner.suffix)
  local warning_color = slip_color(slip)
  local warning = clamp((math.abs(slip) - 0.65) / 0.6, 0, 1)

  draw.rect {
    x = corner.x, y = corner.y, w = 58, h = 98,
    color = "#0b1017", alpha = 0.92 * alpha, rounding = 12,
  }
  draw.rect {
    x = corner.x + 5, y = corner.y + 5, w = 48, h = 88,
    color = temp_color, alpha = 0.5 * alpha, rounding = 9,
  }
  draw.outline {
    x = corner.x, y = corner.y, w = 58, h = 98,
    color = warning_color, alpha = (0.45 + warning * 0.55) * alpha,
    rounding = 12, thickness = 2 + warning * 2,
    glow_color = warning_color, glow_radius = 7,
    glow_intensity = warning * 0.9,
  }
  draw.text {
    font = "default", text = corner.label,
    x = corner.x + 29, y = corner.y + 25, size = 13, align = "center",
    color = "#e9f0f7", alpha = 0.72 * alpha,
  }
  draw.text {
    font = "default", text = string.format("%.0f°", temp),
    x = corner.x + 29, y = corner.y + 57, size = 20, align = "center",
    color = "#ffffff", alpha = alpha, shadow = true,
  }
  draw.text {
    font = "default", text = ctx.metric and "C" or "F",
    x = corner.x + 29, y = corner.y + 78, size = 11, align = "center",
    color = "#d7e0e9", alpha = 0.7 * alpha,
  }

  if settings.show_contacts then
    if tm["wheel_in_puddle_" .. corner.suffix] == true then
      draw.circle {
        cx = corner.x + 10, cy = corner.y + 88, radius = 5,
        color = "#2ca8ff", alpha = alpha,
        glow_color = "#2ca8ff", glow_radius = 4, glow_intensity = 0.7,
      }
    end
    if tm["wheel_on_rumble_strip_" .. corner.suffix] == true then
      draw.circle {
        cx = corner.x + 48, cy = corner.y + 88, radius = 5,
        color = "#ff9f32", alpha = alpha,
        glow_color = "#ff9f32", glow_radius = 4, glow_intensity = 0.7,
      }
    end
  end

  if settings.show_details then
    local travel = telemetry_value(tm, "suspension_travel_meters_", corner.suffix)
    local wheel_speed = telemetry_value(tm, "wheel_rotation_speed_", corner.suffix)
    local text_x = corner.x + 29
    local text_y = corner.y < 200 and corner.y + 111 or corner.y - 40
    draw.text {
      font = "default", text = string.format("S %.2f", slip),
      x = text_x, y = text_y, size = 10, align = "center",
      color = warning_color, alpha = alpha,
    }
    draw.text {
      font = "default", text = string.format("C %02.0f%%  %.0fmm", compression * 100, travel * 1000),
      x = text_x, y = text_y + 13, size = 9, align = "center",
      color = "#c6d1dc", alpha = 0.8 * alpha,
    }
    draw.text {
      font = "default", text = string.format("W %.0f rad/s", wheel_speed),
      x = text_x, y = text_y + 25, size = 9, align = "center",
      color = "#8f9dab", alpha = 0.75 * alpha,
    }
  end

  draw_coilover(draw, corner.shock_x, corner.y, compression, alpha)
end

local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity
  local now = ctx.time
  local dt = last_time and clamp(now - last_time, 0, 0.1) or 0
  last_time = now
  local response = 1 - math.exp(-dt / 0.08)

  draw.rect {
    x = 0, y = 0, w = 520, h = 420,
    color = "#070b10", alpha = 0.86 * alpha, rounding = 18,
  }
  draw.outline {
    x = 0, y = 0, w = 520, h = 420,
    color = "#ffffff", alpha = 0.12 * alpha, rounding = 18, thickness = 1,
  }
  draw.text {
    font = "default", text = "TIRE DYNAMICS",
    x = 24, y = 28, size = 15, align = "left",
    color = "#edf4fa", alpha = alpha,
  }
  draw.text {
    font = "default", text = string.upper(settings.slip_mode) .. " SLIP",
    x = 496, y = 28, size = 11, align = "right",
    color = "#8f9dab", alpha = 0.8 * alpha,
  }

  draw.rect { x = 210, y = 75, w = 100, h = 270, color = "#111923", alpha = 0.95 * alpha, rounding = 34 }
  draw.outline { x = 210, y = 75, w = 100, h = 270, color = "#d8e3ed", alpha = 0.2 * alpha, rounding = 34, thickness = 2 }
  draw.rect { x = 226, y = 103, w = 68, h = 78, color = "#223142", alpha = 0.65 * alpha, rounding = 24 }
  draw.rect { x = 226, y = 239, w = 68, h = 78, color = "#182432", alpha = 0.65 * alpha, rounding = 20 }
  draw.line { x1 = 260, y1 = 58, x2 = 260, y2 = 356, color = "#ffffff", alpha = 0.08 * alpha, thickness = 1 }

  for index, corner in ipairs(corners) do
    local target = tm.available and telemetry_value(tm, "normalized_suspension_travel_", corner.suffix) or 0.5
    target = clamp(target, 0, 1)
    if tm.available and tm.fresh then
      smoothed_suspension[index] = smoothed_suspension[index] + (target - smoothed_suspension[index]) * response
    end
    draw_tire(draw, ctx, corner, smoothed_suspension[index], alpha)
  end

  if settings.show_contacts then
    draw.circle { cx = 212, cy = 389, radius = 4, color = "#2ca8ff", alpha = alpha }
    draw.text { font = "default", text = "PUDDLE", x = 222, y = 389, size = 9, align = "left", color = "#8f9dab", alpha = 0.8 * alpha }
    draw.circle { cx = 304, cy = 389, radius = 4, color = "#ff9f32", alpha = alpha }
    draw.text { font = "default", text = "RUMBLE", x = 314, y = 389, size = 9, align = "left", color = "#8f9dab", alpha = 0.8 * alpha }
  end
end

return {
  api_version = 1,
  id = "forzaosd.tire_telemetry",
  name = "Tire telemetry",
  author = "ForzaOSD",
  version = "1.0.0",
  role = "module",
  visibility = "telemetry",
  layout = { width = 520, height = 420, reference_height = 1440 },
  assets = {},
  fonts = {},
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.16, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.72, min = 0.1, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.4, max = 2, order = 3 },
    slip_mode = { type = "enum", label = "Slip display", default = "Combined", options = { "Combined", "Ratio", "Angle" }, order = 4 },
    show_details = { type = "boolean", label = "Detailed values", default = false, order = 5 },
    show_contacts = { type = "boolean", label = "Surface contacts", default = true, order = 6 },
  },
  render = render,
}
