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

        public virtual void ProcessControllerState(ControllerStateEventArgs e)
        {
            foreach (var button in e.Buttons)
            {
                if (_gameState.ButtonStates.ContainsKey(button.Key))
                {
                    if (_gameState.ButtonStates[button.Key].IsPressed() != button.Value)
                    {
                        _gameState.ButtonStates[button.Key].AddStateChange(button.Value, DateTime.Now);
                    }
                }
            }
        }
    }
}
