using Terraria;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class PresenceOfMind : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var pomPlayer = player.GetModPlayer<PresenceOfMindPlayer>();
            pomPlayer.HasPresenceOfMind = true;
        }
    }

    public class PresenceOfMindPlayer : ModPlayer
    {
        // 是否激活气定神闲效果（不使用 buff 栏，自实现逻辑）
        public bool HasPresenceOfMind;

        // CD计时器（8秒 = 480帧）
        private int cooldownTimer = 0;
        private const int CooldownDuration = 480; // 8秒 * 60帧/秒

        public override void ResetEffects()
        {
            HasPresenceOfMind = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // 检查符文是否选择，直接设置标记（不使用 buff）
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.PresenceOfMindSelected)
            {
                HasPresenceOfMind = true;
            }

            // CD倒计时
            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryPresenceOfMind(target, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryPresenceOfMind(target, damageDone);
        }

        private void TryPresenceOfMind(NPC target, int damageDone)
        {
            if (!HasPresenceOfMind)
                return;

            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.PresenceOfMindSelected)
                return;

            // 1. 击败敌人：回复15% maxMana
            if (damageDone > target.life)
            {
                int manaRestore = (int)(Player.statManaMax2 * 0.15f);
                if (manaRestore > 0)
                {
                    Player.statMana = System.Math.Min(Player.statMana + manaRestore, Player.statManaMax2);
                    Player.ManaEffect(manaRestore);
                }
            }

            // 2. 对敌人造成伤害：回复蓝量（有CD）或血量
            if (cooldownTimer <= 0)
            {
                // 计算回复量：(6 + 38 * maxMana / 220)
                float baseRestore = 6f + 38f * Player.statManaMax2 / 220f;

                // 蓝量未满：回复 baseRestore * 0.8 的蓝量
                if (Player.statMana < Player.statManaMax2)
                {
                    int manaRestore = (int)(baseRestore * 0.8f);
                    if (manaRestore > 0)
                    {
                        Player.statMana = System.Math.Min(Player.statMana + manaRestore, Player.statManaMax2);
                        Player.ManaEffect(manaRestore);
                    }
                }
                // 蓝量满：回复 baseRestore * 0.1 的血量
                else
                {
                    int healthRestore = (int)(baseRestore * 0.1f);
                    if (healthRestore > 0)
                    {
                        Player.Heal(healthRestore);
                    }
                }

                // 触发CD
                cooldownTimer = CooldownDuration;
            }
        }

        public override void UpdateDead()
        {
            HasPresenceOfMind = false;
            cooldownTimer = 0;
        }
    }
}
