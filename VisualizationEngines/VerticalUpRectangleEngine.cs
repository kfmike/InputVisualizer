using FontStashSharp;
using InputVisualizer.Config;
using InputVisualizer.VisualizationEngines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace InputVisualizer.Layouts
{
    public class VerticalUpRectangleEngine : RectangleEngine
    {
        private int RECT_WIDTH = RECT_OFFSET * 2 + 1;

        public override void Update(ViewerConfig config, GameState gameState, GameTime gameTime)
        {
            var xPos = 18;
            var xInc = ROW_HEIGHT;
            var lineLength = config.DisplayConfig.LineLength;
            var lineStart = DateTime.Now;
            var baseY = 73 + lineLength + ROW_HEIGHT;

            foreach (var kvp in gameState.ButtonStates)
            {
                _onRects[kvp.Key].Clear();
                var info = kvp.Value;

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

                    var y = baseY - Math.Floor(xOffset);
                    var height = lengthInPixels;
                    var minY = baseY - lineLength;

                    if (y - height <= minY)
                    {
                        var overflow = (y - height) - minY;
                        height -= overflow + 1;
                    }

                    var rec = new Rectangle();
                    rec.Y = (int)Math.Floor(y - height - 55);
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
            var lineLength = config.DisplayConfig.LineLength;
            var baseY = 35 + lineLength;
            var xInc = ROW_HEIGHT;
            var xPos = 10;

            var squareOuterRect = new Rectangle(0, baseY - 1, 13, 13);
            var squareInnerRect = new Rectangle(0, baseY, 11, 11);
            var offLineRect = new Rectangle(0, 35, 1, lineLength - 1);
            var dimSpeed = config.DisplayConfig.TurnOffLineSpeed;

            foreach (var kvp in gameState.ButtonStates)
            {
                var info = kvp.Value;

                var dimLine = false;
                if (dimSpeed != MAX_DIM_DELAY && !_onRects[kvp.Key].Any())
                {
                    dimLine = config.DisplayConfig.TurnOffLineSpeed == MIN_DIM_DELAY || !kvp.Value.StateChangeHistory.Any();
                }

                var semiTransFactor = !dimLine ? 1.0f : 0.3f;
                var innerBoxSemiTransFactor = !dimLine ? 0.75f : 0.25f;

                var bType = kvp.Value.UnmappedButtonType.ToString();
                if (commonTextures.ButtonImages.ContainsKey(bType) && commonTextures.ButtonImages[bType] != null)
                {
                    spriteBatch.Draw(commonTextures.ButtonImages[bType], new Vector2(xPos - 2, baseY + 22), kvp.Value.Color);
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

                if (config.DisplayConfig.DisplayFrequency)
                {
                    if (gameState.FrequencyDict[kvp.Key] >= config.DisplayConfig.MinDisplayFrequency)
                    {
                        spriteBatch.DrawString(commonTextures.Font18, $"x{gameState.FrequencyDict[kvp.Key]}", new Vector2(xPos - 3, 15), info.Color);
                    }
                }

                xPos += xInc;
            }
        }
    }
}
