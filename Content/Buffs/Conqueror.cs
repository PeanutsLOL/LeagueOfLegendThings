using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;
using Terraria.Audio;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class Conqueror : ModBuff
    {
        // 最大叠加层数
        public const int MaxStacks = 12;
        // 每层伤害加成（自适应之力）
        public const float DamageBonusPerStack = 0.015f; // 1.5% per stack, 18% at max
        // 满层时的生命吸取比例
        public const float FullStacksLifesteal = 0.0008f; // 0.08% lifesteal at max stacks
        // 持续时间（以帧为单位，60帧 = 1秒）
        public const int BuffDuration = 4 * 60; // 4秒

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
            var conquerorPlayer = player.GetModPlayer<ConquerorPlayer>();

            if (conquerorPlayer.ConquerorStacks > 0)
            {
                // 视觉效果：根据层数生成粒子
                if (Main.rand.NextBool(15))
                {
                    // 红色/橙色粒子效果
                    Dust dust = Dust.NewDustDirect(
                        player.position,
                        player.width,
                        player.height,
                        DustID.Torch,
                        0f,
                        0f,
                        100,
                        default,
                        0.6f + (0.05f * conquerorPlayer.ConquerorStacks)
                    );
                    dust.noGravity = true;
                    dust.velocity *= 0.3f;
                }

                // 满层时的额外效果
                if (conquerorPlayer.ConquerorStacks >= MaxStacks)
                {
                    if (Main.rand.NextBool(8))
                    {
                        Dust dust = Dust.NewDustDirect(
                            player.position,
                            player.width,
                            player.height,
                            DustID.RedTorch,
                            0f,
                            -1.5f,
                            150,
                            default,
                            1.3f
                        );
                        dust.noGravity = true;
                    }
                }
            }

            // 检查Buff是否即将结束
            if (player.buffTime[buffIndex] <= 1)
            {
                conquerorPlayer.ResetStacks();
            }
        }
    }

    public class ConquerorPlayer : ModPlayer
    {
        // 征服者的叠加层数（实际显示值）
        public int ConquerorStacks => (int)_conquerorStacksFloat;
        // 浮点层数累积（用于支持小数叠层）
        private float _conquerorStacksFloat;
        // 计时器（帧），大于 0 时递减，到 0 清空层数
        private int conquerorTimer;
        // 满层音效是否已播放
        private bool conquerorProcPlayed;
        // 首次叠层音效是否已播放
        private bool conquerorFirstStackPlayed;
        // 生命吸取缓冲（用于累积小于1的回血量）
        private float healBuffer;

        public void ResetStacks()
        {
            _conquerorStacksFloat = 0f;
            conquerorProcPlayed = false;
            conquerorFirstStackPlayed = false;
            healBuffer = 0f;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleConqueror(item, target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleConqueror(proj, target, damageDone);
        }

        public override void PostUpdateMiscEffects()
        {
            bool hadStacks = ConquerorStacks > 0;
            
            // 计时衰减
            if (conquerorTimer > 0)
            {
                conquerorTimer--;
            }
            if (conquerorTimer <= 0)
            {
                conquerorTimer = 0;
                if (hadStacks)
                {
                    // 层数消失时播放音效
                    try
                    {
                        var clearSfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Conqueror_SFX")
                        {
                            Volume = 0.5f,
                            PitchVariance = 0f
                        };
                        SoundEngine.PlaySound(clearSfx, Player.Center);
                    }
                    catch { }
                }
                _conquerorStacksFloat = 0f;
                conquerorProcPlayed = false;
                conquerorFirstStackPlayed = false;
            }

            // 根据层数提供伤害加成
            if (ConquerorStacks > 0 && conquerorTimer > 0)
            {
                float damageBonus = Conqueror.DamageBonusPerStack * ConquerorStacks;
                Player.GetDamage(DamageClass.Generic) += damageBonus;
            }
        }

        public override void UpdateDead()
        {
            _conquerorStacksFloat = 0f;
            conquerorTimer = 0;
            conquerorProcPlayed = false;
            conquerorFirstStackPlayed = false;
            healBuffer = 0f;
        }

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            // 满层时提供生命吸取效果（在实际造成伤害时处理）
        }

        private void HandleConqueror(object source, NPC target, int damageDone)
        {
            // 未选择符文基石则不触发
            if (!ModContent.GetInstance<RuneSaveSystem>().ConquerorSelected)
                return;

            if (target.lifeMax > 5 && target.friendly == false)
            {
                // 叠加层数
                if (_conquerorStacksFloat < Conqueror.MaxStacks)
                {
                    // 根据伤害类型决定叠层数量
                    float stacksToAdd = 0f;
                    DamageClass damageType = null;
                    
                    if (source is Item item)
                    {
                        damageType = item.DamageType;
                    }
                    else if (source is Projectile proj)
                    {
                        damageType = proj.DamageType;
                        // 召唤物特殊处理
                        if (proj.minion || proj.DamageType == DamageClass.Summon)
                        {
                            stacksToAdd = 0.5f; // 召唤伤害叠0.5层
                        }
                    }
                    
                    // 如果不是召唤物，根据伤害类型判断
                    if (stacksToAdd == 0f && damageType != null)
                    {
                        if (damageType == DamageClass.Melee)
                        {
                            stacksToAdd = 2f; // 近战叠2层
                        }
                        else if (damageType == DamageClass.Ranged)
                        {
                            stacksToAdd = 0.5f; // 远程叠0.25层
                        }
                        else if (damageType == DamageClass.Magic)
                        {
                            stacksToAdd = 1f; // 法术叠1层
                        }
                        else if (damageType == DamageClass.Summon)
                        {
                            stacksToAdd = 0.25f; // 召唤叠0.5层
                        }
                        else
                        {
                            stacksToAdd = 1f; // 其他类型默认叠1层
                        }
                    }
                    
                    _conquerorStacksFloat = Math.Min(_conquerorStacksFloat + stacksToAdd, Conqueror.MaxStacks);
                    
                    // 首次叠层时播放音效
                    if (!conquerorFirstStackPlayed && _conquerorStacksFloat > 0)
                    {
                        try
                        {
                            var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Conqueror_SFX_2")
                            {
                                Volume = 0.67f,
                                PitchVariance = 0f
                            };
                            SoundEngine.PlaySound(sfx, Player.Center);
                        }
                        catch { }
                        conquerorFirstStackPlayed = true;
                    }
                }

                // 刷新计时
                conquerorTimer = Conqueror.BuffDuration;

                // 满层首次触发音效
                if (ConquerorStacks >= Conqueror.MaxStacks && !conquerorProcPlayed)
                {
                    try
                    {
                        var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Conqueror_SFX_3")
                        {
                            Volume = 0.67f,
                            PitchVariance = 0f
                        };
                        SoundEngine.PlaySound(sfx, Player.Center);
                    }
                    catch { }
                    conquerorProcPlayed = true;
                }

                // 满层时的生命吸取效果
                if (ConquerorStacks >= Conqueror.MaxStacks)
                {
                    float healAmount = damageDone * Conqueror.FullStacksLifesteal;
                    
                    // 整数部分直接回血
                    int intHeal = (int)healAmount;
                    if (intHeal > 0)
                    {
                        Player.Heal(intHeal);
                    }
                    
                    // 小数部分加入缓冲
                    healBuffer += healAmount - intHeal;
                    
                    // 缓冲满1时回1点血
                    if (healBuffer >= 1f)
                    {
                        Player.Heal(1);
                        healBuffer -= 1f;
                    }
                        
                    // 治疗视觉效果
                    if ((intHeal > 0 || healBuffer >= 0.5f) && Main.rand.NextBool(3))
                    {
                        Dust dust = Dust.NewDustDirect(
                            Player.Center,
                            0,
                            0,
                            DustID.LifeDrain,
                            0f,
                            -2f,
                            0,
                            default,
                            1f
                        );
                        dust.noGravity = true;
                    }
                }
            }
        }
    }
}
