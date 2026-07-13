MFD_PAGE = 1
GAMEPAD_INDEX = nil
WHEEL_INDEX = nil
CONTROL_MODE = nil

FixedBtnLeft = { x = 0, y = 0, w = 0, h = 0 }
FixedBtnRight = { x = 0, y = 0, w = 0, h = 0 }
FixedBtnUp = { x = 0, y = 0, w = 0, h = 0 }
FixedBtnDown = { x = 0, y = 0, w = 0, h = 0 }
MFD_MODULES = nil

function drawMultiFunctionDisplay()
    MFD_MODULES = table.filter(INI.mfdModules, function(v)
        return v ~= '---'
    end)

    local CFG = {
        width = scale(604),
        height = scale(533),
        x = scale(0),
        y = scale(0),
        cx = nil,
        cy = nil,

        arrow_button_size = scale(21),
        arrow_icon_size = scale(20)
    }
    CFG.cx = CFG.width / 2
    CFG.cy = CFG.height / 2

    if CONTROL_MODE == nil then
        CONTROL_MODE = CFGControls.data.HEADER.INPUT_METHOD or 'KEYBOARD'
    end

    if GAMEPAD_INDEX == nil and CFGControls then
        GAMEPAD_INDEX = CFGControls.data.X360.JOYPAD_INDEX
    end
    if WHEEL_INDEX == nil and CFGControls then
        if CFGControls.data.WHEEL.WHEEL_INDEX ~= '' then
            WHEEL_INDEX = CFGControls.data.WHEEL.WHEEL_INDEX
        end
    end
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    listenToUIInputs()

    ui.setCursor(vec2(0, 0))
    -- ui.image('img/placeholder.png', vec2(CFG.width, CFG.height), rgbm(1, 1, 1, 0.1))
    ui.image('img/mfd_background.png', vec2(CFG.width, CFG.height), rgbm(1, 1, 1, 1))

    ---------------------------- Nav Buttons ----------------------------
    local stx, sty = CFG.x, CFG.y
    local padding_x, padding_y = scale(26), scale(44)
    stx = CFG.x + padding_x
    sty = CFG.y + padding_y


    if CFGControls.data[CONTROL_MODE]['MFD_LEFT'] == '' or
        CFGControls.data[CONTROL_MODE]['MFD_RIGHT'] == ''
        or CFGControls.data[CONTROL_MODE]['MFD_UP'] == ''
        or CFGControls.data[CONTROL_MODE]['MFD_DOWN'] == '' then
        ui.drawRectFilled(vec2(CFG.x, CFG.y), vec2(CFG.x + CFG.width, CFG.y + CFG.height), rgbm(0, 0, 0, 0.5), scale(10))

        ui.setCursor(vec2(CFG.x, CFG.y))
        ui.dwriteTextAligned('No Keybinds Assigned, Assign them in the Config UI', scale(32), 0, 0,
            vec2(CFG.width, CFG.height), true, rgbm.colors.red)
        return
    end


    local active_mfd_module = MFD_MODULES[MFD_PAGE]
    local mfd_page_text = MFD_NAMES_MAP[active_mfd_module]
    if active_mfd_module == 'TCS' then
        --- TCS Page
        local min = 0
        local max = 1
        local current = 0
        local debounceMethod = function(tc) ac.setTC(tc) end

        if CAR.tractionControlModes > 0 then
            current = CAR.tractionControlMode
            min = 0
            max = CAR.tractionControlModes

            listenToInputs(
                debounceMethod,
                current,
                min,
                max,
                1)
        end


        drawMFDControlModule(CFG, current, 0, max - 1, max, rgbm(0.1, 0.62, 0.83, 1), 'LOW',
            'HIGH',
            true, debounceMethod, not (CAR.tractionControlModes > 0))
    elseif active_mfd_module == 'ABS' then
        --- ABS Page
        local min = 0
        local max = 1
        local current = 0

        local debounceMethod = function(abs) ac.setABS(abs) end
        if CAR.absModes > 0 then
            current = CAR.absMode
            min = 0
            max = CAR.absModes

            listenToInputs(
                debounceMethod,
                current,
                min,
                max,
                1)
        end


        drawMFDControlModule(CFG, current, 0, max - 1, max,
            rgbm.from0255(255, 255, 82, 1), 'LOW',
            'HIGH',
            true,
            debounceMethod, not (CAR.absModes > 0))
    elseif active_mfd_module == 'BB' then
        --- BRAKE BALANCE Page

        local min = CAR.brakesBiasLimitUp
        local max = CAR.brakesBiasLimitDown

        local total_steps = 11
        local step = (max - min) / total_steps
        local center = (max - min) / 2
        local norm_val = math.round((CAR.brakeBias - min) / step)

        local debounceMethod = function(bb)
            local un_norm_bb = min + (bb + 6) * step
            ac.setBrakeBias(un_norm_bb)
        end
        listenToInputs(
            debounceMethod,
            norm_val - 6,
            -5,
            5,
            1)

        drawMFDControlModule(CFG,
            norm_val - 6,
            -5,
            5,
            11,
            rgbm.from0255(242, 18, 68, 1),
            'FRONT',
            'REAR',
            not CAR.hasUserBrakeBias,
            debounceMethod)
    elseif active_mfd_module == 'FUEL' then
        --- FUEL Page
        local debounceMethod = function(tc)
            ac.setMGUKDelivery(tc)
        end
        local min = 0
        local max = 5
        local current = 5
        if CAR.mgukDeliveryCount > 1 then
            min = 0
            current = CAR.mgukDelivery
            max = CAR.mgukDeliveryCount - 1

            listenToInputs(
                debounceMethod,
                current,
                min,
                max,
                1)
        end
        drawMFDControlModule(CFG,
            current,
            min,
            max - 1,
            max,
            rgbm.from0255(49, 210, 52, 1), 'LEAN',
            'POWER',
            true,
            debounceMethod, CAR.mgukDeliveryCount > 1 and false or true)
        local x, y = CFG.cx, CFG.y

        if not ANALOG_MODE then
            y = CFG.y - scale(70)
        end

        local rect_x, rect_y = -scale(34), scale(355)
        local rect_w, rect_h = scale(255), scale(100)

        local pos_x = x + rect_x
        local pos_y = y + rect_y
        ui.drawRectFilled(vec2(pos_x - rect_w, pos_y), vec2(pos_x, pos_y + rect_h), rgbm(0, 0, 0, 0.2))

        local title_x, title_y = scale(14), scale(10)
        local sub_x, sub_y = scale(110), scale(53)
        ui.setCursor(vec2(pos_x - rect_w + title_x, pos_y + title_y))
        ui.dwriteText('Laps Remaining', scale(28))

        local sub_text = CAR.fuelPerLap ~= 0 and string.format('%0.1f', math.round(CAR.fuel / CAR.fuelPerLap, 1)) or
            '...'
        ui.setCursor(vec2(pos_x - rect_w + sub_x, pos_y + sub_y))
        ui.dwriteTextAligned(sub_text, scale(34),
            ui.Alignment.End, ui.Alignment.Center,
            vec2(rect_w / 2, scale(34)))

        pos_x = x - rect_x
        pos_y = y + rect_y
        ui.drawRectFilled(vec2(pos_x + rect_w, y + rect_y), vec2(pos_x, y + rect_y + rect_h), rgbm(0, 0, 0, 0.2))
        ui.setCursor(vec2(pos_x + title_x, pos_y + title_y))
        ui.dwriteText('Fuel', scale(28))

        ui.setCursor(vec2(pos_x + sub_x, pos_y + sub_y))
        ui.dwriteTextAligned(math.round(CAR.fuel / CAR.maxFuel * 100) .. '%', scale(34), ui.Alignment.End,
            ui.Alignment.Center,
            vec2(rect_w / 2, scale(34)))
    elseif active_mfd_module == 'SB' then
        drawMFDBestLapModule(CFG)
    elseif active_mfd_module == 'RADAR' then
        drawMFDRadarModule(CFG)
    end

    -------------------- NAV ARROWS ---------------------
    local base_x, base_y = stx, sty

    if not ANALOG_MODE then
        base_y = sty + scale(390)
    end
    ui.drawCircleFilled(
        vec2(base_x, base_y),
        CFG.arrow_button_size,
        rgbm(0, 0, 0, 0.2),
        24
    )

    local arrow_x, arrow_y = CFG.arrow_icon_size / 2, CFG.arrow_icon_size / 2
    ui.setCursor(vec2(stx - arrow_x, base_y - arrow_y))
    ui.image('img/arrow-left.png',
        vec2(CFG.arrow_icon_size, CFG.arrow_icon_size),
        rgbm(1, 1, 1, 1),
        true
    )
    if ui.itemClicked(ui.MouseButton.Left) then
        debounceValues(function(page)
            MFD_PAGE = page
        end, 200, math.max(1, MFD_PAGE - 1))
    end

    local mfd_mode_text_size = scale(32)
    ui.setCursor(vec2(0, base_y - (mfd_mode_text_size / 2)))
    ui.dwriteTextAligned(mfd_page_text, mfd_mode_text_size, ui.Alignment.Center, ui.Alignment.Center,
        vec2(CFG.width, mfd_mode_text_size), false,
        rgbm.colors.white)

    base_x = CFG.width - padding_x
    ui.drawCircleFilled(
        vec2(base_x, base_y),
        CFG.arrow_button_size,
        rgbm(0, 0, 0, 0.2),
        24
    )
    FixedBtnLeft = {
        x = stx - arrow_x - scale(10),
        y = base_y - arrow_y - scale(10),
        w = CFG.arrow_icon_size + scale(20),
        h = CFG.arrow_icon_size +
            scale(20)
    }

    ui.beginRotation()
    local arrow_x, arrow_y = CFG.arrow_icon_size / 2, CFG.arrow_icon_size / 2
    ui.setCursor(vec2(base_x - arrow_x, base_y - arrow_y))
    ui.image('img/arrow-left.png',
        vec2(CFG.arrow_icon_size, CFG.arrow_icon_size),
        rgbm(1, 1, 1, 1),
        true
    )
    ui.endRotation(-90)
    if ui.itemClicked(ui.MouseButton.Left) then
        debounceValues(function(page)
            MFD_PAGE = page
        end, 200, math.min(5, MFD_PAGE + 1))
    end
    FixedBtnRight = {
        x = base_x - arrow_x - scale(10),
        y = base_y - arrow_y - scale(10),
        w = CFG.arrow_icon_size + scale(20),
        h = CFG.arrow_icon_size + scale(20)
    }


    ---- Page Dots
    stx = CFG.cx
    sty = CFG.height
    local circle_d = scale(7.5)
    local circle_color = rgbm.colors.red

    local row_w, row_h = scale(780), scale(10)
    local row_y = sty - row_h - scale(41)
    local row_cx = stx

    local circle_x_values = {}
    local moduleCount = #MFD_MODULES
    local offset = row_w / moduleCount

    if moduleCount == 1 then
        circle_x_values = { 0 }
    else
        local startPosOnUi = -(moduleCount - 1) * offset / 2

        for i = 1, moduleCount do
            circle_x_values[i] = startPosOnUi + (i - 1) * offset
        end
    end

    for i, circle_x in ipairs(circle_x_values) do
        if MFD_PAGE == i then
            circle_color = rgbm.colors.white
            circle_d = scale(9.5)
        else
            circle_color = rgbm(0, 0, 0, 0.2)
            circle_d = scale(7.5)
        end
        ui.drawCircleFilled(vec2(row_cx + scale(circle_x), row_y), circle_d, circle_color, 10)
    end
