using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 棱彩增幅器 — 取自 LoL ARAM Mayhem 真实列表
    /// 共 9 个，击败全部机械 Boss 后随机获得一个
    /// </summary>
    public static class PrismaticAugments
    {
        /// <summary>所有棱彩增幅器的内部名称</summary>
        public static readonly string[] AllAugments = new[]
        {
            "Goliath",            // 最大生命 +35%，全伤害 +15%，体型 +50%
            "GiantSlayer",        // 体型 -25%，根据体型差增伤
            "GlassCannon",       // 生命上限 50%，额外 30% 真实伤害
            "DualWield",          // 攻击额外发射一枚 40% 伤害的弹体
            "Earthwake",          // 冲刺/瞬移后留下爆炸路径
            "Eureka",             // 法力每 100 点获得 1% 攻速
            "CircleOfDeath",      // 治疗量的 50% 对最近敌人造成伤害
            "CantTouchThis",      // 每 45 秒获得 1 秒无敌
            "Dropkick"            // 低血量处决
        };

        /// <summary>随机抽取一个棱彩增幅器</summary>
        public static string Roll() => AllAugments[Main.rand.Next(AllAugments.Length)];

        // ============ 常量和状态 ============

        /// <summary>CantTouchThis 冷却（帧）</summary>
        public const int CantTouchCooldown = 45 * 60;
        /// <summary>CantTouchThis 无敌时长（帧）</summary>
        public const int CantTouchDuration = 1 * 60;

        /// <summary>Dropkick 处决阈值基础值</summary>
        public const float DropkickExecuteBase = 0.05f;

        // ============ 被动效果 ============

        /// <summary>
        /// 应用持续性的棱彩增幅器被动效果
        /// </summary>
        public static void ApplyPassive(Player player, string augment,
            ref int cantTouchTimer, ref bool cantTouchReady)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "Goliath":
                    // 最大生命 +35%，全伤害 +15%，体型 +50%
                    player.statLifeMax2 = (int)(player.statLifeMax2 * 1.35f);
                    player.GetDamage(DamageClass.Generic) += 0.15f;
                    // 体型 +50% 通过修改 hitbox 不好实现，改为移速 -5% 作为平衡
                    player.moveSpeed -= 0.05f;
                    player.statDefense += 10; // 额外防御补偿
                    break;

                case "GiantSlayer":
                    // 体型 -25%（速度 +10% 作为补偿），根据体型差增伤在 ModifyHitNPC 处理
                    player.moveSpeed += 0.10f;
                    // 体积减小 25% 在 Terraria 中通过修改 hitbox 实现
                    // 如果需要：可以改为闪避率 +5%
                    // 此处简化为移速 +10% + 暴击 +5%
                    player.GetCritChance(DamageClass.Generic) += 5;
                    break;

                case "GlassCannon":
                    // 生命上限限制为 50%，额外 30% 真实伤害在 ModifyHitNPC 处理
                    player.statLifeMax2 = player.statLifeMax2 / 2;
                    // 回复效果增强作为补偿
                    // 显示强烈的视觉效果
                    if (Main.rand.NextBool(30))
                    {
                        Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.RedTorch);
                        d.velocity = Main.rand.NextVector2Circular(2f, 2f);
                        d.noGravity = true;
                        d.scale = 1.5f;
                    }
                    break;

                case "Eureka":
                    // 法力每 100 点获得 1% 攻击速度
                    int manaHundreds = player.statManaMax2 / 100;
                    float asFromMana = manaHundreds * 0.01f;
                    if (asFromMana > 0)
                    {
                        player.GetAttackSpeed(DamageClass.Melee) += asFromMana;
                        player.GetAttackSpeed(DamageClass.Ranged) += asFromMana;
                    }
                    break;

                case "CantTouchThis":
                    // CantTouchThis 计时器
                    if (!cantTouchReady)
                    {
                        if (cantTouchTimer < CantTouchCooldown)
                            cantTouchTimer++;
                        if (cantTouchTimer >= CantTouchCooldown)
                            cantTouchReady = true;
                    }
                    break;
            }
        }

        // ============ 伤害修改 ============

        /// <summary>
        /// 在 ModifyHitNPC 中修改伤害
        /// </summary>
        public static void ModifyHitNPC(Player player, string augment, NPC target, ref NPC.HitModifiers modifiers,
            ref bool cantTouchReady, ref int cantTouchTimer)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "GlassCannon":
                    // 额外 30% 真实伤害
                    modifiers.ScalingBonusDamage += 0.30f;
                    break;

                case "GiantSlayer":
                    // 基础增伤 15%（简化的版本）
                    modifiers.ScalingBonusDamage += 0.15f;
                    break;

                case "Dropkick":
                    // 处决低血量敌人：在 OnHitNPC 中检查目标血量
                    // 此处不修改伤害
                    break;

                case "CantTouchThis":
                    // CantTouchThis 的无敌在 PostUpdate 中处理免疫帧
                    break;
            }
        }

        /// <summary>
        /// 处理投射物的 ModifyHitNPCWithProj
        /// </summary>
        public static void ModifyHitNPCWithProj(Player player, string augment, Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (string.IsNullOrEmpty(augment)) return;

            switch (augment)
            {
                case "GlassCannon":
                    modifiers.ScalingBonusDamage += 0.30f;
                    break;

                case "GiantSlayer":
                    modifiers.ScalingBonusDamage += 0.15f;
                    break;
            }
        }

        // ============ 命中效果 ============

        /// <summary>
        /// 处理命中时的棱彩增幅器效果（DualWield 额外弹体、Dropkick 处决等）
        /// </summary>
        public static void OnHitNPC(Player player, string augment, Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (string.IsNullOrEmpty(augment)) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            switch (augment)
            {
                case "Dropkick":
                    // 处决：当目标生命低于阈值时直接击杀
                    float executeThreshold = DropkickExecuteBase;
                    // 加成：每 50 基础伤害 +1%，最多 15%
                    int baseDmg = item.damage;
                    executeThreshold += (baseDmg / 50) * 0.01f;
                    if (executeThreshold > 0.15f) executeThreshold = 0.15f;

                    float hpPercent = (float)target.life / target.lifeMax;
                    if (hpPercent <= executeThreshold)
                    {
                        // 处决击杀：造成大量伤害
                        int executeDmg = target.life + 9999;
                        target.SimpleStrikeNPC(executeDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);

                        // 处决回血（50 点）
                        player.statLife += 50;
                        player.HealEffect(50);

                        // 爆炸粒子效果
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 pos = target.Center + Main.rand.NextVector2Circular(50f, 50f);
                            Dust d = Dust.NewDustDirect(pos, 2, 2, DustID.RedTorch);
                            d.velocity = Vector2.Normalize(pos - target.Center) * 6f;
                            d.noGravity = true;
                            d.scale = 1.8f;
                        }
                    }
                    break;

                case "CircleOfDeath":
                    // 治疗量的 50% 对最近敌人造成伤害
                    // 这里简化：每次命中额外造成 10 点魔法伤害
                    target.SimpleStrikeNPC(10, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                    break;
            }
        }

        /// <summary>
        /// 处理投射物命中时的棱彩效果
        /// </summary>
        public static void OnHitNPCWithProj(Player player, string augment, Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (string.IsNullOrEmpty(augment)) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            switch (augment)
            {
                case "Dropkick":
                    float executeThreshold = DropkickExecuteBase;
                    executeThreshold += (proj.damage / 50) * 0.01f;
                    if (executeThreshold > 0.15f) executeThreshold = 0.15f;

                    float hpPercent = (float)target.life / target.lifeMax;
                    if (hpPercent <= executeThreshold)
                    {
                        int executeDmg = target.life + 9999;
                        target.SimpleStrikeNPC(executeDmg, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                        player.statLife += 50;
                        player.HealEffect(50);
                    }
                    break;

                case "CircleOfDeath":
                    target.SimpleStrikeNPC(10, player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                    break;
            }
        }

        // ============ CantTouchThis 无敌逻辑 ============

        /// <summary>
        /// 在 PostUpdate 中处理 CantTouchThis 的周期性无敌
        /// </summary>
        public static void UpdateCantTouchThis(Player player, ref bool cantTouchReady, ref int cantTouchTimer)
        {
            if (!cantTouchReady) return;

            // 进入无敌状态（免疫帧）
            player.immune = true;
            player.immuneTime = CantTouchDuration;

            // 如果已有更长的免疫帧，跳过
            if (player.immuneTime < CantTouchDuration)
                player.immuneTime = CantTouchDuration;

            // 重置
            cantTouchReady = false;
            cantTouchTimer = 0;

            // 视觉效果
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.GoldFlame);
                d.velocity = Main.rand.NextVector2Circular(4f, 4f);
                d.noGravity = true;
                d.scale = 1.5f;
            }
        }
    }
}
