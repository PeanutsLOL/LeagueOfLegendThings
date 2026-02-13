using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class LastStand : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Buffs/Last_Stand";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var lsPlayer = player.GetModPlayer<LastStandPlayer>();
            lsPlayer.HasLastStand = true;
        }
    }

    public class LastStandPlayer : ModPlayer
    {
        public bool HasLastStand;

        public override void ResetEffects()
        {
            HasLastStand = false;
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.LastStandSelected)
            {
                HasLastStand = true;
            }
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            TryApplyLastStand(target, ref modifiers);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            TryApplyLastStand(target, ref modifiers);
        }

        private void TryApplyLastStand(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!HasLastStand)
                return;

            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.LastStandSelected)
                return;

            if (!target.boss)
                return;

            float healthRatio = (float)Player.statLife / Player.statLifeMax2;
            if (healthRatio >= 0.6f)
                return;

            float bonus;
            if (healthRatio <= 0.3f)
            {
                bonus = 0.11f;
            }
            else
            {
                float t = (0.6f - healthRatio) / 0.3f;
                bonus = 0.05f + (0.11f - 0.05f) * t;
            }

            modifiers.FinalDamage *= 1f + bonus;
        }

        public override void UpdateDead()
        {
            HasLastStand = false;
        }
    }
}