end

function drawMFDControlModule(CFG, current, min, max, totalSteps, color, minText, maxText, is_filled, dounceMethod,
                              is_fake)
    -- starting point
    local stx, sty = CFG.cx, CFG.cy

    if not ANALOG_MODE then
        sty = sty - scale(50)
    end
    local arc_size = scale(194)
    local arc_thickness = scale(10)

    local arc_size_inner = arc_size - scale(19)
    local arc_thickness_inner = scale(13)

    local totalAngle = 123
    local startAngle = -(totalAngle / 2)


    -- circle center point y
    local ccpy = sty + scale(40)
    ui.pathArcTo(vec2(stx, ccpy), arc_size, getAngle(startAngle), getAngle((totalAngle / 2)), 100)
    ui.pathStroke(color, false, arc_thickness)

    for i = min, max, 1 do
        local toAngle
        if i == min then
            toAngle = startAngle + (totalAngle * 1 / totalSteps)
        else
            toAngle = startAngle + (totalAngle * 1 / totalSteps) - 1.5
        end
        ui.pathArcTo(vec2(stx, ccpy), arc_size_inner, getAngle(startAngle), getAngle(toAngle), 100)

        local cts = rgbm(1, 1, 1, 0.1)
        if is_filled then
            cts = (i + 1 <= current) and rgbm.colors.white or rgbm(1, 1, 1, 0.1)
        else
            cts = (i == current) and rgbm.colors.white or rgbm(1, 1, 1, 0.1)
        end
        ui.pathStroke(cts, false, arc_thickness_inner)

        startAngle = toAngle + 1.5
    end


    if not is_fake then
        local btn_y = scale(70)

        ui.drawCircleFilled(
            vec2(stx, sty - btn_y),
            CFG.arrow_button_size,
            rgbm(0, 0, 0, 0.2),
            24
        )

        local arrow_x, arrow_y = CFG.arrow_icon_size / 2, (CFG.arrow_icon_size / 2)
        ui.setCursor(vec2(stx - arrow_x, sty - arrow_y - btn_y))
        ui.beginRotation()
        ui.image('img/arrow-left.png',
            vec2(CFG.arrow_icon_size, CFG.arrow_icon_size),
            rgbm(1, 1, 1, 1),
            true
        )
        ui.endRotation(0)
        if ui.itemClicked(ui.MouseButton.Left) then
            dounceMethod(math.min(totalSteps, current + 1))
        end
        FixedBtnUp = {
            x = stx - arrow_x - scale(10),
            y = sty - arrow_y - btn_y - scale(10),
            w = CFG.arrow_icon_size + scale(20),
            h = CFG.arrow_icon_size + scale(20)
        }

        ui.drawCircleFilled(
            vec2(stx, sty + btn_y),
            CFG.arrow_button_size,
            rgbm(0, 0, 0, 0.2),
            24
        )

        ui.setCursor(vec2(stx - arrow_x, sty - arrow_y + btn_y))
        ui.beginRotation()
        ui.image('img/arrow-left.png',
            vec2(CFG.arrow_icon_size, CFG.arrow_icon_size),
            rgbm(1, 1, 1, 1),
            true
        )
        ui.endRotation(180)
        if ui.itemClicked(ui.MouseButton.Left) then
            dounceMethod(math.max(min, current - 1))
        end
        FixedBtnDown = {
            x = stx - arrow_x - scale(10),
            y = sty - arrow_y + btn_y - scale(10),
            w = CFG.arrow_icon_size + scale(20),
            h = CFG.arrow_icon_size + scale(20)
        }
    end


    local text_size = scale(32)
    local text_x, text_y = scale(165), scale(14)
    local text_w, text_h = text_size * 4, text_size
    local low_x = text_w / 2

    ui.setCursor(vec2(stx - text_x - low_x, sty - text_y))
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Light')
    ui.dwriteTextAligned(
        minText,
        text_size, ui.Alignment.Center, ui.Alignment.Center,
        vec2(text_w, text_h),
        false,
        rgbm(1, 1, 1, 0.8)
    )

    ui.setCursor(vec2(stx + text_x - low_x, sty - text_y))
    ui.dwriteTextAligned(
        maxText,
        text_size, ui.Alignment.Center, ui.Alignment.Center,
        vec2(text_w, text_h),
        false,
        rgbm(1, 1, 1, 0.8)
    )
    ui.popDWriteFont()

    local text_size = scale(84)
    local text_w, text_h = text_size * 2, text_size
    local text_x, text_y = text_w / 2, text_h / 2
    ui.setCursor(vec2(stx - text_x, sty - text_y))

    ui.pushDWriteFont('Arkitech:\\fonts;Weight=Medium')
    ui.dwriteTextAligned(
        tostring(current),
        text_size,
        ui.Alignment.Center,
        ui.Alignment.Center,
        vec2(text_w, text_h),
        false,
        rgbm.colors.white
    )

    ui.popDWriteFont()
