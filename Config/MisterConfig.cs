
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace InputVisualizer.Config
{
    public class MisterConfig
    {
        public string Hostname { get; set; }
        public string Username { get; set; } = "root";
        public string Password { get; set; } = "1";
        public int Controller { get; set; } = 0;
        public GamepadStyle Style { get; set; } = GamepadStyle.NES;
        public bool UseLStickForDpad { get; set; } = false;

        public Dictionary<GamepadStyle, ButtonMappingSet> ButtonMappingSets = new Dictionary<GamepadStyle, ButtonMappingSet>();

        public ButtonMappingSet GetCurrentMappingSet()
        {
            return ButtonMappingSets.ContainsKey( Style ) ? ButtonMappingSets[ Style ] : ButtonMappingSets[GamepadStyle.NES];
        }

        public void GenerateButtonMappings()
        {
            if( !ButtonMappingSets.ContainsKey( GamepadStyle.NES ) ) 
            {
                var nes = new ButtonMappingSet();
                nes.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                nes.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                nes.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                nes.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                nes.AddButton(ButtonType.B, ButtonType.B0, Color.Gold);
                nes.AddButton(ButtonType.A, ButtonType.B1, Color.DeepSkyBlue);
                nes.AddButton(ButtonType.SELECT, ButtonType.B8, Color.PowderBlue);
                nes.AddButton(ButtonType.START, ButtonType.B9, Color.PowderBlue);
                nes.InitOrder();
                ButtonMappingSets.Add(GamepadStyle.NES, nes);
            }

            if (!ButtonMappingSets.ContainsKey(GamepadStyle.SNES))
            {
                var snes = new ButtonMappingSet();
                snes.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                snes.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                snes.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                snes.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                snes.AddButton(ButtonType.B, ButtonType.B1, Color.Gold);
                snes.AddButton(ButtonType.A, ButtonType.B2, Color.DarkRed);
                snes.AddButton(ButtonType.Y, ButtonType.B0, Color.DarkGreen);
                snes.AddButton(ButtonType.X, ButtonType.B3, Color.DeepSkyBlue);
                snes.AddButton(ButtonType.L, ButtonType.B4, Color.Silver);
                snes.AddButton(ButtonType.R, ButtonType.B5, Color.Silver);
                snes.AddButton(ButtonType.SELECT, ButtonType.B8, Color.PowderBlue);
                snes.AddButton(ButtonType.START, ButtonType.B9, Color.PowderBlue);
                snes.InitOrder();
                ButtonMappingSets.Add(GamepadStyle.SNES, snes);
            }

            if (!ButtonMappingSets.ContainsKey(GamepadStyle.Genesis))
            {
                var genesis = new ButtonMappingSet();
                genesis.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                genesis.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                genesis.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                genesis.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                genesis.AddButton(ButtonType.A, ButtonType.B1, Color.Silver);
                genesis.AddButton(ButtonType.B, ButtonType.B2, Color.Silver);
                genesis.AddButton(ButtonType.C, ButtonType.B4, Color.Silver);
                genesis.AddButton(ButtonType.X, ButtonType.B0, Color.DarkSlateGray);
                genesis.AddButton(ButtonType.Y, ButtonType.B3, Color.DarkSlateGray);
                genesis.AddButton(ButtonType.Z, ButtonType.B5, Color.DarkSlateGray);
                genesis.AddButton(ButtonType.START, ButtonType.B9, Color.PowderBlue);
                genesis.AddButton(ButtonType.MODE, ButtonType.NONE, Color.PowderBlue);
                genesis.InitOrder();
                ButtonMappingSets.Add(GamepadStyle.Genesis, genesis);
            }

            if (!ButtonMappingSets.ContainsKey(GamepadStyle.NeoGeo))
            {
                var neogeo = new ButtonMappingSet();
                neogeo.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                neogeo.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                neogeo.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                neogeo.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                neogeo.AddButton(ButtonType.A, ButtonType.B1, Color.DarkRed);
                neogeo.AddButton(ButtonType.B, ButtonType.B0, Color.Gold);
                neogeo.AddButton(ButtonType.C, ButtonType.B3, Color.DarkGreen);
                neogeo.AddButton(ButtonType.D, ButtonType.B5, Color.DeepSkyBlue);
                neogeo.AddButton(ButtonType.SELECT, ButtonType.B8, Color.PowderBlue);
                neogeo.AddButton(ButtonType.START, ButtonType.B9, Color.PowderBlue);
                neogeo.InitOrder();
                ButtonMappingSets.Add(GamepadStyle.NeoGeo, neogeo);
            }

            if (!ButtonMappingSets.ContainsKey(GamepadStyle.Playstation))
            {
                var playstation = new ButtonMappingSet();
                playstation.AddButton(ButtonType.UP, ButtonType.UP, Color.WhiteSmoke);
                playstation.AddButton(ButtonType.DOWN, ButtonType.DOWN, Color.WhiteSmoke);
                playstation.AddButton(ButtonType.LEFT, ButtonType.LEFT, Color.WhiteSmoke);
                playstation.AddButton(ButtonType.RIGHT, ButtonType.RIGHT, Color.WhiteSmoke);
                playstation.AddButton(ButtonType.CROSS, ButtonType.B1, Color.CornflowerBlue);
                playstation.AddButton(ButtonType.CIRCLE, ButtonType.B2, Color.IndianRed);
                playstation.AddButton(ButtonType.SQUARE, ButtonType.B0, Color.Pink);
                playstation.AddButton(ButtonType.TRIANGLE, ButtonType.B3, Color.Teal);
                playstation.AddButton(ButtonType.L1, ButtonType.B4, Color.Silver);
                playstation.AddButton(ButtonType.R1, ButtonType.B5, Color.Silver);
                playstation.AddButton(ButtonType.L2, ButtonType.B6, Color.Silver);
                playstation.AddButton(ButtonType.R2, ButtonType.B7, Color.Silver);
                playstation.AddButton(ButtonType.SELECT, ButtonType.B8, Color.PowderBlue);
                playstation.AddButton(ButtonType.START, ButtonType.B9, Color.PowderBlue);
                playstation.InitOrder();
                ButtonMappingSets.Add(GamepadStyle.Playstation, playstation);
            }
        }
    }
}
