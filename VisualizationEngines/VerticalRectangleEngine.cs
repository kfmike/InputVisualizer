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
            var yOffset = 2;
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
                    rec.X = xPos - 2 - yOffset - 1;
                    rec.Height = (int)Math.Floor(height);
                    rec.Width = yOffset * 2 + 1;
                    _onRects[kvp.Key].Add(rec);
                }
                xPos += xInc;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, ViewerConfig config, GameState gameState, GameTime gameTime, CommonTextures commonTextures)
        {
            var yPos = 35;
            var xInc = ROW_HEIGHT;
            var xPos = 10;
            var rec = Rectangle.Empty;
            var lineLength = config.DisplayConfig.LineLength;
            var labelXInc = ROW_HEIGHT;
            var labelX = xPos;

            foreach (var kvp in gameState.ButtonStates)
            {
                var info = kvp.Value;
                var semiTransFactor = kvp.Value.StateChangeHistory.Any() ? 1.0f : 0.3f;
                var innerBoxSemiTransFactor = kvp.Value.StateChangeHistory.Any() ? 0.75f : 0.25f;

                if (commonTextures.ButtonImages.ContainsKey(kvp.Key) && commonTextures.ButtonImages[kvp.Key] != null)
                {
                    spriteBatch.Draw(commonTextures.ButtonImages[kvp.Key], new Vector2(xPos - 2, yPos), kvp.Value.Color);
                }

                //empty button press rectangle
                rec.X = xPos - 1;
                rec.Y = yPos + 25;
                rec.Width = 13;
                rec.Height = 13;
                spriteBatch.Draw(commonTextures.Pixel, rec, null, info.Color * semiTransFactor, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                rec.X = xPos;
                rec.Y = yPos + 26;
                rec.Width = 11;
                rec.Height = 11;
                spriteBatch.Draw(commonTextures.Pixel, rec, null, Color.Black * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                //draw entire off line
                if (config.DisplayConfig.DrawIdleLines)
                {
                    rec.X = xPos + 5;
                    rec.Y = yPos + 38;
                    rec.Height = lineLength - 1;
                    rec.Width = 1;
                    spriteBatch.Draw(commonTextures.Pixel, rec, null, info.Color * semiTransFactor, 0.0f, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                foreach (var rect in _onRects[kvp.Key])
                {
                    spriteBatch.Draw(commonTextures.Pixel, rect, null, info.Color, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                if (info.IsPressed())
                {
                    //fill in button rect
                    rec.X = xPos - 1;
                    rec.Y = yPos + 25;
                    rec.Width = 12;
                    rec.Height = 12;
                    spriteBatch.Draw(commonTextures.Pixel, rec, null, info.Color * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                xPos += xInc;
                labelX += labelXInc;
            }
        }
    }
}