end

local config = ac.storage {
    tilt = 0.7,
    fov = 0,
    range = 1.3,
}

local drawMesh_track = {
    mesh = ac.SimpleMesh.trackLine(0, 0, 1),
    values = { gSize = vec2() },
    shader =
    'res/track.fx'
}
local drawMesh_pits = { mesh = ac.SimpleMesh.trackLine(1, 0, 1), values = { gSize = vec2() }, shader = 'res/track.fx' }

local focusedCar ---@type ac.StateCar
---@type fun(car: ac.StateCar, alpha: number)
local drawCall_car =
    (function(params, car, alpha) ---@param car ac.StateCar
        local up = vec3(0, 2.5, 0)
        local right = car.look:clone():cross(up)
        local test = car.look * 7.5
        local c1 = car.position - right
        local c2 = car.position + right
        local c3 = (car.position + test + right)
        local c4 = (car.position + test - right)
        render.setBlendMode(render.BlendMode.AlphaBlend)
        render.setDepthMode(2)
        render.setCullMode(render.CullMode.None)

        local arrowTexture
        if focusedCar == car then
            arrowTexture = 'img/arrow-red.png'
        else
            arrowTexture = 'img/arrow-blue.png'
        end
        render.quad(c1, c2, c3, c4, rgbm.colors.white, arrowTexture)
    end):bind({ transform = 'original', textures = {}, values = {}, defines = { MODE = 1 }, shader = 'res/car.fx', cacheKey = 1 })


