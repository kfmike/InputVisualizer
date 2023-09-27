/*  
    Copyright (c) RetroSpy Technologies
*/

using System;
using System.Collections.Generic;

namespace InputVisualizer.retrospy
{
    public class ControllerStateEventArgs : EventArgs
    {
        public static readonly ControllerStateEventArgs Zero = new
            (new Dictionary<string, bool>(), new Dictionary<string, float>(), new Dictionary<string, int>());

        public string? RawPrinterData { get; }

        public IReadOnlyDictionary<string, bool> Buttons { get; private set; }
        public IReadOnlyDictionary<string, float> Analogs { get; private set; }
        public IReadOnlyDictionary<string, int> RawAnalogs { get; private set; }

        public ControllerStateEventArgs(IReadOnlyDictionary<string, bool> buttons, IReadOnlyDictionary<string, float> analogs, IReadOnlyDictionary<string, int> rawAnalogs, string? rawPrinterData = null)
        {
            RawPrinterData = rawPrinterData;
            Buttons = buttons;
            Analogs = analogs;
            RawAnalogs = rawAnalogs;
        }
    }
}
