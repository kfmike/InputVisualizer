
using Microsoft.Xna.Framework;
using System.Linq;

namespace InputVisualizer.Config
{
    public class RetroSpyConfig
    {
        public RetroSpyControllerType ControllerType { get; set; }
        public string ComPortName { get; set; }
        public GamepadButtonMappingSet NES { get; set; } = new GamepadButtonMappingSet();
        public GamepadButtonMappingSet SNES { get; set; } = new GamepadButtonMappingSet();
        public GamepadButtonMappingSet GENESIS { get; set; } = new GamepadButtonMappingSet();

        public GamepadButtonMappingSet GetMappingSet(RetroSpyControllerType controllerType)
        {
            switch (controllerType)
            {
                case RetroSpyControllerType.NES:
                    return NES;
                case RetroSpyControllerType.SNES:
                    return SNES;
                case RetroSpyControllerType.GENESIS:
                    return GENESIS;
                default:
                    return null;
            }
        }

        public void GenerateButtonMappings()
        {
            if (!NES.ButtonMappings.Any())
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
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.DarkRed, MappedButtonType = ButtonType.A, Order = 5 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.Y, Label = "Y", Color = Color.DarkGreen, MappedButtonType = ButtonType.Y, Order = 6 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.X, Label = "X", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.X, Order = 7 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.L, Label = "L", Color = Color.Silver, MappedButtonType = ButtonType.L, Order = 8 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.R, Label = "R", Color = Color.Silver, MappedButtonType = ButtonType.R, Order = 9 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.SELECT, Label = "E", Color = Color.DimGray, MappedButtonType = ButtonType.SELECT, Order = 10 });
                SNES.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 11 });
            }
            if (!GENESIS.ButtonMappings.Any())
            {
                GENESIS = new GamepadButtonMappingSet() { ControllerType = RetroSpyControllerType.GENESIS };

                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.UP, Label = "U", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.UP, Order = 0 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.DOWN, Label = "D", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.DOWN, Order = 1 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.LEFT, Label = "L", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.LEFT, Order = 2 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.RIGHT, Label = "R", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.RIGHT, Order = 3 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.Gold, MappedButtonType = ButtonType.A, Order = 4 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.B, Label = "B", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.B, Order = 5 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.C, Label = "C", Color = Color.DarkRed, MappedButtonType = ButtonType.C, Order = 6 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.X, Label = "X", Color = Color.Gold, MappedButtonType = ButtonType.X, Order = 7 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.Y, Label = "Y", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.Y, Order = 8 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.Z, Label = "Z", Color = Color.DarkRed, MappedButtonType = ButtonType.Z, Order = 9 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 10 });
                GENESIS.ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.MODE, Label = "M", Color = Color.DimGray, MappedButtonType = ButtonType.MODE, Order = 11 });
            }
        }
    }
}
