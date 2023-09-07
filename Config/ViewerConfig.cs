using System;
using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class ViewerConfig
    {
        public RetroSpyConfig RetroSpyConfig { get; set; } = new RetroSpyConfig();
        public DisplayConfig DisplayConfig { get; set; } = new DisplayConfig();
        public string CurrentInputSource { get; set; }
        public List<GamepadConfig> GamepadConfigs { get; set; } = new List<GamepadConfig>();
    }
}