---@type fun(car: ac.StateCar, alpha: number)
local drawCall_pit = (function(params, car, alpha) ---@param car ac.StateCar
    params.mesh = ac.SimpleMesh.carCollider(car.index)
    params.values.gAlpha = alpha ^ 2
    local t = params.transform ---@type mat4x4
    t:set(car.pitTransform)
    t:mulSelf(mat4x4.scaling(vec3(1, 0, 1)))
    t:mulSelf(mat4x4.translation(vec3(0, car.pitTransform.position.y, 0)))
    render.mesh(params)
end):bind({ transform = mat4x4(), textures = {}, values = {}, shader = 'res/pit.fx' })



local canvasScene = {
    opaque = function()
        render.setDepthMode(render.DepthMode.Off)
        render.mesh(drawMesh_pits)
        render.mesh(drawMesh_track)

        render.setDepthMode(render.DepthMode.Normal)
        if focusedCar.pitTransform.position:closerToThan(focusedCar.position, distanceCap) then
            local alpha = 1 -
                focusedCar.pitTransform.position:distanceSquared(focusedCar.position) / (distanceCap * distanceCap)
            alpha = math.lerpInvSat(alpha, 0, 0.5)
            drawCall_pit(focusedCar, alpha)
        end

        local fn = drawCall_car
        for _, c in ac.iterateCars.ordered() do
            if not c.position:closerToThan(focusedCar.position, distanceCap * 2) then
                return
            end
            fn(c, 1)
        end
    end
}

