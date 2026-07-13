local function getCurrentXScaled(value)
    return CurrentX + scale(value)
end

local function getCurrentYScaled(value)
    return CurrentY + scale(value)
end

local function updateCurrentY(value)
    CurrentY = CurrentY + scale(value)
end

function raceSortedDrivers(drivers)
    local sorted = table.clone(drivers)
    table.sort(sorted, function(driver1, driver2)
        if driver1.car.isRetired == driver2.car.isRetired then
            return driver1.racePosition < driver2.racePosition
        else
            return not driver1.car.isRetired
        end
    end)
    return sorted
end

function qualySortedDrivers(drivers)
    local sorted = table.clone(drivers) -- table

    table.sort(sorted, function(driver1, driver2)
        if driver1.fastestLap == -1 then
            return false
        elseif driver2.fastestLap == -1 then
            return true
        else
            return driver1.fastestLap < driver2.fastestLap
        end
    end)
    return sorted
end

function sortedDriverSplits(driver)
    local sorted = table.clone(driver.splits) -- table
    table.sort(sorted, function(split1, split2)
        if split1 ~= nil and split2 ~= nil then
            return tonumber(split1) > tonumber(split2)
        else
            return false
        end
    end)


    return sorted
end

LeaderboardCoords = {}
IsRaceMode = true
PrevFocusedDriverIndex = nil

