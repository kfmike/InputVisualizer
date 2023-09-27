
using System.ComponentModel;

namespace InputVisualizer
{
    public enum GamepadStyle
    {
        XBOX = 0,
        NES = 1,
        [Description("Neo Geo")]
        NeoGeo = 2,
        SNES = 3,
        Genesis = 4,
        Playstation = 5
    }
}
