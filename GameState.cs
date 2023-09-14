using InputVisualizer.Config;
using InputVisualizer.Layouts;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace InputVisualizer
{
    public class GameState
    {
        public GamepadConfig ActiveGamepadConfig { get; set; } = null;
        public InputMode CurrentInputMode { get; set; } = InputMode.RetroSpy;
        public PlayerIndex CurrentPlayerIndex { get; set; } = PlayerIndex.One;
        public DateTime MinAge { get; set; }
        public VisualizerLayout CurrentLayout { get; set; }
        public Dictionary<string, ButtonStateHistory> ButtonStates = new Dictionary<string, ButtonStateHistory>();
        public Dictionary<string, int> FrequencyDict = new Dictionary<string, int>();
        public float PixelsPerMs { get; set; } = 0.05f;
    }
}
