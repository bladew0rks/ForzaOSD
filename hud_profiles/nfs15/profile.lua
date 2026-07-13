local assets = {
  background = "images/background.png",
  flash = "images/flash.png",
  gear_n = "images/chars/nfs15gear/n.png",
  gear_r = "images/chars/nfs15gear/R.png",
  unit_k = "images/chars/nfs15uom/K.png",
  unit_m = "images/chars/nfs15uom/M.png",
}

for i = 1, 359 do
  assets["rev_" .. i] = "images/rev/rev_" .. i .. ".png"
end
for i = 1, 19 do
  assets["zone_" .. i] = "images/revzones/" .. i .. ".png"
end
for i = 0, 9 do
  assets["gear_" .. i] = "images/chars/nfs15gear/" .. i .. ".png"
  assets["speed_" .. i] = "images/chars/nfs15speed/" .. i .. ".png"
end

local last_gear = nil
local flash_until = 0

local function draw_speed(draw, speed, alpha)
  local value = math.max(0, math.min(999, math.floor(speed + 0.5)))
  local text = string.format("%3d", value)
  local x = 0.35 * 331
  local y = 0.74 * 320
  local width = 230 * 0.125
  local height = 368 * 0.125

  for i = 1, 3 do
    local digit = string.sub(text, i, i)
    if digit ~= " " then
      draw.image {
        asset = "speed_" .. digit,
        x = x + (i - 1) * width, y = y, w = width, h = height,
        color = "#ffffff", alpha = alpha,
      }
    end
  end
end

local function draw_gear(draw, label, alpha, bloom)
  local key = string.lower(label)
  if key ~= "n" and key ~= "r" then
    key = string.sub(key, 1, 1)
  end
  draw.image {
    asset = "gear_" .. key,
    x = 0.46 * 331, y = 0.57 * 320,
    w = 230 * 0.1125, h = 368 * 0.1125,
    color = "#ffffff", alpha = alpha,
    glow_radius = 6, glow_intensity = 0.8 * bloom, glow_color = "#ff3018",
  }
end

local function render(ctx)
  local draw, tm, settings = ctx.draw, ctx.telemetry, ctx.settings
  local alpha = ctx.opacity
  local needle_opacity = tonumber(settings.needle_opacity) or 0.82
  local bloom = tonumber(settings.bloom) or 1
  local rpm_ratio = math.max(0, tm.rpm / math.max(1, tm.max_rpm))

  draw.image {
    asset = "background", x = 0, y = 0, w = 331, h = 320,
    color = "#ffffff", alpha = alpha,
    glow_radius = 4, glow_intensity = 0.28 * bloom,
  }

  local rev_frame = math.max(1, math.min(359, math.floor(rpm_ratio / 1.18 * 359) + 1))
  draw.image {
    asset = "rev_" .. rev_frame, x = 0, y = 0, w = 331, h = 320,
    color = "#ffffff", alpha = alpha * needle_opacity,
    glow_radius = 9, glow_intensity = 0.9 * bloom,
  }

  if rpm_ratio >= 0.81 then
    local zone = math.max(1, math.min(19, math.floor((rpm_ratio - 0.81) * 100) + 1))
    local visible = rpm_ratio < 1 or math.floor(ctx.time * 10) % 2 == 0
    if visible then
      draw.image {
        asset = "zone_" .. zone, x = 0, y = 0, w = 331, h = 320,
        color = "#ffffff", alpha = alpha,
        glow_radius = 7, glow_intensity = 0.55 * bloom,
      }
    end
  end

  draw_gear(draw, tm.gear_label, alpha, bloom)
  draw_speed(draw, ctx.metric and tm.speed_kph or tm.speed_mph, alpha)
  draw.image {
    asset = ctx.metric and "unit_k" or "unit_m",
    x = 0.42 * 331, y = 0.91 * 320,
    w = 224 * 0.27, h = 60 * 0.27,
    color = "#ffffff", alpha = alpha,
  }

  if last_gear ~= nil and last_gear ~= tm.gear_label then
    flash_until = ctx.time + 0.1
  end
  last_gear = tm.gear_label
  if settings.gear_flash and ctx.time < flash_until then
    draw.image {
      asset = "flash", x = 0, y = 0, w = 331, h = 320,
      color = "#ffffff", alpha = alpha,
    }
  end
end

return {
  api_version = 1,
  id = "assettocorsa.nfs15ui",
  name = "nfs 2015",
  author = "StoRMiX43 / ForzaOSD adapter",
  version = "1.0",
  asset_root = "assets/skins/NFS15SPD",
  layout = { width = 331, height = 320, reference_height = 1080 },
  assets = assets,
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.87, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.78, min = 0, max = 1, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.3, max = 2, order = 3 },
    gear_flash = { type = "boolean", label = "Flash on gear change", default = true, order = 4 },
    needle_opacity = { type = "number", label = "Needle opacity", default = 0.82, min = 0.1, max = 1, order = 5 },
    bloom = { type = "number", label = "Bloom intensity", default = 1, min = 0, max = 2, order = 6 },
  },
  render = render,
}
