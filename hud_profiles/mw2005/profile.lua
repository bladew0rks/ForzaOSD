local assets = {
  boost_gauge = "img/general/BOOST_LINE_GAUGE.png",
  boost_needle = "img/general/LINE_BOOST.png",
  nos_time = "img/general/NOS_TIME.png",
  shift_flash = "img/general/GEAR_CHANGE.png",
}

local styles = {
  Default = {
    background = "default_background",
    gear_speed = "default_gear_speed",
    needle = "default_needle",
    rpm_line = "default_rpm_line",
  },
  Sonic = {
    background = "sonic_background",
    gear_speed = "sonic_gear_speed",
    needle = "sonic_needle",
    rpm_line = "sonic_rpm_line",
  },
}

for name, folder in pairs({ default = "default", sonic = "sonic" }) do
  assets[name .. "_background"] = "img/styles/" .. folder .. "/BCKGND.png"
  assets[name .. "_gear_speed"] = "img/styles/" .. folder .. "/GEAR-SPEED.png"
  assets[name .. "_needle"] = "img/styles/" .. folder .. "/LINE.png"
  assets[name .. "_rpm_line"] = "img/styles/" .. folder .. "/RPM_LINE.png"
end

for rpm = 2, 9 do
  assets["redline_" .. rpm] = "img/redline/REDLINE_" .. rpm .. "k.png"
end

local colors = {
  ["MW Amber"] = "#ffb070",
  White = "#ffffff",
  ["Dark Gray"] = "#333333",
  Red = "#e02424",
  Pink = "#ff69b5",
  ["Sky Blue"] = "#87cfee",
  Lime = "#80f514",
  Cyan = "#00ffff",
  ["Electric Blue"] = "#0080ff",
  Purple = "#9933cc",
}

local color_options = {
  "MW Amber", "White", "Dark Gray", "Red", "Pink",
  "Sky Blue", "Lime", "Cyan", "Electric Blue", "Purple",
}

local observed_car = nil
local positive_boost_observed = false

local function tint(name)
  return colors[name] or colors["MW Amber"]
end

local function draw_full_image(draw, asset, color, alpha, rotation)
  draw.image {
    asset = asset,
    x = 0, y = 0, w = 390, h = 390,
    color = color or "#ffffff",
    alpha = alpha,
    rotation = rotation or 0,
  }
end

local function draw_readout(draw, tm, metric, alpha)
  local gear = tm.gear_label
  if gear == nil or gear == "" then
    gear = "N"
  end

  draw.text {
    font = "digits", text = "8",
    x = 220, y = 129, size = 60, align = "right",
    color = "#000000", alpha = alpha * 0.4,
  }
  draw.text {
    font = "digits", text = tostring(gear),
    x = 220, y = 129, size = 60, align = "right",
    color = "#000000", alpha = alpha,
  }

  local speed = metric and tm.speed_kph or tm.speed_mph
  speed = math.max(0, math.min(999, math.floor(speed)))
  local hundreds = speed >= 100 and tostring(math.floor(speed / 100)) or ""
  local tens = speed >= 10 and tostring(math.floor(speed / 10) % 10) or ""
  local units = tostring(speed % 10)
  local digits = { hundreds, tens, units }
  local anchors = { 177, 215, 252 }

  for i = 1, 3 do
    draw.text {
      font = "digits", text = "8",
      x = anchors[i], y = 235, size = 100, align = "right",
      color = "#000000", alpha = alpha * 0.4,
    }
    if digits[i] ~= "" then
      draw.text {
        font = "digits", text = digits[i],
        x = anchors[i], y = 235, size = 100, align = "right",
        color = "#000000", alpha = alpha,
      }
    end
  end

  local unit = metric and "km/h" or "MPH"
  local start_x = 195 - #unit * 9 + 9
  for i = 1, #unit do
    draw.text {
      font = "labels", text = string.sub(unit, i, i),
      x = start_x + (i - 1) * 18, y = 310, size = 20, align = "center",
      color = "#ffffff", alpha = alpha,
    }
  end
end

local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity
  local style = styles[settings.style] or styles.Default
  local rpm_color = tint(settings.rpm_color)
  local needle_color = tint(settings.needle_color)
  local lcd_color = tint(settings.lcd_color)

  local max_rpm = tm.max_rpm
  if max_rpm == nil or max_rpm <= 0 then
    max_rpm = 8000
  end
  local rpm = math.max(0, tm.rpm or 0)

  local needle_angle = math.min(rpm / 10000, 1.1) * 225

  local redline = math.max(2, math.min(9, math.floor(max_rpm / 1000)))

  if observed_car ~= tm.car_ordinal then
    observed_car = tm.car_ordinal
    positive_boost_observed = false
  end
  if (tm.boost or 0) > 0.5 then
    positive_boost_observed = true
  end
  local boost_mode = settings.boost_gauge or "Auto"
  local show_boost = boost_mode == "Always"
    or (boost_mode == "Auto" and (positive_boost_observed or ctx.edit_mode))

  draw_full_image(draw, style.background, "#ffffff", alpha)
  draw_full_image(draw, "redline_" .. redline, "#ffffff", alpha)
  draw_full_image(draw, style.gear_speed, lcd_color, alpha)
  draw_full_image(draw, style.rpm_line, rpm_color, alpha)

  if show_boost and boost_mode ~= "Never" then
    draw_full_image(draw, "boost_gauge", rpm_color, alpha)
  end
  draw_full_image(draw, "nos_time", "#ffffff", alpha)

  draw_full_image(draw, "shift_flash", "#000000", alpha * 0.5)
  if rpm > max_rpm * 0.96 then
    local visible = rpm <= max_rpm * 0.99 or math.floor(ctx.time * 20) % 2 == 1
    if visible then
      draw_full_image(draw, "shift_flash", "#ffffff", alpha)
    end
  end

  if show_boost and boost_mode ~= "Never" then
    local boost_bar = (tm.boost or 0) / 14.5037738
    draw_full_image(draw, "boost_needle", needle_color, alpha, -boost_bar * 33)
  end

  draw_readout(draw, tm, ctx.metric, alpha)
  draw_full_image(draw, style.needle, needle_color, alpha, needle_angle)
end

return {
  api_version = 1,
  id = "assettocorsa.fsh_mw2005",
  name = "NFS Most Wanted 2005",
  author = "FSH Motorsport Studio / ForzaOSD adapter",
  version = "0.9.2",
  asset_root = "assets",
  layout = { width = 390, height = 390, reference_height = 1080 },
  assets = assets,
  fonts = {
    digits = { path = "fonts/Seven_Segment_BOLD.ttf", size = 100 },
    labels = { path = "fonts/StackSansHeadline-SemiBold.ttf", size = 24 },
  },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.87, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.78, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.3, max = 2, order = 3 },
    style = { type = "enum", label = "Style", default = "Default", options = { "Default", "Sonic" }, order = 4 },
    needle_color = { type = "enum", label = "Needle color", default = "MW Amber", options = color_options, order = 5 },
    rpm_color = { type = "enum", label = "RPM line color", default = "White", options = color_options, order = 6 },
    lcd_color = { type = "enum", label = "LCD color", default = "MW Amber", options = color_options, order = 7 },
    boost_gauge = { type = "enum", label = "Boost gauge", default = "Auto", options = { "Auto", "Always", "Never" }, order = 8 },
  },
  render = render,
}
