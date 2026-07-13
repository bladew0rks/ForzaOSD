-------------------------------- CLASSES --------------------------------
require('helpers/helpers')
require('helpers/MappedConfig')

-------------------------------- CLASSES --------------------------------
require('classes/driver')
require('classes/blindspot_driver')
require('classes/lap_info')
require('classes/settings')

-------------------------------- SCREENS --------------------------------
require('screens/laps_hud')
require('screens/tach_hud')
require('screens/leaderbord_hud')
require('screens/positions_hud')
require('screens/timer_hud')
require('screens/session_timer_hud')
require('screens/map_hud')
require('screens/multi_function_display_hud')

-------------------------------- Initializing --------------------------------
INI = Settings()

INITIALISED = false
DRIVERS = nil
MY_DRIVER = nil
LBS = 1


SIM                        = ac.getSim()
CAR                        = ac.getCar(SIM.focusedCar)
SESSION_LAP                = CAR.sessionLapCount == 0 and 1 or CAR.sessionLapCount
MY_DRIVER_SECTOR           = 0
SESSION_WAS_STARTED_BEFORE = false
SESSION_TIME               = 0
BASE_SESSION_TIME          = SIM.time
MFD_OPTIONS                = { 'TCS', 'ABS', 'BB', 'FUEL', 'SB', 'RADAR', '---' }
MFD_NAMES_MAP              = {
    TCS = "Traction Control",
    ABS = "ABS",
    BB = "Brake Bias",
    FUEL = "Fuel/Power Info",
    SB = "Session Best",
    RADAR = "Radar",
    ['---'] = "---"
}

ACTIVECAMERAPOINT          = 0
CameraMotion               = nil
INITCAM                    = false

MPTotalLaps                = ac.getSession(SIM.currentSessionIndex).laps
MPFetching                 = false
CURRENTSESSIONID           = 0

if ac.onSessionStart then
    ac.onSessionStart(function()
        initializeData()
    end)
end



function initializeData()
    TrackLength = SIM.trackLengthM
    NSPLITS     = math.min(TrackLength / 50, 1000)
    DRIVERS     = {}
    for i = 0, SIM.carsCount - 1 do
        local driver = Driver(i, ac.getDriverName(i), ac.getCarName(i):split(' ', 2)[2] or '', ac.getDriverNationCode(i))
        DRIVERS[i + 1] = driver;
    end
    INITIALISED       = true

    MY_DRIVER_SECTOR  = CAR.currentSector
    BASE_SESSION_TIME = SIM.time
    INITCAM           = false
end

function script.update(dt)
    if SIM.currentSessionIndex ~= CURRENTSESSIONID then
        INITIALISED = false
        DRIVERS = nil
        MY_DRIVER = nil
        CameraMotion = nil
        INITCAM = false
        MPTotalLaps = ac.getSession(SIM.currentSessionIndex).laps
        CURRENTSESSIONID = SIM.currentSessionIndex
    end


    if SIM.isSessionStarted then
        SESSION_TIME = SIM.time - BASE_SESSION_TIME
    else
        BASE_SESSION_TIME = SIM.time
        SESSION_TIME = 0
    end

    if SIM.isSessionStarted == true then
        SESSION_WAS_STARTED_BEFORE = true
    end

    if SESSION_WAS_STARTED_BEFORE == true and ac.getSim().isSessionStarted == false then
        INITIALISED = false
        SESSION_WAS_STARTED_BEFORE = false
        INITCAM = false
    end

    if INITIALISED == false then
        initializeData()
    end

    if INI.autoScale then
        LBS = ac.getUI().windowSize.y / 2160
    elseif INI.scaleValue then
        LBS = INI.scaleValue
    end

    table.sort(DRIVERS, function(driver1, driver2)
        return driver1.index < driver2.index
    end)
    MY_DRIVER = DRIVERS[SIM.focusedCar + 1]
    for i, driver in pairs(DRIVERS) do
        driver:updateLaps()
        driver:updateSectors()
        driver:updateSplits()
        driver:updateRadar()
        driver:updatePitLaneStatus()
    end
    if INITIALISED and INI.countdownCamera and SIM.raceSessionType == 3 and not SIM.isReplayActive then
        countdownCamera(dt)
    end
end

