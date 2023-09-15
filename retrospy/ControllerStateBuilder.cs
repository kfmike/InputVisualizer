using System;
using System.Collections.Generic;
using System.Globalization;

namespace InputVisualizer.retrospy
{
    public sealed class ControllerStateBuilder
    {
        private readonly Dictionary<string, bool> _buttons = new();
        private readonly Dictionary<string, float> _analogs = new();
        private readonly Dictionary<string, int> _raw_analogs = new();
        private string? _gameboyPrinterData;
        public void SetButton(string? name, bool value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _buttons[name] = value;
            _buttons[name.ToLower(CultureInfo.CurrentUICulture)] = value;
            _buttons[name.ToUpper(CultureInfo.CurrentUICulture)] = value;
        }

        public void SetAnalog(string? name, float value, int rawValue)
        {
            if (name != null)
            {
                _analogs[name] = value;
                _raw_analogs[name + "_raw"] = rawValue;
            }
        }

        public void SetPrinterData(string data)
        {
            _gameboyPrinterData = data;
        }

        public ControllerStateEventArgs Build()
        {
            return new ControllerStateEventArgs(_buttons, _analogs, _raw_analogs, _gameboyPrinterData);
        }
    }
}
