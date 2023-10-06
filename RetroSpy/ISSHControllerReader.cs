using System;

namespace InputVisualizer.RetroSpy
{
    public interface ISSHControllerReader : IControllerReader
    {
        public event EventHandler ControllerConnected;
        public event EventHandler<ControllerConnectionFailedArgs> ControllerConnectionFailed;
    }
}
