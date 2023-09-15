
using Microsoft.Xna.Framework;
using System.Linq;

namespace InputVisualizer.Config
{
    public class RetroSpyConfig
    {
        public RetroSpyControllerType ControllerType { get; set; }
        public string ComPortName { get; set; }
        public RetrySpyButtonMappingSet NES { get; set; } = new RetrySpyButtonMappingSet();
        public RetrySpyButtonMappingSet SNES { get; set; } = new RetrySpyButtonMappingSet();
        public RetrySpyButtonMappingSet GENESIS { get; set; } = new RetrySpyButtonMappingSet();

        public RetrySpyButtonMappingSet GetMappingSet(RetroSpyControllerType controllerType)
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
                NES = new RetrySpyButtonMappingSet() { ControllerType = RetroSpyControllerType.NES };

                NES.AddButton(ButtonType.UP, ButtonType.UP, Color.DarkSeaGreen);
                NES.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.DarkSeaGreen);
                NES.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.DarkSeaGreen);
                NES.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.DarkSeaGreen);
                NES.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                NES.AddButton(ButtonType.A, ButtonType.A, Color.DeepSkyBlue);
                NES.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.DimGray);
                NES.AddButton(ButtonType.START, ButtonType.START, Color.DimGray);
                NES.InitOrder();
            }
            if (!SNES.ButtonMappings.Any())
            {
                SNES = new RetrySpyButtonMappingSet() { ControllerType = RetroSpyControllerType.SNES };

                SNES.AddButton(ButtonType.UP, ButtonType.UP, Color.DarkSeaGreen);
                SNES.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.DarkSeaGreen);
                SNES.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.DarkSeaGreen);
                SNES.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.DarkSeaGreen);
                SNES.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                SNES.AddButton(ButtonType.A, ButtonType.A, Color.DarkRed);
                SNES.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkGreen);
                SNES.AddButton(ButtonType.X, ButtonType.X, Color.DeepSkyBlue);
                SNES.AddButton(ButtonType.L, ButtonType.L, Color.Silver);
                SNES.AddButton(ButtonType.R, ButtonType.R, Color.Silver);
                SNES.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.DimGray);
                SNES.AddButton(ButtonType.START, ButtonType.START, Color.DimGray);
                SNES.InitOrder();
            }
            if (!GENESIS.ButtonMappings.Any())
            {
                GENESIS = new RetrySpyButtonMappingSet() { ControllerType = RetroSpyControllerType.GENESIS };

                GENESIS.AddButton(ButtonType.UP, ButtonType.UP, Color.DarkSeaGreen);
                GENESIS.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.DarkSeaGreen);
                GENESIS.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.DarkSeaGreen);
                GENESIS.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.DarkSeaGreen);
                GENESIS.AddButton(ButtonType.A, ButtonType.A, Color.Silver);
                GENESIS.AddButton(ButtonType.B, ButtonType.B, Color.Silver);
                GENESIS.AddButton(ButtonType.C, ButtonType.C, Color.Silver);
                GENESIS.AddButton(ButtonType.X, ButtonType.X, Color.DarkSlateGray);
                GENESIS.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkSlateGray);
                GENESIS.AddButton(ButtonType.Z, ButtonType.Z, Color.DarkSlateGray);
                GENESIS.AddButton(ButtonType.START, ButtonType.START, Color.DimGray);
                GENESIS.AddButton(ButtonType.MODE, ButtonType.MODE, Color.DimGray);
                GENESIS.InitOrder();
            }
        }
    }
}
