using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    /// <summary>
    /// 守护者 — 仿 LoL Guardian：受到Boss伤害时为自己和附近队友提供治疗+防御+加速
    /// </summary>
    public class GuardianPlayer : ModPlayer
    {
        private const int CooldownTicks = 25 * 60;
        private const int DefenseBuffDuration = 5 * 60;
        private const int MoveBuffDuration = 3 * 60;
        private const int SelfDefense = 30;
        private const float SelfMoveSpeed = 0.30f;
        private const int SelfHeal = 200;
        private const int AllyDefense = 20;
        private const float AllyMoveSpeed = 0.20f;
        private const int AllyHeal = 100;
        private const float AllyRange = 800f; // 50 格

        private int _cooldown;
        private int _defenseTimer;
        private int _moveTimer;

        public override void ResetEffects()
        {
            // 不重置 — 由 PostUpdate 自行管理 buff 计时器
        }

        public override void PostUpdateMiscEffects()
        {
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.GuardianSelected)
                return;

            // 冷却 & buff 计时递减
            if (_cooldown > 0) _cooldown--;
            if (_defenseTimer > 0) _defenseTimer--;
            if (_moveTimer > 0) _moveTimer--;

            // 应用持续 buff
            if (_defenseTimer > 0)
                Player.statDefense += SelfDefense;
            if (_moveTimer > 0)
                Player.moveSpeed += SelfMoveSpeed;

            // 冷却就绪提示音
            if (_cooldown == 0 && _defenseTimer <= 0)
            {
                // 每帧仅触发一次（由 _playedReady 处理太复杂，用简单逻辑）
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (!ModContent.GetInstance<RuneSaveSystem>().GuardianSelected)
                return;

            if (_cooldown > 0)
                return;

            // 仅当附近有活跃 Boss 时触发
            bool bossNearby = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss
                    && Vector2.Distance(Player.Center, Main.npc[i].Center) < 1200f)
                {
                    bossNearby = true;
                    break;
                }
            }

            if (!bossNearby)
                return;

            TriggerGuardian();
        }

        private void TriggerGuardian()
        {
            _cooldown = CooldownTicks;
            _defenseTimer = DefenseBuffDuration;
            _moveTimer = MoveBuffDuration;

            // SFX
            var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Guardian_SFX_4")
            {
                Volume = 0.8f,
                PitchVariance = 0.2f
            };
            SoundEngine.PlaySound(sfx, Player.position);

            // 治疗自身
            Player.statLife += SelfHeal;
            Player.HealEffect(SelfHeal, true);

            // 自身粒子
            SpawnGuardianParticles(Player.Center);

            // —— 队友效果 ——
            Player nearestAlly = FindNearestTeammate();
            if (nearestAlly != null)
            {
                var allyGuardian = nearestAlly.GetModPlayer<GuardianAllyEffect>();
                allyGuardian.ApplyAllyBuff(AllyHeal, AllyDefense, AllyMoveSpeed,
                    DefenseBuffDuration, MoveBuffDuration);

                SpawnGuardianParticles(nearestAlly.Center);

                // 连线粒子：自身 → 队友
                for (int i = 0; i < 6; i++)
                {
                    Vector2 pos = Vector2.Lerp(Player.Center, nearestAlly.Center, (float)i / 6f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch, Vector2.Zero, 100, default, 1.2f);
                    d.noGravity = true;
                }
            }
        }

        private Player FindNearestTeammate()
        {
            Player best = null;
            float bestDist = AllyRange;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var p = Main.player[i];
                if (!p.active || p.whoAmI == Player.whoAmI || p.dead)
                    continue;
                float dist = Vector2.Distance(Player.Center, p.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = p;
                }
            }
            return best;
        }

        private static void SpawnGuardianParticles(Vector2 center)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(40f, 40f);
                Dust d = Dust.NewDustPerfect(center + offset, DustID.BlueTorch,
                    offset.SafeNormalize(Vector2.UnitY) * 3f, 100, default, 1.8f);
                d.noGravity = true;
            }
        }
    }

    /// <summary>
    /// 队友端接收守护者 buff（只负责加属性，不触发新守护者）
    /// </summary>
    public class GuardianAllyEffect : ModPlayer
    {
        private int _defTimer, _moveTimer;
        private int _defBonus, _movePct;

        public void ApplyAllyBuff(int heal, int defense, float moveSpeed, int defDuration, int moveDuration)
        {
            Player.statLife += heal;
            Player.HealEffect(heal, true);
            _defTimer = defDuration;
            _moveTimer = moveDuration;
            _defBonus = defense;
            _movePct = (int)(moveSpeed * 100f);
        }

        public override void PostUpdateMiscEffects()
        {
            if (_defTimer > 0)
            {
                _defTimer--;
                Player.statDefense += _defBonus;
            }
            if (_moveTimer > 0)
            {
                _moveTimer--;
                Player.moveSpeed += _movePct / 100f;
            }
        }
    }

}
