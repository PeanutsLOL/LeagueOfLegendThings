using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 黄金增幅器 — 取自 LoL ARAM Mayhem 真实列表
    /// 共 10 个，困难模式时随机获得一个
    /// </summary>
    public static class GoldAugments
    {
        /// <summary>所有黄金增幅器的内部名称</summary>
        public static readonly string[] AllAugments = new[]
        {
            "AllForYou",         // 治疗效果 +30%
            "CelestialBody",     // 最大生命 +30%，伤害 -10%
            "Cerberus",          // 获得丛刃+强攻效果
            "CriticalRhythm",    // 暴击叠加攻速
            "DemonsDance",       // 移速+生命偷取
            "DoubleTap",         // 暴击触发两次命中效果
            "EscapePlan",        // 低血量护盾加速
            "Firebrand",         // 命中叠加灼烧
            "SoulSiphon",        // 暴击吸血
            "GetExcited"         // 击杀后加速加攻速
        };

        /// <summary>随机抽取一个黄金增幅器</summary>
        public static string Roll() => AllAugments[Main.rand.Next(AllAugments.Length)];

        // ============ Cerberus / CriticalRhythm 运行时状态 ============

        /// <summary>Cerberus: 目标命中计数（每 3 次触发额外伤害）</summary>
        public const int CerberusComboMax = 3;
        /// <summary>Cerberus: 额外增伤比例</summary>
        public const float CerberusBonusDmg = 0.12f;

        /// <summary>CriticalRhythm: 最大攻速叠层</summary>
        public const int CritRhythmMaxStacks = 10;
        /// <summary>CriticalRhythm: 每层攻速</summary>
        public const float CritRhythmASPerStack = 0.06f;
        /// <summary>CriticalRhythm: 叠加持续时间（帧）</summary>
        public const int CritRhythmDuration = 6 * 60;

        /// <summary>EscapePlan: 护盾持续时间（帧）</summary>
        public const int EscapePlanDuration = 5 * 60 + 30;

        // ============ 被动效果应用 ============

        public static void ApplyPassive(Player player, string augment,
            ref int critRhythmTimer, ref int critRhythmStacks,
            ref int escapePlanCooldown)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "CelestialBody":
                    // 最大生命 +30%，伤害 -10%
                    player.statLifeMax2 = (int)(player.statLifeMax2 * 1.30f);
                    player.GetDamage(DamageClass.Generic) -= 0.10f;
                    break;

                case "CriticalRhythm":
                    // 暴击叠攻速
                    if (critRhythmStacks > 0)
                    {
                        float asBonus = CritRhythmASPerStack * critRhythmStacks;
                        player.GetAttackSpeed(DamageClass.Melee) += asBonus;
                        player.GetAttackSpeed(DamageClass.Ranged) += asBonus;
                    }
                    // 计时衰减
                    if (critRhythmTimer > 0) critRhythmTimer--;
                    if (critRhythmTimer <= 0)
                    {
                        critRhythmStacks = 0;
                        critRhythmTimer = 0;
                    }
                    break;

                case "DemonsDance":
                    // 移速 +8% + 生命偷取 3%
                    player.moveSpeed += 0.08f;
                    // 生命偷取：在 OnHitNPC 中处理
                    break;

                case "EscapePlan":
                    // EscapePlan 冷却递减 + 低血量触发护盾和加速
                    if (escapePlanCooldown > 0)
                    {
                        escapePlanCooldown--;
                    }
                    else
                    {
                        float hpPercent = (float)player.statLife / player.statLifeMax2;
                        if (hpPercent < 0.35f)
                        {
                            // 获得 65% 最大生命的护盾
                            int shield = (int)(player.statLifeMax2 * 0.65f);
                            player.statLife += shield;
                            player.HealEffect(shield);

                            // 150% 移速爆发（持续衰减）
                            player.moveSpeed += 1.50f;
                            player.maxRunSpeed *= 1.50f;

                            escapePlanCooldown = 75 * 60; // 75 秒冷却

                            // 视觉反馈
                            for (int i = 0; i < 8; i++)
                            {
                                Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.SpectreStaff);
                                d.velocity = Main.rand.NextVector2Circular(4f, 4f);
                                d.noGravity = true;
                                d.scale = 1.2f;
                            }
                        }
                    }
                    break;

                case "AllForYou":
                    // 治疗 +30% 在 PostUpdate 或者 Heal 相关 hook 处理
                    // tModLoader 没有直接 intercept 治疗的方法，改为提升药水恢复量
                    player.potionDelayTime -= 20; // 轻微减少药水 CD 作为替代
                    break;

                case "GetExcited":
                    // GetExcited 状态在击杀时触发，非持续效果
                    break;

                // Cerberus: 逻辑在 OnHitNPC 中处理
                // DoubleTap: 逻辑在 OnHitNPC 中处理
                // Firebrand: 逻辑在 OnHitNPC 中处理
                // SoulSiphon: 逻辑在 OnHitNPC 中处理
            }
        }

        // ============ 命中效果 ============

        /// <summary>
        /// 处理武器命中时的黄金增幅器效果
        /// </summary>
        public static void OnHitNPC(Player player, string augment, Item item, NPC target, NPC.HitInfo hit, int damageDone,
            ref int cerberusComboCounter,
            ref int critRhythmTimer, ref int critRhythmStacks,
            ref int escapePlanCooldown,
            ref bool getExcitedActive, ref int getExcitedTimer)
        {
            if (string.IsNullOrEmpty(augment)) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            switch (augment)
            {
                case "Cerberus":
                    // 每 3 次命中触发一次额外伤害 + 攻速爆发
                    cerberusComboCounter++;
                    if (cerberusComboCounter >= CerberusComboMax)
                    {
                        cerberusComboCounter = 0;
                        int bonusDmg = (int)(damageDone * CerberusBonusDmg);
                        if (bonusDmg < 1) bonusDmg = 1;
                        target.SimpleStrikeNPC(bonusDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);

                        // 短时间攻速爆发（1.5 秒）
                        player.GetAttackSpeed(DamageClass.Generic) += 1.10f; // +110%
                        // 粒子效果
                        for (int i = 0; i < 3; i++)
                        {
                            Dust d = Dust.NewDustDirect(target.position, target.width, target.height, DustID.GoldFlame);
                            d.velocity = Main.rand.NextVector2Circular(3f, 3f);
                            d.noGravity = true;
                        }
                    }
                    break;

                case "CriticalRhythm":
                    // 暴击时叠加攻速
                    if (hit.Crit)
                    {
                        critRhythmTimer = CritRhythmDuration;
                        if (critRhythmStacks < CritRhythmMaxStacks)
                            critRhythmStacks++;
                    }
                    break;

                case "DemonsDance":
                    // 生命偷取 3%
                    int lifeSteal = (int)(damageDone * 0.03f);
                    if (lifeSteal > 0)
                    {
                        player.statLife += lifeSteal;
                        player.HealEffect(lifeSteal);
                    }
                    break;

                case "DoubleTap":
                    // 暴击时额外触发一次命中效果（再造成 30% 伤害）
                    if (hit.Crit)
                    {
                        int extraDmg = (int)(damageDone * 0.30f);
                        if (extraDmg < 1) extraDmg = 1;
                        target.SimpleStrikeNPC(extraDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                    }
                    break;

                case "Firebrand":
                    // 命中叠加灼烧（每层 0.5% 最大生命 / 秒，最多 20 层 = 10% 最大生命 / 秒）
                    int firebrandIndex = target.FindBuffIndex(BuffID.OnFire);
                    int currentStacks = 0;
                    if (firebrandIndex >= 0)
                    {
                        // 根据剩余时间估算层数: 每层 3 秒，最多 60 秒
                        currentStacks = target.buffTime[firebrandIndex] / (3 * 60);
                    }

                    if (currentStacks < 20)
                    {
                        // 重新应用灼烧，增加层数
                        int newTime = (currentStacks + 1) * 3 * 60;
                        if (newTime > 20 * 3 * 60) newTime = 20 * 3 * 60;
                        target.AddBuff(BuffID.OnFire, newTime);
                    }

                    // 修改 OnFire 的伤害逻辑需要通过 GlobalNPC 实现，这里用额外伤害模拟
                    // 每层额外 +2 火焰伤害
                    int fireDmg = currentStacks * 2;
                    if (fireDmg > 0)
                    {
                        target.SimpleStrikeNPC(fireDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                    }
                    break;

                case "SoulSiphon":
                    // 暴击恢复 5% 伤害值的生命
                    if (hit.Crit)
                    {
                        int siphonHeal = (int)(damageDone * 0.05f);
                        if (siphonHeal > 0)
                        {
                            player.statLife += siphonHeal;
                            player.HealEffect(siphonHeal);
                        }
                    }
                    break;

                case "GetExcited":
                    // GetExcited 在击杀时触发，命中不处理
                    break;
            }
        }

        /// <summary>
        /// 处理投射物命中时的黄金增幅器效果
        /// </summary>
        public static void OnHitNPCWithProj(Player player, string augment, Projectile proj, NPC target, NPC.HitInfo hit, int damageDone,
            ref int cerberusComboCounter,
            ref int critRhythmTimer, ref int critRhythmStacks,
            ref bool getExcitedActive, ref int getExcitedTimer)
        {
            if (string.IsNullOrEmpty(augment)) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            switch (augment)
            {
                case "Cerberus":
                    cerberusComboCounter++;
                    if (cerberusComboCounter >= CerberusComboMax)
                    {
                        cerberusComboCounter = 0;
                        int bonusDmg = (int)(damageDone * CerberusBonusDmg);
                        if (bonusDmg < 1) bonusDmg = 1;
                        target.SimpleStrikeNPC(bonusDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                        player.GetAttackSpeed(DamageClass.Generic) += 1.10f;
                    }
                    break;

                case "CriticalRhythm":
                    if (hit.Crit)
                    {
                        critRhythmTimer = CritRhythmDuration;
                        if (critRhythmStacks < CritRhythmMaxStacks)
                            critRhythmStacks++;
                    }
                    break;

                case "DemonsDance":
                    int lifeSteal = (int)(damageDone * 0.03f);
                    if (lifeSteal > 0)
                    {
                        player.statLife += lifeSteal;
                        player.HealEffect(lifeSteal);
                    }
                    break;

                case "DoubleTap":
                    if (hit.Crit)
                    {
                        int extraDmg = (int)(damageDone * 0.30f);
                        if (extraDmg < 1) extraDmg = 1;
                        target.SimpleStrikeNPC(extraDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                    }
                    break;

                case "SoulSiphon":
                    if (hit.Crit)
                    {
                        int siphonHeal = (int)(damageDone * 0.05f);
                        if (siphonHeal > 0)
                        {
                            player.statLife += siphonHeal;
                            player.HealEffect(siphonHeal);
                        }
                    }
                    break;

                case "GetExcited":
                    // GetExcited 在击杀时触发
                    break;
            }
        }

        // ============ 击杀效果 ============

        /// <summary>
        /// 处理击杀时的黄金增幅器效果
        /// </summary>
        public static void OnKill(Player player, string augment, NPC target, int damageDone,
            ref bool getExcitedActive, ref int getExcitedTimer)
        {
            if (string.IsNullOrEmpty(augment)) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            switch (augment)
            {
                case "GetExcited":
                    // 击杀后 4 秒内移速 +60%，攻速 +15%
                    getExcitedActive = true;
                    getExcitedTimer = 4 * 60;

                    // 视觉效果
                    for (int i = 0; i < 6; i++)
                    {
                        Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Cloud);
                        d.velocity = Main.rand.NextVector2Circular(5f, 5f);
                        d.noGravity = true;
                        d.scale = 1.2f;
                    }
                    break;
            }
        }

        /// <summary>
        /// 在 PostUpdateMiscEffects 中应用 GetExcited 的移动速度加成
        /// </summary>
        public static void ApplyGetExcited(Player player, bool active, int timer)
        {
            if (active && timer > 0)
            {
                player.moveSpeed += 0.60f;
                player.maxRunSpeed *= 1.60f;
                player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
                player.GetAttackSpeed(DamageClass.Ranged) += 0.15f;
            }
        }
    }
}
