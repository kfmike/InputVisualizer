
using System;

namespace InputVisualizer.retrospy
{
    public interface IControllerReader
    {
        event EventHandler<ControllerStateEventArgs> ControllerStateChanged;

        event EventHandler ControllerDisconnected;

        void Finish();
    }
}
