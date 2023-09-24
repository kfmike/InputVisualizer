using InputVisualizer.Layouts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace InputVisualizer.VisualizationEngines
{
    public class RectangleEngine : VisualizerEngine
    {
        protected const int MIN_DIM_DELAY = 0;
        protected const int MAX_DIM_DELAY = 5000;

        protected const int ROW_HEIGHT = 17;
        protected const int RECT_OFFSET = 2;

        protected Dictionary<string, List<Rectangle>> _onRects = new Dictionary<string, List<Rectangle>>();

        public override void Clear(GameState gameState)
        {
            _onRects.Clear();
            foreach (var button in gameState.ButtonStates)
            {
                _onRects.Add(button.Key, new List<Rectangle>());
            }
        }
    }
}
