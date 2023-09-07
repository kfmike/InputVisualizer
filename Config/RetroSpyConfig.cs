﻿
using Microsoft.Xna.Framework;
using System.Linq;

namespace InputVisualizer.Config
{
    public class RetroSpyConfig
    {
        public RetroSpyControllerType ControllerType { get; set; }
        public string ComPortName { get; set; }
        public GamepadButtonMappingSet NES {  get; set; } = new GamepadButtonMappingSet();
        public GamepadButtonMappingSet SNES { get; set; } = new GamepadButtonMappingSet();

        public void GenerateButtonMappings()
        {
            if( !NES.ButtonMappings.Any())
            {
                NES = new GamepadButtonMappingSet() { ControllerType = RetroSpyControllerType.NES };

                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.UP, Label = "U", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.UP, Order = 0 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.DOWN, Label = "D", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.DOWN, Order = 1 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.LEFT, Label = "L", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.LEFT, Order = 2 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.RIGHT, Label = "R", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.RIGHT, Order = 3 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.B, Label = "B", Color = Color.Gold, MappedButtonType = ButtonType.B, Order = 4 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.A, Order = 5 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.SELECT, Label = "E", Color = Color.DimGray, MappedButtonType = ButtonType.SELECT, Order = 6 });
                NES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 7 });
            }
            if (!SNES.ButtonMappings.Any())
            {
                SNES = new GamepadButtonMappingSet() { ControllerType = RetroSpyControllerType.SNES };

                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.UP, Label = "U", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.UP, Order = 0 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.DOWN, Label = "D", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.DOWN, Order = 1 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.LEFT, Label = "L", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.LEFT, Order = 2 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.RIGHT, Label = "R", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.RIGHT, Order = 3 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.B, Label = "B", Color = Color.Gold, MappedButtonType = ButtonType.B, Order = 4 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.A, Order = 5 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.L, Label = "L", Color = Color.DarkBlue, MappedButtonType = ButtonType.L, Order = 6 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.R, Label = "R", Color = Color.DarkBlue, MappedButtonType = ButtonType.R, Order = 7 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.SELECT, Label = "E", Color = Color.DimGray, MappedButtonType = ButtonType.SELECT, Order = 8 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 9 });
            }
        }
    }
}
