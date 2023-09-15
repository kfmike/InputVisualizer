using FontStashSharp;
using InputVisualizer.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InputVisualizer.Layouts
{
    public class HorizontalRectangleEngine : VisualizerEngine
    {
        private const int ROW_HEIGHT = 17;
        private Dictionary<string, List<Rectangle>> _onRects = new Dictionary<string, List<Rectangle>>();

        public override void Clear( GameState gameState)
        {
            _onRects.Clear();
            foreach (var button in gameState.ButtonStates)
            {
                _onRects.Add(button.Key, new List<Rectangle>());
            }
        }

        public override void Update(ViewerConfig config, GameState gameState, GameTime gameTime )
        {
            var yPos = 52;
            var yInc = ROW_HEIGHT;
            var yOffset = 2;
            var lineLength = config.DisplayConfig.LineLength;
            var lineStart = DateTime.Now;

            foreach (var kvp in gameState.ButtonStates)
            {
                _onRects[kvp.Key].Clear();
                var info = kvp.Value;
                var baseX = 41;

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

                    var x = baseX + Math.Floor(xOffset);
                    var width = lengthInPixels;
                    var maxX = baseX + lineLength;

                    if (x + width >= maxX)
                    {
                        var overflow = (x + width) - maxX;
                        width -= overflow;
                    }

                    var rec = new Rectangle();
                    rec.X = (int)Math.Floor(x);
                    rec.Y = yPos - 2 - yOffset - 1;
                    rec.Width = (int)Math.Floor(width);
                    rec.Height = yOffset * 2 + 1;
                    _onRects[kvp.Key].Add(rec);
                }
                yPos += yInc;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, ViewerConfig config, GameState gameState, GameTime gameTime, CommonTextures commonTextures)
        {
            var yPos = 42;
            var yInc = ROW_HEIGHT;
            var rightMargin = 10;

            foreach (var kvp in gameState.ButtonStates)
            {
                var bType = kvp.Value.UnmappedButtonType.ToString();
                if (commonTextures.ButtonImages.ContainsKey(bType) && commonTextures.ButtonImages[bType] != null)
                {
                    spriteBatch.Draw(commonTextures.ButtonImages[bType], new Vector2(rightMargin, yPos), kvp.Value.Color);
                }
                yPos += yInc;
            }

            yPos = 52;
            var baseX = 41;
            var lineLength = config.DisplayConfig.LineLength;
            var infoX = baseX + lineLength + 5;

            foreach (var kvp in gameState.ButtonStates)
            {
                var info = kvp.Value;
                var rec = Rectangle.Empty;
                var semiTransFactor = kvp.Value.StateChangeHistory.Any() ? 1.0f : 0.3f;
                var innerBoxSemiTransFactor = kvp.Value.StateChangeHistory.Any() ? 0.75f : 0.25f;

                //empty button press rectangle
                rec.X = 28;
                rec.Y = yPos - 9;
                rec.Width = 13;
                rec.Height = 13;
                spriteBatch.Draw(commonTextures.Pixel, rec, null, info.Color * semiTransFactor, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                rec.X = 29;
                rec.Y = yPos - 8;
                rec.Width = 11;
                rec.Height = 11;
                spriteBatch.Draw(commonTextures.Pixel, rec, null, Color.Black * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                //draw entire off line
                if (config.DisplayConfig.DrawIdleLines)
                {
                    rec.X = baseX;
                    rec.Y = yPos - 3;
                    rec.Width = lineLength - 1;
                    rec.Height = 1;
                    spriteBatch.Draw(commonTextures.Pixel, rec, null, info.Color * semiTransFactor, 0.0f, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                foreach (var rect in _onRects[kvp.Key])
                {
                    spriteBatch.Draw(commonTextures.Pixel, rect, null, info.Color, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                if (info.IsPressed())
                {
                    //fill in button rect
                    rec.X = 28;
                    rec.Y = yPos - 9;
                    rec.Width = 12;
                    rec.Height = 12;
                    spriteBatch.Draw(commonTextures.Pixel, rec, null, info.Color * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                    if (config.DisplayConfig.DisplayDuration)
                    {
                        var elapsed = info.PressedElapsed();
                        if (elapsed.TotalSeconds > config.DisplayConfig.MinDisplayDuration)
                        {
                            spriteBatch.DrawString(commonTextures.Font18, elapsed.ToString("ss':'f"), new Vector2(infoX, yPos - 11), info.Color);
                        }
                    }
                }

                if (config.DisplayConfig.DisplayFrequency)
                {
                    if (gameState.FrequencyDict[kvp.Key] >= config.DisplayConfig.MinDisplayFrequency)
                    {
                        spriteBatch.DrawString(commonTextures.Font18, $"x{gameState.FrequencyDict[kvp.Key]}", new Vector2(infoX, yPos - 11), info.Color);
                    }
                }
                yPos += yInc;
            }
        }
    }
}