function drawLeaderboardHUD()
    if MY_DRIVER == nil then
        return
    end

    -- local SORTEDDRIVERS
    local myDriverTablePos
    if SIM.raceSessionType == ac.SessionType.Race then
        IsRaceMode = true
        myDriverTablePos = MY_DRIVER.racePosition
    else
        IsRaceMode = false
        SORTEDDRIVERS = qualySortedDrivers(DRIVERS)
        local li = table.findByProperty(SORTEDDRIVERS, 'index', MY_DRIVER.index)
        myDriverTablePos = li and table.indexOf(SORTEDDRIVERS, li) or MY_DRIVER.racePosition
    end

    if PrevFocusedDriverIndex ~= SIM.focusedCar then
        PrevFocusedDriverIndex = SIM.focusedCar
        CAR = ac.getCar(SIM.focusedCar)
    end


    local fastestDriver = fastestDriverInSession()
    CurrentY = 25
    CurrentX = scale(80)

    local myCarRacePosition = CAR.racePosition

    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.setCursor(vec2(0, 0))
    ui.image('img/placeholder.png', scaledVec2(950, 80), rgbm(1, 1, 1, 0), false)

    if ui.rectHovered(scaledVec2(80, 0), scaledVec2(160, 1000)) then
        ui.setMouseCursor(ui.MouseCursor.Hand)
    else
        ui.setMouseCursor(ui.MouseCursor.Arrow)
    end

    -- The Time check is to prevent the event from triggering twice /shrug
    if INI.fixedPos then
        if ui.mouseClicked(ui.MouseButton.Left) then
            local clickedDriver = nil
            table.forEach(SORTEDDRIVERS, function(driver, key, dat)
                local mp = ui.mousePos()
                if driver.leaderboardDrawPos.y + scale(348) < mp.y and
                    mp.y < driver.leaderboardDrawPos.y + scale(348) + scale(84) and
                    driver.leaderboardDrawPos.x + scale(0) < mp.x and
                    mp.x < driver.leaderboardDrawPos.x + scale(0) + scale(84) then
                    clickedDriver = driver.car
                end
            end)
            if clickedDriver then
                ac.focusCar(clickedDriver.index)
            end
        end
    end

    for driverIndex, driver in pairs(SORTEDDRIVERS) do
        if INI.fixedPos then
            driver:updateLeaderboardDrawPos(-1000, -1000)
        end
        local hide = driver.isHidden;
        local SORTEDDRIVERS_BEHIND
        if IsRaceMode then
            SORTEDDRIVERS_BEHIND = (#SORTEDDRIVERS - MY_DRIVER.racePosition)
        else
            SORTEDDRIVERS_BEHIND = (#SORTEDDRIVERS - driverIndex)
        end
        if driver.index == CAR.index then
            if #SORTEDDRIVERS > 8 then
                for _, posDriver in pairs(SORTEDDRIVERS) do
                    local otherDriverPos
                    if IsRaceMode then
                        otherDriverPos = posDriver.racePosition
                    else
                        otherDriverPos = _
                    end

                    local hideDriver = false;
                    if otherDriverPos <= 3 then
                        hideDriver = false
                    else
                        if SORTEDDRIVERS_BEHIND <= 2 then
                            if otherDriverPos > myDriverTablePos and
                                otherDriverPos <= myDriverTablePos + SORTEDDRIVERS_BEHIND then
                                hideDriver = false
                            else
                                local driversExtraAbove = 2 - SORTEDDRIVERS_BEHIND
                                if otherDriverPos < myDriverTablePos and
                                    otherDriverPos >= myDriverTablePos - (driversExtraAbove + 2) then
                                    hideDriver = false
                                else
                                    hideDriver = true
                                end
                            end
                        else
                            local maxVisibleCarsBehind = math.max(2,
                                8 - (3 - math.min(3 - myDriverTablePos, 3)))
                            if otherDriverPos >= (myDriverTablePos - 2) and
                                otherDriverPos <= myDriverTablePos + maxVisibleCarsBehind then
                                hideDriver = false
                            else
                                hideDriver = true
                            end
                        end
                    end
                    if posDriver == MY_DRIVER then
                        hideDriver = false
                    end
                    posDriver.isHidden = hideDriver
                end
            end
        end
        if hide == false then
            local textToWrite
            if IsRaceMode then
                -- Not First Driver and is "active" driver
                if myDriverTablePos ~= 1 and driverIndex == myDriverTablePos then
                    textToWrite = SIM.isSessionStarted and driver.gapToDriverInFront ~= 0 and
                        ('+ ' .. driver.gapToDriverInFront) or '--:--.---'
                end
            else
                if driver.index == MY_DRIVER.index and driverIndex ~= 1 then
                    textToWrite = SIM.isSessionStarted and driver.fastestLap ~= -1 and driver.gapToDriverInFront ~= 0 and
                        SORTEDDRIVERS[driverIndex - 1].fastestLap ~= -1 and
                        ('+ ' .. gapBetweenTimes(driver.fastestLap, SORTEDDRIVERS[driverIndex - 1].fastestLap)) or
                        '--:--.---'
                end
            end
            if textToWrite then
                ui.setCursor(vec2(CurrentX - scale(2), CurrentY - scale(2)))
                ui.image('img/gap_gradient.png', scaledVec2(488, 56), rgbm(1, 1, 1, 0.75), false)
                ui.setCursor(vec2(CurrentX, CurrentY))

                ui.dwriteTextAligned(textToWrite, scale(30),
                    ui.Alignment.End,
                    ui.Alignment.Center,
                    scaledVec2(484 - 20, 52))
                updateCurrentY(52 + 4)
            end

            local boxColor
            local positionBoxColor
            local outlineColor
            local textColor

            if driver.racePosition == myCarRacePosition then
                boxColor = rgbm(1, 1, 1, 0.6)
                positionBoxColor = rgbm(1, 1, 1, 0.6)
                outlineColor = rgbm(1, 1, 1, 0.8)
                textColor = rgbm.colors.black
            else
                boxColor = rgbm(0, 0, 0, 0.3)
                outlineColor = rgbm(1, 1, 1, 0.6)
                positionBoxColor = rgbm(0, 0, 0, 0.6)
                textColor = rgbm.colors.white
            end

            ui.pathLineTo(vec2(getCurrentXScaled(2), getCurrentYScaled(2)))
            ui.pathLineTo(vec2(getCurrentXScaled(84 - 2), getCurrentYScaled(2)))
            ui.pathLineTo(vec2(getCurrentXScaled(84 - 2), getCurrentYScaled(76 - 2)))
            ui.pathLineTo(vec2(getCurrentXScaled(2), getCurrentYScaled(76 - 2)))
            ui.pathStroke(outlineColor, true, scale(4))
            -- ui.drawRect(vec2(CurrentX + scale(2), CurrentY + scale(2)),
            -- vec2(getCurrentXScaled(84 - 2.5), getCurrentYScaled(76 - 2.5)),
            -- outlineColor, 0, nil, scale(5))


            ui.drawRectFilled(vec2(CurrentX, CurrentY), vec2(getCurrentXScaled(84), getCurrentYScaled(76)),
                positionBoxColor)

            if INI.fixedPos then
                driver:updateLeaderboardDrawPos(CurrentX, CurrentY)
            end

            ui.setCursor(vec2(getCurrentXScaled(1), CurrentY))
            ui.dwriteTextAligned(driverIndex, scale(36), ui.Alignment.Center, ui.Alignment.Center,
                scaledVec2(84, 76),
                false, textColor)


            if ui.itemClicked(ui.MouseButton.Left) then
                ac.focusCar(driver.index)
            end


            ui.drawRectFilled(vec2(getCurrentXScaled(84 + 4), CurrentY),
                vec2(getCurrentXScaled(84 + 4 + 396), getCurrentYScaled(76)),
                boxColor)

            ui.setCursor(vec2(getCurrentXScaled(84 + 4 + 18), CurrentY))

            local nameToWrite
            if INI.carName then
                nameToWrite = driver.carName
            else
                nameToWrite = driver.name
            end

            if string.find(nameToWrite, ' ') then
                if not INI.carName then
                    local cleanedName = nameToWrite:gsub("%b()", ""):gsub("%s+$", "")
                    local splitName = cleanedName:split(' ')
                    local firstName = splitName[1]
                    local lastName = splitName[#splitName]
                    nameToWrite = string.sub(firstName, 0, 1) .. '. ' .. lastName
                end
            end
            if string.len(nameToWrite) > 15 then
                nameToWrite = string.sub(nameToWrite, 0, 15)
            end
            ui.dwriteTextAligned(nameToWrite, scale(34), ui.Alignment.Start, ui.Alignment.Center,
                scaledVec2(285, 76)
                ,
                false,
                textColor)

            ui.drawRectFilled(vec2(getCurrentXScaled(484 - 80), getCurrentYScaled(18)),
                vec2(getCurrentXScaled(484 - 80 + 58), getCurrentYScaled(58)),
                rgbm(0, 0, 0, 1))

            ui.setCursor(vec2(getCurrentXScaled(484 - 80 + 1), getCurrentYScaled(20 - 9)))
            ui.image('img/fallback_flag.png', scaledVec2(56, 36 + 18), false)
            ui.setCursor(vec2(getCurrentXScaled(484 - 80), getCurrentYScaled(20 - 11)))
            ui.icon(driver.country, scaledVec2(56 + 2, 36 + 22))

            -- TODO fix with own driver's gap to leader
            if driver.isInPitLane and INI.pitDetails then
                local startX = 0

                if driver.index == MY_DRIVER.index and driverIndex ~= 1 then
                    startX = 166
                end
                local tyreName = ac.getTyresName(driver.index):sub(0, 2)
                local boxColor = rgbm(0, 0, 0, 0.3)
                local textColor = rgbm.colors.white
                local tyreColor = rgb(1, 1, 1)
                if TyreColorMap[tyreName] then
                    tyreColor = TyreColorMap[tyreName]
                end

                -- PIT SIGN
                ui.drawRectFilled(vec2(0, CurrentY), scaledVec2(80, (CurrentY / LBS) + 76), rgbm.colors.white)
                ui.setCursor(vec2(0, CurrentY))
                ui.dwriteTextAligned('P', scale(34), ui.Alignment.Center, ui.Alignment.Center, scaledVec2(80, 76),
                    false, rgbm.colors.black)
                -- TYRE COMPOUND
                ui.setCursor(vec2(getCurrentXScaled(startX + 484 + 4), CurrentY))
                ui.drawRectFilled(vec2(getCurrentXScaled(startX + 484 + 4), CurrentY),
                    vec2(getCurrentXScaled(startX + 484 + 4 + 84), getCurrentYScaled(76)),
                    boxColor)
                ui.drawCircleFilled(vec2(getCurrentXScaled(startX + 484 + 4 + 42), getCurrentYScaled(38)),
                    scale(30),
                    tyreColor, 30)
                ui.dwriteTextAligned(tyreName, scale(28), ui.Alignment.Center,
                    ui.Alignment.Center, vec2(scale(84), scale(76)), false, rgbm.colors.black)

                -- FUEL
                ui.setCursor(vec2(getCurrentXScaled(startX + 564 + 4), CurrentY))
                ui.drawRectFilled(vec2(getCurrentXScaled(startX + 572 + 4), CurrentY),
                    vec2(getCurrentXScaled(startX + 572 + 4 + 84 + 38 + 16), getCurrentYScaled(76)),
                    boxColor)
                ui.setCursor(vec2(getCurrentXScaled(startX + 572 + 4 + 19), getCurrentYScaled(18)))
                ui.image('img/icons/fuel_inner.png', scaledVec2(36, 39), textColor)

                ui.setCursor(vec2(getCurrentXScaled(startX + 572 + 4 + 16 + 41 + 8), CurrentY))
                ui.dwriteTextAligned(tostring(math.round(driver.car.fuel)), scale(38),
                    ui.Alignment.Start, ui.Alignment.Center, vec2(scale(84 + 38 - 16 - 41), scale(76)), false,
                    textColor)
            end

            if driverIndex > 1 then
                if driver.racePosition == MY_DRIVER.racePosition then
                    ui.drawRectFilled(vec2(getCurrentXScaled(484), CurrentY),
                        vec2(getCurrentXScaled(484 + 166), getCurrentYScaled(76)),
                        rgbm(1, 0, 0, 0.8))
                    ui.setCursor(vec2(getCurrentXScaled(530), CurrentY))
                    local textToWrite = ' --.--'
                    if not SIM.isSessionStarted or MY_DRIVER.gapToLeader == 0 then
                        textToWrite = ' --.--'
                    elseif IsRaceMode then
                        textToWrite = MY_DRIVER.gapToLeader
                    elseif fastestDriverInSession() and MY_DRIVER.fastestLap ~= -1 then
                        textToWrite = gapBetweenTimes(MY_DRIVER.fastestLap, fastestDriverInSession().fastestLap)
                    end

                    ui.dwriteTextAligned(textToWrite, scale(32),
                        ui.Alignment.Start,
                        ui.Alignment.Center,
                        scaledVec2(166 + 22, 76))
                    if SIM.isSessionStarted and textToWrite ~= ' --.--' then
                        ui.setCursor(vec2(getCurrentXScaled(505), CurrentY + scale(15)))
                        ui.dwriteText("+", scale(32))
                    end
                end
            end
            ui.setCursor(vec2(0, CurrentY))
            if #driver.laps == tonumber(MPTotalLaps) and driver.laps[#driver.laps].lapTime ~= -1 then
                ui.image('img/finish_flag.png', scaledVec2(80, 76))
            end

            updateCurrentY(76 + 4)

            local bottomTextToWrite
            if IsRaceMode then
                -- not last driver
                if MY_DRIVER.racePosition ~= #SORTEDDRIVERS then
                    -- is my driver
                    if myDriverTablePos == driverIndex then
                        bottomTextToWrite = SIM.isSessionStarted and
                            SORTEDDRIVERS[driverIndex + 1].gapToDriverInFront ~= 0 and
                            ("- " .. SORTEDDRIVERS[driverIndex + 1].gapToDriverInFront) or
                            '--:--.---'
                    end
                end
            else
                if driverIndex == myDriverTablePos and driverIndex ~= #SORTEDDRIVERS then
                    bottomTextToWrite = SIM.isSessionStarted and driver.fastestLap ~= -1 and
                        SORTEDDRIVERS[driverIndex + 1].fastestLap ~= -1 and
                        ('- ' .. gapBetweenTimes(SORTEDDRIVERS[driverIndex + 1].fastestLap, driver.fastestLap)) or
                        '--:--.---'
                end
            end
            if bottomTextToWrite then
                ui.setCursor(vec2(CurrentX - scale(2), CurrentY - scale(2)))
                ui.image('img/gap_gradient.png', scaledVec2(488, 56), rgbm(1, 1, 1, 0.75), false)
                ui.setCursor(vec2(CurrentX, CurrentY))

                ui.dwriteTextAligned(bottomTextToWrite,
                    scale(30),
                    ui.Alignment.End,
                    ui.Alignment.Center,
                    scaledVec2(484 - 22, 52))
                updateCurrentY(52 + 4)
            end
        end
    end


    updateCurrentY(52)
    if fastestDriver and isValidFastestLap(fastestDriver.fastestLap) then
        ui.drawRectFilled(vec2(CurrentX, CurrentY), vec2(getCurrentXScaled(84), getCurrentYScaled(76)),
            rgbm(0, 0, 0, 0.4))
        ui.setCursor(vec2(getCurrentXScaled(2), CurrentY))
        ui.dwriteTextAligned('FL', scale(34), ui.Alignment.Center, ui.Alignment.Center, scaledVec2(84, 76),
            false,
            rgbm.colors.white)
        ui.drawRectFilled(vec2(getCurrentXScaled(84 + 4), CurrentY),
            vec2(getCurrentXScaled(84 + 4 + 396), getCurrentYScaled(76)),
            rgbm(0, 0, 0, 0.4))

        ui.setCursor(vec2(getCurrentXScaled((84 + 4 + 18)), CurrentY))
        local nameToWrite = fastestDriver.name

        if string.len(nameToWrite) > 18 then
            if string.find(nameToWrite, ' ') then
                local cleanedName = nameToWrite:gsub("%b()", ""):gsub("%s+$", "")
                local splitName = cleanedName:split(' ')
                local firstName = splitName[1]
                local lastName = splitName[#splitName]
                nameToWrite = string.sub(firstName, 0, 1) .. '. ' .. lastName
            else
                nameToWrite = string.sub(nameToWrite, 0, 18) .. ' ...'
            end
        end
        ui.dwriteTextAligned(nameToWrite, scale(34), ui.Alignment.Start, ui.Alignment.Center,
            scaledVec2(396, 76), false
            , rgbm.colors.white)

        updateCurrentY(76)

        ui.setCursor(vec2(CurrentX - scale(2), CurrentY - scale(2)))
        ui.image('img/fl_gradient.png', scaledVec2(488, 66), rgbm(1, 1, 1, 0.75), false)
        ui.setCursor(vec2(CurrentX, CurrentY))
        ui.dwriteTextAligned(lapTimeToString(fastestDriver.fastestLap), scale(34), ui.Alignment.End,
            ui.Alignment.Center,
            scaledVec2(484 - 22, 62)
            ,
            false, rgbm.colors.white)
    end



    ui.popDWriteFont()
end

function script.leaderboardHUD()
    if INI.fixedPos then
        ui.transparentWindow('leaderboardHUDFixed', scaledVec2(0, 348), scaledVec2(980, 1040), true, function()
            drawLeaderboardHUD()
        end)
    else
        drawLeaderboardHUD()
    end
end
