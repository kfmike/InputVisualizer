﻿using InputVisualizer.RetroSpy;

namespace InputVisualizer.RetroSpyStateHandlers
{
    public class PlaystationHandler : RetroSpyControllerHandler
    {
        public PlaystationHandler( GameState gameState ) : base( gameState ) { }

        public override void ProcessControllerState(ControllerStateEventArgs e)
        {
            base.ProcessControllerState(e);
        }
    }
}
