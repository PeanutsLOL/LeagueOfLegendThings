using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Projectiles;
using LeagueOfLegendThings.Content.Systems;
using Terraria.Audio;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    public class LethalTempo : ModBuff
    {
        public override string Texture => "LeagueOfLegendThings/Content/Icon/Lethal_Tempo";
        // 最大叠加层数
        public const int MaxStacks = 6;
        // 每层近战攻击速度加成
        public const float MeleeAttackSpeedBonusPerStack = 0.04f;
        // 每层远程攻击速度加成
        public const float RangedAttackSpeedBonusPerStack = 0.025f;
        // 持续时间（以帧为单位，60帧 = 1秒）
        public const int BuffDuration = 6 * 60;

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
            var lethalTempoPlayer = player.GetModPlayer<LethalTempoPlayer>();

            if (lethalTempoPlayer.LethalTempoStacks > 0)
            {
                float attackSpeedBonus = 0f;
                if (player.HeldItem.DamageType == DamageClass.Melee)
                {
                    attackSpeedBonus = MeleeAttackSpeedBonusPerStack * lethalTempoPlayer.LethalTempoStacks;
                }
                else if (player.HeldItem.DamageType == DamageClass.Ranged)
                {
                    attackSpeedBonus = RangedAttackSpeedBonusPerStack * lethalTempoPlayer.LethalTempoStacks;
                }
                player.GetAttackSpeed(player.HeldItem.DamageType) += attackSpeedBonus;

                // 添加视觉效果
                if (Main.rand.NextBool(20)) // 每帧有5%的概率生成粒子
                {
                    Dust dust = Dust.NewDustDirect(
                        player.position,
                        player.width,
                        player.height,
                        DustID.Electric, // 使用电气效果
                        0f,
                        0f,
                        100,
                        default,
                        0.8f + (0.2f * lethalTempoPlayer.LethalTempoStacks)
                    );
                    dust.noGravity = true;
                    dust.velocity *= 0.3f;
                }

                // 满层时的额外效果
                if (lethalTempoPlayer.LethalTempoStacks >= MaxStacks)
                {
                    if (Main.rand.NextBool(10)) // 每帧10%概率
                    {
                        Dust dust = Dust.NewDustDirect(
                            player.position,
                            player.width,
                            player.height,
                            DustID.GoldFlame,
                            0f,
                            -2f,
                            150,
                            default,
                            1.2f
                        );
                        dust.noGravity = true;
                    }
                }
            }

            // 检查Buff是否即将结束
            if (player.buffTime[buffIndex] <= 1)
            {
                // 重置层数
                lethalTempoPlayer.LethalTempoStacks = 0;
            }
        }
    }

    public class LethalTempoPlayer : ModPlayer
    {
        // 致命节奏的叠加层数
        public int LethalTempoStacks;
        // 计时器（帧），大于 0 时递减，到 0 清空层数
        private int lethalTempoTimer;

        // 轮流发射上弧/下弧（0 = 上弧，1 = 下弧）
        private int nextNoteSide = 0;

        private bool lethalTempoProcPlayed;

        // 近战投射物叠层减半：每 2 次命中叠 1 层
        private int _meleeProjHitCounter;

        // 满层后每 4 次命中发射一个投射物
        private int _maxStackNoteCounter;

        public override void OnHitNPCWithItem(Item item,
                                              NPC target,
                                              NPC.HitInfo hit,
                                              int damageDone
                                              )
        {
            HandleLethalTempo(item, target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 由致命节奏投射物自身造成的命中不再叠层/触发
            if (proj.type == ModContent.ProjectileType<LethalTempoNoteProjectile>())
                return;

            HandleLethalTempo(proj, target, damageDone);
        }

        public override void PostUpdateMiscEffects()
        {
            bool hadStacks = LethalTempoStacks > 0;
            // 攻速加成与计时衰减在此处理
            if (lethalTempoTimer > 0)
            {
                lethalTempoTimer--;
            }
            if (lethalTempoTimer <= 0)
            {
                lethalTempoTimer = 0;
                if (hadStacks)
                {
                    var clearSfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Lethal_Tempo_SFX_4")
                    {
                        Volume = 0.67f,
                        PitchVariance = 0f
                    };
                    SoundEngine.PlaySound(clearSfx, Player.Center);
                }
                LethalTempoStacks = 0;
                lethalTempoProcPlayed = false;
                _meleeProjHitCounter = 0;
                _maxStackNoteCounter = 0;
            }

            // 攻速加成由 LethalTempo.Update() 统一施加，此处仅处理计时与层数清零
        }

        public override void UpdateDead()
        {
            LethalTempoStacks = 0;
            lethalTempoTimer = 0;
            lethalTempoProcPlayed = false;
            _meleeProjHitCounter = 0;
            _maxStackNoteCounter = 0;
        }

        private void HandleLethalTempo(object source, NPC target, int damageDone)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().LethalTempoSelected)
                return;

            if (target.lifeMax <= 5 || target.friendly)
                return;

            // ── 叠层：真近战满层，近战投射物减半，远程正常 ──
            bool gainedStack = false;
            if (LethalTempoStacks < LethalTempo.MaxStacks)
            {
                if (source is Item)
                {
                    // 真近战 → 每击 1 层
                    LethalTempoStacks++;
                    gainedStack = true;
                }
                else if (source is Projectile proj)
                {
                    if (proj.DamageType == DamageClass.Melee)
                    {
                        // 近战投射物 → 每 2 击叠 1 层
                        _meleeProjHitCounter++;
                        if (_meleeProjHitCounter >= 2)
                        {
                            _meleeProjHitCounter = 0;
                            LethalTempoStacks++;
                            gainedStack = true;
                        }
                    }
                    else if (proj.DamageType == DamageClass.Ranged)
                    {
                        // 远程投射物 → 正常叠层
                        LethalTempoStacks++;
                        gainedStack = true;
                    }
                    // 其他伤害类型不叠层
                }
            }

            // 每次命中都刷新计时
            lethalTempoTimer = LethalTempo.BuffDuration;

            // ── 满层音效 ──
            if (LethalTempoStacks >= LethalTempo.MaxStacks && !lethalTempoProcPlayed)
            {
                var sfx1 = new SoundStyle("LeagueOfLegendThings/Content/SFX/Lethal_Tempo_SFX_3")
                {
                    Volume = 0.67f,
                    PitchVariance = 0f
                };
                SoundEngine.PlaySound(sfx1, Player.Center);
                lethalTempoProcPlayed = true;
            }

            // ── 满层投射物：每次命中计数，每 4 次发射 1 个，伤害 ×2 ──
            if (LethalTempoStacks >= LethalTempo.MaxStacks)
            {
                _maxStackNoteCounter++;

                if (_maxStackNoteCounter >= 4)
                {
                    _maxStackNoteCounter = 0;

                    DamageClass damageClass;
                    int panelDamage = Player.HeldItem?.damage ?? 0;

                    if (source is Item item)
                    {
                        panelDamage = item.damage;
                        damageClass = item.DamageType;
                    }
                    else if (source is Projectile proj)
                    {
                        damageClass = proj.DamageType;
                    }
                    else
                    {
                        return;
                    }

                    float baseDamage;
                    if (damageClass == DamageClass.Melee)
                        baseDamage = panelDamage * 0.60f; // 原 0.30 × 2
                    else if (damageClass == DamageClass.Ranged)
                        baseDamage = panelDamage * 1.40f; // 原 0.70 × 2
                    else
                        return;

                    float extraAttackSpeed = Player.GetTotalAttackSpeed(damageClass) - 1f;
                    float finalDamage = baseDamage * (1f + extraAttackSpeed);

                    float side = nextNoteSide;
                    Projectile.NewProjectile(
                        new EntitySource_OnHit(Player, target),
                        Player.Center,
                        Player.DirectionTo(target.Center) * 10f,
                        ModContent.ProjectileType<LethalTempoNoteProjectile>(),
                        (int)finalDamage,
                        0f,
                        Player.whoAmI,
                        side,
                        0f
                    );

                    nextNoteSide ^= 1;
                }
            }
        }
    }
}
