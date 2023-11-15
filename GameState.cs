using InputVisualizer.Config;
using InputVisualizer.Layouts;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Timers;

namespace InputVisualizer
{
    public class GameState
    {
        public GamepadConfig ActiveGamepadConfig { get; set; } = null;
        public JoystickConfig ActiveJoystickConfig { get; set; } = null;
        public InputMode CurrentInputMode { get; set; } = InputMode.XInputOrKeyboard;
        public PlayerIndex CurrentPlayerIndex { get; set; } = PlayerIndex.One;
        public int CurrentJoystickIndex { get; set; }
        public DateTime MinAge { get; set; }
        public bool ConnectedToMister { get; set; } = false;
        public VisualizerEngine CurrentLayout { get; set; }
        public Dictionary<string, ButtonStateHistory> ButtonStates = new Dictionary<string, ButtonStateHistory>();
        public Dictionary<string, int> FrequencyDict = new Dictionary<string, int>();
        public float PixelsPerMs { get; set; } = 0.05f;
        public float LineMilliseconds { get; set; } = 0f;
        private Timer _purgeTimer = null;
        private float _currentPurgeDelay = 0f;
        public float AnalogStickDeadZoneTolerance = 0.2f;
        public bool DisplayIllegalInputs { get; set; }
        public DateTime CurrentTimeStamp { get; set; } = DateTime.Now;

        public GameState()
        {
        }

        public void ResetPurgeTimer(float turnOffLineSpeed)
        {
            _currentPurgeDelay = turnOffLineSpeed;

            if (_purgeTimer != null)
            {
                _purgeTimer.Stop();
                _purgeTimer.Dispose();
            }
            _purgeTimer = new Timer(500);
            _purgeTimer.Elapsed += OnPurgeTimerElapsed;
            _purgeTimer.AutoReset = false;
            _purgeTimer.Start();
        }

        private void OnPurgeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var button in ButtonStates.Values)
            {
                button.RemoveOldStateChanges(LineMilliseconds + _currentPurgeDelay + 500);
            }
            _purgeTimer.Start();
        }

        public void UpdateMinAge(int currentLineLength)
        {
            LineMilliseconds = currentLineLength / PixelsPerMs;
            MinAge = CurrentTimeStamp.AddMilliseconds(-LineMilliseconds);
        }

        public void UpdateSpeed(float currentSpeed)
        {
            PixelsPerMs = 0.05f * currentSpeed;
        }

        public void ProcessIllegalDpadStates( DPadState dPadState, DateTime timeStamp )
        {
            if( !DisplayIllegalInputs )
            {
                return;
            }

            var upDownPressed = ButtonStates["updown_violation"].IsPressed();
            var leftRightPressed = ButtonStates["leftright_violation"].IsPressed();

            if(  upDownPressed != (dPadState.Up && dPadState.Down ) )
            {
                ButtonStates["updown_violation"].AddStateChange(dPadState.Up && dPadState.Down, timeStamp);
            }
            if (leftRightPressed != (dPadState.Left && dPadState.Right))
            {
                ButtonStates["leftright_violation"].AddStateChange(dPadState.Left && dPadState.Right, timeStamp);
            }
        }
    }
}
