function drawSessionTimerHUD()

    local baseY = INI.fixedPos and 0 or 20 / LBS



    ui.setCursor(scaledVec2(0, baseY + 10))
    ui.pushDWriteFont('MyTimingFont:\\fonts')
    ui.dwriteTextAligned(lapTimeToString(SESSION_TIME), scale(42), ui.Alignment.Center, ui.Alignment.Center,
        scaledVec2(500, 60), false, rgbm(1, 1, 1, 1))
    ui.popDWriteFont()
end

function script.sessionTimerHUD()
    if INI.fixedPos then
        local maxY = ac.getUI().windowSize.y
        ui.transparentWindow('sessionTimerHUD', vec2(scale(-10), maxY - scale(124)), scaledVec2(500, 80), true
            ,
            function()
                drawSessionTimerHUD()
            end)

    else
        drawSessionTimerHUD()
    end

end
