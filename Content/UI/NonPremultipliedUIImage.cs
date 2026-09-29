using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace LeagueOfLegendThings.Content.UI
{
    /// <summary>
    /// 使用 BlendState.NonPremultiplied 绘制贴图的 UI 元素。
    /// 避免透明 PNG 在 UI 缩放时产生边缘色溢（fringing）问题。
    /// 通过整数坐标 Rectangle 绘制，无子像素采样偏移。
    /// </summary>
    public class NonPremultipliedUIImage : UIElement
    {
        private readonly Asset<Texture2D> _texture;

        public NonPremultipliedUIImage(Asset<Texture2D> texture)
        {
            _texture = texture;
            IgnoresMouseInteraction = true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_texture?.Value == null) return;
            var tex = _texture.Value;
            CalculatedStyle dim = GetDimensions();

            float scale = MathHelper.Min(dim.Width / tex.Width, dim.Height / tex.Height);
            float drawW = tex.Width * scale;
            float drawH = tex.Height * scale;
            float x = dim.X + (dim.Width - drawW) / 2f;
            float y = dim.Y + (dim.Height - drawH) / 2f;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);

            spriteBatch.Draw(tex, new Rectangle((int)x, (int)y, (int)drawW, (int)drawH),
                null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }
    }
}
