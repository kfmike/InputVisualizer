# InputVisualizer

View your PC gamepad and classic retro console controller inputs in an entirely new way.
InputVisualizer allows you to see your controller input graphically over time, including press duration and button mash frequency.
Useful for speedrunners and streamers alike.

**Features**

- PC XInput/DirectInput gamepad, keyboard, and mouse support with customizable input mapping
- RetroSpy/NintendoSpy support for NES, SNES, Sega Genesis, and Playstation 1/2 consoles
- RetroSpy MiSTer script support
- SD2SNES/FXPAK PRO support for SNES hardware using Usb2Snes/QUsb2Snes/SNI
- Customizable display with background transparency for easy integration into streaming layouts
- Displays button press frequency and duration metrics
- Display frame duration metrics for the last button press, configurable per button up to a max of 20 frames
- Compact display view option to only show active buttons
- Option to display illegal d-pad input combinations such as up + down at the same time

![visualizer_sample](https://github.com/kfmike/InputVisualizer/assets/57804306/fda458a9-939a-4f20-98e3-3fccf51d01db)

# OBS Settings

Add InputVisualizer as a "Game Capture" source.

**Transparent Background**
- Check "Allow Transparency" in the game input source properties.
- Set the InputVisualizer background color alpha value to zero (default)

# Frame Counts
When configuring input mappings, there is a column named "Max Frames". If you select a value greater than zero for a specific button, the visualizer will show you how many frames the last button press was, 60fps, up to the maximum frame count specified.
So, if for example you only care about viewing frame duration for the A button up to 5 frames, you would set that button's "Max Frames" to 5, and whenever you do a press between 1f-5f, it will show up in the visualizer display next to the line.

# Compact View
By default, the visualizer will be in compact view, and only show a maximum # of lines. Buttons that are pressed but cannot appear on a line will be displayed below or next to the lines and flash to indicate activity.
If a line opens up, they will move up into the lines and the entire line history will be available for that button.

By selecting "All" in the display configuration for Max Lines, you will have the original functionality of prior versions.

# RetroSpy MiSTer Script Support
You can use this software to view inputs from a MiSTer FPGA by installing the [Retro-Spy MiSTer Script](https://retro-spy.com/wiki/setting-up-retrospy-for-the-mister/)

# SD2SNES/FXPAK PRO Game Support
Please check the following document to see an incomplete list of games that are currently supported:

https://docs.google.com/spreadsheets/d/1nq40DwiOmKDQm1oxOPezcIoIM7wi8jIx71q46V8Fz0k/edit?usp=sharing

You can update InputVisualizer with the current list by replacing the usb2snesGameList.json file with the most recent version here:

https://github.com/kfmike/InputVisualizer/blob/master/usb2snesGameList.json

# Contributing to InputVisualizer

I'm not accepting any pull requests at this time.

I don't have time to properly manage reviewing contributions, and would like to maintain and grow this project at my own pace.

Please feel free to submit issues and bug reports if you come across something that should be added or needs fixing.

This is a fun personal project for me, so I initially added features for what I do most often.
I will do my best to listen to feedback and continue to add new stuff, but it might take time.
I appreciate your patience and support!

# Credits

Game framework and UI libraries: 
  - MonoGame (https://www.monogame.net/)
  - Myra (https://github.com/rds1983/Myra)

RetroSpy signal reader code:
  - https://retro-spy.com/
  - https://github.com/retrospy/RetroSpy




