using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace InputVisualizer.Config
{
    public class ButtonMapping
    {
        public ButtonType ButtonType { get; set; }
        public ButtonType MappedButtonType { get; set; }
        public Keys MappedKey { get; set; }
        public ButtonMappingType MappingType { get; set; } = ButtonMappingType.Button;
        public Color Color { get; set; }
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; }
        public int JoystickHatIndex { get; set; } = -1;
    }
}
