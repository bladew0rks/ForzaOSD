MapInit = false
SCALEFACTOR = nil
---@type ac.GeometryShot
CANVAS = nil
---@type ac.GeometryShot
IMAGE_CANVAS = nil
TRACKLENGTH_SCALED = nil
CENTER_X = nil
CENTER_Z = nil

local focusedCar ---@type ac.StateCar
---@type fun(car: ac.StateCar, focus,scaleFactor)
local drawCall_car =
    (function(car, focus, scaleFactor) ---@param car ac.StateCar
        if focus then scaleFactor = scaleFactor * 1.2 end
        local up = vec3(0, 80 * scaleFactor, 0)
        local right = car.look:clone():cross(up)
        local test = car.look * 200 * scaleFactor
        local c1 = car.position - test / 2 - right
        local c2 = car.position - test / 2 + right
        local c3 = (car.position + test / 2 + right)
        local c4 = (car.position + test / 2 - right)
        render.setBlendMode(render.BlendMode.AlphaBlend)
        render.setCullMode(render.CullMode.Front)

        local arrowTexture = 'img/arrow-blue.png'
        if focus then arrowTexture = 'img/arrow-red.png' end
        render.quad(c1, c2, c3, c4, rgbm.colors.white, arrowTexture)
    end)

local drawCall_sector =
    (function(i, split)
        local current = ac.trackProgressToWorldCoordinate(split)
        local ahead = ac.trackProgressToWorldCoordinate(split + 0.01)
        local look = (ahead - current):normalize():add(vec3(0, 0.4, 0))

        if i == 0 then
            local currentForward = ac.trackProgressToWorldCoordinate(split + 0.001 * SCALEFACTOR)
            render.rectangle(current, look, (70 * SCALEFACTOR), 60 * SCALEFACTOR, rgbm(0.4, 0.4, 0.4, 1))
            render.rectangle(currentForward, look, (70 * SCALEFACTOR), 60 * SCALEFACTOR,
                rgbm(0.4, 0.4, 0.4, 1))
        else
            render.rectangle(current, look, (70 * SCALEFACTOR), (70 * SCALEFACTOR), rgbm(0.4, 0.4, 0.4, 1))
        end
    end)

ALL = {}

MAP_DIM_W = 2048
MAP_DIM_H = 1280


function initMap()
    if ac.getPatchVersionCode() >= 2000 then
        local all = {}
        local minX, minY, minZ, maxX, maxY, maxZ = nil, nil, nil, nil, nil, nil
        for i = 1, 1000, 1 do
            table.insert(all, ac.trackProgressToWorldCoordinate(i / 1000))
        end
        for i = 1, SIM.carsCount - 1, 1 do
            table.insert(all, ac.getCar(i).pitTransform.position)
        end
        ALL = all

        for _, v in ipairs(all) do
            if minX == nil or v.x < minX then
                minX = v.x
            end
            if minY == nil or v.y < minY then
                minY = v.y
            end
            if minZ == nil or v.z < minZ then
                minZ = v.z
            end
            if maxX == nil or v.x > maxX then
                maxX = v.x
            end
            if maxY == nil or v.y > maxY then
                maxY = v.y
            end
            if maxZ == nil or v.z > maxZ then
                maxZ = v.z
            end
        end

        CENTER_X = (minX + maxX) / 2
        CENTER_Z = (minZ + maxZ) / 2

        local width = maxX - minX
        local height = maxZ - minZ
        TRACKLENGTH_SCALED = math.max(width, height)
        local canvasAspectRatio = MAP_DIM_W / MAP_DIM_H
        local trackAspectRatio = width / height
        local subRatioPercentage = trackAspectRatio / canvasAspectRatio
        local limitingDimension = height
        if subRatioPercentage > 1 then
            limitingDimension = height * subRatioPercentage
        end

        REQUIRED_HEIGHT = (limitingDimension / 2) / math.tan(math.rad(3) / 2) * 1.1
        SCALEFACTOR = REQUIRED_HEIGHT / 40000




        local drawMesh_pits_empty = {
            mesh = ac.SimpleMesh.trackLine(1, 0.001, 0),
            shader = 'res/track_map.fx',
        }
        local drawMesh_track = {
            mesh = ac.SimpleMesh.trackLine(0, 20 * SCALEFACTOR, 0),
            shader = 'res/track_map.fx',
        }
        local drawMesh_track_outline = {
            mesh = ac.SimpleMesh.trackLine(0, 30 * SCALEFACTOR, 0),
            shader = 'res/track_outline.fx',

        }
        local drawMesh_pits = {
            mesh = ac.SimpleMesh.trackLine(1, 10 * SCALEFACTOR, 0),
            shader = 'res/track_map.fx',
        }
        local drawMesh_pits_outline = {
            mesh = ac.SimpleMesh.trackLine(1, 15 * SCALEFACTOR, 0),
            shader = 'res/track_outline.fx',
        }


        local canvasScene = {
            opaque = function()
                render.setDepthMode(render.DepthMode.ReadOnlyLessEqual)
                render.mesh(drawMesh_pits_outline)
                render.mesh(drawMesh_pits)
                render.mesh(drawMesh_track_outline)
                render.mesh(drawMesh_track)
            end
        }


        local canvasSceneEmpty = {
            opaque = function()
                render.setDepthMode(render.DepthMode.ReadOnlyLessEqual)
                render.mesh(drawMesh_pits_empty)

                for i, s in ipairs(ac.getSim().lapSplits) do
                    drawCall_sector(i, s)
                end
                local fn = drawCall_car
                for _, c in ac.iterateCars.ordered() do
                    if c.index ~= focusedCar.index then
                        fn(c, false, SCALEFACTOR)
                    end
                end
                fn(focusedCar, true, SCALEFACTOR)
            end
        }


        CANVAS = ac.GeometryShot(canvasSceneEmpty, vec2(MAP_DIM_W, MAP_DIM_H), 1, false,
            render.AntialiasingMode.None)

        CANVAS:setShadersType(render.ShadersType.Simplest):setClippingPlanes(3000, REQUIRED_HEIGHT + 1000)

        IMAGE_CANVAS = ac.GeometryShot(canvasScene, vec2(MAP_DIM_W, MAP_DIM_H), 1, false,
            render.AntialiasingMode.None)

        IMAGE_CANVAS:setShadersType(render.ShadersType.Simplest):setClippingPlanes(3000, REQUIRED_HEIGHT + 1000)
    end
