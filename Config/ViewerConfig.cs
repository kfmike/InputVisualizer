using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace InputVisualizer.Config
{
    public class ViewerConfig
    {
        public static string CONFIG_PATH = @"config.json";

        public RetroSpyConfig RetroSpyConfig { get; set; } = new RetroSpyConfig();
        public DisplayConfig DisplayConfig { get; set; } = new DisplayConfig();
        public string CurrentInputSource { get; set; }
        public List<GamepadConfig> GamepadConfigs { get; set; } = new List<GamepadConfig>();

        public GamepadConfig CreateGamepadConfig(string id, GamepadStyle gamepadStyle)
        {
            var config = new GamepadConfig();

            config.Id = id;
            config.Style = gamepadStyle;
            config.GenerateButtonMappings();
            GamepadConfigs.Add(config);
            return config;
        }

        public void Save()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CONFIG_PATH);
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}

