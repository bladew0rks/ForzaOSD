TIMER = 0
PreviousSector = 1
TimerSector = 1
DisplayState = ''

function getFastestSectorTime(sector)
    local fastestSectorTimes = {}
    table.forEach(DRIVERS, function(driver)
        table.insert(fastestSectorTimes, driver.fastestSectors[sector])
    end)
    table.sort(fastestSectorTimes, function(sectorTime1, sectorTime2)
        return sectorTime1 < sectorTime2
    end)
    return fastestSectorTimes[1]
end

Beep = ui.MediaPlayer('sounds/beep.mp3')
BeepFL = ui.MediaPlayer('sounds/beep_fastestLap.mp3')
BeepFS = ui.MediaPlayer('sounds/beep_fastestSector.mp3')


function drawTimerHUD()
    if INI.beepSound then
        Beep:setVolume(ac.getAudioVolume(ac.AudioChannel.Main) * INI.beepSoundMulti)
        BeepFL:setVolume(ac.getAudioVolume(ac.AudioChannel.Main) * INI.beepSoundMulti)
        BeepFS:setVolume(ac.getAudioVolume(ac.AudioChannel.Main) * INI.beepSoundMulti)
    else
        Beep:setVolume(0)
        BeepFL:setVolume(0)
        BeepFS:setVolume(0)
    end


    local timerY = 25 / LBS

    ui.pushDWriteFont('MyFont:\\fonts;Weight=Medium')
    -- ui.popFont()
    ui.setCursor(scaledVec2(0, 0))
    ui.image('img/placeholder.png', scaledVec2(775, timerY + 120), rgbm(1, 1, 1, 0), false)

    ui.drawRectFilled(scaledVec2(142, timerY + 48), scaledVec2(142 + 455, timerY + 48 + 75), rgbm(0, 0, 0, 0.3),
        scale(8),
        ui.CornerFlags.All)
    ui.setCursor(scaledVec2(142, timerY + 48))

    local timeText = CAR.lapTimeMs
    local timeAlpha = 1
    local newSectorIndex = CAR.currentSector + 1


    if newSectorIndex ~= PreviousSector then
        TimerSector = PreviousSector
        PreviousSector = newSectorIndex
        TIMER = SIM.time + 3000
        local fastestSectorTimeinSector = getFastestSectorTime(TimerSector)

        if MY_DRIVER.currentLap ~= 1 then
            if PreviousSector == 1 and MY_DRIVER.currentLap <= 2 then
                DisplayState = ''
            else
                if PreviousSector == 1 then
                    -- SHOW LAP RELATED --
                    -- Check if fastest overall
                    if fastestDriverInSession() and
                        MY_DRIVER.laps[#MY_DRIVER.laps - 1].lapTime <= fastestDriverInSession().fastestLap then
                        BeepFL:play()
                        DisplayState = 'fastest'
                    elseif MY_DRIVER.laps[#MY_DRIVER.laps - 1].lapTime <= MY_DRIVER.fastestLap then
                        Beep:play()
                        DisplayState = 'best'
                    else
                        Beep:play()
                        DisplayState = 'slower'
                    end
                else
                    -- SHOW SECTOR RELATED --
                    if (fastestSectorTimeinSector >= MY_DRIVER.previousSectors[TimerSector]) then
                        DisplayState = 'fastest'
                        BeepFS:play()
                    elseif MY_DRIVER.sectorTimeGap < 0 then
                        DisplayState = 'best'
                        Beep:play()
                    else
                        DisplayState = 'slower'
                        Beep:play()
                    end
                end
            end
        end
        ------------
    end

    if SIM.time < TIMER and MY_DRIVER.currentLap ~= 0 and MY_DRIVER.currentLap ~= 1 then
        if CAR.currentSector == 0 and isLastLap() then
            -- announce final lap
            ui.drawRectFilled(scaledVec2(266, timerY + 0), scaledVec2(266 + 222, timerY + 44), rgbm(1, 0, 0, 0.8),
                scale(8))
            ui.setCursor(scaledVec2(266, timerY + 0))
            ui.dwriteTextAligned('FINAL  LAP', scale(30), ui.Alignment.Center,
                ui.Alignment.Center,
                scaledVec2(222, 44), false, rgbm.colors.white)
        end

        if PreviousSector == 1 then
            timeText = MY_DRIVER.laps[#MY_DRIVER.laps - 1].lapTime
        else
            timeText = 0
            for i = 1, CAR.currentSector, 1 do
                timeText = timeText + MY_DRIVER.previousSectors[i]
            end
        end
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
        local sectorColor
        local blockColor

        if DisplayState == 'fastest' then
            sectorColor = rgbm(0.58, 0.23, 0.75, 0.7)
            blockColor = rgbm(0.58, 0.23, 0.75, 0.7)
        elseif DisplayState == 'best' then
            sectorColor = rgbm(0, 0.38, 0.82, 0.8)
            blockColor = rgbm(0, 0.69, 0, 0.8)
        else
            sectorColor = rgbm(1, 0, 0, 0.8)
            blockColor = rgbm(0.58, 0.23, 0.75, 0.7)
        end


        if not (MY_DRIVER.currentLap == 2 and CAR.currentSector == 0) then
            if SIM.time < TIMER - 1000 then
                timeAlpha = blinkingAlpha(10, -0.4)
            end
            -- sectorColor = rgbm(0, 0.38, 0.82, 0.8)
            if DisplayState == 'fastest' or DisplayState == 'best' then
                ui.drawRectFilled(scaledVec2(0, timerY + 48), scaledVec2(138, timerY + 48 + 75), blockColor,
                    scale(8))
                ui.setCursor(scaledVec2(0, timerY + 48))
                ui.dwriteTextAligned('BEST', scale(36), ui.Alignment.Center, ui.Alignment.Center,
                    scaledVec2(138, 75), false, rgbm.colors.white)
            end
            if not (MY_DRIVER.sectorTimeGap == 0 and #MY_DRIVER.laps - 1 == 2) then
                ui.drawRectFilled(scaledVec2(142 + 460, timerY + 48), scaledVec2(142 + 460 + 168, timerY + 48 + 75),
                    sectorColor,
                    scale(8))
                ui.setCursor(scaledVec2(142 + 460, timerY + 48))

                ui.dwriteTextAligned(gapToString(MY_DRIVER.sectorTimeGap), scale(36), ui.Alignment.Center,
                    ui.Alignment.Center,
                    scaledVec2(168, 75), false, rgbm.colors.white)
                ui.popDWriteFont()
            end
        end
    end

    -- Current Lap-Timer

    ui.setCursor(scaledVec2(142, timerY + 48))
    ui.popDWriteFont()
    ui.pushDWriteFont('MyTimingFontOutline:\\fonts')
    ui.dwriteTextAligned(lapTimeToString(timeText), scale(50), ui.Alignment.Center, ui.Alignment.Center,
        scaledVec2(455, 75), false, rgbm(0, 0, 0, timeAlpha - 0.4))
    ui.setCursor(scaledVec2(142, timerY + 48))
    ui.popDWriteFont()
    ui.pushDWriteFont('MyTimingFont:\\fonts')
    local timerColor = CAR.isLapValid and rgbm(1, 1, 1, timeAlpha) or rgbm(1, 0, 0, timeAlpha)
    ui.dwriteTextAligned(lapTimeToString(timeText), scale(50), ui.Alignment.Center, ui.Alignment.Center,
        scaledVec2(455, 75), false, timerColor)

    ui.popDWriteFont()
end

function script.timerHUD(dt)
    if INI.fixedPos then
        local centery = ac.getUI().windowSize.y / 2 - LBS * 400
        local centerx = ac.getUI().windowSize.x / 2
        local elementWidth = scale(775)
        local elementHeight = scale(25 / LBS + 130)

        ui.transparentWindow('timerHUDFixed',
            vec2(centerx - (elementWidth / 2) + scale(20), centery - (elementHeight / 2) - scale(205)),
            vec2(elementWidth, elementHeight), true,
            function()
                drawTimerHUD()
            end)
    else
        drawTimerHUD()
    end
end