end

function GeneratePNGv2(firstPass)
    if not focusedCar or focusedCar.index ~= SIM.closelyFocusedCar then
        focusedCar = CAR or ac.getCar(0)
    end
    local outputCommand = {
        p1 = vec2(),
        p2 = vec2(),
        textures = { txImage = '' },
        shader = 'res/output_map.fx',
        async = true
    }

    local canvasPosition = vec2(0, 0)
    local canvasDimensions = scaledVec2(MAP_DIM_W / 2048 * 800, MAP_DIM_H / 2048 * 800)
    local requiredHeight = REQUIRED_HEIGHT
    local fixedCamDir = vec3(0, -1, 0)  -- Camera pointing directly downwards
    local fixedCamLook = vec3(0, 0, -1) -- Assuming forward direction along the Z-axis
    local fixedCamPos = vec3(CENTER_X, requiredHeight, CENTER_Z)
    outputCommand.p1:set(canvasPosition)
    outputCommand.p2:set(canvasPosition):add(canvasDimensions)
    if firstPass then
        outputCommand.textures.txImage = IMAGE_CANVAS
        IMAGE_CANVAS:update(fixedCamPos, fixedCamDir, fixedCamLook, 3)
        ui.renderShader(outputCommand)
    else
        outputCommand.textures.txImage = CANVAS
        CANVAS:update(fixedCamPos, fixedCamDir, fixedCamLook, 3)
        ui.renderShader(outputCommand)
    end
end

REQUIRED_HEIGHT = 10000
IMG2 = nil;
function drawMapHUD()
    if not MapInit then
        initMap()
        GeneratePNGv2(true)
        IMAGE_CANVAS:accessData(function(err, data)
            IMG2 = ui.decodeImage(IMAGE_CANVAS:encode())
        end)
        MapInit = true
        return
    end

    if IMG2 then
        ui.setCursor(scaledVec2(0, 30))
        ui.image(IMG2, scaledVec2(MAP_DIM_W / 2048 * 800, MAP_DIM_H / 2048 * 800), rgbm(1, 1, 1, 1))
    end

    -- if INI.fixedPos ~= true then
    --     ui.image('img/placeholder.png', scaledVec2(MAP_DIM_W / 2048 * 800, MAP_DIM_H / 2048 * 800 + 95),
    --         rgbm(1, 1, 1, 0.1))
    -- end
    ui.setCursor(scaledVec2(0, 30))
    ui.childWindow('tst', scaledVec2(MAP_DIM_W / 2048 * 800, MAP_DIM_H / 2048 * 800), function()
        GeneratePNGv2(false)
    end)



    local windSpeedMs = math.round(SIM.windSpeedKmh / 3.6, 1)
    ui.setCursor(scaledVec2(300, 580 - 66))
    ui.beginRotation()
    ui.image('img/map_arrow.png', scaledVec2(48, 66), rgbm(1, 1, 1, 1))
    ui.endRotation(-SIM.windDirectionDeg - 90)

    ui.setCursor(scaledVec2(370, 520))
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Medium')
    ui.dwriteText(windSpeedMs, scale(46), rgbm.colors.black)
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.setCursor(scaledVec2(370, 520))
    ui.dwriteText(windSpeedMs, scale(46), rgbm.colors.white)


    local width = ui.measureDWriteText(windSpeedMs, scale(46))
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Medium')
    ui.setCursor(scaledVec2(370 + width.x / LBS + 15, 532))
    ui.dwriteText('m/s', scale(32), rgbm.colors.black)
    ui.popDWriteFont()
    ui.pushDWriteFont('MyFont:\\fonts;Weight=Regular')
    ui.setCursor(scaledVec2(370 + width.x / LBS + 15, 532))
    ui.dwriteText('m/s', scale(32), rgbm.colors.white)
    ui.popDWriteFont()
end

function script.mapHUD()
    if INI.fixedPos then
        local maxX = ac.getUI().windowSize.x
        ui.transparentWindow('mapHUDFixed', vec2(maxX - scale(800 + 120), scale(10)),
            scaledVec2(800, 800),
            true,
            function()
                -- ui.drawRectFilled(vec2(0, 0), vec2(10000, 10000), rgbm.colors.red)
                drawMapHUD()
            end)
    else
        drawMapHUD()
    end
end
