using Microsoft.Xna.Framework;
using System.Linq;

namespace InputVisualizer.Config
{
    public class Usb2SnesConfig
    {
        public ButtonMappingSet ButtonMappingSet { get; set; } = new ButtonMappingSet();
        
        public void GenerateButtonMappings()
        {
            if (!ButtonMappingSet.ButtonMappings.Any())
            {
                ButtonMappingSet.AddButton(ButtonType.UP, ButtonType.UP, Color.LightGreen);
                ButtonMappingSet.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.LightGreen);
                ButtonMappingSet.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.LightGreen);
                ButtonMappingSet.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.LightGreen);
                ButtonMappingSet.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                ButtonMappingSet.AddButton(ButtonType.A, ButtonType.A, Color.DarkRed);
                ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkGreen);
                ButtonMappingSet.AddButton(ButtonType.X, ButtonType.X, Color.DeepSkyBlue);
                ButtonMappingSet.AddButton(ButtonType.L, ButtonType.L, Color.Silver);
                ButtonMappingSet.AddButton(ButtonType.R, ButtonType.R, Color.Silver);
                ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                ButtonMappingSet.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                ButtonMappingSet.InitOrder();
            }
        }
    }
}
