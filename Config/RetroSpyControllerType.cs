
using System.ComponentModel;

namespace InputVisualizer.Config
{
    public enum RetroSpyControllerType
    {
        NES,
        SNES,
        [Description("Genesis")]
        Genesis,
        [Description("Playstation 1/2")]
        Playstation
    }
}
