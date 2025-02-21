-- main.lua

require("globals")         -- load global table
local MainMenu = require("lib.ui.menu")
local Font = require("lib.ui.font")

function love.load()
    Font.load()       -- Load custom fonts.
    MainMenu.load()   -- Initialize the main menu UI.
end

function love.update(dt)
    MainMenu.update(dt)  -- Update the main menu UI each frame.
end

function love.draw()
    MainMenu.draw()      -- Draw the main menu UI.
end
