
using Microsoft.Xna.Framework;

namespace InputVisualizer.Config
{
    public class DisplayConfig
    {
        public Color BackgroundColor { get; set; } = Color.Black;
        public bool DisplayDuration { get; set; } = true;
        public int MinDisplayDuration { get; set; } = 2;
        public bool DisplayFrequency { get; set; } = true;
        public int MinDisplayFrequency { get; set; } = 5;
        public int DisplaySeconds { get; set; } = 4;
        public LayoutStyle Layout { get; set; } = LayoutStyle.Horizontal;
        public bool DrawIdleLines { get; set; } = true;
    }
}
