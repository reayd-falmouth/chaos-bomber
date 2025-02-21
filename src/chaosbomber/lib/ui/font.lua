-- src/ui/font.lua
local Font = {}

function Font.load()
    -- m6x11 sizes: 16, 32, 48
    Font.m6x11 = {
        small = love.graphics.newFont("assets/fonts/m6x11.ttf", 16),
        medium = love.graphics.newFont("assets/fonts/m6x11.ttf", 32),
        large = love.graphics.newFont("assets/fonts/m6x11.ttf", 48)
    }

    -- m6x11plus sizes: 18, 36, 54
    Font.m6x11plus = {
        small = love.graphics.newFont("assets/fonts/m6x11plus.ttf", 18),
        medium = love.graphics.newFont("assets/fonts/m6x11plus.ttf", 36),
        large = love.graphics.newFont("assets/fonts/m6x11plus.ttf", 54)
    }
end

-- Getters for m6x11
function Font.getM6x11(size)
    if size == "small" then return Font.m6x11.small end
    if size == "medium" then return Font.m6x11.medium end
    if size == "large" then return Font.m6x11.large end
end

-- Getters for m6x11plus
function Font.getM6x11Plus(size)
    if size == "small" then return Font.m6x11plus.small end
    if size == "medium" then return Font.m6x11plus.medium end
    if size == "large" then return Font.m6x11plus.large end
end

return Font
