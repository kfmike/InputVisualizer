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
        public MisterConfig MisterConfig { get; set; } = new MisterConfig();
        public Usb2SnesConfig Usb2SnesConfig { get; set; } = new Usb2SnesConfig();
        public DisplayConfig DisplayConfig { get; set; } = new DisplayConfig();
        public string CurrentInputSource { get; set; }
        public List<GamepadConfig> GamepadConfigs { get; set; } = new List<GamepadConfig>();
        public List<JoystickConfig> JoystickConfigs { get; set; } = new List<JoystickConfig>();

        public GamepadConfig CreateGamepadConfig(string id, GamepadStyle gamepadStyle)
        {
            var config = new GamepadConfig();

            config.Id = id;
            config.Style = gamepadStyle;
            config.GenerateButtonMappings();
            GamepadConfigs.Add(config);
            return config;
        }

        public JoystickConfig CreateJoystickConfig(string id, GamepadStyle gamepadStyle)
        {
            var config = new JoystickConfig();

            config.Id = id;
            config.Style = gamepadStyle;
            config.GenerateButtonMappings();
            JoystickConfigs.Add(config);
            return config;
        }

        public void Save()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CONFIG_PATH);
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}

