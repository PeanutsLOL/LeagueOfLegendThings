using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;
using Terraria.Audio;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class FleetFootwork : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Buffs/Fleet_Footwork";

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
            // 移动速度加成
            player.moveSpeed *= 1.75f;
            player.maxRunSpeed *= 1.75f;
            player.accRunSpeed *= 1.75f;
            
            // 垂直移动速度加成（跳跃和下落）
            if (player.velocity.Y != 0)
            {
                player.jumpSpeedBoost += Player.jumpSpeed * 0.75f;
            }
        }
    }

    public class FleetFootworkPlayer : ModPlayer
    {
        // 能量值（0-100）
        private float energy;
        public float Energy => energy;
        public const float MaxEnergy = 100f;
        
        // 移动累积：每25像素1点能量
        private const float PixelsPerEnergy = 10f * 16f; // 25方块 = 25*16像素
        private Vector2 lastPosition;
        
        // 攻击累积：每4次攻击1点能量
        private int attackCounter;
        private const int AttacksPerEnergy = 2;
        
        // 满层音效是否已播放
        private bool fullEnergyPlayed;
        // 触发后移速效果的计时器（不使用Buff栏）
        private int speedTimer;
        
        // 移速buff持续时间
        private const int SpeedBuffDuration = 60; // 1秒

        public override void Initialize()
        {
            energy = 0f;
            attackCounter = 0;
            lastPosition = Vector2.Zero;
            fullEnergyPlayed = false;
            speedTimer = 0;
        }

        public override void OnEnterWorld()
        {
            lastPosition = Player.position;
        }

        public override void PostUpdate()
        {
            // 未选择符文则不处理
            if (!ModContent.GetInstance<RuneSaveSystem>().FleetFootworkSelected)
                return;

            // 移动累积能量
            if (lastPosition != Vector2.Zero)
            {
                float distance = Vector2.Distance(Player.position, lastPosition);
                if (distance > 0 && distance < 1000) // 防止传送时大量累积
                {
                    float energyGain = distance / PixelsPerEnergy;
                    AddEnergy(energyGain);
                }
            }
            lastPosition = Player.position;

            // 触发后的移速效果（隐藏，不占用buff栏）
            if (speedTimer > 0)
            {
                ApplySpeedBuff();
                speedTimer--;
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().FleetFootworkSelected)
                return;

            // 累积攻击能量
            AddAttackEnergy();
            
            // 满能量触发
            if (energy >= MaxEnergy)
            {
                // 真近战：没有射弹的近战武器
                bool isTrueMelee = item.DamageType == DamageClass.Melee && item.shoot <= ProjectileID.None;
                
                TriggerFleetFootwork(target, item.damage, isTrueMelee, false, 1f);
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().FleetFootworkSelected)
                return;

            // 召唤物只能累积，不能触发
            if (proj.minion || proj.DamageType == DamageClass.Summon)
            {
                AddAttackEnergy();
                return;
            }

            // 累积攻击能量
            AddAttackEnergy();
            
            // 满能量触发
            if (energy >= MaxEnergy)
            {
                // 检查是否为鞭子
                bool isWhip = ProjectileID.Sets.IsAWhip[proj.type];
                
                if (isWhip)
                {
                    // 鞭子根据攻速调节回血
                    float attackSpeed = Player.GetTotalAttackSpeed(DamageClass.Summon);
                    float whipHealMultiplier = 0.5f + (attackSpeed - 1f) * 0.5f; // 基础0.5，攻速越快越高
                    whipHealMultiplier = Math.Clamp(whipHealMultiplier, 0.25f, 2f);
                    
                    TriggerFleetFootwork(target, Player.HeldItem?.damage ?? 0, false, true, whipHealMultiplier);
                }
                else
                {
                    TriggerFleetFootwork(target, Player.HeldItem?.damage ?? 0, false, false, 1f);
                }
            }
        }

        private void AddEnergy(float amount)
        {
            if (energy >= MaxEnergy) return;
            
            float oldEnergy = energy;
            energy = Math.Min(energy + amount, MaxEnergy);
            
            // 满层音效
            if (oldEnergy < MaxEnergy && energy >= MaxEnergy && !fullEnergyPlayed)
            {
                try
                {
                    var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Fleet_Footwork_SFX_3")
                    {
                        Volume = 0.67f,
                        PitchVariance = 0f
                    };
                    SoundEngine.PlaySound(sfx, Player.Center);
                }
                catch { }
                fullEnergyPlayed = true;
            }
        }

        private void AddAttackEnergy()
        {
            attackCounter++;
            if (attackCounter >= AttacksPerEnergy)
            {
                attackCounter = 0;
                AddEnergy(1f);
            }
        }

        private void TriggerFleetFootwork(NPC target, int weaponDamage, bool isTrueMelee, bool isWhip, float healMultiplier)
        {
            // 重置能量
            energy = 0f;
            attackCounter = 0;
            fullEnergyPlayed = false;
            
            // 播放触发音效（随机1或5）
            try
            {
                string sfxPath = Main.rand.NextBool() 
                    ? "LeagueOfLegendThings/Content/Buffs/Fleet_Footwork_SFX_1"
                    : "LeagueOfLegendThings/Content/Buffs/Fleet_Footwork_SFX_5";
                var sfx = new SoundStyle(sfxPath)
                {
                    Volume = 0.67f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(sfx, Player.Center);
            }
            catch { }
            
            // 计算治疗量
            // Max(5, 5 + 75 * (maxMana + maxHealth) / 700 - maxDef * 0.2)
            float baseHeal = 5f + 75f * (Player.statManaMax2 + Player.statLifeMax2) / 700f - Player.statDefense * 0.2f;
            baseHeal = Math.Max(5f, baseHeal);
            
            // 真近战额外回复
            float trueMeleeBonus = 0f;
            if (isTrueMelee)
            {
                trueMeleeBonus = weaponDamage * 0.5f;
            }
            
            float totalHeal = (baseHeal + trueMeleeBonus) * healMultiplier;
            
            // 非Boss单位只能获得15%回复
            if (!target.boss)
            {
                totalHeal *= 0.15f;
            }
            
            // 执行回血
            int healAmount = (int)totalHeal;
            if (healAmount > 0)
            {
                Player.Heal(healAmount);
                
                // 播放回血音效（随机2、4、6）
                try
                {
                    int[] healSfxOptions = { 2, 4, 6 };
                    string sfxPath = $"LeagueOfLegendThings/Content/Buffs/Fleet_Footwork_SFX_{healSfxOptions[Main.rand.Next(3)]}";
                    var sfx = new SoundStyle(sfxPath)
                    {
                        Volume = 0.5f,
                        PitchVariance = 0f
                    };
                    SoundEngine.PlaySound(sfx, Player.Center);
                }
                catch { }
            }
            
            // 应用移速buff
            speedTimer = SpeedBuffDuration;
            
            // 视觉效果
            for (int i = 0; i < 15; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Player.position,
                    Player.width,
                    Player.height,
                    DustID.HealingPlus,
                    Main.rand.NextFloat(-2f, 2f),
                    Main.rand.NextFloat(-3f, -1f),
                    100,
                    default,
                    1.2f
                );
                dust.noGravity = true;
            }
            
            // 移速加成视觉效果
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Player.position,
                    Player.width,
                    Player.height,
                    DustID.Cloud,
                    Player.velocity.X * 0.5f,
                    Player.velocity.Y * 0.5f,
                    50,
                    default,
                    1f
                );
                dust.noGravity = true;
            }
        }

        public override void UpdateDead()
        {
            energy = 0f;
            attackCounter = 0;
            fullEnergyPlayed = false;
            speedTimer = 0;
        }

        private void ApplySpeedBuff()
        {
            Player.moveSpeed *= 1.75f;
            Player.maxRunSpeed *= 1.75f;
            Player.accRunSpeed *= 1.75f;

            // 垂直移动速度加成（跳跃和下落）
            if (Player.velocity.Y != 0)
            {
                Player.jumpSpeedBoost += Player.jumpSpeed * 0.75f;
            }
        }
    }
}
