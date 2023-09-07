using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class GamepadButtonMappingSet
    {
        public RetroSpyControllerType ControllerType;
        public List<GamepadButtonMapping> ButtonMappings { get; set; } = new List<GamepadButtonMapping>();
    }
}