local canvas ---@type ac.GeometryShot
local camDir = vec3()
local camPos = vec3()
local fading = 1
local lastSizeKey = 0
local actualFOV = 0
local actualDistance = 0

local function updateValues()
    distanceCap = math.lerp(5, 150, config.range ^ 2) * math.lerp(1.4, 2.2, config.tilt)
    actualFOV = 5 + 40 * config.fov
    actualDistance = -math.lerp(5, 15, config.range ^ 2) * math.lerp(1.6, 1, config.tilt) /
        math.tan(math.rad(actualFOV) / 2)
end

updateValues()

local outputCommand = {
    p1 = vec2(),
    p2 = vec2(),
    blendMode = render.BlendMode.AlphaBlend,
    textures = { txImage = '' },
    values = { gAlpha = 1 },
    shader = 'res/output.fx'
}

function drawMFDRadarModule(CFG)
    -- starting point
    local padding_x, padding_y = scale(0), scale(100)
    if not ANALOG_MODE then
        padding_y = scale(30)
    end
    local stx = CFG.x + padding_x
    local sty = CFG.y + padding_y
    local radar_x = stx + scale(31)
    local radar_y = sty + scale(40)
    ui.setCursor(vec2(radar_x, radar_y))
    ui.image('img/radar-background.png', vec2(scale(542), scale(321)), rgbm(1, 1, 1, 0.9))

    if not focusedCar or focusedCar.index ~= SIM.closelyFocusedCar then
        focusedCar = CAR or ac.getCar(0)
    end
    local windowFading = ac.windowFading()
    fading = 1

    ui.setCursor(vec2(0, padding_y - scale(40)))
    local c = ui.getCursor()
    local s = vec2(CFG.width, CFG.height - scale(120))
    if lastSizeKey ~= s.x * 1e4 + s.y then
        lastSizeKey = s.x * 1e4 + s.y

        if canvas then canvas:dispose() end
        local size = s:clone():scale(2)
        canvas = ac.GeometryShot(canvasScene, size, 1, true, render.AntialiasingMode.None)
        canvas:setShadersType(render.ShadersType.Simplest)
        canvas:setClippingPlanes(1, 1000)
        drawMesh_track.values.gSize = 2 / size
        drawMesh_pits.values.gSize = 2 / size
        outputCommand.textures.txImage = canvas
    end

    camDir:setScaled(CAR.up, -1):addScaled(CAR.look, config.tilt)
    camPos:set(CAR.position):addScaled(camDir, actualDistance)
    canvas:update(camPos, camDir, CAR.look, actualFOV)

    outputCommand.p1:set(c)
    outputCommand.p2:set(c):add(s)
    outputCommand.values.gAlpha = fading
    ui.renderShader(outputCommand)


    ui.pushStyleVarAlpha(1 - windowFading)
    ui.beginOutline()
    for x = 0, 1 do
        for y = 0, 1 do
            local p = vec2(c.x + x * s.x, c.y + s.y * y)
            ui.pathLineTo(p + vec2(x == 0 and 20 or -20, 0))
            ui.pathLineTo(p)
            ui.pathLineTo(p + vec2(0, y == 0 and 20 or -20))
            ui.pathStroke(rgbm.colors.white, false, 1)
        end
    end

    ui.endOutline(rgbm.colors.black, 1 - windowFading)
    ui.popStyleVar()
end

