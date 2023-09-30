
using Microsoft.Xna.Framework;

namespace InputVisualizer.Config
{
    public class DisplayConfig
    {
        public Color BackgroundColor { get; set; } = new Color(32, 32, 32, 0);
        public bool DisplayDuration { get; set; } = true;
        public int MinDisplayDuration { get; set; } = 2;
        public bool DisplayFrequency { get; set; } = true;
        public int MinDisplayFrequency { get; set; } = 5;
        public DisplayLayoutStyle Layout { get; set; } = DisplayLayoutStyle.Horizontal;
        public bool DrawIdleLines { get; set; } = true;
        public float Speed { get; set; } = 4;
        public int LineLength { get; set; } = 150;
        public float TurnOffLineSpeed { get; set; } = 200;
        public int MaxContainers { get; set; } = 4;
    }
}
