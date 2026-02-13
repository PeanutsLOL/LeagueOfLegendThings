using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class AbsorbLife : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // 表明这个Buff不是减益效果
            Main.debuff[Type] = false;
            // 退出世界时不保存此Buff
            Main.buffNoSave[Type] = true;
            // 显示这个Buff的剩余时间
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var absorbLifePlayer = player.GetModPlayer<AbsorbLifePlayer>();
            absorbLifePlayer.HasAbsorbLifeBuff = true;
        }
    }

    // 基础框架：标记状态 + 预留治疗逻辑
    public class AbsorbLifePlayer : ModPlayer
    {
        // 是否激活吸取生命效果（不使用 buff 栏，自实现逻辑）
        public bool HasAbsorbLifeBuff;

        public override void ResetEffects()
        {
            HasAbsorbLifeBuff = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // 检查符文是否选择，直接设置标记（不使用 buff）
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.AbsorbLifeSelected)
            {
                HasAbsorbLifeBuff = true;
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryAbsorbLife(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryAbsorbLife(target, damageDone);
        }

        private void TryAbsorbLife(NPC target, int damageDone)
        {
            if (!HasAbsorbLifeBuff)
                return;

            // 仅当符文已选择（主系或副系 Precision）才触发
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.AbsorbLifeSelected)
                return;

            // 仅当造成的伤害大于目标当前剩余血量时触发
            if (damageDone > target.life)
            {
                float healValue = (1f + 22f * (Player.statManaMax2 + Player.statLifeMax2) / 720f)
                                   + Player.lifeRegen * 2f;
                int healAmount = (int)healValue;
                if (healAmount > 0)
                {
                    Player.Heal(healAmount);
                }
            }
        }

        public override void UpdateDead()
        {
            HasAbsorbLifeBuff = false;
        }
    }
}