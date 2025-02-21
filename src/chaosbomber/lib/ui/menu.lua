-- src/ui/menu.lua
local Menu = {}
local background
local scaleX, scaleY
local offsetY
local shader
local totalTime = 0  -- Track time for animation effects

function Menu.load()
    background = love.graphics.newImage("assets/images/menu/background.png")

    -- Get window dimensions dynamically
    local windowWidth = love.graphics.getWidth()
    local windowHeight = love.graphics.getHeight()

    -- Get image dimensions
    local imageWidth = background:getWidth()
    local imageHeight = background:getHeight()

    -- Scale to fit the width and maintain aspect ratio
    scaleX = windowWidth / imageWidth
    scaleY = scaleX -- Maintain aspect ratio by using the same scale factor

    -- Calculate the new height
    local newHeight = imageHeight * scaleY

    -- Calculate the offset to clip excess height
    if newHeight > windowHeight then
        offsetY = (newHeight - windowHeight) / 2
    else
        offsetY = 0
    end

    -- Load the Custom CRT Shader with Bulge Effect
    shader = love.graphics.newShader("assets/shaders/CRT_bulge.fs")

    -- Send Uniforms to Shader
    shader:send("resolution", { windowWidth, windowHeight })
    shader:send("distortion_fac", 1.2)      -- Bulge Intensity
    shader:send("feather_fac", 0.05)         -- Edge Feathering
    shader:send("noise_fac", 0.02)           -- Noise Flicker
    shader:send("scanline_intensity", 0.2)   -- Scanline Intensity
    shader:send("bloom_fac", 0.1)            -- Bloom Intensity
end

function Menu.update(dt)
    -- Update total time
    totalTime = totalTime + dt

    -- Update time uniform for animation effects
    if shader:hasUniform("time") then
        shader:send("time", totalTime)
    end
end

function Menu.draw()
    -- Apply the Custom CRT Shader
    love.graphics.setShader(shader)

    -- Draw the background image with scaling and clipping
    love.graphics.setScissor(0, 0, love.graphics.getWidth(), love.graphics.getHeight())
    love.graphics.draw(background, 0, -offsetY, 0, scaleX, scaleY)
    love.graphics.setScissor()

    -- Reset the shader for text drawing
    love.graphics.setShader()

    -- Get window width dynamically
    local windowWidth = love.graphics.getWidth()

    -- Load Fonts
    local Font = require("lib.ui.font")
    local titleFont = Font.getM6x11("medium")
    local enterFont = Font.getM6x11Plus("small")

    -- Shadow Offset
    local shadowOffset = 2

    -- Draw Title Shadow
    love.graphics.setFont(titleFont)
    love.graphics.setColor(0, 0, 0, 0.5) -- Black shadow with 50% transparency
    love.graphics.printf("ChaosBomber", shadowOffset, 100 + shadowOffset, windowWidth, "center")

    -- Draw Title
    love.graphics.setColor(1, 1, 1, 1) -- White text
    love.graphics.printf("ChaosBomber", 0, 100, windowWidth, "center")

    -- Draw "Press Enter to Start" Shadow
    love.graphics.setFont(enterFont)
    love.graphics.setColor(0, 0, 0, 0.5) -- Black shadow with 50% transparency
    love.graphics.printf("Press Enter to Start", shadowOffset, 400 + shadowOffset, windowWidth, "center")

    -- Draw "Press Enter to Start"
    love.graphics.setColor(1, 1, 1, 1) -- White text
    love.graphics.printf("Press Enter to Start", 0, 400, windowWidth, "center")
end

return Menu
