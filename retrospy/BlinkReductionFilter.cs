using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputVisualizer.retrospy
{
    public class BlinkReductionFilter
    {
        public bool ButtonEnabled { get; set; }
        public bool AnalogEnabled { get; set; }
        public bool MassEnabled { get; set; }

        private readonly List<ControllerStateEventArgs> _states = new();
        private ControllerStateEventArgs _lastUnfiltered = ControllerStateEventArgs.Zero;

        public BlinkReductionFilter()
        {
            MassEnabled = AnalogEnabled = ButtonEnabled = false;
            _states.Add(ControllerStateEventArgs.Zero);
            _states.Add(ControllerStateEventArgs.Zero);
            _states.Add(ControllerStateEventArgs.Zero);
        }

        public ControllerStateEventArgs Process(ControllerStateEventArgs state)
        {
            if (!ButtonEnabled && !AnalogEnabled && !MassEnabled)
            {
                return state;
            }

            bool revert = false;
            bool filtered = false;
            _states.RemoveAt(0); // move by
            _states.Add(state);  // one frame
            ControllerStateBuilder filteredStateBuilder = new();

            {
                uint massCounter = 0;
                foreach (string button in _states[0].Buttons.Keys)
                {
                    filteredStateBuilder.SetButton(button, _states[2].Buttons[button]);

                    if (ButtonEnabled)
                    {
                        // previous previous frame    equals      current frame
                        if (_states[0].Buttons[button] == _states[2].Buttons[button] &&
                        // AND current frame       not equals    previous frame
                        _states[2].Buttons[button] != _states[1].Buttons[button])
                        {
                            filteredStateBuilder.SetButton(button, false); // if noisy, we turn the button off
                            filtered = true;
                        }
                    }
                    if (MassEnabled)
                    {
                        if (_states[2].Buttons[button])
                        {
                            massCounter++;
                        }
                    }
                }

                foreach (string button in _states[0].Analogs.Keys)
                {
                    int rawValue = _states[2].RawAnalogs[button + "_raw"];
                    filteredStateBuilder.SetAnalog(button, _states[2].Analogs[button], rawValue);
                    if (MassEnabled)
                    {
                        if (Math.Abs(Math.Abs(_states[2].Analogs[button]) - Math.Abs(_states[1].Analogs[button])) > 0.3)
                        {
                            massCounter++;
                        }
                    }
                    if (AnalogEnabled)
                    {
                        // If we traveled over 0.5 Analog between the last three frames
                        // but less than 0.1 in the frame before
                        // we drop the change for this input
                        if (Math.Abs(_states[2].Analogs[button] - _states[1].Analogs[button]) > .5f &&
                            Math.Abs(_states[1].Analogs[button] - _states[0].Analogs[button]) < 0.1f)
                        {
                            filteredStateBuilder.SetAnalog(button, _lastUnfiltered.Analogs[button], _lastUnfiltered.RawAnalogs[button + "_raw"]);
                            filtered = true;
                        }
                    }
                }
                // if over 80% of the buttons are used we revert (this is either a reset button combo or a blink)
                if (massCounter > (_states[0].Analogs.Count + _states[0].Buttons.Count) * 0.8)
                {
                    revert = true;
                }
            }

            if (revert)
            {
                return _lastUnfiltered;
            }
            if (filtered)
            {
                return filteredStateBuilder.Build();
            }
            _lastUnfiltered = _states[2];
            return _states[2];
        }
    }
}
