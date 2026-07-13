function blinkingAlpha(blinkRate, minimum)
    local zeroToOne = 1 -
        ((1 + math.sin((blinkRate * SIM.time / 1000))) / 2 *
            (1 + math.sin((blinkRate * SIM.time / 1000))) / 2)
    -- todo fix this 1.7
    return minimum + (zeroToOne * (1 - minimum))
end

function drawPositionsHUD()
    local totalCars = SIM.carsCount
    if (MY_DRIVER == nil) then
        return
    end

    local li = table.findByProperty(SORTEDDRIVERS, 'index', MY_DRIVER.index)
    local myDriverTablePos = li and table.indexOf(SORTEDDRIVERS, li)
    local carPosition = tostring(myDriverTablePos)
    local myLaps = math.max(1, #MY_DRIVER.laps)
    local totalLaps = 0
    local posHudY = INI.fixedPos and 0 or 25 / LBS

    local posHudX = 0
    ui.setCursor(scaledVec2(posHudX, posHudY))
    ui.image('img/placeholder.png', scaledVec2(802, 152), rgbm(1, 1, 1, 0), false)
    if ac.getSession(0) ~= nil then
        totalLaps = ac.getSession(SIM.currentSessionIndex).laps
    else
        totalLaps = tonumber(MPTotalLaps)
    end


    if myLaps > totalLaps and totalLaps ~= 0 then
        myLaps = totalLaps
    end

    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.pathLineTo(scaledVec2(2.5, posHudY + 2.5))
    ui.pathLineTo(scaledVec2(152 - 2.5, posHudY + 2.5))
    ui.pathLineTo(scaledVec2(152 - 2.5, posHudY + 152 - 2.5))
    ui.pathLineTo(scaledVec2(2.5, posHudY + 152 - 2.5))
    ui.pathStroke(rgbm(1, 1, 1, 0.6), true, scale(5))

    ui.drawRectFilled(scaledVec2(0, posHudY), scaledVec2(152, posHudY + 152), rgbm(0, 0, 0, 0.6))
    ui.setCursor(scaledVec2(1, posHudY))
    ui.dwriteTextAligned(carPosition, scale(77), ui.Alignment.Center, ui.Alignment.Center,
        scaledVec2(152, 152), false,
        rgbm.colors.white)

    ui.setCursor(scaledVec2(155 + 30, posHudY + 10))
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Bold')
    ui.dwriteText('POSITION', scale(38), rgbm(0, 0, 0, 0.8))
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')

    ui.setCursor(scaledVec2(155 + 30, posHudY + 10))
    ui.dwriteText('POSITION', scale(38), rgbm.colors.white)

    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Black')
    ui.setCursor(scaledVec2(155 + 35, posHudY + 72))
    ui.dwriteText('/ ' .. totalCars, scale(60), rgbm(0, 0, 0, 0.8))
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.setCursor(scaledVec2(155 + 35, posHudY + 72))
    ui.dwriteText('/ ' .. totalCars, scale(60), rgbm.colors.white)

    local lapsX = 515
    local lapsY = posHudY + 10
    local lapsText = tostring(myLaps)
    if totalLaps ~= 0 then
        lapsText = myLaps .. ' / ' .. totalLaps
    end
    if SIM.raceSessionType ~= ac.SessionType.Race and SIM.sessionTimeLeft > 0 then
        lapsText = timeToString(SIM.sessionTimeLeft)
    end
    -- Implement session time left

    ui.pushDWriteFont('MyFont:\\fonts;Weight=Black')
    ui.setCursor(scaledVec2(lapsX, lapsY + 62))
    ui.dwriteText(lapsText, scale(60), rgbm(0, 0, 0, 0.8))
    ui.popDWriteFont()
    -- ui.setCursor(vec2(lapsX, 1000))
    if isLastLap() then
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
        ui.drawRectFilled(scaledVec2(lapsX - 20, posHudY), scaledVec2(lapsX + 208, posHudY + 70),
            rgbm(0.9, 0, 0, blinkingAlpha(8, 0.2)),
            scale(10))
        ui.setCursor(scaledVec2(lapsX - 20, posHudY))
        ui.dwriteTextAligned('FINAL LAP', scale(36), ui.Alignment.Center, ui.Alignment.Center,
            scaledVec2(228, 70),
            false, rgbm.colors.white)
        ui.popDWriteFont()
    else
        local textToWrite = 'LAP'
        if SIM.raceSessionType ~= ac.SessionType.Race and SIM.sessionTimeLeft > 0 then
            textToWrite = 'TIME LEFT'
        end

        ui.setCursor(scaledVec2(lapsX, lapsY))
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Bold')
        ui.dwriteText(textToWrite, scale(38), rgbm(0, 0, 0, 0.8))
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
        ui.setCursor(scaledVec2(lapsX, lapsY))
        ui.dwriteText(textToWrite, scale(38), rgbm.colors.white)
        ui.popDWriteFont()
    end
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.setCursor(scaledVec2(lapsX, lapsY + 62))
    ui.dwriteText(lapsText, scale(60), rgbm.colors.white)
    ui.popDWriteFont()
end

function script.positionsHUD()
    if INI.fixedPos then
        ui.transparentWindow('posHUDFixed', scaledVec2(80, 80), scaledVec2(802, 152), true, function()
            drawPositionsHUD()
            return nil
        end)
    else
        drawPositionsHUD()
    end
    ui.popDWriteFont()
end