function script.windowMain(dt)
    ui.tabBar('someTabBarID', function()
        ui.tabItem('Scale and Position', function()
            ui.newLine(5)
            if ui.checkbox('Accurate Fixed Position', INI.fixedPos) then
                INI:update('EXPERIMENTAL', 'FIXED_POS', not INI.fixedPos)
                INI:reload()
            end
            ui.newLine(5)

            if ui.checkbox('Automatic Scale', INI.autoScale) then
                INI:update('SCALE', 'AUTOSCALE', not INI.autoScale)
                INI:reload()
                if INI.autoScale then
                    LBS = ac.getUI().windowSize.y / 2160
                end
            end

            if ui.radioButton('1080p Scale', LBS == 0.50) then
                INI:update('SCALE', 'SCALEVALUE', 0.50)
                INI:update('SCALE', 'AUTOSCALE', false)
                INI:reload()
            end

            if ui.radioButton('1440p Scale', LBS == math.round(1440 / 2160, 2)) then
                INI:update('SCALE', 'SCALEVALUE', math.round(1440 / 2160, 2))
                INI:update('SCALE', 'AUTOSCALE', false)
                INI:reload()
            end
            if ui.radioButton('4k Scale', LBS == 1) then
                INI:update('SCALE', 'SCALEVALUE', 1)
                INI:update('SCALE', 'AUTOSCALE', false)
                INI:reload()
            end
            local value, changed = ui.slider('Scale Slider', LBS, 0.2, 3, '%.2f')
            if changed then
                INI:update('SCALE', 'SCALEVALUE', value)
                INI:update('SCALE', 'AUTOSCALE', false)
                INI:reload()
            end
        end)
        ui.tabItem('Units', function()
            ui.newLine(5)

            if ui.radioButton('Imperial (mph)', INI.isImperial == true) then
                INI:update('UNITS', 'IMPERIAL', true)
                INI:reload()
            end
            if ui.radioButton('Metric (kph)', INI.isImperial == false) then
                INI:update('UNITS', 'IMPERIAL', false)
                INI:reload()
            end
        end)
        ui.tabItem('Sounds', function()
            ui.newLine(5)
            if ui.checkbox('Sector Beep', INI.beepSound) then
                INI:update('SOUND', 'BEEP', not INI.beepSound)
                INI:reload()
            end

            local value, changed = ui.slider('Beep Volume', INI.beepSoundMulti * 10, 1, 10, "%.f%")
            if changed then
                INI:update('SOUND', 'BEEP_VOLUME_MULTI', value / 10)
                INI:reload()
            end

            if ui.checkbox('Countdown Sound', INI.countdownSound) then
                INI:update('SOUND', 'COUNTDOWN', not INI.countdownSound)
                INI:reload()
            end

            local cdValue, cdChanged = ui.slider('Countdown Volume', INI.countdownSoundMulti * 10, 1, 10, "%.f%")
            if cdChanged then
                INI:update('SOUND', 'COUNTDOWN_VOLUME_MULTI', cdValue / 10)
                INI:reload()
            end
        end)

        ui.tabItem('Features', function()
            ui.newLine(5)
            if ui.checkbox('Analog Tachometer', INI.analogMode) then
                if INI.analogMode then
                    INI:update('FEATURES', 'ANALOG_TACH_AUTO', false)
                end
                INI:update('FEATURES', 'ANALOG_TACH', not INI.analogMode)
                INI:reload()
            end
            if ui.checkbox('Automatic Mode (Switch to Analog view in bumper cam)', INI.analogModeAuto) then
                INI:update('FEATURES', 'ANALOG_TACH_AUTO', not INI.analogModeAuto)
                INI:reload()
            end
            if ui.checkbox('Use car name instead of driver name in leaderboard', INI.carName) then
                INI:update('FEATURES', 'CARNAME', not INI.carName)
                INI:reload()
            end

            ui.newLine(5)
            ui.drawLine(vec2(ui.getCursorX() + 10, ui.getCursorY()), vec2(ui.windowWidth() - 10, ui.getCursor().y),
                rgbm(1, 1, 1, 0.5), 1)
            ui.newLine()
            if ui.checkbox('Countdown Camera Movement', INI.countdownCamera) then
                INI:update('FEATURES', 'COUNTDOWN_CAMERA', not INI.countdownCamera)
                INI:reload()
            end
            if ui.checkbox('Countdown Animation', INI.countdown) then
                INI:update('FEATURES', 'COUNTDOWN', not INI.countdown)
                INI:reload()

                local startingLightsTex = ac.getFolder(ac.FolderID.Root) .. '\\content\\texture\\off.png'
                local startingLightsHiddenTex = ac.getFolder(ac.FolderID.ACApps) .. '\\lua\\GT7HUD\\img\\off.png'
                local startingLightsOgTex = ac.getFolder(ac.FolderID.Root) .. '\\content\\texture\\off_og.png'
                if not INI.countdown then
                    io.copyFile(startingLightsOgTex, startingLightsTex, false)
                else
                    io.copyFile(startingLightsHiddenTex, startingLightsTex, false)
                end
            end
            if ui.checkbox('Pitlane Status', INI.pitDetails) then
                INI:update('FEATURES', 'INPITLANEDETAILS', not INI.pitDetails)
                INI:reload()
            end
            if ui.checkbox('Extended Tyre Temps', INI.extendedTyreTemps) then
                INI:update('FEATURES', 'EXTENDED_TYRE_TEMPS', not INI.extendedTyreTemps)
                INI:reload()
            end
        end)
        ui.tabItem('MFD Config', function()
            local previewCallback = function(itemIndex)
                return MFD_NAMES_MAP[INI.mfdModules[itemIndex]]
            end
            local deleteCallback = function(itemIndex)
                local clone = table.clone(INI.mfdModules, true)
                table.remove(clone, itemIndex)
                MFD_PAGE = math.max(1, MFD_PAGE - 1)
                INI:update('MFD', 'MODULES', clone, true)
                INI:reload()
            end
            local addCallback = function()
                local clone = table.clone(INI.mfdModules, true)
                table.insert(clone, '---')
                INI:update('MFD', 'MODULES', clone, true)
                INI:reload()
            end
            if INI.mfdModules == nil then
                return
            end

            for i = 1, #INI.mfdModules, 1 do
                ui.pushID(i)
                combo(i, MFD_OPTIONS, previewCallback, function()
                    for it = 1, #MFD_OPTIONS, 1 do
                        if ui.selectable(MFD_NAMES_MAP[MFD_OPTIONS[it]]) then
                            local clone = table.clone(INI.mfdModules, true)
                            clone[i] = MFD_OPTIONS[it]
                            INI:update('MFD', 'MODULES', clone, true)
                            INI:reload()
                        end
                    end
                end, deleteCallback, addCallback)
            end


            DrawInputSettings()
        end)
    end)
