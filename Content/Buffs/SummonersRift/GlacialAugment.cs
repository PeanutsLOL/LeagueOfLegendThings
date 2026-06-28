using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // Glacial Augment
    public class GlacialAugmentPlayer : ModPlayer
    {
        public bool HasGlacialAugment;

        public override void ResetEffects()
        {
            HasGlacialAugment = false;
        }

        public override void PostUpdateMiscEffects()
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            HasGlacialAugment = runeSave.GlacialAugmentSelected;
        }

        public override void PostUpdate()
        {
            if (!HasGlacialAugment)
                return;

            if (Main.rand.NextBool(20))
            {
                Dust dust = Dust.NewDustDirect(
                    Player.position,
                    Player.width,
                    Player.height,
                    DustID.Ice,
                    0f,
                    0f,
                    100,
                    default(Color),
                    1.5f
                );
                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }
        }

        public override void UpdateDead()
        {
            HasGlacialAugment = false;
        }
    }
}