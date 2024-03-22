using InputVisualizer.RetroSpy;

namespace InputVisualizer.RetroSpyStateHandlers
{
    public class MisterHandler : RetroSpyControllerHandler
    {
        public MisterHandler(GameState gameState) : base(gameState) { }

        public override void ProcessControllerState(ControllerStateEventArgs e, int currentFrame)
        {
            base.ProcessControllerState(e, currentFrame);
        }
    }
}
