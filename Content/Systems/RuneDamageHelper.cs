using System;
using Terraria;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Systems
{
    /// <summary>
    /// 符文伤害计算的共享辅助。
    /// </summary>
    public static class RuneDamageHelper
    {
        /// <summary>
        /// 获取玩家最高的单一职业伤害加成（含全伤害加成）。
        /// 例：12%近战 + 8%全伤 → 返回 0.20
        /// </summary>
        public static float GetHighestClassBonus(Player player)
        {
            float melee  = player.GetTotalDamage(DamageClass.Melee).Additive;
            float ranged = player.GetTotalDamage(DamageClass.Ranged).Additive;
            float magic  = player.GetTotalDamage(DamageClass.Magic).Additive;
            float summon = player.GetTotalDamage(DamageClass.Summon).Additive;
            return Math.Max(Math.Max(melee, ranged), Math.Max(magic, summon));
        }

        /// <summary>手持武器的原始面板伤害</summary>
        public static int GetHeldWeaponDamage(Player player)
        {
            return player.HeldItem?.damage ?? 0;
        }

        /// <summary>
        /// 目标最大生命值的百分比伤害。上限 500（防秒Boss）。
        /// </summary>
        public static int GetPercentHPDamage(NPC target, float percent)
        {
            if (target == null || !target.active) return 0;
            int raw = (int)(target.lifeMax * percent);
            return Math.Min(raw, 500);
        }
    }
}