end

function script.onShowWindowMain()
    if INI.autoScale then
        LBS = ac.getUI().windowSize.y / 2160
    end
end

Countdown = ui.MediaPlayer('sounds/countdown.mp3')
function script.startingSequence(dt)
    if not INI.countdown then return end
    if (SIM.isPaused or SIM.isInMainMenu) and Countdown then
        Countdown:pause()
    end

    local centery = ac.getUI().windowSize.y / 2
    local centerx = ac.getUI().windowSize.x / 2
    local textToWrite = math.floor(SIM.timeToSessionStart / 1000) + 1
    local firstCount = (SIM.timeToSessionStart / 1000 + 1) % 1

    if textToWrite > 3 or textToWrite < 1 then return end

    if textToWrite == 3 and INI.countdownSound then
        Countdown:setVolume(ac.getAudioVolume(ac.AudioChannel.Main) * (INI.countdownSoundMulti + 0.35))
        Countdown:play()
    end

    ui.setCursor(vec2(centerx - scale(240), centery - scale(240)))
    ui.image('img/countdown-stripes.png', scaledVec2(480, 480), rgbm.colors.white)
    local easeInValue = inSine(firstCount * 1000, 0, 1, 1000)
    local easeOutValue = outInSine(firstCount * 1000, 0, 1, 1000)

    if firstCount > 0.5 then
        ui.pathArcTo(vec2(centerx, centery), scale(240 - (12 / 2)),
            getAngle(0),
            getAngle(360 - 360 * (((easeInValue - 0.5) / 0.5 * 1))), 80)
    else
        ui.pathArcTo(vec2(centerx, centery), scale(240 - (12 / 2)),
            getAngle(360 - 360 * easeOutValue / 0.5 * 1),
            getAngle(360), 80)
    end
    ui.pathStroke(rgbm.colors.white, false, scale(12))
    ui.pushDWriteFont('Arkitech:\\fonts;Weight=Regular')
    ui.setCursor(vec2(centerx - scale(400), centery - scale(240)))

    ui.dwriteTextAligned(textToWrite, scale(160), ui.Alignment.Center, ui.Alignment.Center,
        scaledVec2(800, 480), false, rgbm.colors.white)
    ui.popDWriteFont()
