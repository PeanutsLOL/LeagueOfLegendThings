using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class CutDown : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Buffs/Cut_Down";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var cdPlayer = player.GetModPlayer<CutDownPlayer>();
            cdPlayer.HasCutDown = true;
        }
    }

    public class CutDownPlayer : ModPlayer
    {
        public bool HasCutDown;

        public override void ResetEffects()
        {
            HasCutDown = false;
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.CutDownSelected)
            {
                HasCutDown = true;
            }
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            TryApplyCutDown(target, ref modifiers);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            TryApplyCutDown(target, ref modifiers);
        }

        private void TryApplyCutDown(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!HasCutDown)
                return;

            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.CutDownSelected)
                return;

            if (!target.boss || target.lifeMax <= 0)
                return;

            float lifeRatio = (float)target.life / target.lifeMax;
            if (lifeRatio > 0.6f)
            {
                // 高于60%生命时，伤害提高8%
                modifiers.FinalDamage *= 1.08f;
            }
        }

        public override void UpdateDead()
        {
            HasCutDown = false;
        }
    }
}
