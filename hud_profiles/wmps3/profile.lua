local rpm_faces = {
  3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500,
  9000, 9500, 10000, 10500, 11000, 11500, 12000, 13000, 14000, 15000,
  16000, 18000,
}

local rpm_thresholds = {
  3949, 4099, 4949, 5099, 5949, 6099, 6949, 7099, 7949, 8099, 8949,
  9099, 9949, 10099, 10949, 11099, 11949, 12099, 13099, 14099, 15099,
  16099, 18099,
}

local assets = {
  tach_background = "img/tachometer/tachometer_background.png",
  tach_carbon = "img/tachometer/tachometer_carbon.png",
  tach_needle = "img/tachometer/tachometer_needle.png",
  tach_dot = "img/tachometer/tachometer_dot.png",
  dash_background = "img/digital_dash/digital_dash_background.png",
  dash_gears = "img/digital_dash/digital_gears.png",
  dash_speed = "img/digital_dash/digital_speed_numbers.png",
  dash_uom = "img/digital_dash/speed_uom.png",
  dash_handbrake = "img/digital_dash/handbrake.png",
  boost_background = "img/turbo_gauge/turbo_gauge_background.png",
  boost_carbon = "img/turbo_gauge/turbo_gauge_carbon.png",
  boost_unit = "img/turbo_gauge/turbo_gauge_unit_x100kpa.png",
  boost_needle = "img/turbo_gauge/turbo_gauge_needle.png",
  boost_dot = "img/turbo_gauge/turbo_gauge_dot.png",
}
for _, rpm in ipairs(rpm_faces) do
  assets["rpm_" .. rpm] = "img/tachometer_rpm/rpm_" .. rpm .. ".png"
end

local observed_car = nil
local positive_boost_observed = false

local function rpm_state(max_rpm)
  local state = 1
  for i = 2, #rpm_thresholds do
    if rpm_thresholds[i] <= max_rpm then
      state = i
    else
      break
    end
  end
  return state
end

local function draw_tachometer(draw, tm, alpha, background_opacity)
  local state = rpm_state(tm.max_rpm)
  local red_rpm = state <= 17 and 12000 or (state <= 21 and 16000 or 20000)
  local angle = math.max(-144, math.min(143.5, tm.rpm / red_rpm * 287.5 - 144))
  local x, y, size = 163, 136, 235

  draw.image {
    asset = "tach_background", x = x, y = y, w = size, h = size,
    color = "#ffffff", alpha = alpha * background_opacity,
  }
  draw.image { asset = "tach_carbon", x = x, y = y, w = size, h = size, color = "#ffffff", alpha = alpha }
  draw.image { asset = "rpm_" .. rpm_faces[state], x = x, y = y, w = size, h = size, color = "#ffffff", alpha = alpha }
  draw.image {
    asset = "tach_needle", x = x, y = y, w = size, h = size,
    color = "#ffffff", alpha = alpha, rotation = angle,
  }
  draw.image { asset = "tach_dot", x = x, y = y, w = size, h = size, color = "#ffffff", alpha = alpha }
end

local function draw_speed_digits(draw, speed, alpha)
  local text = tostring(math.max(0, math.min(999, math.floor(speed + 0.5))))
  local count = string.len(text)
  local width, height = 147.29 * 0.5, 172.70 * 0.5
  local start_x = 563

  for i = 1, count do
    local digit = tonumber(string.sub(text, i, i))
    local source_x = start_x - (count - i) * 147.29 / 1.22
    draw.image {
      asset = "dash_speed",
      x = 2 + source_x * 0.5, y = 353.5, w = width, h = height,
      uv_x1 = digit / 10, uv_x2 = (digit + 1) / 10,
      color = "#ffffff", alpha = alpha,
    }
  end
end