end

AnimTimer = 0

local CarDimToAdd = vec2(CAR.aabbSize.x / 2, CAR.aabbSize.y * 1.4)
local waitingPosS1 = vec3(-2 - CarDimToAdd.x, 1, 1.5 + CarDimToAdd.y)
local waitingPosE1 = vec3(-2 - CarDimToAdd.x, 1, 3 + CarDimToAdd.y)
local waitingPosS2 = vec3(-1 - CarDimToAdd.x, 1, -3 - CarDimToAdd.y)
local waitingPosE2 = vec3(1 + CarDimToAdd.x, 0.4, -3 - CarDimToAdd.y)
local waitingPosS3 = vec3(-1 - CarDimToAdd.x, 1, 3 + CarDimToAdd.y)
local waitingPosE3 = vec3(1 + CarDimToAdd.x, 0.4, 3 + CarDimToAdd.y)


function countdownCamera(dt)
    if SIM.isPaused or SIM.isInMainMenu then
        if CameraMotion then
            CameraMotion.transform = CameraMotion.transformOriginal
            CameraMotion:dispose()
        end
        INITCAM = false
        ACTIVECAMERAPOINT = 0
        return
    end
    if SIM.timeToSessionStart < -2000 then return end
    if SIM.timeToSessionStart > 19000 then
        INITCAM = false
        ACTIVECAMERAPOINT = 0
        return
    end
    if CAR.isInPit and CameraMotion then
        if CameraMotion then
            CameraMotion.transform = CameraMotion.transformOriginal
            CameraMotion:dispose()
        end
        return
    end


    if not INITCAM then
        if CameraMotion then CameraMotion:dispose() end
        CameraMotion = ac.grabCamera('starting anim')
        INITCAM = true
    end

    if SIM.timeToSessionStart > 0 and ACTIVECAMERAPOINT == 0 then
        ACTIVECAMERAPOINT = 1
        AnimTimer = SIM.timeToSessionStart / 1000 - 3
    end


    if ACTIVECAMERAPOINT == 1 then
        if AnimTimer > 0 then
            AnimTimer = AnimTimer - dt
            if CameraMotion then
                local localPos = math.lerp(waitingPosS1, waitingPosE1, (1 - AnimTimer))
                CameraMotion.transform.position = CAR.transform:transformPoint(localPos)
                CameraMotion.transform.look = -CAR.transform:transformVector(localPos)
                CameraMotion.transform.up = vec3(0, 1, 0)
            end
        else
            ACTIVECAMERAPOINT = 2
            AnimTimer = 1
        end
    end

    if ACTIVECAMERAPOINT == 2 then cameraTransition(dt, waitingPosS2, waitingPosE2, 3) end
    if ACTIVECAMERAPOINT == 3 then cameraTransition(dt, waitingPosS3, waitingPosE3, 4) end

    if ACTIVECAMERAPOINT == 4 then
        if AnimTimer > 0 then
            AnimTimer = AnimTimer - dt / 1
            if CameraMotion then
                local localPos = waitingPosE3
                CameraMotion.transform.position = CAR.transform:transformPoint(localPos)
                CameraMotion.transform.look = -CAR.transform:transformVector(localPos)
                CameraMotion.transform.up = vec3(0, 1, 0)
                CameraMotion.ownShare = math.smoothstep(math.lerpInvSat(AnimTimer, 0, 1))
            end
        else
            ACTIVECAMERAPOINT = 0
            CameraMotion:dispose()
        end
    end
end

function cameraTransition(dt, startPos, endPos, nextPoint)
    if AnimTimer > 0 then
        AnimTimer = AnimTimer - dt

        if CameraMotion then
            local localPos = math.lerp(startPos, endPos, (1 - AnimTimer) ^ 0.4)
            CameraMotion.transform.position = CAR.transform:transformPoint(localPos)
            CameraMotion.transform.look = -CAR.transform:transformVector(localPos)
            CameraMotion.transform.up = vec3(0, 1, 0)
        end
    else
        ACTIVECAMERAPOINT = nextPoint
        AnimTimer = 1
    end
end
