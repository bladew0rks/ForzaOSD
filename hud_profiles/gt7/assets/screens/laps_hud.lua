-- Helper methods
local function sortedLaps(laps)
    local sorted = table.clone(laps) -- table
    sorted = table.filter(sorted, function(lap)
        return lap ~= -1
    end)
    table.sort(sorted, function(lap1, lap2)
        return lap1.lap > lap2.lap
    end)
    return sorted
end

function drawLapsHUD()
    if MY_DRIVER == nil then return end

    -- UI Scale
    -- ui.image('img/placeholder.png', scaledVec2(386, 650), rgbm(1, 1, 1, 0), false)

    -- UI Params
    local lapsHudY = 25
    local lapsHudX = 0

    local laps = sortedLaps(MY_DRIVER.laps)
    if laps == nil then
        return
    end


    local last10Laps = table.slice(laps, 0, 10)

    -- Font
    -- Main
    for i, lapInfo in pairs(last10Laps) do
        local lapTime = lapTimeToString(lapInfo.lapTime)
        local isBlinking = lapInfo.lapTime <= 0 and true or false
        ui.drawRectFilled(vec2(lapsHudX, lapsHudY), vec2(lapsHudX + scale(88), lapsHudY + scale(65)),
            rgbm(1, 1, 1, 0.9), scale(5))
        ui.setCursor(vec2(lapsHudX, lapsHudY))
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
        ui.dwriteTextAligned(lapInfo.lap, scale(34), ui.Alignment.Center, ui.Alignment.Center,
            scaledVec2(88, 65), false, rgbm(0, 0, 0, 1))
        ui.popDWriteFont()
        ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')

        ui.setCursor(vec2(lapsHudX + scale(93), lapsHudY))
        ui.beginRotation()
        local color = lapInfo.lap == 1 and SIM.raceSessionType == ac.SessionType.Race and rgbm(0, 0, 0, 0.7) or
            getColorBasedOnTime(lapInfo)
        ui.image('img/gradient.png', scaledVec2(300, 65),
            color)
        ui.endRotation(270)

        ui.setCursor(vec2(lapsHudX + scale(115), lapsHudY))
        ui.dwriteTextAligned(lapTime, scale(34), ui.Alignment.Start, ui.Alignment.Center,
            scaledVec2(300 - 25, 65), false, rgbm(1, 1, 1, isBlinking and blinkingAlpha(7, 0.3) or 1))
        if lapInfo.inPits then
            ui.setCursor(vec2(lapsHudX + scale(300 - 25), lapsHudY))
            ui.dwriteTextAligned('P', scale(34), ui.Alignment.Start, ui.Alignment.Center,
                scaledVec2(25, 65), false, rgbm.colors.white)
        end
        ui.popDWriteFont()
        lapsHudY = lapsHudY + scale(68)
    end
end

function script.lapsHUD()
    if INI.fixedPos then
        local maxX = ac.getUI().windowSize.x
        ui.transparentWindow('lapsHUDFixed', vec2(maxX - scale(386), scale(834)),
            vec2(scale(386), 25 + scale(680)), true,
            function()
                drawLapsHUD()
            end)
    else
        drawLapsHUD()
    end

end
