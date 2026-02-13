using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class CoupDeGrace : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Buffs/Coup_de_Grace";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var cdgPlayer = player.GetModPlayer<CoupDeGracePlayer>();
            cdgPlayer.HasCoupDeGrace = true;
        }
    }

    public class CoupDeGracePlayer : ModPlayer
    {
        public bool HasCoupDeGrace;

        public override void ResetEffects()
        {
            HasCoupDeGrace = false;
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.CoupDeGraceSelected)
            {
                HasCoupDeGrace = true;
            }
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            TryApplyCoupDeGrace(target, ref modifiers);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            TryApplyCoupDeGrace(target, ref modifiers);
        }

        private void TryApplyCoupDeGrace(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!HasCoupDeGrace)
                return;

            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.CoupDeGraceSelected)
                return;

            if (!target.boss || target.lifeMax <= 0)
                return;

            float lifeRatio = (float)target.life / target.lifeMax;
            if (lifeRatio < 0.4f)
            {
                // 低于40%生命时，伤害提高8%
                modifiers.FinalDamage *= 1.08f;
            }
        }

        public override void UpdateDead()
        {
            HasCoupDeGrace = false;
        }
    }
}
