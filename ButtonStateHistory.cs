using System;
using System.Collections.Generic;
using System.Linq;
using Color = Microsoft.Xna.Framework.Color;

namespace InputVisualizer
{
    public class ButtonStateHistory
    {
        public List<ButtonStateValue> StateChangeHistory { get; private set; } = new List<ButtonStateValue>();
        public Color Color { get; set; }
        public string Label { get; set; }
        private object _modifyLock = new object();

        public void AddStateChange(bool state, DateTime time)
        {
            if (StateChangeHistory.Any())
            {
                var last = StateChangeHistory.Last();
                last.EndTime = time;
                last.Completed = true;
            }
            lock (_modifyLock)
            {
                StateChangeHistory.Add(new ButtonStateValue { IsPressed = state, StartTime = time });
            }
        }

        public void RemoveOldStateChanges(double ms)
        {
            lock (_modifyLock)
            {
                var removeItems = new List<ButtonStateValue>();
                foreach (var change in StateChangeHistory)
                {
                    if (change.Completed && change.EndTime < DateTime.Now.AddMilliseconds(-ms))
                    {
                        removeItems.Add(change);
                    }
                    if (!change.IsPressed && change.StartTime < DateTime.Now.AddMilliseconds(-ms))
                    {
                        removeItems.Add(change);
                    }
                }
                foreach (var item in removeItems)
                {
                    StateChangeHistory.Remove(item);
                }
            }
        }

        public bool IsPressed()
        {
            if (!StateChangeHistory.Any())
            {
                return false;
            }
            return StateChangeHistory.Last().IsPressed;
        }

        public TimeSpan PressedElapsed()
        {
            if (!StateChangeHistory.Any())
            {
                return TimeSpan.Zero;
            }
            var sc = StateChangeHistory.Last();
            if (!sc.IsPressed)
            {
                return TimeSpan.Zero;
            }
            return DateTime.Now - sc.StartTime;
        }

        public int GetPressedLastSecond()
        {
            var frequency = 0;
            var oneSecondAgo = DateTime.Now.AddSeconds(-1);

            var numChanges = StateChangeHistory.Count;
            for (var i = numChanges - 1; i >= 0; i--)
            {
                if (StateChangeHistory[i].StartTime < oneSecondAgo)
                {
                    break;
                }
                if (StateChangeHistory[i].IsPressed)
                {
                    frequency++;
                }
            }
            return frequency;
        }
    }
}
