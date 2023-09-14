using InputVisualizer.Config;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InputVisualizer.Layouts
{
    public class VisualizerLayout
    {
        public virtual void Clear( GameState gameState) { }
        public virtual void Update(ViewerConfig config, GameState gameState, GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch, ViewerConfig config, GameState gameState, GameTime gameTime, CommonTextures commonTextures) { }
    }
}
