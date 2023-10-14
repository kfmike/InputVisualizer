using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace InputVisualizer
{
    public class CommonTextures
    {
        public Texture2D Pixel {  get; set; }
        public Texture2D IllegalInput { get; set; }

        public Dictionary<string, Texture2D> ButtonImages = new Dictionary<string, Texture2D>();
        public SpriteFontBase Font18;
        public FontSystem FontSystem;

        public void Init( GraphicsDevice graphicsDevice, ContentManager content )
        {
            Pixel = new Texture2D(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Pixel.SetData(new Color[] { Color.White });

            ButtonImages.Add(ButtonType.UP.ToString(), content.Load<Texture2D>("up_button"));
            ButtonImages.Add(ButtonType.DOWN.ToString(), content.Load<Texture2D>("down_button"));
            ButtonImages.Add(ButtonType.LEFT.ToString(), content.Load<Texture2D>("left_button"));
            ButtonImages.Add(ButtonType.RIGHT.ToString(), content.Load<Texture2D>("right_button"));
            ButtonImages.Add(ButtonType.A.ToString(), content.Load<Texture2D>("a_button"));
            ButtonImages.Add(ButtonType.B.ToString(), content.Load<Texture2D>("b_button"));
            ButtonImages.Add(ButtonType.C.ToString(), content.Load<Texture2D>("c_button"));
            ButtonImages.Add(ButtonType.D.ToString(), content.Load<Texture2D>("d_button"));
            ButtonImages.Add(ButtonType.X.ToString(), content.Load<Texture2D>("x_button"));
            ButtonImages.Add(ButtonType.Y.ToString(), content.Load<Texture2D>("y_button"));
            ButtonImages.Add(ButtonType.Z.ToString(), content.Load<Texture2D>("z_button"));
            ButtonImages.Add(ButtonType.SELECT.ToString(), content.Load<Texture2D>("select_button"));
            ButtonImages.Add(ButtonType.START.ToString(), content.Load<Texture2D>("start_button"));
            ButtonImages.Add(ButtonType.L.ToString(), content.Load<Texture2D>("left_shoulder_button"));
            ButtonImages.Add(ButtonType.R.ToString(), content.Load<Texture2D>("right_shoulder_button"));
            ButtonImages.Add(ButtonType.LT.ToString(), content.Load<Texture2D>("lt_button"));
            ButtonImages.Add(ButtonType.RT.ToString(), content.Load<Texture2D>("rt_button"));
            ButtonImages.Add(ButtonType.MODE.ToString(), content.Load<Texture2D>("mode_button"));
            ButtonImages.Add(ButtonType.CROSS.ToString(), content.Load<Texture2D>("cross_button"));
            ButtonImages.Add(ButtonType.CIRCLE.ToString(), content.Load<Texture2D>("circle_button"));
            ButtonImages.Add(ButtonType.TRIANGLE.ToString(), content.Load<Texture2D>("triangle_button"));
            ButtonImages.Add(ButtonType.SQUARE.ToString(), content.Load<Texture2D>("square_button"));
            ButtonImages.Add(ButtonType.L1.ToString(), content.Load<Texture2D>("l1_button"));
            ButtonImages.Add(ButtonType.L2.ToString(), content.Load<Texture2D>("l2_button"));
            ButtonImages.Add(ButtonType.R1.ToString(), content.Load<Texture2D>("r1_button"));
            ButtonImages.Add(ButtonType.R2.ToString(), content.Load<Texture2D>("r2_button"));
            ButtonImages.Add(ButtonType.NONE.ToString(), content.Load<Texture2D>("empty_button"));

            IllegalInput = content.Load<Texture2D>("illegal_input");
            
            FontSystem = new FontSystem();
            FontSystem.AddFont(File.ReadAllBytes(@"Fonts\DroidSans.ttf"));
            Font18 = FontSystem.GetFont(18);
        }
    }
}
