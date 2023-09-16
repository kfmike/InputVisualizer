using InputVisualizer.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InputVisualizer.Layouts
{
    public class VerticalRectangleEngine : VisualizerEngine
    {
        private const int ROW_HEIGHT = 17;
        private const int RECT_OFFSET = 2;
        private int RECT_WIDTH = RECT_OFFSET * 2 + 1;

        private Dictionary<string, List<Rectangle>> _onRects = new Dictionary<string, List<Rectangle>>();

        public override void Clear(GameState gameState)
        {
            _onRects.Clear();
            foreach (var button in gameState.ButtonStates)
            {
                _onRects.Add(button.Key, new List<Rectangle>());
            }
        }

        public override void Update(ViewerConfig config, GameState gameState, GameTime gameTime)
        {
            var xPos = 18;
            var xInc = ROW_HEIGHT;
            var lineLength = config.DisplayConfig.LineLength;
            var lineStart = DateTime.Now;

            foreach (var kvp in gameState.ButtonStates)
            {
                _onRects[kvp.Key].Clear();
                var info = kvp.Value;
                var baseY = 73;

                for (var i = info.StateChangeHistory.Count - 1; i >= 0; i--)
                {
                    if (!info.StateChangeHistory[i].IsPressed)
                    {
                        continue;
                    }

                    var endTime = info.StateChangeHistory[i].EndTime == DateTime.MinValue ? lineStart : info.StateChangeHistory[i].EndTime;

                    if (endTime < gameState.MinAge)
                    {
                        break;
                    }

                    var xOffset = (lineStart - endTime).TotalMilliseconds * gameState.PixelsPerMs;
                    var startTime = info.StateChangeHistory[i].StartTime < gameState.MinAge ? gameState.MinAge : info.StateChangeHistory[i].StartTime;
                    var lengthInMs = (endTime - startTime).TotalMilliseconds;
                    var lengthInPixels = (lengthInMs * gameState.PixelsPerMs);
                    if (lengthInPixels < 1)
                    {
                        lengthInPixels = 1;
                    }

                    var y = baseY + Math.Floor(xOffset);
                    var height = lengthInPixels;
                    var maxY = baseY + lineLength;

                    if (y + height >= maxY)
                    {
                        var overflow = (y + height) - maxY;
                        height -= overflow;
                    }

                    var rec = new Rectangle();
                    rec.Y = (int)Math.Floor(y);
                    rec.X = xPos - 2 - RECT_OFFSET - 1;
                    rec.Height = (int)Math.Floor(height);
                    rec.Width = RECT_WIDTH;
                    _onRects[kvp.Key].Add(rec);
                }
                xPos += xInc;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, ViewerConfig config, GameState gameState, GameTime gameTime, CommonTextures commonTextures)
        {
            var baseY = 35;
            var xInc = ROW_HEIGHT;
            var xPos = 10;

            var squareOuterRect = new Rectangle(0, baseY + 25, 13, 13);
            var squareInnerRect = new Rectangle(0, baseY + 26, 11, 11);
            var offLineRect = new Rectangle(0, baseY + 38, 1, config.DisplayConfig.LineLength - 1);

            foreach (var kvp in gameState.ButtonStates)
            {
                var info = kvp.Value;
                var hasActiveObjects = kvp.Value.StateChangeHistory.Any();
                var semiTransFactor = hasActiveObjects ? 1.0f : 0.3f;
                var innerBoxSemiTransFactor = hasActiveObjects ? 0.75f : 0.25f;

                if (commonTextures.ButtonImages.ContainsKey(kvp.Key) && commonTextures.ButtonImages[kvp.Key] != null)
                {
                    spriteBatch.Draw(commonTextures.ButtonImages[kvp.Key], new Vector2(xPos - 2, baseY), kvp.Value.Color);
                }

                squareOuterRect.X = xPos - 1;
                squareInnerRect.X = xPos;
                spriteBatch.Draw(commonTextures.Pixel, squareOuterRect, null, info.Color * semiTransFactor, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                spriteBatch.Draw(commonTextures.Pixel, squareInnerRect, null, Color.Black * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                if (config.DisplayConfig.DrawIdleLines)
                {
                    offLineRect.X = xPos + 5;
                    spriteBatch.Draw(commonTextures.Pixel, offLineRect, null, info.Color * semiTransFactor, 0.0f, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                foreach (var rect in _onRects[kvp.Key])
                {
                    spriteBatch.Draw(commonTextures.Pixel, rect, null, info.Color, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                if (info.IsPressed())
                {
                    spriteBatch.Draw(commonTextures.Pixel, squareOuterRect, null, info.Color * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                xPos += xInc;
            }
        }
    }
}