local function draw_digital_dash(draw, tm, metric, alpha)
  local x, y = 2, 355
  draw.image {
    asset = "dash_background", x = x, y = y, w = 432, h = 123.5,
    color = "#ffffff", alpha = alpha,
  }

  local gear_index
  if tm.gear_label == "R" then
    gear_index = 0
  elseif tm.gear_label == "N" then
    gear_index = 1
  else
    gear_index = math.min(10, (tonumber(tm.gear_label) or 1) + 1)
  end
  draw.image {
    asset = "dash_gears", x = x + 21, y = y - 2.5, w = 71.635, h = 83.465,
    uv_x1 = gear_index / 11, uv_x2 = (gear_index + 1) / 11,
    color = "#ffffff", alpha = alpha,
  }

  draw_speed_digits(draw, metric and tm.speed_kph or tm.speed_mph, alpha)
  local unit_y = metric and 0 or 0.5
  draw.image {
    asset = "dash_uom", x = x + 342.5, y = y + 51.5, w = 89.5, h = 33.5,
    uv_y1 = unit_y, uv_y2 = unit_y + 0.5,
    color = "#ffffff", alpha = alpha,
  }

  if tm.handbrake then
    draw.image {
      asset = "dash_handbrake", x = x + 93.5, y = y + 5.25, w = 68.5, h = 53.5,
      color = "#ffffff", alpha = alpha,
    }
  end
end

local function draw_boost_gauge(draw, boost_psi, alpha, background_opacity)
  local x, y, size = 292, 8, 158
  local boost_bar = math.max(0, math.min(3, boost_psi / 14.5037738))
  local angle = boost_bar * 60 - 90

  draw.image {
    asset = "boost_background", x = x, y = y, w = size, h = size,
    color = "#ffffff", alpha = alpha * background_opacity,
  }
  draw.image { asset = "boost_carbon", x = x, y = y, w = size, h = size, color = "#ffffff", alpha = alpha }
  draw.image { asset = "boost_unit", x = x, y = y, w = size, h = size, color = "#ffffff", alpha = alpha }
  draw.image {
    asset = "boost_needle", x = x, y = y, w = size, h = size,
    color = "#ffffff", alpha = alpha, rotation = angle,
  }
  draw.image { asset = "boost_dot", x = x, y = y, w = size, h = size, color = "#ffffff", alpha = alpha }
end

local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity
  local background_opacity = tonumber(settings.background_opacity) or 1
  local boost_background_opacity = tonumber(settings.boost_background_opacity) or 1

  if observed_car ~= tm.car_ordinal then
    observed_car = tm.car_ordinal
    positive_boost_observed = false
  end
  if tm.boost > 0.5 then
    positive_boost_observed = true
  end

  draw_tachometer(draw, tm, alpha, background_opacity)
  draw_digital_dash(draw, tm, ctx.metric, alpha)

  local boost_mode = settings.boost_gauge or "Auto"
  local show_boost = boost_mode == "Always"
    or (boost_mode == "Auto" and (positive_boost_observed or ctx.edit_mode))
  if show_boost and boost_mode ~= "Never" then
    draw_boost_gauge(draw, tm.boost, alpha, boost_background_opacity)
  end
end

return {
  api_version = 1,
  id = "assettocorsa.wmps3hud",
  name = "wmps3",
  author = "StoRMiX43 / ForzaOSD adapter",
  version = "1.0.2",
  asset_root = "assets",
  layout = { width = 450, height = 480, reference_height = 1080 },
  assets = assets,
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.846, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.734, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.3, max = 2, order = 3 },
    background_opacity = { type = "number", label = "Tachometer background opacity", default = 1, min = 0, max = 1, order = 4 },
    boost_gauge = { type = "enum", label = "Boost gauge", default = "Auto", options = { "Auto", "Always", "Never" }, order = 5 },
    boost_background_opacity = { type = "number", label = "Boost background opacity", default = 1, min = 0, max = 1, order = 6 },
  },
  render = render,
}
