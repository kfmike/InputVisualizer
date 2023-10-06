
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
                nes.AddButton(ButtonType.B, ButtonType.NONE, Color.Gold);
                nes.AddButton(ButtonType.A, ButtonType.NONE, Color.DeepSkyBlue);
                nes.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                nes.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
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
                snes.AddButton(ButtonType.B, ButtonType.NONE, Color.Gold);
                snes.AddButton(ButtonType.A, ButtonType.NONE, Color.DarkRed);
                snes.AddButton(ButtonType.Y, ButtonType.NONE, Color.DarkGreen);
                snes.AddButton(ButtonType.X, ButtonType.NONE, Color.DeepSkyBlue);
                snes.AddButton(ButtonType.L, ButtonType.NONE, Color.Silver);
                snes.AddButton(ButtonType.R, ButtonType.NONE, Color.Silver);
                snes.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                snes.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
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
                genesis.AddButton(ButtonType.A, ButtonType.NONE, Color.Silver);
                genesis.AddButton(ButtonType.B, ButtonType.NONE, Color.Silver);
                genesis.AddButton(ButtonType.C, ButtonType.NONE, Color.Silver);
                genesis.AddButton(ButtonType.X, ButtonType.NONE, Color.DarkSlateGray);
                genesis.AddButton(ButtonType.Y, ButtonType.NONE, Color.DarkSlateGray);
                genesis.AddButton(ButtonType.Z, ButtonType.NONE, Color.DarkSlateGray);
                genesis.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
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
                neogeo.AddButton(ButtonType.A, ButtonType.NONE, Color.DarkRed);
                neogeo.AddButton(ButtonType.B, ButtonType.NONE, Color.Gold);
                neogeo.AddButton(ButtonType.C, ButtonType.NONE, Color.DarkGreen);
                neogeo.AddButton(ButtonType.D, ButtonType.NONE, Color.DeepSkyBlue);
                neogeo.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                neogeo.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
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
                playstation.AddButton(ButtonType.CROSS, ButtonType.NONE, Color.CornflowerBlue);
                playstation.AddButton(ButtonType.CIRCLE, ButtonType.NONE, Color.IndianRed);
                playstation.AddButton(ButtonType.SQUARE, ButtonType.NONE, Color.Pink);
                playstation.AddButton(ButtonType.TRIANGLE, ButtonType.NONE, Color.Teal);
                playstation.AddButton(ButtonType.L1, ButtonType.NONE, Color.Silver);
                playstation.AddButton(ButtonType.R1, ButtonType.NONE, Color.Silver);
                playstation.AddButton(ButtonType.L2, ButtonType.NONE, Color.Silver);
                playstation.AddButton(ButtonType.R2, ButtonType.NONE, Color.Silver);
                playstation.AddButton(ButtonType.SELECT, ButtonType.NONE, Color.PowderBlue);
                playstation.AddButton(ButtonType.START, ButtonType.NONE, Color.PowderBlue);
                playstation.InitOrder();
                ButtonMappingSets.Add(GamepadStyle.Playstation, playstation);
            }
        }
    }
}
