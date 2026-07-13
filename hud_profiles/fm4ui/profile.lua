local assets = {
  base = "img/background/background.png",
  rpm_16500 = "img/background/rpm_16500.png",
  rpm_14200 = "img/background/rpm_14200.png",
  rpm_12100 = "img/background/rpm_12100.png",
  rpm_10300 = "img/background/rpm_10300.png",
  rpm_9400 = "img/background/rpm_9400.png",
  rpm_8800 = "img/background/rpm_8800.png",
  rpm_8000 = "img/background/rpm_8000.png",
  rpm_6400 = "img/background/rpm_6400.png",
  rpm_6200 = "img/background/rpm_6200.png",
  rpm_4600 = "img/background/rpm_4600.png",
  rpm_3300 = "img/background/rpm_3300.png",
  gear_R = "img/gears/gear_R.png",
  gear_N = "img/gears/gear_N.png",
}

for i = 0, 269 do
  assets["rev_" .. i] = string.format("img/rev/rev_%03d.png", i)
end

for i = 0, 499 do
  assets["speed_" .. i] = string.format("img/speed/speed_%03d.png", i)
end

for i = 1, 9 do
  assets["gear_" .. i] = string.format("img/gears/gear_%d.png", i)
end

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

-- Each dial face has its own scale and animation rate.
local rpm_bands = {
  { minimum = 17000, face = "rpm_16500", frame_rate = 0.01285 },
  { minimum = 15000, face = "rpm_14200", frame_rate = 0.01485 },
  { minimum = 12000, face = "rpm_12100", frame_rate = 0.01500 },
  { minimum = 11000, face = "rpm_10300", frame_rate = 0.02235 },
  { minimum = 10000, face = "rpm_9400", frame_rate = 0.02240 },
  { minimum = 9000, face = "rpm_8800", frame_rate = 0.02705 },
  { minimum = 8000, face = "rpm_8000", frame_rate = 0.02690 },
  { minimum = 7000, face = "rpm_6400", frame_rate = 0.03390 },
  { minimum = 6000, face = "rpm_6200", frame_rate = 0.03875 },
  -- The upstream 0.39875 value is a decimal-place typo.
  { minimum = 5000, face = "rpm_4600", frame_rate = 0.039875 },
  { minimum = 3500, face = "rpm_3300", frame_rate = 0.05350 },
}

local fallback_gauge = {
  face = "rpm_8800",
  frame_rate = 0.02705,
}

local function select_gauge(maximum_rpm)
  for _, band in ipairs(rpm_bands) do
    if maximum_rpm >= band.minimum then
      return band
    end
  end
  return fallback_gauge
end

local function gear_asset(gear_label)
  if gear_label == "R" then
    return "gear_R"
  end
  if gear_label == "N" then
    return "gear_N"
  end

  local gear = tonumber(gear_label)
  if gear and gear >= 1 and gear <= 9 then
    return "gear_" .. gear
  end
  return nil
end

local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity

  local maximum_rpm = tm.max_rpm
  local current_rpm = tm.rpm
  local speed = ctx.metric and tm.speed_kph or tm.speed_mph
  local gear_label = tm.gear_label

  if ctx.edit_mode and not tm.available then
    maximum_rpm = 8000
    current_rpm = 6000
    speed = 114
    gear_label = "3"
  end

  local gauge = select_gauge(math.max(0, maximum_rpm))
  local rev_frame = clamp(math.floor(math.max(0, current_rpm) * gauge.frame_rate + 0.5), 0, 269)
  local speed_frame = clamp(math.floor(math.max(0, speed) + 0.5), 0, 499)

  draw.image {
    asset = gauge.face,
    x = 0,
    y = 0,
    w = 270,
    h = 270,
    color = "#ffffff",
    alpha = alpha,
  }
  draw.image {
    asset = "base",
    x = 0,
    y = 0,
    w = 270,
    h = 270,
    color = "#ffffff",
    alpha = alpha,
  }

  local selected_gear = gear_asset(gear_label)
  if selected_gear then
    draw.image {
      asset = selected_gear,
      x = 113.5,
      y = 73.5,
      w = 52,
      h = 68,
      color = "#ffffff",
      alpha = alpha,
    }
  else
    draw.text {
      font = "default",
      text = gear_label,
      x = 139.5,
      y = 107.5,
      size = 43,
      align = "center",
      color = "#ffffff",
      alpha = alpha,
      shadow = true,
    }
  end

  draw.image {
    asset = "rev_" .. rev_frame,
    x = 0,
    y = 0,
    w = 270,
    h = 270,
    color = "#ffffff",
    alpha = alpha,
  }
  draw.image {
    asset = "speed_" .. speed_frame,
    x = 0,
    y = 0,
    w = 270,
    h = 270,
    color = "#ffffff",
    alpha = alpha,
  }

  if settings.show_units then
    draw.text {
      font = "default",
      text = ctx.metric and "KM/H" or "MPH",
      x = 135,
      y = 213,
      size = 11,
      align = "center",
      color = "#ffffff",
      alpha = 0.72 * alpha,
      shadow = true,
    }
  end
end

return {
  api_version = 1,
  id = "assettocorsa.fm4ui",
  name = "fm4",
  author = "StoRMiX43 / ForzaOSD adapter",
  version = "1.0.0",
  asset_root = "assets",
  layout = { width = 270, height = 270, reference_height = 1080 },
  assets = assets,
  fonts = {},
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.9, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.82, min = 0.15, max = 1.05, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.4, max = 2, order = 3 },
    show_units = { type = "boolean", label = "Speed units", default = true, order = 4 },
  },
  render = render,
}
