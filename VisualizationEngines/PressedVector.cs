using Microsoft.Xna.Framework.Graphics;

namespace InputVisualizer.VisualizationEngines
{
    public class PressedVector
    {
        public float StartPoint {  get; set; }
        public float Length { get; set; }
        public bool LengthNormalized { get; set; }
        public int RectStartPoint { get; set; }
        public int RectLength { get; set; }
    }
}
