local palettes = {
  Aqua = {
    primary = "#8ffff0",
    accent = "#dcfffb",
    dim = "#173c39",
  },
  Amber = {
    primary = "#ffc15a",
    accent = "#fff0c9",
    dim = "#493719",
  },
  Green = {
    primary = "#78ff91",
    accent = "#dcffe3",
    dim = "#173d20",
  },
  Ice = {
    primary = "#b9e8ff",
    accent = "#f3fbff",
    dim = "#203744",
  },
}

local function clamp(value, minimum, maximum)
  return math.max(minimum, math.min(maximum, value))
end

local function phosphor_text(draw, options, color, alpha, bloom)
  options.color = color
  options.alpha = alpha
  options.glow_color = color
  options.glow_radius = 8
  options.glow_intensity = bloom
  draw.text(options)
end

local function phosphor_cell(draw, x, y, width, height, color, alpha, bloom)
  draw.rect {
    x = x,
    y = y,
    w = width,
    h = height,
    color = color,
    alpha = alpha,
    rounding = 1.2,
    glow_color = color,
    glow_radius = 5,
    glow_intensity = bloom,
  }
end

local function display_source(audio)
  if audio.source == nil or audio.source == "" then
    return "WINDOWS MEDIA"
  end

  local source = string.upper(audio.source)
  source = string.gsub(source, "%.EXE$", "")
  if #source > 35 then
    source = string.sub(source, 1, 35)
  end
  return source
end

local function display_track(audio)
  local title = audio.title or ""
  local artist = audio.artist or ""

  if title == "" and artist == "" then
    return audio.available and "AUDIO SIGNAL" or "NO MEDIA SESSION"
  elseif artist == "" then
    return string.upper(title)
  elseif title == "" then
    return string.upper(artist)
  end
  return string.upper(artist .. "  /  " .. title)
end

local function draw_marquee(draw, text, settings, palette, alpha, bloom, time)
  local clip_x, clip_y, clip_w, clip_h = 22, 36, 856, 54
  local glyphs = utf8.len(text) or #text
  local estimated_width = glyphs * 19

  if estimated_width <= clip_w - 8 then
    phosphor_text(draw, {
      font = "display",
      text = text,
      x = clip_x,
      y = 63,
      size = 34,
      clip_x = clip_x,
      clip_y = clip_y,
      clip_w = clip_w,
      clip_h = clip_h,
    }, palette.accent, alpha, 0.9 * bloom)
    return
  end

  local gap = 150
  local cycle = estimated_width + gap
  local offset = (time * settings.marquee_speed) % cycle
  for copy = 0, 1 do
    phosphor_text(draw, {
      font = "display",
      text = text,
      x = clip_x - offset + copy * cycle,
      y = 63,
      size = 34,
      clip_x = clip_x,
      clip_y = clip_y,
      clip_w = clip_w,
      clip_h = clip_h,
    }, palette.accent, alpha, 0.9 * bloom)
  end
end

local function preview_band(index, count, time)
  local wave = 0.5 + 0.5 * math.sin(time * 2.7 + index * 0.68)
  local shape = 0.45 + 0.55 * math.sin(index / count * math.pi)
  return clamp(0.16 + wave * shape * 0.77, 0, 1)
end

