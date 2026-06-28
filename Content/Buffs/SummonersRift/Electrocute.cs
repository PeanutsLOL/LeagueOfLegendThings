using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using LeagueOfLegendThings.Content.Systems;
using LeagueOfLegendThings;
using System;
using Microsoft.Xna.Framework;

namespace LeagueOfLegendThings.Content.Buffs.SummonersRift
{
    // Electrocute
    public class ElectrocutePlayer : ModPlayer
    {
        private const int ProcCooldown = 15 * 60;

        private int hitCount;
        private int lastTargetId = -1;
        private int buffTimer;
        private int cooldownTimer;

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleElectrocute(target, damageDone);
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            HandleElectrocute(target, damageDone);
        }
        public override void PostUpdateMiscEffects()
        {
            bool hadBuff = buffTimer > 0;

            if (buffTimer > 0)
            {
                buffTimer--;
            }

            if (cooldownTimer > 0)
            {
                cooldownTimer--;
            }

            if (buffTimer <= 0 && hadBuff)
            {
                cooldownTimer = ProcCooldown;
                hitCount = 0;
                lastTargetId = -1;
            }
        }

        public override void UpdateDead()
        {
            // 死亡时触发积累的电刑，伤害加倍
            if (hitCount >= 3 && lastTargetId >= 0 && lastTargetId < Main.maxNPCs)
            {
                NPC target = Main.npc[lastTargetId];
                if (target.active && !target.friendly && target.lifeMax > 5)
                {
                    var runeSave = ModContent.GetInstance<RuneSaveSystem>();
                    if (runeSave.ElectrocuteSelected)
                    {
                        // 死亡电刑伤害加倍
                        float healthMana = Player.statLifeMax2 + Player.statManaMax2;
                        float ratio = (float)System.Math.Sqrt(healthMana / 2480f);
                        float scalingFactor = System.Math.Min(ratio, 20f);  // 上限20倍
                        int bonusDamage = (int)(2 * (300f + 1100f * scalingFactor));

                        target.SimpleStrikeNPC(bonusDamage, Player.direction, crit: true, knockBack: 0f, damageType: DamageClass.Generic);
                        CombatText.NewText(Player.Hitbox, Color.DarkRed, $"Dealt {bonusDamage * 2} DMG");

                        GetLightningPath(target, out Vector2 startPos, out Vector2 endPos);
                        if (Main.netMode == NetmodeID.Server)
                            BroadcastElectrocuteFx(startPos, endPos);
                        SpawnLightningEffect(target);
                        PlayElectrocuteSfx(target.Center);

                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, number: target.whoAmI);
                        }
                    }
                }
            }

            hitCount = 0;
            lastTargetId = -1;
            buffTimer = 0;
            cooldownTimer = 0;
        }

        private void HandleElectrocute(NPC target, int damageDone)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.ElectrocuteSelected)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            if (buffTimer > 0)
            {
                return;
            }

            if (cooldownTimer > 0)
                return;

            if (target.whoAmI != lastTargetId)
            {
                lastTargetId = target.whoAmI;
                hitCount = 0;
            }

            hitCount++;

            if (hitCount >= 3)
            {
                // 基础伤害：随HP+Mana缩放
                float healthMana = Player.statLifeMax2 + Player.statManaMax2;
                float ratio = (float)System.Math.Sqrt(healthMana / 2480f);
                float scalingFactor = System.Math.Min(ratio, 20f);
                int baseDmg = (int)(300f + 1100f * scalingFactor);

                // 增强：最高职业加成 + 武器面板 + 1%目标HP
                float classBonus = RuneDamageHelper.GetHighestClassBonus(Player);
                int weaponDmg = RuneDamageHelper.GetHeldWeaponDamage(Player);
                int hpDmg = RuneDamageHelper.GetPercentHPDamage(target, 0.01f);
                int bonusDamage = (int)(baseDmg * (1f + classBonus)) + weaponDmg + hpDmg;

                target.SimpleStrikeNPC(bonusDamage, Player.direction, crit: true, knockBack: 0f, damageType: DamageClass.Generic);
                CombatText.NewText(Player.Hitbox, Color.Red, $"Dealt {bonusDamage * 2} DMG");

                // 特效：单人/客户端本地播放，服务器广播
                GetLightningPath(target, out Vector2 startPos, out Vector2 endPos);
                if (Main.netMode == NetmodeID.Server)
                {
                    BroadcastElectrocuteFx(startPos, endPos);
                }
                SpawnLightningEffect(target);
                PlayElectrocuteSfx(target.Center);

                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: target.whoAmI);
                }

                hitCount = 0;
                cooldownTimer = ProcCooldown;  // 30秒CD
            }
        }

        private void SpawnLightningEffect(NPC target)
        {
            // 从目标头上方到目标中心生成闪电
            GetLightningPath(target, out Vector2 startPos, out Vector2 endPos);

            LightningBoltSystem.SpawnBolt(startPos, endPos, Color.Red, duration: 60, width: 7.5f, segments: 14);

            // 闪电末端生成爆炸特效（仅视觉，不造成伤害）
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projId = Projectile.NewProjectile(
                    Player.GetSource_Misc("Electrocute"),
                    endPos - new Vector2(0, 10f),
                    Vector2.Zero,
                    ProjectileID.DD2ExplosiveTrapT3Explosion,
                    0,  // 0伤害
                    0f,
                    Player.whoAmI
                );
                if (projId >= 0 && projId < Main.maxProjectiles)
                {
                    Main.projectile[projId].friendly = false;
                    Main.projectile[projId].hostile = false;
                }
            }
        }

        private static void GetLightningPath(NPC target, out Vector2 startPos, out Vector2 endPos)
        {
            startPos = target.Center + new Vector2(0, -target.height / 2 - 120);
            endPos = target.Center - new Vector2(0, -target.height / 2);
        }

        private static void PlayElectrocuteSfx(Vector2 pos)
        {
            var sfx = new SoundStyle("LeagueOfLegendThings/Content/SFX/Electrocute_SFX")
            {
                Volume = 0.75f,
                PitchVariance = 0.5f
            };
            SoundEngine.PlaySound(sfx, pos);
        }

        private static void BroadcastElectrocuteFx(Vector2 startPos, Vector2 endPos)
        {
            var modPacket = ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>().GetPacket();
            modPacket.Write((byte)LeaguePacketType.ElectrocuteFx);
            modPacket.Write(startPos.X);
            modPacket.Write(startPos.Y);
            modPacket.Write(endPos.X);
            modPacket.Write(endPos.Y);
            modPacket.Send();
        }
    }
}