local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local palettes = {
  Aqua = {
    primary = "#8ffff0",
    accent = "#d2fff9",
    dim = "#183b38",
    hot = "#ff584d",
  },
  Amber = {
    primary = "#ffc15a",
    accent = "#ffe5ad",
    dim = "#493719",
    hot = "#ff4b3f",
  },
  Green = {
    primary = "#78ff91",
    accent = "#d4ffdc",
    dim = "#193f21",
    hot = "#ff5548",
  },
  Ice = {
    primary = "#b9e8ff",
    accent = "#f3fbff",
    dim = "#203744",
    hot = "#ff6170",
  },
}

local function phosphor_text(draw, options, color, alpha, bloom)
  options.color = color
  options.alpha = alpha
  options.glow_radius = 9
  options.glow_intensity = bloom
  options.glow_color = color
  draw.text(options)
end

local function phosphor_rect(draw, options, color, alpha, bloom)
  options.color = color
  options.alpha = alpha
  options.glow_radius = 5
  options.glow_intensity = bloom
  options.glow_color = color
  draw.rect(options)
end

local function draw_panel(draw, x, y, width, height, palette, alpha, backplate)
  if backplate > 0 then
    draw.gradient {
      x = x,
      y = y,
      w = width,
      h = height,
      color = "#081411",
      color2 = "#010303",
      color3 = "#07100e",
      direction = "vertical",
      alpha = backplate * alpha,
      rounding = 8,
    }
  end

  draw.outline {
    x = x,
    y = y,
    w = width,
    h = height,
    color = palette.primary,
    alpha = 0.20 * alpha,
    rounding = 8,
    thickness = 1.2,
  }
end

local function draw_cell_bar(draw, x, y, width, height, count, value, color, dim, alpha, bloom)
  local gap = 3
  local cell_width = (width - gap * (count - 1)) / count
  local lit = math.floor(clamp(value, 0, 1) * count + 0.5)

  for index = 1, count do
    local cell_x = x + (index - 1) * (cell_width + gap)
    draw.rect {
      x = cell_x,
      y = y,
      w = cell_width,
      h = height,
      color = dim,
      alpha = 0.42 * alpha,
      rounding = 1,
    }

    if index <= lit then
      phosphor_rect(draw, {
        x = cell_x,
        y = y,
        w = cell_width,
        h = height,
        rounding = 1,
      }, color, alpha, 0.58 * bloom)
    end
  end
end

local function engine_max_rpm(telemetry)
  if telemetry.available and telemetry.max_rpm >= 1000 then
    return clamp(telemetry.max_rpm, 1000, 30000)
  end
  return 8500
end

local boost_car = nil
local peak_positive_boost = 0

local function update_boost_state(telemetry)
  if not telemetry.available then
    return
  end

  if boost_car ~= telemetry.car_ordinal then
    boost_car = telemetry.car_ordinal
    peak_positive_boost = 0
  end

  peak_positive_boost = math.max(peak_positive_boost, telemetry.boost)
end

local function draw_rpm(draw, telemetry, palette, alpha, bloom, backplate, time)
  local maximum_rpm = engine_max_rpm(telemetry)
  local rpm_ratio = clamp(telemetry.rpm / maximum_rpm, 0, 1)
  local redline_ratio = 0.90
  local shift = rpm_ratio >= redline_ratio
  local flash = shift and math.floor(time * 12) % 2 == 0

  local x = 170
  local y = 37
  local width = 930
  local count = 64
  local gap = 2
  local cell_width = (width - gap * (count - 1)) / count
  local lit = math.floor(rpm_ratio * count + 0.5)
  local redline_cell = math.floor(redline_ratio * count + 0.5)

  draw_panel(draw, 0, 0, 1120, 82, palette, alpha, backplate)
  draw.text {
    font = "alpha",
    text = "RPM x1000",
    x = 18,
    y = 20,
    size = 14,
    color = palette.primary,
    alpha = 0.68 * alpha,
  }
  draw.text {
    font = "alpha",
    text = string.format("MAX %d", math.floor(maximum_rpm + 0.5)),
    x = 18,
    y = 50,
    size = 12,
    color = palette.primary,
    alpha = 0.42 * alpha,
  }

  local tick_step = maximum_rpm <= 12000 and 1000 or (maximum_rpm <= 20000 and 2000 or 5000)
  local last_tick = math.floor(maximum_rpm / tick_step) * tick_step
  for value = 0, last_tick, tick_step do
    local ratio = value / maximum_rpm
    draw.text {
      font = "digits",
      text = string.format("%.0f", value / 1000),
      x = x + width * ratio,
      y = 17,
      size = 15,
      align = value == 0 and "left" or "center",
      color = palette.primary,
      alpha = 0.62 * alpha,
    }
  end

  if maximum_rpm - last_tick > tick_step * 0.2 then
    draw.text {
      font = "digits",
      text = string.format("%.1f", maximum_rpm / 1000),
      x = x + width,
      y = 17,
      size = 15,
      align = "right",
      color = palette.primary,
      alpha = 0.62 * alpha,
    }
  end

  for index = 1, count do
    local cell_x = x + (index - 1) * (cell_width + gap)
    local is_hot = index >= redline_cell
    local active_color = is_hot and palette.hot or palette.primary

    draw.rect {
      x = cell_x,
      y = y,
      w = cell_width,
      h = 24,
      color = is_hot and "#4b1917" or palette.dim,
      alpha = 0.48 * alpha,
      rounding = 0.7,
    }

    if index <= lit or (flash and is_hot) then
      phosphor_rect(draw, {
        x = cell_x,
        y = y,
        w = cell_width,
        h = 24,
        rounding = 0.7,
      }, active_color, alpha, bloom)
    end
  end

  draw.line {
    x1 = x,
    y1 = 68,
    x2 = x + width,
    y2 = 68,
    color = palette.primary,
    alpha = 0.20 * alpha,
    thickness = 1,
  }

  return shift
