using System;
using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class ViewerConfig
    {
        public RetroSpyConfig RetroSpyConfig { get; set; } = new RetroSpyConfig();
        public DisplayConfig DisplayConfig { get; set; } = new DisplayConfig();
        public ControllerType CurrentControllerType { get; set; } = ControllerType.Gamepad;
        public string CurrentGamepad { get; set; }
        public List<GamepadConfig> Gamepads { get; set; } = new List<GamepadConfig>();
    }
}

