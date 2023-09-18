using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace InputVisualizer.Config
{
    public class GamepadConfig
    {
        public string Id { get; set; }
        public GamepadStyle Style { get; set; }
        public bool UseLStickForDpad { get; set; } = false;
        public ButtonMappingSet ButtonMappingSet { get; set; } = new ButtonMappingSet();
        public bool IsKeyboard => string.Equals(Id, "keyboard", System.StringComparison.InvariantCultureIgnoreCase);

        public void GenerateButtonMappings()
        {
            ButtonMappingSet = new ButtonMappingSet();
            ButtonMappingSet.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
            ButtonMappingSet.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
            ButtonMappingSet.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
            ButtonMappingSet.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);

            switch (Style)
            {
                case GamepadStyle.NES:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.A, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.SNES:
                    {
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.B, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.A, Color.DarkRed);
                        ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkGreen);
                        ButtonMappingSet.AddButton(ButtonType.X, ButtonType.X, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.L, ButtonType.L, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.R, ButtonType.R, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.Genesis:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.A, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.B, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.C, ButtonType.C, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.X, ButtonType.X, Color.DarkSlateGray);
                        ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.Y, Color.DarkSlateGray);
                        ButtonMappingSet.AddButton(ButtonType.Z, ButtonType.Z, Color.DarkSlateGray);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.MODE, ButtonType.MODE, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.XBOX:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.A, Color.DarkGreen);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.B, Color.DarkRed);
                        ButtonMappingSet.AddButton(ButtonType.X, ButtonType.X, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.Y, ButtonType.Y, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.L, ButtonType.L, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.R, ButtonType.R, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.LT, ButtonType.LT, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.RT, ButtonType.RT, Color.Silver);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                        break;
                    }
                case GamepadStyle.Arcade:
                    {
                        ButtonMappingSet.AddButton(ButtonType.A, ButtonType.A, Color.DarkRed);
                        ButtonMappingSet.AddButton(ButtonType.B, ButtonType.X, Color.Gold);
                        ButtonMappingSet.AddButton(ButtonType.C, ButtonType.Y, Color.DarkGreen);
                        ButtonMappingSet.AddButton(ButtonType.D, ButtonType.R, Color.DeepSkyBlue);
                        ButtonMappingSet.AddButton(ButtonType.SELECT, ButtonType.SELECT, Color.PowderBlue);
                        ButtonMappingSet.AddButton(ButtonType.START, ButtonType.START, Color.PowderBlue);
                        break;
                    }
            }

            if (IsKeyboard)
            {
                ButtonMappingSet.MapToKey(ButtonType.UP, Keys.Up);
                ButtonMappingSet.MapToKey(ButtonType.DOWN, Keys.Down);
                ButtonMappingSet.MapToKey(ButtonType.LEFT, Keys.Left);
                ButtonMappingSet.MapToKey(ButtonType.RIGHT, Keys.Right);
                ButtonMappingSet.MapToKey(ButtonType.A, Keys.A);
                ButtonMappingSet.MapToKey(ButtonType.B, Keys.S);
                ButtonMappingSet.MapToKey(ButtonType.SELECT, Keys.OemOpenBrackets);
                ButtonMappingSet.MapToKey(ButtonType.START, Keys.OemCloseBrackets);

                foreach (var mapping in ButtonMappingSet.ButtonMappings.Where(m => m.MappingType != ButtonMappingType.Key))
                {
                    mapping.MappingType = ButtonMappingType.Key;
                    mapping.MappedButtonType = ButtonType.NONE;
                }
            }
            ButtonMappingSet.InitOrder();
        }
    }
}