end

local function draw_gear(draw, telemetry, palette, alpha, bloom, backplate, shift)
  draw_panel(draw, 0, 94, 170, 164, palette, alpha, backplate)
  draw.text {
    font = "alpha",
    text = "GEAR",
    x = 85,
    y = 111,
    size = 13,
    align = "center",
    color = palette.primary,
    alpha = 0.58 * alpha,
  }
  draw.text {
    font = "alpha",
    text = "8",
    x = 85,
    y = 174,
    size = 92,
    align = "center",
    color = palette.dim,
    alpha = 0.17 * alpha,
  }

  local gear_color = shift and palette.hot or palette.accent
  phosphor_text(draw, {
    font = "alpha",
    text = telemetry.gear_label,
    x = 85,
    y = 174,
    size = 92,
    align = "center",
  }, gear_color, alpha, shift and 1.45 * bloom or bloom)

  local state = "DRIVE"
  if telemetry.handbrake then
    state = "PARK"
  elseif telemetry.gear_label == "R" then
    state = "REVERSE"
  elseif telemetry.gear_label == "N" then
    state = "NEUTRAL"
  end
  phosphor_text(draw, {
    font = "alpha",
    text = state,
    x = 85,
    y = 240,
    size = 12,
    align = "center",
  }, telemetry.handbrake and palette.hot or palette.primary, 0.58 * alpha, 0.35 * bloom)
end

local function draw_speed(draw, telemetry, settings, palette, alpha, bloom, backplate, metric)
  local speed = metric and telemetry.speed_kph or telemetry.speed_mph
  speed = math.floor(clamp(speed, 0, 999) + 0.5)
  local speed_text = settings.leading_zeroes and string.format("%03d", speed) or tostring(speed)

  draw_panel(draw, 184, 94, 440, 164, palette, alpha, backplate)
  draw.text {
    font = "alpha",
    text = "VELOCITY",
    x = 204,
    y = 111,
    size = 13,
    color = palette.primary,
    alpha = 0.58 * alpha,
  }
  draw.text {
    font = "digits",
    text = "888",
    x = 520,
    y = 171,
    size = 98,
    align = "right",
    color = palette.dim,
    alpha = 0.14 * alpha,
  }
  phosphor_text(draw, {
    font = "digits",
    text = speed_text,
    x = 520,
    y = 171,
    size = 98,
    align = "right",
  }, palette.accent, alpha, 1.10 * bloom)
  phosphor_text(draw, {
    font = "alpha",
    text = metric and "KM/H" or "MPH",
    x = 542,
    y = 187,
    size = 17,
  }, palette.primary, 0.82 * alpha, 0.48 * bloom)

  if not settings.show_pedals then
    return
  end

  draw.text {
    font = "alpha",
    text = "THR",
    x = 204,
    y = 238,
    size = 11,
    color = palette.primary,
    alpha = 0.52 * alpha,
  }
  draw_cell_bar(draw, 244, 231, 150, 10, 10, telemetry.throttle, palette.primary, palette.dim, alpha, bloom)

  draw.text {
    font = "alpha",
    text = "BRK",
    x = 408,
    y = 238,
    size = 11,
    color = palette.primary,
    alpha = 0.52 * alpha,
  }
  draw_cell_bar(draw, 450, 231, 150, 10, 10, telemetry.brake, palette.hot, palette.dim, alpha, bloom)
end

