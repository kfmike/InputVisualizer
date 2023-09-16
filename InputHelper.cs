using Microsoft.Xna.Framework.Input;
using System;

namespace InputVisualizer
{
    public static class InputHelper
    {
        public struct AnalogDpadState
        {
            public AnalogDpadState() 
            {
                UpDown = ButtonType.NONE;
                LeftRight = ButtonType.NONE;
            }
            public ButtonType UpDown { get; set; }
            public ButtonType LeftRight { get; set; }
        }

        public static AnalogDpadState GetAnalogDpadMovement( GamePadState state, float tolerance )
        {
            var x = Math.Abs(state.ThumbSticks.Left.X);
            var y = Math.Abs(state.ThumbSticks.Left.Y);

            var result = new AnalogDpadState();

            if( x > tolerance )
            {
                result.LeftRight = state.ThumbSticks.Left.X > 0 ? ButtonType.RIGHT : ButtonType.LEFT;
            }
            if( y > tolerance )
            {
                result.UpDown = state.ThumbSticks.Left.Y > 0 ? ButtonType.UP : ButtonType.DOWN;
            }
            return result;
        }
    }
}
