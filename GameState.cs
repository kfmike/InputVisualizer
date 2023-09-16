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
        public InputMode CurrentInputMode { get; set; } = InputMode.RetroSpy;
        public PlayerIndex CurrentPlayerIndex { get; set; } = PlayerIndex.One;
        public DateTime MinAge { get; set; }
        public VisualizerEngine CurrentLayout { get; set; }
        public Dictionary<string, ButtonStateHistory> ButtonStates = new Dictionary<string, ButtonStateHistory>();
        public Dictionary<string, int> FrequencyDict = new Dictionary<string, int>();
        public float PixelsPerMs { get; set; } = 0.05f;
        public float LineMilliseconds { get; set; } = 0f;
        private Timer _purgeTimer = null;
        private float _currentPurgeDelay = 0f;
        public float AnalogStickDeadZoneTolerance = 0.2f;

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
            _purgeTimer.AutoReset = true;
            _purgeTimer.Enabled = true;
        }

        private void OnPurgeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var button in ButtonStates.Values)
            {
                button.RemoveOldStateChanges(LineMilliseconds + _currentPurgeDelay + 500);
            }
        }

        public void UpdateMinAge(int currentLineLength)
        {
            LineMilliseconds = currentLineLength / PixelsPerMs;
            MinAge = DateTime.Now.AddMilliseconds(-LineMilliseconds);
        }

        public void UpdateSpeed(float currentSpeed)
        {
            PixelsPerMs = 0.05f * currentSpeed;
        }
    }
}
