using System;
using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 统计铁砧
    /// 根据游戏进度提供持续性的属性加成，铁砧层级越高加成越多
    /// </summary>
    public static class StatBonusAnvil
    {
        /// <summary>当前可用铁砧总层级数</summary>
        public const int MaxTier = 3;

        /// <summary>
        /// 应用统计铁砧的属性加成
        /// </summary>
        /// <param name="player">目标玩家</param>
        /// <param name="tier">当前层级（0 = 无加成，1-3 = 对应层级）</param>
        public static void ApplyBonuses(Player player, int tier)
        {
            if (tier <= 0) return;

            // 层级 1：基础属性
            if (tier >= 1)
            {
                // 防御 +5
                player.statDefense += 5;
                // 最大生命 +50
                player.statLifeMax2 += 50;
            }

            // 层级 2：进阶属性
            if (tier >= 2)
            {
                // 防御 +10
                player.statDefense += 10;
                // 最大生命 +100
                player.statLifeMax2 += 100;
                // 全伤害 +5%
                player.GetDamage(DamageClass.Generic) += 0.05f;
            }

            // 层级 3：高级属性
            if (tier >= 3)
            {
                // 防御 +5
                player.statDefense += 5;
                // 最大生命 +50
                player.statLifeMax2 += 50;
                // 全伤害 +5%
                player.GetDamage(DamageClass.Generic) += 0.05f;
                // 攻击速度 +5%
                player.GetAttackSpeed(DamageClass.Generic) += 0.05f;
            }
        }

        /// <summary>
        /// 根据游戏进度计算当前铁砧层级
        /// </summary>
        /// <param name="silverUnlocked">白银是否解锁</param>
        /// <param name="goldUnlocked">黄金是否解锁</param>
        /// <param name="prismaticUnlocked">棱彩是否解锁</param>
        /// <returns>当前铁砧层级 (0-3)</returns>
        public static int CalculateTier(bool silverUnlocked, bool goldUnlocked, bool prismaticUnlocked)
        {
            int tier = 0;
            if (silverUnlocked) tier++;       // 初始：层级 1
            if (goldUnlocked) tier++;          // 困难模式：层级 2
            if (prismaticUnlocked) tier++;     // 机械 Boss：层级 3
            return Math.Min(tier, MaxTier);
        }
    }
}
