using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Systems
{
    public class LightningBolt
    {
        public Vector2[] Points;
        public int Timer;
        public int MaxTimer;
        public Color BoltColor;
        public float Width;
    }

    public class LightningBoltSystem : ModSystem
    {
        private static List<LightningBolt> activeBolts = new();

        /// <summary>
        /// 生成一组闪电特效（主线 + 分支线）
        /// </summary>
        public static void SpawnBolt(Vector2 start, Vector2 end, Color color, int duration = 12, float width = 2.5f, int segments = 10)
        {
            // 主闪电
            activeBolts.Add(new LightningBolt
            {
                Timer = duration,
                MaxTimer = duration,
                BoltColor = color,
                Width = width,
                Points = GenerateBoltPoints(start, end, segments, 25f)
            });

            // 辅助闪电（更细、偏移不同）
            activeBolts.Add(new LightningBolt
            {
                Timer = duration,
                MaxTimer = duration,
                BoltColor = color * 0.5f,
                Width = width * 0.5f,
                Points = GenerateBoltPoints(start, end, segments, 40f)
            });

            // 分支闪电
            int branchCount = Main.rand.Next(2, 4);
            for (int b = 0; b < branchCount; b++)
            {
                float splitT = Main.rand.NextFloat(0.2f, 0.7f);
                Vector2 splitPoint = Vector2.Lerp(start, end, splitT);
                Vector2 branchEnd = splitPoint + new Vector2(Main.rand.NextFloat(-70f, 70f), Main.rand.NextFloat(15f, 80f));

                activeBolts.Add(new LightningBolt
                {
                    Timer = (int)(duration * 0.7f),
                    MaxTimer = (int)(duration * 0.7f),
                    BoltColor = color * 0.4f,
                    Width = width * 0.4f,
                    Points = GenerateBoltPoints(splitPoint, branchEnd, 5, 20f)
                });
            }
        }

        private static Vector2[] GenerateBoltPoints(Vector2 start, Vector2 end, int segments, float maxOffset)
        {
            var points = new Vector2[segments + 1];
            points[0] = start;
            points[segments] = end;

            Vector2 diff = end - start;
            Vector2 perp = new Vector2(-diff.Y, diff.X);
            if (perp != Vector2.Zero)
                perp.Normalize();

            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector2 basePos = Vector2.Lerp(start, end, t);
                // 中间段偏移更大，两端偏移趋近于0
                float envelope = 4f * t * (1f - t);
                float offset = Main.rand.NextFloat(-1f, 1f) * maxOffset * envelope;
                points[i] = basePos + perp * offset;
            }

            return points;
        }

        public override void PostUpdateEverything()
        {
            for (int i = activeBolts.Count - 1; i >= 0; i--)
            {
                activeBolts[i].Timer--;
                if (activeBolts[i].Timer <= 0)
                    activeBolts.RemoveAt(i);
            }
        }

        public override void PostDrawTiles()
        {
            if (activeBolts.Count == 0)
                return;

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            foreach (var bolt in activeBolts)
            {
                float alpha = (float)bolt.Timer / bolt.MaxTimer;

                for (int i = 0; i < bolt.Points.Length - 1; i++)
                {
                    Vector2 start = bolt.Points[i] - Main.screenPosition;
                    Vector2 end = bolt.Points[i + 1] - Main.screenPosition;

                    // 绘制发光底层（宽 + 半透明）
                    DrawLine(pixel, start, end, bolt.BoltColor * alpha * 0.3f, bolt.Width * 3f);
                    // 绘制主线
                    DrawLine(pixel, start, end, bolt.BoltColor * alpha, bolt.Width);
                    // 绘制高亮芯（白色细线）
                    DrawLine(pixel, start, end, Color.White * alpha * 0.8f, bolt.Width * 0.4f);
                }
            }

            Main.spriteBatch.End();
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 0.01f)
                return;

            float rotation = (float)Math.Atan2(diff.Y, diff.X);

            Main.spriteBatch.Draw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation,
                new Vector2(0, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }
    }
}
