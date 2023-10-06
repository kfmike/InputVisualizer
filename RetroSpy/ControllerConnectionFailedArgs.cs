using System;

namespace InputVisualizer.RetroSpy
{
    public class ControllerConnectionFailedArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }
}