function drawMFDBestLapModule(CFG)
    -- starting point
    local padding_x, padding_y = scale(0), scale(100)
    if not ANALOG_MODE then
        padding_y = scale(30)
    end
    local stx = CFG.x + padding_x
    local sty = CFG.y + padding_y

    local box_spacing_x, box_spacing_y = scale(2), scale(5)
    local sector_text_size = scale(35)
    local base_h = scale(54)
    local sector_x, sector_y = stx + scale(85), sty
    local sector_w, sector_h = scale(80), base_h

    local time_x, time_y = sector_x + sector_w + box_spacing_x, sty
    local time_w, time_h = scale(245), base_h

    local gap_x, gap_y = time_x + time_w + box_spacing_x, sty
    local gap_w, gap_h = scale(175), base_h

    local box_rounding = scale(4)

    ---- SPLITS
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    local lb_y = 0
    for i = 1, math.min(3, #SIM.lapSplits), 1 do
        local index = '--'
        local timeGap
        local time
        if CAR.currentSector >= i or CAR.currentSector + 1 == 1 then
            time = MY_DRIVER.previousSectors[i]
            if time == nil or time == 9999999 then
                time = nil
            end
            if MY_DRIVER.fastestSectors[i] ~= 9999999 then
                timeGap = time - MY_DRIVER.fastestSectors[i]
            end

            if timeGap == 0 and MY_DRIVER.prevFastestSectors[i] ~= 9999999 then
                timeGap = MY_DRIVER.fastestSectors[i] - MY_DRIVER.prevFastestSectors[i]
            end
            if timeGap == 0 then
                timeGap = nil
            end
            if time then
                index = tostring(i)
            end
        end
        local gapColor = rgbm(0, 0, 0, 0.5)
        if timeGap ~= nil then
            gapColor = timeGap > 0 and rgbm(1, 0, 0, 0.8) or rgbm(0, 0.38, 0.82, 0.8)
        end


        ui.drawRectFilled(vec2(sector_x, lb_y + sector_y), vec2(sector_x + sector_w, lb_y + sector_y + sector_h),
            rgbm(1, 1, 1, 0.7),
            box_rounding)
        ui.setCursor(vec2(sector_x, lb_y + sector_y))
        ui.dwriteTextAligned(index, sector_text_size, ui.Alignment.Center, ui.Alignment.Center,
            vec2(sector_w, sector_h), false, rgbm(0, 0, 0, 1))


        ui.drawRectFilled(vec2(time_x, lb_y + time_y), vec2(time_x + time_w, lb_y + time_y + time_h),
            rgbm(0, 0, 0, 0.5),
            box_rounding)
        ui.setCursor(vec2(time_x, lb_y + time_y))
        ui.dwriteTextAligned(secondsTimeToString(time), sector_text_size, ui.Alignment.End, ui.Alignment.Center,
            vec2(time_w - scale(20), time_h), false, rgbm(1, 1, 1, 1))

        ui.drawRectFilled(vec2(gap_x, lb_y + gap_y), vec2(gap_x + gap_w, lb_y + gap_y + gap_h), gapColor,
            box_rounding)
        ui.setCursor(vec2(gap_x, lb_y + gap_y))
        ui.dwriteTextAligned(gapToString(timeGap), sector_text_size, ui.Alignment.End, ui.Alignment.Center,
            vec2(gap_w - scale(20), gap_h), false, rgbm(1, 1, 1, 1))
        lb_y = lb_y + base_h + box_spacing_y
    end

    local name_text_size = scale(26)
    sector_x             = stx + scale(10)
    sector_w             = scale(155)
    time_x               = sector_x + sector_w + box_spacing_x

    local fastestLap     = (MY_DRIVER.fastestLap)
    local lastLap        = MY_DRIVER.laps[MY_DRIVER.currentLap - 1]


    local lastLapTime
    if lastLap then
        lastLapTime = lastLap.lapTime
    end


    local timeGap
    if lastLapTime and fastestLap and fastestLap ~= -1 then
        timeGap = lastLapTime - fastestLap
    end
    if lastLapTime == fastestLap and MY_DRIVER.prevFastestLap ~= -1 then
        timeGap = lastLapTime - MY_DRIVER.prevFastestLap
    end

    if timeGap == 0 then
        timeGap = nil
    end


    --- LAST LAP
    local gapColor = rgbm(0, 0, 0, 0.5)
    if timeGap ~= nil then
        gapColor = timeGap > 0 and rgbm(1, 0, 0, 0.8) or rgbm(0, 0.38, 0.82, 0.8)
    end

    ui.drawRectFilled(vec2(sector_x, lb_y + sector_y), vec2(sector_x + sector_w, lb_y + sector_y + sector_h),
        rgbm(1, 1, 1, 0.7),
        box_rounding)
    ui.setCursor(vec2(sector_x, lb_y + sector_y))
    ui.dwriteTextAligned('LAST', name_text_size, ui.Alignment.End, ui.Alignment.Center,
        vec2(sector_w - scale(10), sector_h), false, rgbm(0, 0, 0, 1))

    ui.drawRectFilled(vec2(time_x, lb_y + time_y), vec2(time_x + time_w, lb_y + time_y + time_h), rgbm(0, 0, 0, 0.5),
        box_rounding)
    ui.setCursor(vec2(time_x, lb_y + time_y))
    ui.dwriteTextAligned(lapTimeToString(lastLapTime), sector_text_size,
        ui.Alignment.End,
        ui.Alignment.Center,
        vec2(time_w - scale(20), time_h), false, rgbm(1, 1, 1, 1))

    ui.drawRectFilled(vec2(gap_x, lb_y + gap_y), vec2(gap_x + gap_w, lb_y + gap_y + gap_h), gapColor,
        box_rounding)
    ui.setCursor(vec2(gap_x, lb_y + gap_y))
    ui.dwriteTextAligned(gapToString(timeGap), sector_text_size, ui.Alignment.End, ui.Alignment.Center,
        vec2(gap_w - scale(20), gap_h), false, rgbm(1, 1, 1, 1))



    --- BEST LAP
    lb_y = lb_y + base_h + box_spacing_y
    ui.drawRectFilled(vec2(sector_x, lb_y + sector_y), vec2(sector_x + sector_w, lb_y + sector_y + sector_h),
        rgbm.from0255(51, 199, 44, 1),
        box_rounding)
    ui.setCursor(vec2(sector_x, lb_y + sector_y))
    ui.dwriteTextAligned('BEST', name_text_size, ui.Alignment.End, ui.Alignment.Center,
        vec2(sector_w - scale(10), sector_h), false, rgbm(0, 0, 0, 1))

    ui.drawRectFilled(vec2(time_x, lb_y + time_y), vec2(time_x + time_w, lb_y + time_y + time_h), rgbm(0, 0, 0, 0.5),
        box_rounding)
    ui.setCursor(vec2(time_x, lb_y + time_y))
    ui.dwriteTextAligned(lapTimeToString(fastestLap), sector_text_size,
        ui.Alignment.End,
        ui.Alignment.Center,
        vec2(time_w - scale(20), time_h), false, rgbm(1, 1, 1, 1))


    --- OPTIMAL LAP

    local fastestSectors = MY_DRIVER.fastestSectors
    fastestSectors = table.filter(fastestSectors, function(item, index, callbackData)
        if item ~= 9999999 then
            return item
        end
    end)
    local optimal
    if #fastestSectors == #SIM.lapSplits then
        optimal = 0
        for index, value in ipairs(fastestSectors) do
            optimal = optimal + value
        end
    end

    lb_y = lb_y + base_h + box_spacing_y
    ui.drawRectFilled(vec2(sector_x, lb_y + sector_y), vec2(sector_x + sector_w, lb_y + sector_y + sector_h),
        rgbm(0, 0, 0, 0.7),
        box_rounding)
    ui.setCursor(vec2(sector_x, lb_y + sector_y))
    ui.dwriteTextAligned('OPT', name_text_size, ui.Alignment.End, ui.Alignment.Center,
        vec2(sector_w - scale(10), sector_h), false, rgbm(1, 1, 1, 1))

    ui.drawRectFilled(vec2(time_x, lb_y + time_y), vec2(time_x + time_w, lb_y + time_y + time_h), rgbm(0, 0, 0, 0.5),
        box_rounding)
    ui.setCursor(vec2(time_x, lb_y + time_y))
    ui.dwriteTextAligned(lapTimeToString(optimal), sector_text_size,
        ui.Alignment.End,
        ui.Alignment.Center,
        vec2(time_w - scale(20), time_h), false, rgbm(1, 1, 1, 1))

    ui.popDWriteFont()
end

function listenToUIInputs()
    if not CONTROL_MODE or not GAMEPAD_INDEX then
        return
    end
    local keyLeft = CFGControls.data[CONTROL_MODE]['MFD_LEFT']
    local keyRight = CFGControls.data[CONTROL_MODE]['MFD_RIGHT']

    if keyLeft == '' then keyLeft = nil end
    if keyRight == '' then keyRight = nil end

    if keyLeft and keyRight and CONTROL_MODE == 'X360' then
        if ac.isGamepadButtonPressed(GAMEPAD_INDEX, GamepadNames[keyLeft][1]) then
            debounceValues(function(page)
                MFD_PAGE = page
            end, 200, math.max(1, MFD_PAGE - 1))
        end
        if ac.isGamepadButtonPressed(GAMEPAD_INDEX, GamepadNames[keyRight][1]) then
            debounceValues(function(page)
                MFD_PAGE = page
            end, 200, math.min(#MFD_MODULES, MFD_PAGE + 1))
        end
    end



    if keyLeft and keyRight and CONTROL_MODE == 'WHEEL' then
        local numberLeft = tonumber(keyLeft)
        local numberRight = tonumber(keyRight)
        if numberLeft == 0 or numberLeft == 9000 or numberLeft == 18000 or numberLeft == 27000 then
            if tonumber(ac.getJoystickDpadValue(WHEEL_INDEX, 0)) == numberLeft then
                debounceValues(function(page)
                    MFD_PAGE = page
                end, 300, math.max(1, MFD_PAGE - 1))
            end
        else
            if ac.isJoystickButtonPressed(WHEEL_INDEX, numberLeft) then
                debounceValues(function(page)
                    MFD_PAGE = page
                end, 200, math.max(1, MFD_PAGE - 1))
            end
        end

        if numberRight == 0 or numberRight == 9000 or numberRight == 18000 or numberRight == 27000 then
            if tonumber(ac.getJoystickDpadValue(WHEEL_INDEX, 0)) == numberRight then
                debounceValues(function(page)
                    MFD_PAGE = page
                end, 300, math.min(#MFD_MODULES, MFD_PAGE + 1))
            end
        else
            if ac.isJoystickButtonPressed(WHEEL_INDEX, numberRight) then
                debounceValues(function(page)
                    MFD_PAGE = page
                end, 200, math.min(#MFD_MODULES, MFD_PAGE + 1))
            end
        end
    end

    if keyLeft and keyRight and CONTROL_MODE == 'KEYBOARD' then
        if ac.isKeyDown(keyLeft) then
            debounceValues(function(page)
                MFD_PAGE = page
            end, 200, math.max(1, MFD_PAGE - 1))
        end
        if ac.isKeyDown(keyRight) then
            debounceValues(function(page)
                MFD_PAGE = page
            end, 200, math.min(#MFD_MODULES, MFD_PAGE + 1))
        end
    end

    if ui.mouseClicked(ui.MouseButton.Left) then
        local norm_pos = ui.mousePos() - ui.windowPos()

        if FixedBtnLeft.y < norm_pos.y and
            norm_pos.y < FixedBtnLeft.y + FixedBtnLeft.h and
            FixedBtnLeft.x < norm_pos.x and
            norm_pos.x < FixedBtnLeft.x + FixedBtnLeft.w then
            debounceValues(function(page)
                MFD_PAGE = page
            end, 10, math.max(1, MFD_PAGE - 1))
        end
        if FixedBtnRight.y < norm_pos.y and
            norm_pos.y < FixedBtnRight.y + FixedBtnRight.h and
            FixedBtnRight.x < norm_pos.x and
            norm_pos.x < FixedBtnRight.x + FixedBtnRight.w then
            debounceValues(function(page)
                MFD_PAGE = page
            end, 10, math.min(#MFD_MODULES, MFD_PAGE + 1))
        end
    end
end

function listenToInputs(fnc, current, min, max, increment)
    if not CONTROL_MODE then
        return
    end
    local keyUp = CFGControls.data[CONTROL_MODE]['MFD_UP']
    local keyDown = CFGControls.data[CONTROL_MODE]['MFD_DOWN']


    if keyUp == '' then keyUp = nil end
    if keyDown == '' then keyDown = nil end
    if keyUp and keyDown and CONTROL_MODE == 'X360' then
        if ac.isGamepadButtonPressed(GAMEPAD_INDEX, GamepadNames[keyUp][1]) then
            debounceValues(fnc, 200, math.min(current + increment, max))
        end
        if ac.isGamepadButtonPressed(GAMEPAD_INDEX, GamepadNames[keyDown][1]) then
            debounceValues(fnc, 200, math.max(current - increment, min))
        end
    end

    if keyUp and keyDown and CONTROL_MODE == 'WHEEL' then
        local numberUp = tonumber(keyUp)
        local numberDown = tonumber(keyDown)
        if numberUp == 0 or numberUp == 9000 or numberUp == 18000 or numberUp == 27000 then
            if tonumber(ac.getJoystickDpadValue(WHEEL_INDEX, 0)) == numberUp then
                debounceValues(fnc, 200, math.min(current + increment, max))
            end
        else
            if ac.isJoystickButtonPressed(WHEEL_INDEX, numberUp) then
                debounceValues(fnc, 200, math.min(current + increment, max))
            end
        end

        if numberDown == 0 or numberDown == 9000 or numberDown == 18000 or numberDown == 27000 then
            if tonumber(ac.getJoystickDpadValue(WHEEL_INDEX, 0)) == numberDown then
                debounceValues(fnc, 200, math.max(current - increment, min))
            end
        else
            if ac.isJoystickButtonPressed(WHEEL_INDEX, numberDown) then
                debounceValues(fnc, 200, math.max(current - increment, min))
            end
        end
    end
    if keyUp and keyDown and CONTROL_MODE == 'KEYBOARD' then
        if ac.isKeyDown(keyUp) then
            debounceValues(fnc, 200, math.min(current + increment, max))
        end
        if ac.isKeyDown(keyDown) then
            debounceValues(fnc, 200, math.max(current - increment, min))
        end
    end
    if ui.mouseClicked(ui.MouseButton.Left) then
        local norm_pos = ui.mousePos() - ui.windowPos()

        if FixedBtnUp.y < norm_pos.y and
            norm_pos.y < FixedBtnUp.y + FixedBtnUp.h and
            FixedBtnUp.x < norm_pos.x and
            norm_pos.x < FixedBtnUp.x + FixedBtnUp.w then
            debounceValues(fnc, 10, math.min(current + increment, max))
        end
        if FixedBtnDown.y < norm_pos.y and
            norm_pos.y < FixedBtnDown.y + FixedBtnDown.h and
            FixedBtnDown.x < norm_pos.x and
            norm_pos.x < FixedBtnDown.x + FixedBtnDown.w then
            debounceValues(fnc, 10, math.max(current - increment, min))
        end
    end
end

function script.multiFunctionDisplayHud()
    if INI.fixedPos then
        local centerx = ac.getUI().windowSize.x / 2
        local width = scale(600)
        local height = scale(533)


        local posY = ac.getUI().windowSize.y - height
        local posX = centerx - width / 2

        if not ANALOG_MODE then
            posX = ac.getUI().windowSize.x - width - scale(100)
        end

        ui.transparentWindow(
            'multiFunctionDisplayFixed',
            vec2(posX, posY),
            vec2(posX + width / 2, posY + height), true,
            function()
                return drawMultiFunctionDisplay()
            end)
    else
        drawMultiFunctionDisplay()
    end
end
