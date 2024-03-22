using InputVisualizer.RetroSpy;
using System;

namespace InputVisualizer.RetroSpyStateHandlers
{
    public class RetroSpyControllerHandler
    {
        protected GameState _gameState;

        public RetroSpyControllerHandler(GameState gameState)
        {
            _gameState = gameState;
        }

        public virtual void ProcessControllerState(ControllerStateEventArgs e, int currentFrame)
        {
            var dpadState = new DPadState();
            var timeStamp = _gameState.CurrentTimeStamp;

            foreach (var button in e.Buttons)
            {
                if (!_gameState.ButtonStates.ContainsKey(button.Key))
                {
                    continue;
                }

                if (_gameState.ButtonStates[button.Key].IsPressed() != button.Value)
                {
                    _gameState.ButtonStates[button.Key].AddStateChange(button.Value, timeStamp, currentFrame);
                }

                switch (button.Key)
                {
                    case "UP":
                        {
                            dpadState.Up = button.Value;
                            break;
                        }
                    case "DOWN":
                        {
                            dpadState.Down = button.Value;
                            break;
                        }
                    case "LEFT":
                        {
                            dpadState.Left = button.Value;
                            break;
                        }
                    case "RIGHT":
                        {
                            dpadState.Right = button.Value;
                            break;
                        }
                }
            }
            _gameState.ProcessIllegalDpadStates(dpadState, timeStamp, _gameState.CurrentFrame);
        }
    }
}
