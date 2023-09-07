using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class ViewerConfig
    {
        public RetroSpyConfig RetroSpyConfig { get; set; } = new RetroSpyConfig();
        public DisplayConfig DisplayConfig { get; set; } = new DisplayConfig();
        public string CurrentInputSource { get; set; }
        public List<GamepadConfig> GamepadConfigs { get; set; } = new List<GamepadConfig>();

        public GamepadConfig CreateGamepadConfig( string id, GamepadStyle gamepadStyle )
        {
            var config = new GamepadConfig();

            config.Id = id;
            config.Style = gamepadStyle;
            config.GenerateButtonMappings();
            GamepadConfigs.Add(config);
            return config;
        }
    }
}

