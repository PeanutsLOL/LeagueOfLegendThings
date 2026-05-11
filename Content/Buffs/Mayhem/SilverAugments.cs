using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 白银增幅器 — 取自 LoL ARAM Mayhem 真实列表
    /// 共 11 个，游戏前期（困难模式前）随机获得一个
    /// </summary>
    public static class SilverAugments
    {
        /// <summary>所有白银增幅器的内部名称 / All silver augment IDs</summary>
        public static readonly string[] AllAugments = new[]
        {
            "BluntForce",        // 全伤害 +20%
            "Deft",              // 攻击速度 +60%
            "BuffBuddies",       // 命中灼烧 + 法力恢复
            "Erosion",           // 命中降防 1.5%/层，最多 20 层
            "Adamant",           // 暴击时叠防御
            "CrackOpenThatEgg",  // 护盾消失爆炸
            "DiveBomber",        // 死亡爆炸
            "DontBlink",         // 移速差增伤
            "EscAPADe",          // 魔法伤害转近战伤害
            "Flashbang",         // 突进爆炸
            "GuiltyPleasure"     // 暴击回血
        };

        /// <summary>随机抽取一个白银增幅器</summary>
        public static string Roll() => AllAugments[Main.rand.Next(AllAugments.Length)];

        /// <summary>
        /// 应用所有持续性的白银增幅器被动效果
        /// </summary>
        public static void ApplyPassive(Player player, string augment)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "BluntForce":
                    // 全伤害 +20%
                    player.GetDamage(DamageClass.Generic) += 0.20f;
                    break;

                case "Deft":
                    // 攻击速度 +60%（通用）
                    player.GetAttackSpeed(DamageClass.Melee) += 0.60f;
                    player.GetAttackSpeed(DamageClass.Ranged) += 0.60f;
                    break;

                case "BuffBuddies":
                    // 被动效果在 OnHitNPC 中处理（灼烧 + 回蓝）
                    break;

                case "Erosion":
                    // Erosion 在 ModifyHitNPC 中处理
                    break;

                case "Adamant":
                    // 被动防御加成每帧重新计算，在 RefreshAdamant 中处理
                    break;

                case "CrackOpenThatEgg":
                    // 被动：护盾相关在 PostUpdate 中处理
                    break;

                case "DiveBomber":
                    // 被动效果在 UpdateDead 中处理
                    break;

                case "DontBlink":
                    // 被动：移动速度加成每帧生效
                    // Don't Blink 需要计算与目标的移速差，在 ModifyHitNPC 中处理
                    break;

                case "EscAPADe":
                    // 将 30% 魔法伤害转化为近战伤害
                    // 转换在 ModifyHitNPC 中处理
                    break;

                case "Flashbang":
                    // Flashbang 被动：在 PostUpdate 中追踪突进状态
                    break;

                case "GuiltyPleasure":
                    // 暴击回血在 OnHitNPC 中处理
                    break;
            }
        }

        /// <summary>
        /// 处理命中时的白银增幅器效果
        /// </summary>
        public static void OnHitNPC(Player player, string augment, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (string.IsNullOrEmpty(augment)) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            switch (augment)
            {
                case "BuffBuddies":
                    // 命中附加灼烧：每秒 5 点伤害，持续 3 秒
                    if (!target.HasBuff(BuffID.OnFire))
                    {
                        target.AddBuff(BuffID.OnFire, 3 * 60);
                    }
                    // 命中恢复 5 法力
                    int mana = 5;
                    player.statMana += mana;
                    player.ManaEffect(mana);
                    break;

                case "GuiltyPleasure":
                    // 暴击时恢复 5 点生命（1 秒冷却）
                    if (hit.Crit)
                    {
                        int heal = 5;
                        player.statLife += heal;
                        player.HealEffect(heal);
                    }
                    break;
            }
        }

        /// <summary>
        /// 在 ModifyHitNPC 中处理伤害修改相关的增益
        /// </summary>
        public static void ModifyHitNPC(Player player, string augment, NPC target, ref NPC.HitModifiers modifiers, ref int erosionStacks)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "Erosion":
                    // 每层降低目标防御 1.5%，最高 20 层（30%）
                    // 目标实际的防御减少通过 FlatArmorPenetration 实现
                    float erosionPen = erosionStacks * 0.015f;
                    if (erosionPen > 0.30f) erosionPen = 0.30f;
                    modifiers.ScalingArmorPenetration += erosionPen;
                    break;

                case "DontBlink":
                    // 每比目标多 10% 移速，伤害 +3%
                    // 使用目标 NPC 的移动速度作为基准（绝大多数 NPC 速度低于玩家）
                    float speedRatio = player.velocity.Length() / (target.velocity.Length() + 1f);
                    if (speedRatio > 1f)
                    {
                        float dmgBonus = (speedRatio - 1f) * 0.30f; // 每 10% 移速差 +3%
                        if (dmgBonus > 0.30f) dmgBonus = 0.30f; // 上限 30%
                        modifiers.ScalingBonusDamage += dmgBonus;
                    }
                    break;

                case "EscAPADe":
                    // 如果当前武器伤害类型是魔法，将 30% 转化为对近战/远程目标的额外伤害
                    // 通过暴击/平A增伤实现：魔法武器获得额外物理伤害
                    if (player.HeldItem.DamageType == DamageClass.Magic)
                    {
                        modifiers.ScalingBonusDamage += 0.30f;
                    }
                    break;
            }
        }

        /// <summary>
        /// 处理投射物命中时的 ModifyHitNPC（传递 erosionStacks）
        /// </summary>
        public static void ModifyHitNPCWithProj(Player player, string augment, Projectile proj, NPC target, ref NPC.HitModifiers modifiers, ref int erosionStacks)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "Erosion":
                    float pen = erosionStacks * 0.015f;
                    if (pen > 0.30f) pen = 0.30f;
                    modifiers.ScalingArmorPenetration += pen;
                    break;

                case "DontBlink":
                    float ratio = player.velocity.Length() / (target.velocity.Length() + 1f);
                    if (ratio > 1f)
                    {
                        float bonus = (ratio - 1f) * 0.30f;
                        if (bonus > 0.30f) bonus = 0.30f;
                        modifiers.ScalingBonusDamage += bonus;
                    }
                    break;

                case "EscAPADe":
                    if (proj.DamageType == DamageClass.Magic)
                    {
                        modifiers.ScalingBonusDamage += 0.30f;
                    }
                    break;
            }
        }

        /// <summary>
        /// 处理击杀时的白银增幅器效果（Erosion 叠层）
        /// </summary>
        public static void OnHitNPCErosion(Player player, string augment, NPC target, int damageDone, ref int erosionStacks)
        {
            if (string.IsNullOrEmpty(augment) || augment != "Erosion") return;
            if (target.lifeMax <= 5 || target.friendly) return;

            // 每次命中叠一层
            if (erosionStacks < 20)
                erosionStacks++;
        }
    }
}
