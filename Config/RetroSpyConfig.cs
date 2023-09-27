
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
        public RetrySpyButtonMappingSet Genesis { get; set; } = new RetrySpyButtonMappingSet();
        public RetrySpyButtonMappingSet Playstation { get; set; } = new RetrySpyButtonMappingSet();

        public RetrySpyButtonMappingSet GetMappingSet(RetroSpyControllerType controllerType)
        {
            switch (controllerType)
            {
                case RetroSpyControllerType.NES:
                    return NES;
                case RetroSpyControllerType.SNES:
                    return SNES;
                case RetroSpyControllerType.Genesis:
                    return Genesis;
                case RetroSpyControllerType.Playstation:
                    return Playstation;
                default:
                    return null;
            }
        }

        public void GenerateButtonMappings()
        {
            if (!NES.ButtonMappings.Any())
            {
                NES = new RetrySpyButtonMappingSet() { ControllerType = RetroSpyControllerType.NES };

                NES.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                NES.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                NES.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                NES.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                NES.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                NES.AddButton(ButtonType.A, ButtonType.A, Color.DeepSkyBlue);
                NES.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                NES.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                NES.InitOrder();
            }
            if (!SNES.ButtonMappings.Any())
            {
                SNES = new RetrySpyButtonMappingSet() { ControllerType = RetroSpyControllerType.SNES };

                SNES.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                SNES.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                SNES.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                SNES.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                SNES.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                SNES.AddButton(ButtonType.A, ButtonType.A, Color.DarkRed);
                SNES.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkGreen);
                SNES.AddButton(ButtonType.X, ButtonType.X, Color.DeepSkyBlue);
                SNES.AddButton(ButtonType.L, ButtonType.L, Color.Silver);
                SNES.AddButton(ButtonType.R, ButtonType.R, Color.Silver);
                SNES.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                SNES.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                SNES.InitOrder();
            }
            if (!Genesis.ButtonMappings.Any())
            {
                Genesis = new RetrySpyButtonMappingSet() { ControllerType = RetroSpyControllerType.Genesis };

                Genesis.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                Genesis.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                Genesis.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                Genesis.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                Genesis.AddButton(ButtonType.A, ButtonType.A, Color.Silver);
                Genesis.AddButton(ButtonType.B, ButtonType.B, Color.Silver);
                Genesis.AddButton(ButtonType.C, ButtonType.C, Color.Silver);
                Genesis.AddButton(ButtonType.X, ButtonType.X, Color.DarkSlateGray);
                Genesis.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkSlateGray);
                Genesis.AddButton(ButtonType.Z, ButtonType.Z, Color.DarkSlateGray);
                Genesis.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                Genesis.AddButton(ButtonType.MODE, ButtonType.MODE, Color.PowderBlue);
                Genesis.InitOrder();
            }
            if( !Playstation.ButtonMappings.Any())
            {
                Playstation.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                Playstation.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                Playstation.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                Playstation.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                Playstation.AddButton(ButtonType.CROSS, ButtonType.CROSS, Color.CornflowerBlue);
                Playstation.AddButton(ButtonType.CIRCLE, ButtonType.CIRCLE, Color.IndianRed);
                Playstation.AddButton(ButtonType.SQUARE, ButtonType.SQUARE, Color.Pink);
                Playstation.AddButton(ButtonType.TRIANGLE, ButtonType.TRIANGLE, Color.Teal);
                Playstation.AddButton(ButtonType.L1, ButtonType.L1, Color.Silver);
                Playstation.AddButton(ButtonType.R1, ButtonType.R1, Color.Silver);
                Playstation.AddButton(ButtonType.L2, ButtonType.L2, Color.Silver);
                Playstation.AddButton(ButtonType.R2, ButtonType.R2, Color.Silver);
                Playstation.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                Playstation.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                Playstation.InitOrder();
            }
        }
    }
}
