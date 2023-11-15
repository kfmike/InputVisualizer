using Microsoft.Xna.Framework;

namespace InputVisualizer.Config
{
    public class JoystickConfig
    {
        public string Id { get; set; }
        public GamepadStyle Style { get; set; } = GamepadStyle.NES;
        public bool UseLStickForDpad { get; set; } = false;

        public ButtonMappingSet ButtonMappingSet { get; set; } = new ButtonMappingSet();

        public void GenerateButtonMappings()
        {
            ButtonMappingSet = new ButtonMappingSet();
            ButtonMappingSet.AddButton(ButtonType.UP, ButtonType.NONE, Color.WhiteSmoke);
            ButtonMappingSet.AddButton(ButtonType.DOWN, ButtonType.NONE, Color.WhiteSmoke);
            ButtonMappingSet.AddButton(ButtonType.LEFT, ButtonType.NONE, Color.WhiteSmoke);
            ButtonMappingSet.AddButton(ButtonType.RIGHT, ButtonType.NONE, Color.WhiteSmoke);

            switch (Style)
            {
                case GamepadStyle.NES:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.NONE, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.NONE, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.SNES:
                    {
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.NONE, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.NONE, Color.DarkRed);
                        ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.NONE, Color.DarkGreen);
                        ButtonMappingSet.AddButton(ButtonType.X, ButtonType.NONE, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.L, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.R, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.Genesis:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.C, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.X, ButtonType.NONE, Color.DarkSlateGray);
                        ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.NONE, Color.DarkSlateGray);
                        ButtonMappingSet.AddButton(ButtonType.Z, ButtonType.NONE, Color.DarkSlateGray);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.MODE, ButtonType.NONE, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.XBOX:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.NONE, Color.DarkGreen);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.NONE, Color.DarkRed);
                        ButtonMappingSet.AddButton(ButtonType.X, ButtonType.NONE, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.NONE, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.L, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.R, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.LT, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.RT, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.Playstation:
                    {
                        ButtonMappingSet.AddButton(ButtonType.CROSS, ButtonType.NONE, Color.CornflowerBlue);
                        ButtonMappingSet.AddButton(ButtonType.CIRCLE, ButtonType.NONE, Color.IndianRed);
                        ButtonMappingSet.AddButton(ButtonType.SQUARE, ButtonType.NONE, Color.Pink);
                        ButtonMappingSet.AddButton(ButtonType.TRIANGLE, ButtonType.NONE, Color.Teal);
                        ButtonMappingSet.AddButton(ButtonType.L1, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.R1, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.L2, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.R2, ButtonType.NONE, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.NeoGeo:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.NONE, Color.DarkRed);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.NONE, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.C, ButtonType.NONE, Color.DarkGreen);
                        ButtonMappingSet.AddButton(ButtonType.D, ButtonType.NONE, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                        break;
                    }
            }
            ButtonMappingSet.InitOrder();
        }
    }
}