local function draw_spectrum(draw, audio, settings, palette, alpha, bloom, time, edit_mode)
  local bands = audio.bands or {}
  local band_count = math.max(1, #bands)
  if band_count == 1 and bands[1] == nil then
    band_count = 28
  end

  local left, top, width, height = 22, 105, 856, 126
  local band_gap = 5
  local band_width = (width - band_gap * (band_count - 1)) / band_count
  local segments, segment_gap = 9, 3
  local segment_height = (height - segment_gap * (segments - 1)) / segments

  draw.line {
    x1 = left,
    y1 = top + height + 8,
    x2 = left + width,
    y2 = top + height + 8,
    color = palette.primary,
    alpha = 0.22 * alpha,
    thickness = 1,
  }

  for band = 1, band_count do
    local value = bands[band] or 0
    if edit_mode and not audio.available then
      value = preview_band(band, band_count, time)
    end
    value = clamp(value * settings.sensitivity, 0, 1)
    local lit = math.floor(value * segments + 0.5)
    local x = left + (band - 1) * (band_width + band_gap)

    for segment = 1, segments do
      local y = top + height - segment * segment_height - (segment - 1) * segment_gap
      draw.rect {
        x = x,
        y = y,
        w = band_width,
        h = segment_height,
        color = palette.dim,
        alpha = 0.31 * alpha,
        rounding = 1.2,
      }

      if segment <= lit then
        local intensity = 0.72 + 0.28 * segment / segments
        phosphor_cell(
          draw,
          x,
          y,
          band_width,
          segment_height,
          palette.primary,
          intensity * alpha,
          (0.42 + 0.45 * value) * bloom
        )
      end
    end
  end
end

local function render(ctx)
  local draw, audio, settings = ctx.draw, ctx.audio, ctx.settings
  local palette = palettes[settings.palette] or palettes.Aqua
  local alpha = clamp(ctx.opacity * settings.brightness, 0, 1)
  local bloom = settings.bloom

  phosphor_text(draw, {
    font = "display",
    text = audio.playing and "NOW PLAYING" or "RADIO MONITOR",
    x = 22,
    y = 16,
    size = 15,
  }, palette.primary, 0.68 * alpha, 0.48 * bloom)

  if settings.show_source then
    draw.text {
      font = "display",
      text = display_source(audio),
      x = 878,
      y = 16,
      size = 13,
      align = "right",
      color = palette.primary,
      alpha = 0.48 * alpha,
    }
  end

  draw_marquee(draw, display_track(audio), settings, palette, alpha, bloom, ctx.time)
  draw_spectrum(draw, audio, settings, palette, alpha, bloom, ctx.time, ctx.edit_mode)

  local level = clamp(audio.rms * 3.2, 0, 1)
  draw.line {
    x1 = 22,
    y1 = 251,
    x2 = 22 + 856 * level,
    y2 = 251,
    color = palette.accent,
    alpha = 0.7 * alpha,
    thickness = 2,
    glow_color = palette.accent,
    glow_radius = 4,
    glow_intensity = 0.55 * bloom,
  }
end

return {
  api_version = 1,
  id = "forzaosd.vfd_radio",
  name = "VFD Radio",
  author = "ForzaOSD",
  version = "1.1.0",
  role = "module",
  visibility = "audio",
  asset_root = "assets",
  layout = { width = 900, height = 260, reference_height = 1440 },
  assets = {},
  fonts = {
    display = { path = "fonts/DSEG14Modern-Regular.ttf", size = 104 },
  },
  settings = {
    x = { type = "number", label = "Horizontal", default = 0.5, min = 0, max = 1, order = 1 },
    y = { type = "number", label = "Vertical", default = 0.78, min = 0.1, max = 1.05, order = 2 },
    scale = { type = "number", label = "Scale", default = 1, min = 0.4, max = 2, order = 3 },
    palette = { type = "enum", label = "Phosphor", default = "Aqua", options = { "Aqua", "Amber", "Green", "Ice" }, order = 4 },
    brightness = { type = "number", label = "Brightness", default = 1, min = 0.25, max = 1.5, order = 5 },
    bloom = { type = "number", label = "Bloom", default = 1, min = 0, max = 2.5, order = 6 },
    sensitivity = { type = "number", label = "Spectrum sensitivity", default = 1.15, min = 0.35, max = 2.5, order = 7 },
    marquee_speed = { type = "number", label = "Marquee speed", default = 62, min = 10, max = 180, order = 8 },
    show_source = { type = "boolean", label = "Media source", default = true, order = 9 },
  },
  render = render,
}
