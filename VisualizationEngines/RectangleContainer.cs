using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InputVisualizer.VisualizationEngines
{
    public class RectangleContainer
    {
        protected const int MIN_DIM_DELAY = 0;
        protected const int MAX_DIM_DELAY = 5000;

        public string ButtonName { get; set; }
        public string UnmappedButtonName { get; set; }
        public bool ButtonIsCurrentlyPressed { get; set; }
        public int LastPressFrameCount { get; set; }
        public TimeSpan ButtonPressedElapsedTime { get; set; } = TimeSpan.Zero;
        public Color Color { get; set; } = Color.PapayaWhip;
        public Dictionary<long,PressedVector> PressedVectors { get; set; } = new Dictionary<long, PressedVector>();
        public RectangleContainerState State { get; set; } = RectangleContainerState.None;
        public int LastPosition { get; set; } = -1;
        public int CurrentPosition { get; set; } = -1;
        public bool IsEmptyContainer { get; set; } = false;
        public float RepositionRemainingPixels { get; set; } = 0.0f;
        public float FadeInAmount { get; set; } = 0.0f;
        public float FadeOutAmount {  get; set; } = 0.0f;

        public Vector2 ButtonVector = new Vector2();
        public Rectangle OffLineRect = new Rectangle(0, 0, 1, 1);
        public Rectangle SquareOuterRect = new Rectangle(0, 0, 13, 13);
        public Rectangle SquareInnerRect = new Rectangle(0, 0, 11, 11);
        public List<Rectangle> DrawRects = new List<Rectangle>();
        public List<Rectangle> IllegalInputDrawRects = new List<Rectangle>();
        public Vector2 InfoVector = new Vector2();
        public Vector2 IllegalInputVector = new Vector2();

        public bool IsContainerActive(ButtonStateHistory stateHistory, float dimSpeed)
        {
            var inactive = false;
            if (dimSpeed != MAX_DIM_DELAY && !PressedVectors.Any())
            {
                inactive = dimSpeed == MIN_DIM_DELAY || (DateTime.Now - stateHistory.LastActiveCompletedTime).TotalMilliseconds > dimSpeed;
            }
            return !inactive;
        }

        public void IncrementRepositioningPixels( int amount )
        {
            RepositionRemainingPixels += amount;
            State = RepositionRemainingPixels > 0.0f ? RectangleContainerState.Repositioning : RectangleContainerState.Active;
        }

        public void DecrementRepositioningPixels()
        {
            RepositionRemainingPixels--;
            State = RepositionRemainingPixels <= 0 ? RectangleContainerState.Active : RectangleContainerState.Repositioning;
        }

        public void IncrementFadeIn()
        {
            FadeInAmount += 0.01f;
            if (FadeInAmount >= 1.0f)
            {
                FadeInAmount = 1.0f;
                State = RectangleContainerState.Dim;
            }
        }

        public float GetDimFactor()
        {
            return State == RectangleContainerState.Active ? 1.0f : 0.3f;
        }
    }
}
