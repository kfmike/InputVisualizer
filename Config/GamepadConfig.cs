using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class GamepadConfig
    {
        public string Id { get; set; }
        public GamepadStyle Style { get; set; }
        public List<GamepadButtonMapping> ButtonMappings { get; set; } = new List<GamepadButtonMapping>();

        public void GenerateButtonMappings()
        {
            ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.UP, Label = "U", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.UP, Order = 0 });
            ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.DOWN, Label = "D", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.DOWN, Order = 1 });
            ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.LEFT, Label = "L", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.LEFT, Order = 2 });
            ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.RIGHT, Label = "R", Color = Color.DarkSeaGreen, MappedButtonType = ButtonType.RIGHT, Order = 3 });

            switch (Style)
            {
                case GamepadStyle.NES:
                    {
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.A, Order = 4 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.B, Label = "B", Color = Color.Gold, MappedButtonType = ButtonType.B, Order = 5 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.SELECT, Label = "E", Color = Color.DimGray, MappedButtonType = ButtonType.SELECT, Order = 6 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 7 });

                        break;
                    }
                case GamepadStyle.XBOX:
                    {
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.DarkGreen, MappedButtonType = ButtonType.A, Order = 4 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.B, Label = "B", Color = Color.DarkRed, MappedButtonType = ButtonType.B, Order = 5 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.X, Label = "X", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.X, Order = 6 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.Y, Label = "Y", Color = Color.Gold, MappedButtonType = ButtonType.Y, Order = 7 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.L, Label = "L", Color = Color.DarkBlue, MappedButtonType = ButtonType.L, Order = 8 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.R, Label = "R", Color = Color.DarkBlue, MappedButtonType = ButtonType.R, Order = 9 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.SELECT, Label = "E", Color = Color.DimGray, MappedButtonType = ButtonType.SELECT, Order = 10 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 11 });

                        break;
                    }
                case GamepadStyle.Arcade:
                    {
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.A, Label = "A", Color = Color.DarkRed, MappedButtonType = ButtonType.A, Order = 4 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.X, Label = "B", Color = Color.Gold, MappedButtonType = ButtonType.X, Order = 5 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.Y, Label = "C", Color = Color.DarkGreen, MappedButtonType = ButtonType.Y, Order = 6 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.R, Label = "D", Color = Color.DeepSkyBlue, MappedButtonType = ButtonType.R, Order = 7 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.SELECT, Label = "E", Color = Color.DimGray, MappedButtonType = ButtonType.SELECT, Order = 8 });
                        ButtonMappings.Add(new GamepadButtonMapping { ButtonType = ButtonType.START, Label = "S", Color = Color.DimGray, MappedButtonType = ButtonType.START, Order = 9 });
                        break;
                    }
            }
        }
    }
}
