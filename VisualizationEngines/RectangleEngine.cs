using FontStashSharp;
using InputVisualizer.Config;
using InputVisualizer.Layouts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace InputVisualizer.VisualizationEngines
{
    public class RectangleEngine : VisualizerEngine
    {
        protected const int MIN_DIM_DELAY = 0;
        protected const int MAX_DIM_DELAY = 5000;

        protected const int TOP_LEFT_X = 15;
        protected const int TOP_LEFT_Y = 30;
        protected const int CONTAINER_WIDTH = 17;
        protected const int RECT_OFFSET = 2;
        protected int RECT_HEIGHT = RECT_OFFSET * 2 + 1;

        protected RectangeOrientation _orientation = RectangeOrientation.Right;
        protected int _maxContainers = -1;
        private Color _illegalInputColor;

        private Dictionary<string, RectangleContainer> _buttonContainers = new Dictionary<string, RectangleContainer>();
        private List<RectangleContainer> _emptyContainers = new List<RectangleContainer>();
        private List<RectangleContainer> _visibleContainers = new List<RectangleContainer>();
        private List<RectangleContainer> _visibleCompactButtons = new List<RectangleContainer>();

        public void SetOrientation(RectangeOrientation orientation)
        {
            _orientation = orientation;
        }

        public void UpdateContainerSettings(int maxContainers, Color emptyContainercolor, Color illegalInputColor)
        {
            _illegalInputColor = illegalInputColor;
            _maxContainers = maxContainers;
            _emptyContainers.Clear();
            for (var i = 0; i < _maxContainers; i++)
            {
                var emptyContainer = new RectangleContainer { ButtonName = "NONE", UnmappedButtonName = "NONE", IsEmptyContainer = true, Color = emptyContainercolor };
                _emptyContainers.Add(emptyContainer);
            }
            InitVisibleContainers();
        }

        public override void Clear(GameState gameState)
        {
            _buttonContainers.Clear();
            foreach (var button in gameState.ButtonStates)
            {
                _buttonContainers.Add(button.Key, new RectangleContainer { ButtonName = button.Key, Color = button.Value.Color, UnmappedButtonName = button.Value.UnmappedButtonType.ToString() });
            }
            InitVisibleContainers();
        }

        public override void Update(ViewerConfig config, GameState gameState, GameTime gameTime)
        {
            var lineLength = config.DisplayConfig.LineLength;
            var dimSpeed = config.DisplayConfig.TurnOffLineSpeed;
            var pixelAdvance = (float)gameTime.ElapsedGameTime.TotalMilliseconds * gameState.PixelsPerMs;

            foreach (var container in _buttonContainers.Values)
            {
                var stateHistory = gameState.ButtonStates[container.ButtonName];
                BuildContainerPressedVectors(container, gameState.ButtonStates[container.ButtonName], lineLength, gameState, pixelAdvance);
                container.State = container.IsContainerActive(stateHistory, dimSpeed) ? RectangleContainerState.Active : RectangleContainerState.Dim;
                container.ButtonIsCurrentlyPressed = stateHistory.IsPressed();
                if (container.ButtonIsCurrentlyPressed)
                {
                    container.ButtonPressedElapsedTime = stateHistory.PressedElapsed();
                }
            }

            var compactMode = config.DisplayConfig.MaxContainers > 0;
            if (!compactMode)
            {
                BuildFullControllerVisibleContainers(gameState);
            }
            else
            {
                BuildCompactModeVisibleContainers(gameState);
            }

            switch (_orientation)
            {
                case RectangeOrientation.Right:
                    {
                        UpdateHorizontalContainerDrawData(lineLength, gameState.DisplayIllegalInputs);
                        break;
                    }
                case RectangeOrientation.Down:
                    {
                        UpdateVerticalDownContainerDrawData(lineLength, gameState.DisplayIllegalInputs);
                        break;
                    }
                case RectangeOrientation.Up:
                    {
                        UpdateVerticalUpContainerDrawData(lineLength, gameState.DisplayIllegalInputs);
                        break;
                    }
            }

            foreach (var container in _visibleContainers)
            {
                if (container.State == RectangleContainerState.FadingIn)
                {
                    container.IncrementFadeIn();
                }
                else if (container.State == RectangleContainerState.Repositioning)
                {
                    container.DecrementRepositioningPixels();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, ViewerConfig config, GameState gameState, GameTime gameTime, CommonTextures textures)
        {
            foreach (var container in _visibleContainers)
            {
                var dimFactor = container.GetDimFactor();
                var squareInnerFactor = 0.75f;
                var buttonFactor = 1.0f;

                if (container.State == RectangleContainerState.FadingIn)
                {
                    var fadeInAmount = container.FadeInAmount;
                    dimFactor = fadeInAmount < 0.3f ? fadeInAmount : dimFactor;
                    squareInnerFactor = fadeInAmount < 0.75f ? fadeInAmount : squareInnerFactor;
                    buttonFactor = fadeInAmount < 1.0f ? fadeInAmount : buttonFactor;
                }

                var bType = container.UnmappedButtonName;
                if (textures.ButtonImages.ContainsKey(bType) && textures.ButtonImages[bType] != null)
                {
                    spriteBatch.Draw(textures.ButtonImages[bType], container.ButtonVector, container.Color * buttonFactor);
                }

                spriteBatch.Draw(textures.Pixel, container.SquareOuterRect, null, container.Color * dimFactor, 0, Vector2.Zero, SpriteEffects.None, 0);
                spriteBatch.Draw(textures.Pixel, container.SquareInnerRect, null, Color.Black * squareInnerFactor, 0, Vector2.Zero, SpriteEffects.None, 0);

                if (config.DisplayConfig.DrawIdleLines)
                {
                    spriteBatch.Draw(textures.Pixel, container.OffLineRect, null, container.Color * dimFactor, 0.0f, Vector2.Zero, SpriteEffects.None, 0);
                }

                if (container.IsEmptyContainer)
                {
                    continue;
                }

                foreach (var rect in container.DrawRects)
                {
                    spriteBatch.Draw(textures.Pixel, rect, null, container.Color, 0, Vector2.Zero, SpriteEffects.None, 0);
                }
                if (gameState.DisplayIllegalInputs)
                {
                    foreach (var rect in container.IllegalInputDrawRects)
                    {
                        spriteBatch.Draw(textures.Pixel, rect, null, _illegalInputColor, 0, Vector2.Zero, SpriteEffects.None, 0);
                    }
                }

                if (gameState.DisplayIllegalInputs && container.IllegalInputDrawRects.Count > 0)
                {
                    spriteBatch.Draw(textures.IllegalInput, container.IllegalInputVector, Color.White);
                }
                else
                {
                    if (container.ButtonIsCurrentlyPressed)
                    {
                        spriteBatch.Draw(textures.Pixel, container.SquareOuterRect, null, container.Color * 0.75f, 0, Vector2.Zero, SpriteEffects.None, 0);

                        if (config.DisplayConfig.DisplayDuration && _orientation == RectangeOrientation.Right)
                        {
                            var elapsed = container.ButtonPressedElapsedTime;
                            if (elapsed.TotalSeconds > config.DisplayConfig.MinDisplayDuration)
                            {
                                spriteBatch.DrawString(textures.Font18, elapsed.ToString("ss':'f"), container.InfoVector, container.Color);
                            }
                        }
                    }

                    if (config.DisplayConfig.DisplayFrequency)
                    {
                        if (gameState.FrequencyDict[container.ButtonName] >= config.DisplayConfig.MinDisplayFrequency)
                        {
                            spriteBatch.DrawString(textures.Font18, $"x{gameState.FrequencyDict[container.ButtonName]}", container.InfoVector, container.Color);
                        }
                    }
                }
            }

            var lineLength = config.DisplayConfig.LineLength;
            for (var i = 0; i < _visibleCompactButtons.Count; i++)
            {
                DrawCompactButton(spriteBatch, textures, _visibleCompactButtons[i], i, lineLength);
            }
        }

        private void DrawCompactButton(SpriteBatch spriteBatch, CommonTextures textures, RectangleContainer container, int position, int lineLength)
        {
            int currX;
            int currY;
            var bType = container.UnmappedButtonName;
            var buttonVector = new Vector2();

            switch (_orientation)
            {
                case RectangeOrientation.Right:
                    {
                        currX = TOP_LEFT_X + (9 * position);
                        currY = TOP_LEFT_Y + 3 + (CONTAINER_WIDTH * _maxContainers);
                        buttonVector = new Vector2(currX, currY);
                        break;
                    }
                case RectangeOrientation.Down:
                    {
                        currX = TOP_LEFT_X + 3 + (CONTAINER_WIDTH * _maxContainers);
                        currY = TOP_LEFT_Y + (9 * position);
                        buttonVector = new Vector2(currX, currY);
                        break;
                    }
                case RectangeOrientation.Up:
                    {
                        currX = TOP_LEFT_X + 3 + (CONTAINER_WIDTH * _maxContainers);
                        currY = TOP_LEFT_Y + lineLength + 36 - (9 * position);
                        buttonVector = new Vector2(currX, currY);
                        break;
                    }
            }

            if (textures.ButtonImages.ContainsKey(bType) && textures.ButtonImages[bType] != null)
            {
                var semiTransFactor = container.ButtonIsCurrentlyPressed ? 1.0f : 0.3f;
                spriteBatch.Draw(textures.ButtonImages[bType], buttonVector, container.Color * semiTransFactor);
            }
        }

        private void InitVisibleContainers()
        {
            _visibleContainers.Clear();
            for (var i = 0; i < _maxContainers; i++)
            {
                _emptyContainers[i].State = RectangleContainerState.Dim;
                _visibleContainers.Add(_emptyContainers[i]);
            }
        }

        private void UpdateHorizontalContainerDrawData(int lineLength, bool processIllegalInputs)
        {
            var currX = TOP_LEFT_X;
            var currY = TOP_LEFT_Y;

            for (var i = 0; i < _visibleContainers.Count; i++)
            {
                var container = _visibleContainers[i];

                var offset = CONTAINER_WIDTH * i;
                var targetY = TOP_LEFT_Y + offset;
                currY = targetY;
                if (container.State == RectangleContainerState.Repositioning)
                {
                    currY = TOP_LEFT_Y + offset + (int)container.RepositionRemainingPixels;
                    if (currY < targetY)
                    {
                        currY = targetY;
                    }
                }

                container.ButtonVector = new Vector2(TOP_LEFT_X, currY);
                container.SquareOuterRect.X = currX + 18;
                container.SquareOuterRect.Y = currY + 1;
                container.SquareInnerRect.X = currX + 19;
                container.SquareInnerRect.Y = currY + 2;
                container.OffLineRect.X = currX + 31;
                container.OffLineRect.Y = currY + 7;
                container.OffLineRect.Width = lineLength;
                container.OffLineRect.Height = 1;
                container.InfoVector = new Vector2(currX + 31 + lineLength + 5, currY - 1);
                container.IllegalInputVector = new Vector2(currX + 31 + lineLength + 5, currY + 1);

                foreach (var vector in container.PressedVectors.Values)
                {
                    container.DrawRects.Add(new Rectangle(currX + 31 + vector.RectStartPoint, currY + 8 - RECT_OFFSET - 1, vector.RectLength, RECT_HEIGHT));
                }
                if (processIllegalInputs)
                {
                    if (container.UnmappedButtonName == "UP" || container.UnmappedButtonName == "DOWN")
                    {
                        foreach (var vector in _buttonContainers["updown_violation"].PressedVectors.Values)
                        {
                            container.IllegalInputDrawRects.Add(new Rectangle(currX + 31 + vector.RectStartPoint, currY + 8 - RECT_OFFSET - 1, vector.RectLength, RECT_HEIGHT));
                        }
                    }
                    else if (container.UnmappedButtonName == "LEFT" || container.UnmappedButtonName == "RIGHT")
                    {
                        foreach (var vector in _buttonContainers["leftright_violation"].PressedVectors.Values)
                        {
                            container.IllegalInputDrawRects.Add(new Rectangle(currX + 31 + vector.RectStartPoint, currY + 8 - RECT_OFFSET - 1, vector.RectLength, RECT_HEIGHT));
                        }
                    }
                }
            }
        }

        private void UpdateVerticalDownContainerDrawData(int lineLength, bool processIllegalInputs)
        {
            var currX = TOP_LEFT_X;
            var currY = TOP_LEFT_Y;

            for (var i = 0; i < _visibleContainers.Count; i++)
            {
                var container = _visibleContainers[i];

                var offset = CONTAINER_WIDTH * i;
                var targetX = TOP_LEFT_X + offset;
                currX = targetX;
                if (container.State == RectangleContainerState.Repositioning)
                {
                    currX = TOP_LEFT_X + offset + (int)container.RepositionRemainingPixels;
                    if (currX < targetX)
                    {
                        currX = targetX;
                    }
                }

                container.ButtonVector = new Vector2(currX, TOP_LEFT_Y);
                container.SquareOuterRect.X = currX + 1;
                container.SquareOuterRect.Y = currY + 18;
                container.SquareInnerRect.X = currX + 2;
                container.SquareInnerRect.Y = currY + 19;
                container.OffLineRect.X = currX + 7;
                container.OffLineRect.Y = currY + 31;
                container.OffLineRect.Width = 1;
                container.OffLineRect.Height = lineLength;
                container.InfoVector = new Vector2(currX - 1, currY + 31 + lineLength + 5);
                container.IllegalInputVector = new Vector2(currX + 1, currY + 31 + lineLength + 7);

                foreach (var vector in container.PressedVectors.Values)
                {
                    container.DrawRects.Add(new Rectangle(currX + 2 + RECT_OFFSET + 1, currY + 31 + (int)vector.RectStartPoint, RECT_HEIGHT, (int)vector.RectLength));
                }
                if (processIllegalInputs)
                {
                    if (container.UnmappedButtonName == "UP" || container.UnmappedButtonName == "DOWN")
                    {
                        foreach (var vector in _buttonContainers["updown_violation"].PressedVectors.Values)
                        {
                            container.IllegalInputDrawRects.Add(new Rectangle(currX + 2 + RECT_OFFSET + 1, currY + 31 + (int)vector.RectStartPoint, RECT_HEIGHT, (int)vector.RectLength));
                        }
                    }
                    else if (container.UnmappedButtonName == "LEFT" || container.UnmappedButtonName == "RIGHT")
                    {
                        foreach (var vector in _buttonContainers["leftright_violation"].PressedVectors.Values)
                        {
                            container.IllegalInputDrawRects.Add(new Rectangle(currX + 2 + RECT_OFFSET + 1, currY + 31 + (int)vector.RectStartPoint, RECT_HEIGHT, (int)vector.RectLength));
                        }
                    }
                }
            }
        }

        private void UpdateVerticalUpContainerDrawData(int lineLength, bool processIllegalInputs)
        {
            var currX = TOP_LEFT_X;
            var currY = TOP_LEFT_Y + lineLength + 31 + 5;

            for (var i = 0; i < _visibleContainers.Count; i++)
            {
                var container = _visibleContainers[i];

                var offset = CONTAINER_WIDTH * i;
                var targetX = TOP_LEFT_X + offset;
                currX = targetX;
                if (container.State == RectangleContainerState.Repositioning)
                {
                    currX = TOP_LEFT_X + offset + (int)container.RepositionRemainingPixels;
                    if (currX < targetX)
                    {
                        currX = targetX;
                    }
                }

                container.ButtonVector = new Vector2(currX, currY);
                container.SquareOuterRect.X = currX + 1;
                container.SquareOuterRect.Y = currY - 15;
                container.SquareInnerRect.X = currX + 2;
                container.SquareInnerRect.Y = currY - 14;
                container.OffLineRect.X = currX + 7;
                container.OffLineRect.Y = currY - 15 - lineLength;
                container.OffLineRect.Width = 1;
                container.OffLineRect.Height = lineLength;
                container.InfoVector = new Vector2(currX - 1, currY - 31 - lineLength - 5);
                container.IllegalInputVector = new Vector2(currX + 1, currY - 31 - lineLength - 3);

                foreach (var vector in container.PressedVectors.Values)
                {
                    container.DrawRects.Add(new Rectangle(currX + 2 + RECT_OFFSET + 1, currY - 15 - (int)vector.RectStartPoint - (int)vector.RectLength, RECT_HEIGHT, (int)vector.RectLength));
                }
                if (processIllegalInputs)
                {
                    if (container.UnmappedButtonName == "UP" || container.UnmappedButtonName == "DOWN")
                    {
                        foreach (var vector in _buttonContainers["updown_violation"].PressedVectors.Values)
                        {
                            container.IllegalInputDrawRects.Add(new Rectangle(currX + 2 + RECT_OFFSET + 1, currY - 15 - (int)vector.RectStartPoint - (int)vector.RectLength, RECT_HEIGHT, (int)vector.RectLength));
                        }
                    }
                    else if (container.UnmappedButtonName == "LEFT" || container.UnmappedButtonName == "RIGHT")
                    {
                        foreach (var vector in _buttonContainers["leftright_violation"].PressedVectors.Values)
                        {
                            container.IllegalInputDrawRects.Add(new Rectangle(currX + 2 + RECT_OFFSET + 1, currY - 15 - (int)vector.RectStartPoint - (int)vector.RectLength, RECT_HEIGHT, (int)vector.RectLength));
                        }
                    }
                }
            }
        }

        private void BuildFullControllerVisibleContainers(GameState gameState)
        {
            _visibleContainers.Clear();
            foreach (var kvp in gameState.ButtonStates)
            {
                if (kvp.Value.IsViolationStateHistory)
                {
                    continue;
                }
                var container = _buttonContainers[kvp.Key];
                if (container == null)
                {
                    continue;
                }
                _visibleContainers.Add(container);
            }
        }

        private void BuildCompactModeVisibleContainers(GameState gameState)
        {
            var transitionAmount = 0;
            var originalVisibleButtonCount = 0;
            var activeButtonCount = 0;
            var bumpCount = 0;
            _visibleCompactButtons.Clear();

            for (var i = 0; i < _maxContainers; i++)
            {
                var container = _visibleContainers[i];

                if (container.IsEmptyContainer)
                {
                    continue;
                }
                originalVisibleButtonCount++;

                var stateHistory = gameState.ButtonStates[container.ButtonName];
                var isActive = container.IsContainerActive(stateHistory, 0.0f);

                if (isActive)
                {
                    container.IncrementRepositioningPixels(transitionAmount);
                    _visibleContainers[i - bumpCount] = container;
                    activeButtonCount++;
                }
                else
                {
                    bumpCount++;
                    transitionAmount += CONTAINER_WIDTH;
                }
            }

            if (bumpCount > 0)
            {
                var remaining = originalVisibleButtonCount - bumpCount;
                for (var i = remaining; i < remaining + bumpCount; i++)
                {
                    var container = _emptyContainers[i];
                    _visibleContainers[i] = container;
                    container.State = RectangleContainerState.FadingIn;
                    container.FadeInAmount = 0.0f;
                }
            }

            foreach (var kvp in gameState.ButtonStates)
            {
                if (kvp.Value.IsViolationStateHistory)
                {
                    continue;
                }
                var container = _buttonContainers[kvp.Key];
                if (_visibleContainers.Contains(container))
                {
                    continue;
                }

                var stateHistory = kvp.Value;
                var shouldShow = container.IsContainerActive(stateHistory, 0.0f);

                if (shouldShow)
                {
                    if (activeButtonCount < _maxContainers)
                    {
                        container.State = RectangleContainerState.Active;
                        container.RepositionRemainingPixels = 0.0f;
                        _visibleContainers[activeButtonCount] = container;
                        activeButtonCount++;
                    }
                    else
                    {
                        _visibleCompactButtons.Add(container);
                    }
                }
            }
        }

        private void BuildContainerPressedVectors(RectangleContainer container, ButtonStateHistory stateHistory, int lineLength, GameState gameState, float pixelAdvance)
        {
            var minAge = gameState.MinAge;

            container.IllegalInputDrawRects.Clear();
            container.DrawRects.Clear();

            var changes = stateHistory.GetCurrentStateHistory();
            if (changes.Length < 1)
            {
                container.PressedVectors.Clear();
                return;
            }

            for (var i = changes.Length - 1; i >= 0; i--)
            {
                if (!changes[i].IsPressed)
                {
                    continue;
                }

                var vectorKey = changes[i].StartTime.Ticks;
                var isActivelyPressed = changes[i].EndTime == DateTime.MinValue;

                if (!isActivelyPressed && changes[i].EndTime <= minAge)
                {
                    if (container.PressedVectors.ContainsKey(vectorKey))
                    {
                        container.PressedVectors.Remove(vectorKey);
                    }
                    continue;
                }

                if (!container.PressedVectors.ContainsKey(vectorKey))
                {
                    container.PressedVectors[vectorKey] = new PressedVector
                    {
                        StartPoint = 0,
                        Length = pixelAdvance
                    };
                    continue;
                }

                var pressedVector = container.PressedVectors[vectorKey];

                if (isActivelyPressed)
                {
                    pressedVector.Length += pixelAdvance;
                }
                else
                {
                    pressedVector.StartPoint += pixelAdvance;
                }

                pressedVector.RectStartPoint = (int)Math.Round(pressedVector.StartPoint);
                pressedVector.RectLength = (int)Math.Round(pressedVector.Length);

                var overflow = pressedVector.RectStartPoint + pressedVector.RectLength - lineLength;
                if (overflow > 0)
                {
                    pressedVector.RectLength -= overflow;
                }
            }
        }
    }
}
