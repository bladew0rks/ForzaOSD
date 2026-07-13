ui.setAsynchronousImagesLoading(false)
ANALOG_MODE = false
function drawTachHUDOld()
    CFG = {
        width = scale(2760),
        height = scale(362),
        x = scale(0),
        y = scale(50),
        cx = nil,
        cy = nil,
    }
    ANALOG_MODE = INI.analogMode
    if INI.analogModeAuto then
        ANALOG_MODE = SIM.driveableCameraMode == 3
    end



    if not INI.fixedPos then CFG.y = CFG.y + 25 end
    if ANALOG_MODE then CFG.height = scale(774) end
    CFG.cx = CFG.width / 2
    CFG.cy = CFG.height / 2

    -- Debug Image
    ui.setCursor(vec2(CFG.x, CFG.y))

    -- ui.image(
    --     'img/debug/tach-analog-sb-background.png',
    --     vec2(CFG.width, CFG.height),
    --     rgbm(1, 1, 1, 0.2)
    -- )


    -- Starting Point
    local stx, sty = CFG.cx - scale(36), CFG.y
    ------------------------- DELTA -------------------------

    if MY_DRIVER.fastestLap ~= -1 and SIM.raceSessionType ~= ac.SessionType.Race then
        local sign = MY_DRIVER.currentSplitGap > 0 and "+" or "-"
        local text = sign .. "  " .. string.format("%.3f", math.abs(MY_DRIVER.currentSplitGap) / 1000)

        ui.setCursor(vec2(stx, sty - scale(30)))
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Medium')
        ui.dwriteTextAligned(
            text, scale(36),
            ui.Alignment.Start, ui.Alignment.Center,
            vec2(scale(250), scale(36)), false, rgbm.colors.black
        )

        ui.setCursor(vec2(stx, sty - scale(30)))
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')

        ui.dwriteTextAligned(
            text, scale(36),
            ui.Alignment.Start, ui.Alignment.Center,
            vec2(scale(250), scale(36)), false, rgbm.colors.white
        )

        local triangle_color = rgbm.colors.red
        if MY_DRIVER.currentSplitLarger then
            ui.beginRotation()
            triangle_color = rgbm.from0255(82, 154, 200, 1)
        end
        ui.setCursor(vec2(stx - scale(50), sty - scale(22)))
        ui.image('img/gap_arrow.png', vec2(scale(26), scale(26)), triangle_color, true)
        ui.popDWriteFont()
        if MY_DRIVER.currentSplitLarger then ui.endRotation(-90) end
    end

    ------------------------- CENTER -------------------------
    local stx, sty                 = CFG.cx, CFG.y
    local tach_background          = 'img/tach/background.png'
    local rev_background           = 'img/tach/line.png'
    local rev_overlay              = 'img/tach/dashes.png'
    local tach_x, tach_y           = -scale(400), scale(50)
    local tach_main_w, tach_main_h = scale(800), scale(196)
    local tach_rev_w, tach_rev_h   = scale(800), scale(60)



    stx = CFG.cx
    sty = CFG.y + tach_y

    ui.setCursor(vec2(stx + tach_x, sty))

    -- Tinted background
    ui.image_cursor(tach_background, vec2(tach_main_w, tach_main_h), false)

    -- Rev background
    ui.image_cursor(rev_background, vec2(tach_rev_w, tach_rev_h + scale(1)), rgbm(1, 1, 1, 0.15), true)

    -- Rev color
    local start_percentage = 80   -- What RPM % rev should start at (0 = entire RPM range)
    local fade_delay       = 50   -- What % of the rev to start fading color
    local limiter_bias     = 0.05 -- Raise to counter limiter bouncing (offsets RPM)
    local limiter_blink    = math.floor(SIM.time / 50) % 2 == 0
    local rpm_percentage   = interpolate(
        0, CAR.rpmLimiter,            -- input range
        CAR.rpm * (1 + limiter_bias), -- input
        0, 100                        -- output range
    )
    local progress         = interpolate(start_percentage, 100, rpm_percentage, 0, 100)
    local color_progress   = (progress - 50) / 50

    -- base color
    local color            = { r = 1 - (color_progress * 0.2), g = (color_progress * 0.6), b = color_progress * 0.9, a = 1 }
    local color_end        = { r = 0.5, g = 0.95, b = 0.9, a = 0.9 }

    -- Ignore lower RPM
    if (rpm_percentage < start_percentage) then rpm_percentage = 0 end

    -- Limiter
    if progress >= 100 then
        color = color_end
        if limiter_blink then color.a = 0 end
    end

    local rev_level = progress / 100 * 80
    ui.image_cursor('img/tach/rev/rev_back_' .. math.round(rev_level) .. '.png', scaledVec2(800, 60)
        , rgbm(color.r, color.g, color.b, color.a),
        false)

    -- Rev overlay
    ui.image_cursor(rev_overlay, vec2(tach_rev_w, tach_rev_h), rgbm(0, 0, 0, 0.25))


    -- Center info
    local speed = math.floor(CAR.speedKmh)
    local gear  = CAR.gear


    -- Dividing line
    local w, h = scale(1), scale(125)
    local x, y = scale(0), scale(52)
    ui.pathLineTo(vec2(stx + x, sty + y))
    ui.pathLineTo(vec2(stx + x, sty + y + h))
    ui.pathStroke(rgbm.colors.white, false, w)



    -- Speed
    local units = 'km/h'
    if INI.isImperial then
        units = 'mph'
        speed = math.floor(speed * 0.6213711922)
    end

    -- Number
    w, h = scale(260), scale(68)
    x, y = -scale(290), scale(62)
    ui.setCursor(vec2(stx + x, sty + y))
    ui.pushDWriteFont('Arkitech:\\fonts;Weight=Medium')
    ui.dwriteTextAligned(
        tostring(speed), scale(68),
        ui.Alignment.End, ui.Alignment.Center,
        vec2(w, h)
    )

    -- Unit
    w, h = scale(70), scale(48)
    x, y = -scale(100), scale(135)
    ui.setCursor(vec2(stx + x, sty + y))
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.dwriteTextAligned(
        units, scale(27),
        ui.Alignment.End, ui.Alignment.Center,
        vec2(w, h)
    )
    ui.popDWriteFont()


    -- Gear
    local gear = CAR.gear
    if gear == 0 then
        gear = 'N'
    elseif gear == -1 then
        gear = 'R'
    end

    w, h = scale(140), scale(120)
    x, y = scale(18), scale(48)
    ui.setCursor(vec2(stx + x, sty + y))
    ui.pushDWriteFont('Arkitech:\\fonts;Weight=')
    ui.dwriteTextAligned(
        tostring(gear), scale(116),
        ui.Alignment.End, ui.Alignment.Center,
        vec2(w, h)
    )

    -- Steering dot

    local dot_size = scale(6)
    y = sty - scale(20) - dot_size
    local steerPercent = CAR.steer / CAR.steerLock

    local curve = (math.abs(steerPercent)) * (tach_rev_h / 2 - dot_size / 2)
    local curve_y = (math.abs(steerPercent) * curve)

    ui.setCursor(vec2(stx - dot_size, y))

    -- Neutral dot
    ui.drawCircleFilled_cursor(dot_size, rgbm(1, 1, 1, 0.3), 10)

    -- Steering dot
    ui.moveCursor((tach_rev_w - dot_size * 3) / 2 * steerPercent, curve_y)
    ui.drawCircleFilled_cursor(dot_size, rgbm(1, 0, 0, 0.7), 10)

    -- Indicators
    local turningLeft  = CAR.turningLeftLights
    local turningRight = CAR.turningRightLights
    x                  = scale(790)
    y                  = scale(165)
    local left_x       = -(x + scale(50)) / 2
    local right_x      = (x - scale(50)) / 2

    -- Left Arrow
    ui.setCursor(vec2(stx + left_x, sty + y))
    ui.moveCursor(-scale(4), -scale(8))
    if turningLeft then
        ui.drawCircleFilled_cursor(scale(32),
            rgbm(0, 0, 0, blinkingAlpha(8, 0) * 0.3), 20)
    end
    ui.setCursor(vec2(stx + left_x, sty + y))
    ui.image_cursor('img/indicator_left.png',
        scaledVec2(50, 50),
        not turningLeft and rgbm(1, 1, 1, 0.2) or rgbm(1, 1, 1, blinkingAlpha(8, 0)),
        false
    )

    -- Right arrow
    ui.setCursor(vec2(stx + right_x, sty + y))
    ui.moveCursor(-scale(10), -scale(7))
    if turningRight then
        ui.drawCircleFilled_cursor(scale(32),
            rgbm(0, 0, 0, blinkingAlpha(8, 0) * 0.3), 20)
    end

    ui.setCursor(vec2(stx + right_x, sty + y))
    ui.image_cursor('img/indicator_right.png',
        scaledVec2(50, 50),
        not turningRight and rgbm(1, 1, 1, 0.2) or rgbm(1, 1, 1, blinkingAlpha(8, 0)),
        false
    )

    -- Nearby vehicles
    x, y           = scale(686), scale(86)
    w, h           = scale(104), scale(104)
    local left_x   = -(x + w) / 2
    local right_x  = (x - w) / 2

    -- Nearby Car tables
    local leftCars = table.filter(MY_DRIVER.nearbyCars, function(item, key, callbackData)
        if item.isLeft and item.distance < 15 and item.splinePosition < CAR.splinePosition then
            return true
        end
        return false
    end)
    table.sort(leftCars, function(a, b)
        return a.distance < b.distance
    end)

    local rightCars = table.filter(MY_DRIVER.nearbyCars, function(item, key, callbackData)
        if item.isRight and item.distance < 15 and item.splinePosition < CAR.splinePosition then
            return true
        end
        return false
    end)
    table.sort(rightCars, function(a, b)
        return a.distance < b.distance
    end)
    --

    -- Testing
    -- if (nearby_testing) then
    -- rightCars[1] = { distance = 1 }
    -- leftCars[1] = { distance = 10 }
    -- end

    -- State
    local leftState = 0
    local rightState = 0
    if #rightCars > 0 then
        if rightCars[1].distance < 5 then
            rightState = 3
        elseif rightCars[1].distance < 10 then
            rightState = 2
        else
            rightState = 1
        end
    else
        rightState = 0
    end

    if #leftCars > 0 then
        if leftCars[1].distance < 5
        then
            leftState = 3
        elseif leftCars[1].distance < 10
        then
            leftState = 2
        else
            leftState = 1
        end
    else
        leftState = 0
    end
    --

    -- Helper
    local function drawNearby(i)
        local r, g, b = 1, 0, 0
        local m = i == 3 and 0.7 or i == 2 and 0.4 or 0.2
        ui.image_cursor(
            'img/blindspot-' .. i .. '.png',
            vec2(w, h),
            rgbm(r, g, b, m)
        )
    end

    -- Draw right side
    ui.setCursor(vec2(stx + right_x, sty + y))
    for z = 0, rightState, 1 do drawNearby(z) end

    -- Draw left side
    ui.setCursor(vec2(stx + left_x, sty + y))
    ui.beginRotation()
    for z = 0, leftState, 1 do drawNearby(z) end
    ui.endRotation(0)
    --

    ------------------------- GAUGES -------------------------
    stx                            = CFG.cx
    sty                            = CFG.y
    x, y                           = scale(466), scale(67)
    w, h                           = scale(62), scale(190)
    local icon_w, icon_h           = scale(39), scale(39)
    local icon_x, icon_y           = -scale(11), h / 2 - icon_h / 2 - scale(1)
    local bar_x, bar_y             = scale(15), scale(11)
    local throttle_color           = rgbm.colors.white
    local braking_color            = rgbm.colors.white
    local gauge_bar_h, gauge_bar_w = scale(168), scale(24)


    if ANALOG_MODE then
        x, y = scale(316), scale(333)
        icon_x, icon_y = -scale(53), h + scale(6)
    end

    -- Gauge: brake
    ui.setCursor(vec2(stx - x - w, sty + y))
    ui.image_cursor('img/gauge_left.png', vec2(w, h))

    ui.moveCursor(w - gauge_bar_w - bar_x, bar_y)
    ui.drawRectFilled(
        vec2(
            ui.getCursorX(),
            ui.getCursorY() + gauge_bar_h - (gauge_bar_h * CAR.brake)
        ),
        vec2(
            ui.getCursorX() + gauge_bar_w,
            ui.getCursorY() + gauge_bar_h
        ),
        braking_color, 0
    )

    -- Brakes Icon
    ui.setCursor(vec2(stx - x, sty + y))
    ui.moveCursor(-w - icon_x - icon_w, icon_y)
    ui.image_cursor('img/icons/brakes_icon.png', vec2(icon_w, icon_h))

    -- Gauge: throttle
    ui.setCursor(vec2(stx + x, sty + y))
    ui.image_cursor('img/gauge_right.png', vec2(w, h))

    ui.moveCursor(bar_x, bar_y)
    ui.drawRectFilled(
        vec2(
            ui.getCursorX(),
            ui.getCursorY() + gauge_bar_h - (gauge_bar_h * CAR.gas)
        ),
        vec2(
            ui.getCursorX() + gauge_bar_w,
            ui.getCursorY() + gauge_bar_h
        ),
        throttle_color, 0
    )

    -- Throttle Icon
    ui.setCursor(vec2(stx + x, sty + y))
    ui.moveCursor(w + icon_x, icon_y)
    ui.image_cursor('img/icons/throttle_icon.png', vec2(icon_w, icon_h))
    --

    ------------------------- ICONS -------------------------
    x, y                  = scale(576), scale(90)
    local icon_size       = scale(32)
    local icon_space_x    = scale(84)
    local icon_space_y    = scale(80)
    local icon_image_size = vec2(scale(64), scale(64))


    if ANALOG_MODE then
        x, y         = scale(500), scale(665)
        icon_space_x = scale(90)
        icon_space_y = -scale(336)
    end

    -- Icons right side

    -- Icon: Headlights
    local headlights = {
        color = rgbm(1.0, 1.0, 1.0, 0.2),
        low   = rgbm(1.0, 1.0, 1.0, 0.7),
    }
    if CAR.headlightsActive then
        headlights.color = headlights.low
    end
    if ANALOG_MODE then
        ui.setCursor(vec2(stx + x, sty + y + icon_space_y))
    else
        ui.setCursor(vec2(stx + x, sty + y))
    end
    ui.drawCircleFilled_cursor(icon_size, headlights.color, 20)
    ui.image_cursor('img/icons/headlights-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))

    -- Icon: TCS
    local tcsDisabled = CAR.tractionControlMode == 0
    local tcsActive = CAR.tractionControlInAction
    local tcsColor = tcsDisabled and rgbm(1, 1, 1, 0.2) or
        tcsActive and rgbm(0.75, 0, 0, 0.9) or rgbm(1, 1, 1, 0.7)
    if ANALOG_MODE then
        ui.moveCursor(0, -icon_space_y)
    else
        ui.moveCursor(icon_space_x - icon_space_x, icon_space_y)
    end
    ui.drawCircleFilled_cursor(icon_size, tcsColor, 20)
    ui.image_cursor('img/icons/tcs-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))

    -- Icon: Wheel
    if ANALOG_MODE then
        ui.moveCursor(2 * icon_space_x, 0)
    else
        ui.moveCursor(icon_space_x, -icon_space_y)
    end
    ui.drawCircleFilled_cursor(icon_size, rgbm(1, 1, 1, 0.2), 20)
    ui.image_cursor('img/icons/wheel-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))


    -- Icon: Warning
    if ANALOG_MODE then
        ui.moveCursor(-icon_space_x, 0)
    else
        ui.moveCursor(0, icon_space_y)
    end
    ui.drawCircleFilled_cursor(icon_size, rgbm(1, 1, 1, 0.2), 20)
    ui.image_cursor('img/icons/warning-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))


    -- Icons left side
    local left_offset = scale(4) -- spacing on left side is off slightly?! I cannot figure it out

    -- Icon: Handbrake
    local handbrakeActive = CAR.handbrake > 0
    local handbrakeColor = handbrakeActive and rgbm(1, 1, 1, 0.7) or rgbm(1, 1, 1, 0.2)
    if ANALOG_MODE then
        ui.setCursor(vec2(stx - x - icon_size * 2 - left_offset, sty + y + icon_space_y))
    else
        ui.setCursor(vec2(stx - x - icon_size * 2 - left_offset, sty + y))
    end

    ui.drawCircleFilled_cursor(icon_size, handbrakeColor, 20)
    ui.image_cursor('img/icons/handbrake-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))

    -- Icon: Auto steering
    if ANALOG_MODE then
        ui.moveCursor(0, -icon_space_y)
    else
        ui.moveCursor(0, icon_space_y)
    end
    ui.drawCircleFilled_cursor(icon_size, rgbm(1, 1, 1, 0.2), 20)
    ui.image_cursor('img/icons/auto-steering-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))

    ui.moveCursor(left_offset, 0)

    -- Icon: ABS
    if ANALOG_MODE then
        ui.moveCursor(2 * -icon_space_x, 0)
    else
        ui.moveCursor(-(icon_space_x), -icon_space_y)
    end
    local absDisabled = CAR.absMode == 0
    local absActive = CAR.absInAction
    local absColor = absDisabled and rgbm(1, 1, 1, 0.2) or absActive
        and rgbm(0.75, 0, 0, 0.9) or rgbm(1, 1, 1, 0.7)
    ui.drawCircleFilled_cursor(icon_size, absColor, 20)
    ui.image_cursor('img/icons/abs-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))

    -- Icon: Auto brakes
    if ANALOG_MODE then
        ui.moveCursor(icon_space_x, 0)
    else
        ui.moveCursor(0, icon_space_y)
    end
    ui.drawCircleFilled_cursor(icon_size, rgbm(1, 1, 1, 0.2), 20)
    ui.image_cursor('img/icons/auto-brakes-hq.png', icon_image_size, rgbm(1, 1, 1, 0.8))


    ------------------------- ANALOG DIALS -------------------------

    if ANALOG_MODE then
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
        local rpm = CAR.rpm
        local rpmLimiter = CAR.rpmLimiter

        local function round_to_nearest(num)
            local nearest = 32000
            local img = 6
            if num <= 6000 then
                nearest, img = 6000, 6
            elseif num <= 8000 then
                nearest, img = 8000, 8
            elseif num <= 12000 then
                nearest, img = 12000, 12
            elseif num <= 2 * 8000 then
                nearest, img = 16000, 8
            elseif num <= 4 * 6000 then
                nearest, img = 24000, 6
            elseif num <= 4 * 8000 then
                nearest, img = 32000, 8
            end
            return nearest, img
        end

        local nearest, img = round_to_nearest(rpmLimiter)


        stx = CFG.cx
        sty = CFG.y

        local dial_x, dial_y = stx + scale(624), sty + scale(428)
        local rev_pct = math.min(rpm / nearest * 100, 100)
        local red_pct = math.min(rpmLimiter / nearest * 100, 100)
        local rev_deg = (-45.5 + (rev_pct / 100) * 271)
        local red_deg = (-45.5 + (red_pct / 100) * 271)

        ui.setCursor(vec2(dial_x - scale(243), dial_y - scale(243)))
        ui.image('img/tach/analog_' .. img .. '.png', vec2(scale(486), scale(486)))

        -- Redline
        ui.pathArcTo(vec2(dial_x, dial_y), scale(234), math.rad(-180 + red_deg), math.rad(-180 + 225.5), 100)
        ui.pathStroke(rgbm(0.7, 0, 0, 1), false, scale(11))

        -- Current Rev
        local color_progress = (progress - 50) / 50

        -- base color
        local color = { r = 1, g = 1 - (color_progress), b = 1 - color_progress, a = 1 }
        ui.pathArcTo(vec2(dial_x, dial_y), scale(234), math.rad(-180 - 45.5), math.rad(-180 + rev_deg), 100)
        ui.pathStroke(rgbm(color.r, color.g, color.b, color.a), false, scale(11))

        -- Rev Numbers
        local function getRpm(index)
            return tostring(nearest / 1000 / img * index)
        end

        -- Set initial position and text size
        local pos_x, pos_y = 0, 0
        local txt_w, txt_h = scale(100), scale(30)

        -- Set the positions of the text based on the value of img
        local function getPositions(base)
            if base == 6 then
                return {
                    { dial_x - scale(188),         dial_y + scale(119) },
                    { dial_x - scale(247),         dial_y - scale(17) },
                    { dial_x - scale(188),         dial_y - scale(155) },
                    { dial_x - txt_w / 2,          dial_y - scale(214) },
                    { dial_x + scale(188) - txt_w, dial_y - scale(155) },
                    { dial_x + scale(247) - txt_w, dial_y - scale(17) },
                    { dial_x + scale(188) - txt_w, dial_y + scale(119) }
                }
            elseif base == 8 then
                return {
                    { dial_x - scale(188),         dial_y + scale(119) },
                    { dial_x - scale(240),         dial_y + scale(21) },
                    { dial_x - scale(227),         dial_y - scale(91) },
                    { dial_x - scale(160),         dial_y - scale(170) },
                    { dial_x - txt_w / 2,          dial_y - scale(214) },
                    { dial_x + scale(160) - txt_w, dial_y - scale(170) },
                    { dial_x + scale(227) - txt_w, dial_y - scale(91) },
                    { dial_x + scale(240) - txt_w, dial_y + scale(21) },
                    { dial_x + scale(188) - txt_w, dial_y + scale(119) }
                }
            else
                return {
                    { dial_x - scale(188),         dial_y + scale(119) },
                    { dial_x - scale(232),         dial_y + scale(57) },
                    { dial_x - scale(247),         dial_y - scale(17) },
                    { dial_x - scale(232),         dial_y - scale(93) },
                    { dial_x - scale(188),         dial_y - scale(155) },
                    { dial_x - scale(125),         dial_y - scale(199) },
                    { dial_x - txt_w / 2,          dial_y - scale(214) },
                    { dial_x + scale(125) - txt_w, dial_y - scale(199) },
                    { dial_x + scale(188) - txt_w, dial_y - scale(155) },
                    { dial_x + scale(232) - txt_w, dial_y - scale(93) },
                    { dial_x + scale(247) - txt_w, dial_y - scale(17) },
                    { dial_x + scale(232) - txt_w, dial_y + scale(57) },
                    { dial_x + scale(188) - txt_w, dial_y + scale(119) },

                }
            end
        end

        local positions = getPositions(img)

        ui.pushDWriteFont('MyFont:\\fonts;Weight=Light')
        -- Iterate through the positions and draw the text at each position
        for i, position in ipairs(positions) do
            pos_x, pos_y = position[1], position[2]
            ui.setCursor(vec2(pos_x, pos_y))
            ui.dwriteTextAligned(getRpm(i - 1), scale(30), 0, 0, vec2(txt_w, txt_h))
        end
        ui.popDWriteFont()

        --- Speed Dial
        dial_x, dial_y = stx - scale(624), sty + scale(428)

        local topSpeed = MY_DRIVER.topSpeed
        local currentSpeed = CAR.speedKmh
        if INI.isImperial then
            currentSpeed = math.floor(CAR.speedKmh * 0.6213711922)
            topSpeed = math.floor(topSpeed * 0.6213711922)
        end


        local function closest_larger_multiple(num)
            local multiples = { 60, 80, 120, 160, 240, 320, 360, 400, 480 }
            local bases = { 6, 8, 12, 8, 12, 8, 12, 8, 12 }
            for i, multiple in ipairs(multiples) do
                if multiple >= num then
                    return multiple, bases[i]
                end
            end
        end

        nearest, img = closest_larger_multiple(topSpeed)

        rev_pct = math.min(currentSpeed / nearest * 100, 100)
        red_pct = math.min(rpmLimiter / nearest * 100, 100)
        rev_deg = (-45.5 + (rev_pct / 100) * 271)
        red_deg = (-45.5 + (red_pct / 100) * 271)

        ui.setCursor(vec2(dial_x - scale(243), dial_y - scale(243)))
        ui.image('img/tach/analog_' .. img .. '.png', vec2(scale(486), scale(486)))


        -- base color
        ui.pathArcTo(vec2(dial_x, dial_y), scale(234), math.rad(-180 - 45.5), math.rad(-180 + rev_deg), 100)
        ui.pathStroke(rgbm.colors.white, false, scale(11))


        local positions = getPositions(img)

        -- Speed numbers
        local function getSpeed(index)
            return tostring(nearest / img * index)
        end

        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Light')
        -- Iterate through the positions and draw the text at each position
        for i, position in ipairs(positions) do
            pos_x, pos_y = position[1], position[2]
            ui.setCursor(vec2(pos_x, pos_y))
            ui.dwriteTextAligned(getSpeed(i - 1), scale(20), 0, 0, vec2(txt_w, txt_h))
        end
        -- Unit

        pos_x, pos_y = -scale(670), scale(274)
        w, h = scale(70), scale(48)
        -- Speed
        local units = 'km/h'
        if INI.isImperial then units = 'mph' end


        ui.setCursor(vec2(stx + pos_x, sty + pos_y))
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Light')
        ui.dwriteTextAligned(
            units, scale(23),
            ui.Alignment.End, ui.Alignment.Center,
            vec2(w, h)
        )
        ui.popDWriteFont()

        pos_x, pos_y = scale(561), scale(274)
        w, h = scale(120), scale(48)

        ui.setCursor(vec2(stx + pos_x, sty + pos_y))
        ui.dwriteTextAligned(
            'x1000 rpm', scale(23),
            ui.Alignment.End, ui.Alignment.Center,
            vec2(w, h)
        )
        ui.popDWriteFont()
    end


    ------------------------- FUEL -------------------------
    stx = CFG.cx
    sty = CFG.y



    local fuel        = CAR.fuel
    local maxFuel     = CAR.maxFuel
    local fuelPercent = fuel / maxFuel * 100
    local fuelLow     = fuelPercent < 10

    x                 = stx - scale(984)
    y                 = sty + scale(64)

    if ANALOG_MODE then
        x = stx - scale(735)
        y = sty + scale(387)
    end

    local fuel_arc_pos_x               = x + scale(111)
    local fuel_arc_pos_y               = y + scale(97)
    local fuel_icon_pos_x              = x + scale(111)
    local fuel_icon_pos_y              = y + scale(97)

    local ers_dim_w, ers_dim_h         = scale(75), scale(30)
    local ers_padding_w, ers_padding_h = scale(7), scale(7)
    local ers_inner_w, ers_inner_h     = ers_dim_w - ers_padding_w * 2, ers_dim_h - ers_padding_w * 2
    local ers_pos_x                    = x + scale(111) - ers_dim_w / 2
    local ers_pos_y                    = y + scale(97) - ers_dim_h / 2
    local ers_arrow_w, ers_arrow_h     = scale(32), scale(21)
    local ers_arrow_offset_x           = scale(55)
    local ers_arrow_pos_y              = y + scale(119)

    local odo_pos_x                    = x + scale(15)
    local odo_pos_y                    = y + scale(148)
    local odo_dim_w, odo_dim_h         = scale(192), scale(40)


    ui.setCursor(vec2(x, y))
    ui.image('img/fuel_background.png', scaledVec2(222, 110), rgbm(1, 1, 1, 1), true)

    local fuelRad = math.round(-math.pi * (100 - fuelPercent) / 100, 4)
    ui.pathArcTo(vec2(fuel_arc_pos_x, fuel_arc_pos_y), scale(75), -math.pi - 0,
        fuelRad, 100)
    local fuelRadColor = fuelLow and rgbm(0.75, 0, 0, 1) or rgbm(1, 1, 1, 1)
    ui.pathStroke(fuelRadColor, false, scale(9))

    local fuelColor = fuelLow and rgbm(0.75, 0, 0, blinkingAlpha(8, 0.5)) or rgbm(1, 1, 1, 0.2)
    local fuelIconColor = fuelLow and rgbm(1 - blinkingAlpha(8, 0), 1 - blinkingAlpha(8, 0), 1 - blinkingAlpha(8, 0),
        blinkingAlpha(8, 0.5)) or rgbm(1, 1, 1, 1)

    if CAR.kersPresent then
        local lapKersPercent
        if CAR.kersMaxKJ == 0 then
            lapKersPercent = math.max(0, CAR.kersCharge)
        else
            lapKersPercent = math.max(0, (CAR.kersMaxKJ - CAR.kersCurrentKJ) / CAR.kersMaxKJ)
        end
        local kersBottom20Percent = 1

        if lapKersPercent < 0.40 then
            kersBottom20Percent = lapKersPercent / 40 * 100
        end
        -- Battery Outline
        ui.drawRect(vec2(ers_pos_x, ers_pos_y),
            vec2(ers_pos_x + ers_dim_w, ers_pos_y + ers_dim_h),
            rgbm(1, 1, 1, 1), scale(2.5),
            ui.CornerFlags.All, scale(4))
        -- Battery Inner
        local h, s, l = percentage_to_hsl(kersBottom20Percent, 0, 120)
        local r, g, b = hslToRgb(h, s, l)
        ui.drawRectFilled(
            vec2(ers_pos_x + ers_padding_w, ers_pos_y + ers_padding_h),
            vec2(ers_pos_x + ers_padding_w + ers_inner_w * lapKersPercent, ers_pos_y + ers_padding_h + ers_inner_h),
            rgbm(math.max(r, 0.34), math.max(g, 0.34), math.max(b, 0.34), 1),
            0)
        if CAR.kersCharging then
            ui.setCursor(vec2(fuel_arc_pos_x + ers_arrow_offset_x - ers_arrow_w / 2, ers_arrow_pos_y))
            ui.image('img/triangle-green.png', vec2(ers_arrow_w, ers_arrow_h))
        else
            ui.setCursor(vec2(fuel_arc_pos_x - ers_arrow_offset_x - ers_arrow_w / 2, ers_arrow_pos_y))
            ui.image('img/triangle-red.png', vec2(ers_arrow_w, ers_arrow_h))
        end
    else
        ui.drawCircleFilled(vec2(fuel_icon_pos_x, fuel_icon_pos_y), scale(30), fuelColor, 20)
        ui.setCursor(vec2(fuel_icon_pos_x - scale(41 / 2), fuel_icon_pos_y - scale(41 / 2)))
        ui.image('img/icons/fuel_inner.png', scaledVec2(41, 41), fuelIconColor)
    end

    -- Odometer
    ui.drawRectFilled(vec2(odo_pos_x, odo_pos_y),
        vec2(odo_pos_x + odo_dim_w, odo_pos_y + odo_dim_h),
        rgbm(0, 0, 0, 0.2)
        , 5)
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')

    ui.setCursor(vec2(odo_pos_x, odo_pos_y))
    ui.dwriteTextAligned(string.format('%08.1f', math.round(CAR.distanceDrivenTotalKm, 1)), scale(32),
        ui.Alignment.Center,
        ui.Alignment.Center,
        vec2(odo_dim_w, odo_dim_h), false,
        rgbm(0.9, 0.9, 0.9, 1))
    ui.popDWriteFont()

    ------------------------- DRS -------------------------
    local drs_test     = false -- true to force on
    local drsPresent   = (drs_test) or CAR.drsPresent
    local drsAvailable = (drs_test) or CAR.drsAvailable
    local drsActive    = (drs_test) or CAR.drsActive

    x, y               = stx + scale(594), sty + scale(22)
    w, h               = scale(114), scale(48)
    if ANALOG_MODE then
        x, y = stx + scale(645), sty + scale(335)
    end
    local pad_x, pad_y = scale(7), scale(7)
    if drsPresent then
        local drsColor = rgbm(1, 1, 1, 0.2)
        local drsTextColor = rgbm(0, 0, 0, 0.4)
        if drsAvailable then
            drsColor = rgbm(1, 1, 1, 0.7)
            drsTextColor = rgbm(0, 0, 0, 0.7)
        end
        if drsActive then
            drsColor = rgbm(blinkingAlpha(9, 0.5), 0, 0, 0.6)
            drsTextColor = rgbm(0, 0, 0, 0.7)
        end
        ui.popDWriteFont()
        ui.pushDWriteFont('Arkitech:\\fonts;Weight=Bold')
        ui.drawRectFilled(
            vec2(x, y),
            vec2(x + w, y + h),
            drsColor, scale(6))

        ui.setCursor(vec2(x, y))
        ui.dwriteTextAligned('DRS', scale(25), ui.Alignment.Center, ui.Alignment.Center,
            vec2(w, h - scale(2)), false, drsTextColor)

        ui.drawRect(vec2(x + pad_x, y + pad_y),
            vec2(x + w - pad_x, y + h - pad_y), drsTextColor,
            scale(3), ui.CornerFlags.All, scale(2))
        ui.popDWriteFont()
        ui.pushDWriteFont('Arkitech:\\fonts;Weight=')
    end

    ------------------------- BOOST -------------------------

    if MY_DRIVER.carMaxEngineBoost > 0 then
        local boost = CAR.turboBoost

        local boost_img = 'img/turbo_background.png'
        x = stx + scale(758)
        y = sty + scale(37)

        if ANALOG_MODE then
            x = stx + scale(509)
            y = sty + scale(360)
        end

        local boost_img_dim_x, boost_img_dim_y = scale(228), scale(248)

        local boost_center_x = x + boost_img_dim_x / 2
        local boost_center_y = y + boost_img_dim_y / 2

        local boost_txt_dim_x, boost_txt_dim_y = scale(80), scale(30)

        local boost_hl_x, boost_hl_y = boost_center_x - scale(40), y + scale(8)
        local boost_ze_x, boost_ze_y = x + scale(10), boost_center_y - scale(18)
        local boost_min_x, boost_min_y = boost_center_x - scale(40), boost_center_y + scale(81)
        local boost_max_x, boost_max_y = boost_center_x + scale(90), boost_center_y - scale(18)


        ui.setCursor(vec2(x, y))
        ui.image(boost_img, vec2(boost_img_dim_x, boost_img_dim_y), rgbm.colors.white, true)
        local boostRad = math.round(-math.pi * (1 - boost / MY_DRIVER.carMaxEngineBoost), 4)

        ui.pathArcTo(vec2(boost_center_x, boost_center_y), scale(75.75), -math.pi, boostRad,
            100)
        ui.pathStroke(rgbm.colors.white, false, scale(9))
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=')
        -- boost half value
        ui.setCursor(vec2(boost_hl_x, boost_hl_y))
        ui.dwriteTextAligned(tostring(MY_DRIVER.carMaxEngineBoost / 2), scale(24), ui.Alignment.Center,
            ui.Alignment.Start,
            vec2(boost_txt_dim_x, boost_txt_dim_y), false, rgbm.colors.white)

        -- boost 0 value
        ui.setCursor(vec2(boost_ze_x, boost_ze_y))
        ui.dwriteTextAligned('0', scale(24), ui.Alignment.Start, ui.Alignment.Center,
            vec2(boost_txt_dim_x, boost_txt_dim_y), false
            , rgbm.colors.white)

        -- -- boost minus value
        ui.setCursor(vec2(boost_min_x, boost_min_y))
        ui.dwriteTextAligned('-' .. MY_DRIVER.carMaxEngineBoost / 2, scale(24), ui.Alignment.Center,
            ui.Alignment.End, vec2(boost_txt_dim_x, boost_txt_dim_y), false, rgbm.colors.white)

        -- -- boost max value
        ui.setCursor(vec2(boost_max_x, boost_max_y))
        ui.dwriteTextAligned(MY_DRIVER.carMaxEngineBoost, scale(24), ui.Alignment.Start, ui.Alignment.Center,
            vec2(boost_txt_dim_x, boost_txt_dim_y), false, rgbm.colors.white)
    end

    ------------------------- TYRES -------------------------

    stx = CFG.cx - scale(1244)
    sty = CFG.y + scale(45)
    if ANALOG_MODE then
        stx = CFG.cx - scale(1160)
        sty = CFG.y + scale(503)
    end


    x, y = stx + scale(10), sty + scale(10)
    w, h = scale(201), scale(217)
    local base_cx, base_cy = x + w / 2, y + h / 2



    ui.setCursor(vec2(x, y))
    ui.image('img/damage/base.png', vec2(w, h), rgbm(1, 1, 1, 0.8), true)
    local flDmg = scaleDamageValue(CAR.wheels[0].suspensionDamage)
    local flColor = rgbm(1, 1 - flDmg, 1 - flDmg, 1 * flDmg + 0.8)

    ui.setCursor(vec2(x, y))
    ui.image('img/damage/fl.png', vec2(w, h), flColor, true)

    local frDmg = scaleDamageValue(CAR.wheels[1].suspensionDamage)
    local frColor = rgbm(1, 1 - frDmg, 1 - frDmg, 1 * frDmg + 0.8)
    ui.setCursor(vec2(x, y))
    ui.image('img/damage/fr.png', vec2(w, h), frColor, true)

    local frontDmg = CAR.damage[0] / 100
    local frontColor = rgbm(1, 1 - frontDmg, 1 - frontDmg, 1 * frontDmg + 0.8)
    ui.setCursor(vec2(x, y))
    ui.image('img/damage/front.png', vec2(w, h), frontColor, true)

    local engDmg = (1000 - CAR.engineLifeLeft) / 1000
    local engColor = rgbm(1, 1 - engDmg, 1 - engDmg, 1 * engDmg + 0.8)
    ui.setCursor(vec2(x, y))
    ui.image('img/damage/engine.png', vec2(w, h), engColor, true)

    local rrDmg = scaleDamageValue(CAR.wheels[3].suspensionDamage)
    local rrColor = rgbm(1, 1 - rrDmg, 1 - rrDmg, 1 * rrDmg + 0.8)
    ui.setCursor(vec2(x, y))
    ui.image('img/damage/rr.png', vec2(w, h), rrColor, true)

    local rlDmg = scaleDamageValue(CAR.wheels[2].suspensionDamage)
    local rlColor = rgbm(1, 1 - rlDmg, 1 - rlDmg, 1 * rlDmg + 0.8)
    ui.setCursor(vec2(x, y))
    ui.image('img/damage/rl.png', vec2(w, h), rlColor, true)

    local rearDmg = CAR.damage[1] / 100
    local rearColor = rgbm(1, 1 - rearDmg, 1 - rearDmg, 1 * rearDmg + 0.8)
    ui.setCursor(vec2(x, y))
    ui.image('img/damage/rear.png', vec2(w, h), rearColor, true)

    local wear_fl = math.max(0, math.round(CAR.wheels[0].tyreWear, 3))
    local wear_fr = math.max(0, math.round(CAR.wheels[1].tyreWear, 3))
    local wear_rl = math.max(0, math.round(CAR.wheels[2].tyreWear, 3))
    local wear_rr = math.max(0, math.round(CAR.wheels[3].tyreWear, 3))

    local temp_fl = math.round(CAR.wheels[0].tyreMiddleTemperature, 3)
    local temp__opt_fl = math.round(CAR.wheels[0].tyreOptimumTemperature, 3)
    local temp__pct_fl = math.min(math.max(0, temp_fl / temp__opt_fl), 2)
    local temp_fr = math.round(CAR.wheels[1].tyreMiddleTemperature, 3)
    local temp__opt_fr = math.round(CAR.wheels[1].tyreOptimumTemperature, 3)
    local temp__pct_fr = math.min(math.max(0, temp_fr / temp__opt_fr), 2)
    local temp_rl = math.round(CAR.wheels[2].tyreMiddleTemperature, 3)
    local temp__opt_rl = math.round(CAR.wheels[2].tyreOptimumTemperature, 3)
    local temp__pct_rl = math.min(math.max(0, temp_rl / temp__opt_rl), 2)
    local temp_rr = math.round(CAR.wheels[3].tyreMiddleTemperature, 3)
    local temp__opt_rr = math.round(CAR.wheels[3].tyreOptimumTemperature, 3)
    local temp__pct_rr = math.min(math.max(0, temp_rr / temp__opt_rr), 2)

    stx = base_cx
    sty = base_cy

    local off_x, off_y = scale(75), scale(84)
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Bold')
    ui.setCursor(vec2(stx - off_x, sty - off_y - scale(33)))
    ui.dwriteTextAligned(ac.getTyresName(SIM.focusedCar), scale(30), ui.Alignment.Center,
        ui.Alignment.Center,
        scaledVec2(150, 33), false,
        rgbm(0, 0, 0, 1))

    ui.setCursor(vec2(stx - off_x, sty + off_y))
    ui.dwriteTextAligned(ac.getTyresName(SIM.focusedCar), scale(30), ui.Alignment.Center,
        ui.Alignment.Center,
        scaledVec2(150, 33), false,
        rgbm(0, 0, 0, 1))

    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.setCursor(vec2(stx - off_x, sty - off_y - scale(33)))
    ui.dwriteTextAligned(ac.getTyresName(SIM.focusedCar), scale(30), ui.Alignment.Center,
        ui.Alignment.Center,
        scaledVec2(150, 33), false,
        rgbm(1, 1, 1, 0.9))
    ui.setCursor(vec2(stx - off_x, sty + off_y))
    ui.dwriteTextAligned(ac.getTyresName(SIM.focusedCar), scale(30), ui.Alignment.Center,
        ui.Alignment.Center,
        scaledVec2(150, 33), false,
        rgbm(1, 1, 1, 0.9))


    off_x = scale(96)
    local off_top = scale(92)
    local off_bot = scale(89)
    local tyre_w, tyre_h = scale(24), scale(72)
    ui.drawRectFilled(vec2(stx - off_x, sty - off_top),
        vec2(stx - off_x + tyre_w, sty - off_top + tyre_h),
        rgbm(1, 0, 0, 0.8))
    ui.drawRectFilled(vec2(stx - off_x, sty + off_bot - tyre_h),
        vec2(stx - off_x + tyre_w, sty + off_bot),
        rgbm(1, 0, 0, 0.8))

    ui.drawRectFilled(vec2(stx + off_x - tyre_w, sty - off_top),
        vec2(stx + off_x, sty - off_top + tyre_h),
        rgbm(1, 0, 0, 0.8))

    ui.drawRectFilled(vec2(stx + off_x - tyre_w, sty + off_bot - tyre_h),
        vec2(stx + off_x, sty + off_bot),
        rgbm(1, 0, 0, 0.8))

    ui.drawRectFilled(vec2(stx - off_x, sty - off_top + (tyre_h * wear_fl)),
        vec2(stx - off_x + tyre_w, sty - off_top + tyre_h),
        rgbm.colors.white)
    ui.drawRectFilled(vec2(stx - off_x, sty + off_bot - tyre_h + (tyre_h * wear_rl)),
        vec2(stx - off_x + tyre_w, sty + off_bot),
        rgbm.colors.white)

    ui.drawRectFilled(vec2(stx + off_x - tyre_w, sty - off_top + (tyre_h * wear_fr)),
        vec2(stx + off_x, sty - off_top + tyre_h),
        rgbm.colors.white)

    ui.drawRectFilled(vec2(stx + off_x - tyre_w, sty + off_bot - tyre_h + (tyre_h * wear_rr)),
        vec2(stx + off_x, sty + off_bot),
        rgbm.colors.white)


    off_x = off_x + scale(11)
    off_top = off_top + scale(10)
    off_bot = off_bot + scale(10)
    local out_w, out_h = scale(46), scale(94)
    function getColor(percent)
        local pct = percent
        local r, g, b, a = 0.2, 0.2, 0.2, 0.9 -- start with white
        if percent < 1 and INI.extendedTyreTemps then
            pct = math.max(percent, 0.5)
            r = math.max(0.2, math.min((pct) * 1.5, 0.9) * 0.3)
            g = math.max(0.2, math.min((pct) * 1.5, 0.9) * 0.3)
            b = math.min((pct) * 1.5, 0.9)
            a = math.max(0.15, (1.2 - pct) * 1.5)
        else
            r = math.min((pct - 1) * 1.5, 0.9)
            g = 0
            b = 0
            a = math.max(0.15, (pct - 1) * 1.5)
        end
        return rgbm(r, g, b, a)
    end

    local color_fl = getColor(temp__pct_fl)
    local color_rl = getColor(temp__pct_rl)
    local color_fr = getColor(temp__pct_fr)
    local color_rr = getColor(temp__pct_rr)


    ui.setCursor(vec2(stx - off_x, sty - off_top))
    ui.image('img/tyre-outline.png', vec2(out_w, out_h), color_fl, false)
    ui.setCursor(vec2(stx - off_x, sty + off_bot - out_h))
    ui.image('img/tyre-outline.png', vec2(out_w, out_h), color_rl, false)

    ui.setCursor(vec2(stx + off_x - out_w, sty - off_top))
    ui.image('img/tyre-outline.png', vec2(out_w, out_h), color_fr, false)
    ui.setCursor(vec2(stx + off_x - out_w, sty + off_bot - out_h))
    ui.image('img/tyre-outline.png', vec2(out_w, out_h), color_rr, false)


    -- Rain Gauge

    -- stx = CFG.cx - scale(1244)
    -- sty = CFG.y + scale(45)
    -- if ANALOG_MODE then
    --     stx = CFG.cx - scale(1160)
    --     sty = CFG.y + scale(502)
    -- end

    stx = CFG.cx - scale(1384)
    sty = CFG.y + scale(67)

    if ANALOG_MODE then
        stx = CFG.cx - scale(1304)
        sty = CFG.y + scale(525)
    end

    x, y = stx + scale(30), sty
    w, h = scale(62), scale(190)
    local bar_x, bar_y = stx + scale(54), sty + scale(11)
    local bar_w, bar_h = scale(24), scale(168)
    local icon_w, icon_h = scale(44), scale(42)
    local icon_x, icon_y = stx + scale(0), y + h / 2 - icon_h / 2

    ui.setCursor(vec2(x, y))
    ui.image('img/gauge_rain.png', vec2(w, h), rgbm(1, 1, 1, 1), false)


    if SIM.rainWetness > 0 then
        ui.drawRectFilled(vec2(bar_x, bar_y + bar_h * (1 - SIM.rainWetness)),
            vec2(bar_x + bar_w, bar_y + bar_h), rgbm.colors.white, 1)
    end

    ui.setCursor(vec2(icon_x, icon_y))
    ui.image('img/icons/rain_icon.png', vec2(icon_w, icon_h), rgbm(1, 1, 1, 1), true)



    ui.popDWriteFont()
    -- ui.setAsynchronousImagesLoading(false)
end

function script.tachHUD()
    if INI.fixedPos then
        local centerx = ac.getUI().windowSize.x / 2
        local maxX = ac.getUI().windowSize.x
        local maxY = ac.getUI().windowSize.y

        local width = scale(2760)
        local height = scale(362)





        if ANALOG_MODE then height = scale(824) end


        ui.transparentWindow('tachHUDFixed',
            vec2(centerx - width / 2, maxY - height),
            scaledVec2(maxX / LBS, maxY + height), true,

            function()
                drawTachHUDOld()
            end)
    else
        drawTachHUDOld()
    end
end