local function draw_engine(draw, telemetry, settings, palette, alpha, bloom, backplate, edit_mode)
  local rpm = math.floor(math.max(0, telemetry.rpm) + 0.5)

  draw_panel(draw, 638, 94, 482, 164, palette, alpha, backplate)
  draw.text {
    font = "alpha",
    text = "ENGINE RPM",
    x = 658,
    y = 111,
    size = 13,
    color = palette.primary,
    alpha = 0.58 * alpha,
  }
  draw.text {
    font = "digits",
    text = "88888",
    x = 1098,
    y = 145,
    size = 43,
    align = "right",
    color = palette.dim,
    alpha = 0.14 * alpha,
  }
  phosphor_text(draw, {
    font = "digits",
    text = string.format("%05d", math.min(99999, rpm)),
    x = 1098,
    y = 145,
    size = 43,
    align = "right",
  }, palette.primary, alpha, bloom)

  if settings.show_fuel then
    local fuel = clamp(telemetry.fuel, 0, 1)
    if edit_mode and not telemetry.available then
      fuel = 0.68
    end
    draw.text {
      font = "alpha",
      text = string.format("FUEL %02d%%", math.floor(fuel * 100 + 0.5)),
      x = 658,
      y = 184,
      size = 12,
      color = palette.primary,
      alpha = 0.58 * alpha,
    }
    draw_cell_bar(draw, 768, 177, 330, 11, 16, fuel, palette.primary, palette.dim, alpha, bloom)
  end

  local status_text
  if settings.show_boost and (peak_positive_boost > 0.5 or edit_mode and not telemetry.available) then
    local boost = edit_mode and not telemetry.available and 8.7 or telemetry.boost
    status_text = string.format("BOOST %+04.1f BAR", boost * 0.0689476)
  else
    status_text = string.format("LATERAL G %+04.2f", telemetry.lateral_g)
  end

  phosphor_text(draw, {
    font = "alpha",
    text = status_text,
    x = 658,
    y = 239,
    size = 13,
  }, palette.primary, 0.72 * alpha, 0.36 * bloom)

  if telemetry.race_position > 0 then
    phosphor_text(draw, {
      font = "alpha",
      text = string.format("P %02d  L %02d", telemetry.race_position, telemetry.lap_number + 1),
      x = 1098,
      y = 239,
      size = 13,
      align = "right",
    }, palette.primary, 0.72 * alpha, 0.36 * bloom)
  end
end

local function render(ctx)
  local draw = ctx.draw
  local telemetry = ctx.telemetry
  local settings = ctx.settings
  local palette = palettes[settings.palette] or palettes.Aqua
  local alpha = clamp(ctx.opacity * settings.brightness, 0, 1)
  local bloom = settings.bloom
  local backplate = clamp(settings.backplate, 0, 0.85)

  update_boost_state(telemetry)
  local shift = draw_rpm(draw, telemetry, palette, alpha, bloom, backplate, ctx.time)
  draw_gear(draw, telemetry, palette, alpha, bloom, backplate, shift)
  draw_speed(draw, telemetry, settings, palette, alpha, bloom, backplate, ctx.metric)
  draw_engine(draw, telemetry, settings, palette, alpha, bloom, backplate, ctx.edit_mode)
end

return {
  api_version = 1,
  id = "forzaosd.vfd",
  name = "VFD",
  author = "ForzaOSD",
  version = "1.1.0",
  asset_root = "assets",
  layout = { width = 1120, height = 258, reference_height = 1440 },
  assets = {},
  fonts = {
    digits = { path = "fonts/DSEG7Modern-Bold.ttf", size = 104 },
    alpha = { path = "fonts/DSEG14Modern-Regular.ttf", size = 96 },
  },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.5, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.86, min = 0.15, max = 1.05, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.4, max = 2, order = 3 },
    palette = { type = "enum", label = "Phosphor", default = "Aqua", options = { "Aqua", "Amber", "Green", "Ice" }, order = 4 },
    brightness = { type = "number", label = "Brightness", default = 1, min = 0.3, max = 1.5, order = 5 },
    bloom = { type = "number", label = "Bloom", default = 1, min = 0, max = 2.5, order = 6 },
    backplate = { type = "number", label = "Backplate", default = 0.42, min = 0, max = 0.85, order = 7 },
    leading_zeroes = { type = "boolean", label = "Leading speed zeroes", default = false, order = 8 },
    show_pedals = { type = "boolean", label = "Pedal cells", default = true, order = 9 },
    show_fuel = { type = "boolean", label = "Fuel cells", default = true, order = 10 },
    show_boost = { type = "boolean", label = "Boost readout", default = true, order = 11 },
  },
  render = render,
}
