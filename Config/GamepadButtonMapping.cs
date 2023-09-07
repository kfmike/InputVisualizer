using Microsoft.Xna.Framework;

namespace InputVisualizer.Config
{
    public class GamepadButtonMapping
    {
        public ButtonType ButtonType { get; set; }
        public string Label { get; set; }
        public Color Color { get; set; }
        public ButtonType MappedButtonType { get; set; }
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; }
    }
}
