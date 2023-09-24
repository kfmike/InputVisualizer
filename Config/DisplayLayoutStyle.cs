
using System.ComponentModel;

namespace InputVisualizer.Config
{
    public enum DisplayLayoutStyle
    {
        Horizontal = 0,
        [Description("Vertical Down")]
        VerticalDown = 1,
        [Description("Vertical Up")]
        VerticalUp = 2
    }
}
