/*  
    Copyright (c) RetroSpy Technologies
*/

using System;

namespace InputVisualizer.RetroSpy
{
    public interface IControllerReader
    {
        event EventHandler<ControllerStateEventArgs> ControllerStateChanged;
        event EventHandler ControllerDisconnected;

        void Finish();
        void Start();
    }
}
