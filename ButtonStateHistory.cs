using InputVisualizer.Config;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Color = Microsoft.Xna.Framework.Color;

namespace InputVisualizer
{
    public class ButtonStateHistory
    {
        private List<ButtonStateValue> StateChangeHistory { get; set; } = new List<ButtonStateValue>(100);
        public Color Color { get; set; }
        private readonly object _modifyLock = new();
        public ButtonMappingType MappingType { get; set; }
        public ButtonType UnmappedButtonType { get; set; }
        public int JoystickHatIndex { get; set; }
        public int JoystickAxisIndex { get; set; }
        public bool JoystickAxisDirectionIsNegative { get; set; }
        public Keys MappedKey { get; set; }
        public MouseButtonType MappedMouseButton { get; set; }
        public int StateChangeCount { get; private set; }
        public DateTime LastActiveCompletedTime { get; set; } = DateTime.MinValue;
        private bool _isPressed { get; set; }
        private ButtonStateValue _lastState { get; set; }
        public bool IsViolationStateHistory { get; set; }

        public void AddStateChange(bool state, DateTime time)
        {
            lock (_modifyLock)
            {
                if (_lastState != null)
                {
                    _lastState.EndTime = time;
                    _lastState.Completed = true;
                    LastActiveCompletedTime = _lastState.IsPressed ? DateTime.Now : DateTime.MinValue;
                }
                var newState = new ButtonStateValue { IsPressed = state, StartTime = time };
                StateChangeHistory.Add(newState);
                StateChangeCount++;
                _lastState = newState;
                _isPressed = state;
            }
        }

        public void RemoveOldStateChanges(double ms)
        {
            lock (_modifyLock)
            {
                if (StateChangeCount < 1)
                {
                    return;
                }
                var removeItems = new List<ButtonStateValue>();
                foreach (var change in StateChangeHistory)
                {
                    if (change.Completed && change.EndTime < DateTime.Now.AddMilliseconds(-ms))
                    {
                        removeItems.Add(change);
                    }
                    else if (!change.IsPressed && change.StartTime < DateTime.Now.AddMilliseconds(-ms))
                    {
                        removeItems.Add(change);
                    }
                }
                foreach (var item in removeItems)
                {
                    StateChangeHistory.Remove(item);
                    StateChangeCount--;
                }
                if (StateChangeCount == 0)
                {
                    _lastState = null;
                    _isPressed = false;
                }
            }
        }

        public bool IsPressed()
        {
            lock (_modifyLock)
            {
                return _isPressed;
            }
        }

        public TimeSpan PressedElapsed()
        {
            lock (_modifyLock)
            {
                if (StateChangeCount < 1 || !_isPressed || _lastState == null)
                {
                    return TimeSpan.Zero;
                }
                return DateTime.Now - _lastState.StartTime;
            }
        }

        public int GetPressedLastSecond(DateTime timeStamp)
        {
            lock (_modifyLock)
            {
                var frequency = 0;
                var oneSecondAgo = timeStamp.AddSeconds(-1);

                var numChanges = StateChangeHistory.Count;
                for (var i = numChanges - 1; i >= 0; i--)
                {
                    var sc = StateChangeHistory[i];
                    if (sc.StartTime < oneSecondAgo)
                    {
                        break;
                    }
                    if (sc.IsPressed)
                    {
                        frequency++;
                    }
                }
                return frequency;
            }
        }

        public ButtonStateValue[] GetCurrentStateHistory()
        {
            lock (_modifyLock)
            {
                return StateChangeHistory.ToArray();
            }
        }
    }
}
