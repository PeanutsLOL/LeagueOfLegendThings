using System;
using System.IO;
using System.Linq;
using LeagueOfLegendThings.Content.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

public class SummonAery : ModBuff
{
    public override string Texture => "LeagueOfLegendThings/Content/Buffs/SummonAeryBuff";

    public override void SetStaticDefaults()
    {
        Main.lightPet[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        var saPlayer = player.GetModPlayer<SummonAeryPlayer>();
        saPlayer.HasSummonAery = true;
    }
}

public class SummonAeryPlayer : ModPlayer
{
    private static bool EnableAeryDebugLog = false;

    public bool HasSummonAery;
    private int aeryProjectileId = -1;
    private int pendingPotionHealLife;
    private int pendingPotionHealMana;
    private int pendingPotionTimer;

    public override void ResetEffects()
    {
        HasSummonAery = false;
    }

    public override void PostUpdateMiscEffects()
    {
        var save = ModContent.GetInstance<RuneSaveSystem>();
        if (save.SummonAerySelected)
        {
            HasSummonAery = true;
            if (!Player.HasBuff(ModContent.BuffType<SummonAery>()))
            {
                Player.AddBuff(ModContent.BuffType<SummonAery>(), 2);
            }
        }
    }

    public override void PostUpdate()
    {
        EnsureAeryProjectile();
        HandlePendingPotionAery();
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (HasSummonAery && target.boss)
            TriggerAery(target, null);
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (HasSummonAery && target.boss)
            TriggerAery(target, null);
    }

    public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
    {
        if (!HasSummonAery || healValue <= 0 || !item.potion)
            return;

        pendingPotionHealLife = healValue;
        pendingPotionTimer = 60;
        DebugAery($"Capture life potion heal={healValue}, quickHeal={quickHeal}, owner={Player.whoAmI}");
    }

    public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
    {
        if (!HasSummonAery || healValue <= 0 || !item.potion)
            return;

        pendingPotionHealMana = healValue;
        pendingPotionTimer = 60;
        DebugAery($"Capture mana potion heal={healValue}, quickHeal={quickHeal}, owner={Player.whoAmI}");
    }

    private void EnsureAeryProjectile()
    {
        if (!HasSummonAery)
        {
            if (TryGetAeryProjectile(out Projectile existing))
                existing.Kill();
            aeryProjectileId = -1;
            return;
        }

        if (TryGetAeryProjectile(out _))
            return;

        if (Main.myPlayer != Player.whoAmI)
            return;

        int projId = Projectile.NewProjectile(
            Player.GetSource_FromThis(),
            Player.Center,
            Vector2.Zero,
            ModContent.ProjectileType<AeryProj>(),
            0,
            0f,
            Player.whoAmI
        );
        aeryProjectileId = projId;
    }

    private void HandlePendingPotionAery()
    {
        if (!HasSummonAery || pendingPotionTimer <= 0)
            return;

        if (pendingPotionTimer % 15 == 0)
            DebugAery("Pending tick");

        // 只在客户端限制拥有者；服务端(myPlayer=255)不应提前清空
        if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer != Player.whoAmI)
        {
            pendingPotionTimer = 0;
            pendingPotionHealLife = 0;
            pendingPotionHealMana = 0;
            DebugAery($"Skip pending on non-owner client. owner={Player.whoAmI}, myPlayer={Main.myPlayer}");
            return;
        }

        // 必须有实际药水治疗量才触发
        if (pendingPotionHealLife <= 0 && pendingPotionHealMana <= 0)
        {
            pendingPotionTimer = 0;
            DebugAery("Skip pending: no valid captured potion heal values");
            return;
        }

        pendingPotionTimer--;

        if (!TryGetAeryProjectile(out Projectile proj))
        {
            DebugAery("Pending wait: Aery projectile not found yet");
            return;
        }

        AeryProj aery = proj.ModProjectile as AeryProj;
        if (aery == null || aery.State != AeryProj.Idle)
        {
            DebugAery($"Pending wait: projectile state busy state={(aery == null ? -1 : aery.State)}");
            return;
        }

        Player targetTeammate = FindBestTeammate(120 * 16);
        if (targetTeammate != null)
        {
            int sendLife = (int)MathF.Floor(pendingPotionHealLife * 0.5f);
            int sendMana = (int)MathF.Floor(pendingPotionHealMana * 0.5f);
            int effectiveLife = Math.Max(0, Math.Min(sendLife, targetTeammate.statLifeMax2 - targetTeammate.statLife));
            int effectiveMana = Math.Max(0, Math.Min(sendMana, targetTeammate.statManaMax2 - targetTeammate.statMana));

            if (effectiveLife <= 0 && effectiveMana <= 0)
            {
                DebugAery($"Potion trigger skipped: teammate already full, rawLife={sendLife}, rawMana={sendMana}, targetLife={targetTeammate.statLife}/{targetTeammate.statLifeMax2}, targetMana={targetTeammate.statMana}/{targetTeammate.statManaMax2}");
                pendingPotionHealLife = 0;
                pendingPotionHealMana = 0;
                pendingPotionTimer = 0;
                return;
            }

            DebugAery($"Potion trigger -> teammate={targetTeammate.whoAmI}, life={pendingPotionHealLife}, mana={pendingPotionHealMana}, effectiveLife={effectiveLife}, effectiveMana={effectiveMana}");
            TriggerAery(null, targetTeammate);
        }
        else
        {
            DebugAery("Potion trigger aborted: no teammate in range/team");
        }

        pendingPotionHealLife = 0;
        pendingPotionHealMana = 0;
        pendingPotionTimer = 0;
        DebugAery("Pending cache cleared after trigger attempt");
    }

    private bool TryGetAeryProjectile(out Projectile proj)
    {
        proj = null;

        if (aeryProjectileId >= 0 && aeryProjectileId < Main.maxProjectiles)
        {
            Projectile candidate = Main.projectile[aeryProjectileId];
            if (candidate.active && candidate.owner == Player.whoAmI && candidate.type == ModContent.ProjectileType<AeryProj>())
            {
                proj = candidate;
                return true;
            }
        }

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile candidate = Main.projectile[i];
            if (!candidate.active)
                continue;
            if (candidate.owner != Player.whoAmI)
                continue;
            if (candidate.type != ModContent.ProjectileType<AeryProj>())
                continue;

            aeryProjectileId = i;
            proj = candidate;
            return true;
        }

        return false;
    }

    private void TriggerAery(NPC targetNpc, Player targetPlayer)
    {
        DebugAery($"Trigger called npc={(targetNpc == null ? -1 : targetNpc.whoAmI)} teammate={(targetPlayer == null ? -1 : targetPlayer.whoAmI)}");

        if (!TryGetAeryProjectile(out Projectile proj))
        {
            DebugAery("Trigger failed: Aery projectile not found");
            return;
        }

        AeryProj aery = proj.ModProjectile as AeryProj;
        if (aery == null || aery.State != AeryProj.Idle)
        {
            DebugAery($"Trigger blocked: state={(aery == null ? -1 : aery.State)}");
            return;
        }

        if (targetNpc != null)
        {
            DebugAery($"Send to boss npc={targetNpc.whoAmI}");
            aery.SendToNpc(targetNpc.whoAmI);
        }
        else if (targetPlayer != null)
        {
            int healLife = (int)MathF.Floor(pendingPotionHealLife * 0.5f);
            int healMana = (int)MathF.Floor(pendingPotionHealMana * 0.5f);
            DebugAery($"Send to teammate={targetPlayer.whoAmI}, life={healLife}, mana={healMana}");
            aery.SendToPlayer(targetPlayer.whoAmI, healLife, healMana);
        }
    }

    private Player FindBestTeammate(float range)
    {
        // 优先级：血量最低 -> 蓝量百分比最低
        return Main.player.Where(p => p.active && !p.dead && p.whoAmI != Player.whoAmI && Player.Distance(p.Center) < range && p.team == Player.team && p.team != 0)
            .OrderBy(p => p.statLife)
            .ThenBy(p => (float)p.statMana / p.statManaMax2)
            .FirstOrDefault();
    }

    private void DebugAery(string message)
    {
        if (!EnableAeryDebugLog)
            return;

        ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>()
            .Logger.Info($"[AeryDebug][Player:{Player.whoAmI}] {message} | cacheLife={pendingPotionHealLife}, cacheMana={pendingPotionHealMana}, cacheTimer={pendingPotionTimer}, aeryProjId={aeryProjectileId}");
    }
}

public class AeryProj : ModProjectile
{
    private static bool EnableAeryDebugLog = false;

    public int State { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
    public const int Idle = 0;
    public const int ToEnemy = 1;
    public const int ToTeammate = 2;
    public const int Returning = 3;

    private const int AnimStartFrame = 6;
    private const int AnimEndFrame = 10;
    private const int AnimTicksPerFrame = 6;
    private const float ReturnCompleteDistance = 16f * 9f;

    private int targetId => (int)Projectile.ai[1];
    private int pendingHealLife => (int)Projectile.localAI[0];
    private int pendingHealMana => (int)Projectile.localAI[1];

    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BlackCat;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Main.projFrames[ProjectileID.BlackCat];
    }

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 18000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }

    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        Projectile.frame = AnimStartFrame;
        Projectile.frameCounter = 0;
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead)
        {
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = 18000;

        Projectile.frameCounter++;
        if (Projectile.frameCounter >= AnimTicksPerFrame)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
            if (Projectile.frame > AnimEndFrame || Projectile.frame < AnimStartFrame)
                Projectile.frame = AnimStartFrame;
        }

        float baseSpeed = Math.Max(owner.velocity.Length(), owner.maxRunSpeed);
        if (baseSpeed <= 0.1f)
            baseSpeed = 4f;

        switch (State)
        {
            case Idle:
                FollowPlayer(owner);
                break;
            case ToEnemy:
            case ToTeammate:
                MoveToTarget(GetTargetEntity(), baseSpeed * 2.0f, 0.18f);
                CheckArrival(owner);
                break;
            case Returning:
                MoveToTarget(owner, baseSpeed * 0.7f, 0.12f);
                if (Projectile.Distance(owner.Center) < ReturnCompleteDistance)
                {
                    Projectile.ai[1] = 0f;
                    Projectile.localAI[0] = 0f;
                    Projectile.localAI[1] = 0f;
                    State = Idle;
                    Projectile.netUpdate = true;
                }
                break;
        }

        if (Math.Abs(Projectile.velocity.X) > 0.05f)
        {
            Projectile.spriteDirection = Projectile.velocity.X > 0 ? -1 : 1;
        }
    }

    public void SendToNpc(int npcId)
    {
        State = ToEnemy;
        Projectile.ai[1] = npcId + 1;
        Projectile.netUpdate = true;
    }

    public void SendToPlayer(int playerId, int healLife, int healMana)
    {
        State = ToTeammate;
        Projectile.ai[1] = -(playerId + 1);
        Projectile.localAI[0] = healLife;
        Projectile.localAI[1] = healMana;
        Projectile.netUpdate = true;
    }

    private Entity GetTargetEntity()
    {
        if (targetId == 0)
            return null;

        if (targetId > 0)
        {
            int npcId = targetId - 1;
            if (npcId >= 0 && npcId < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcId];
                if (npc.active)
                    return npc;
            }
        }
        else
        {
            int playerId = -targetId - 1;
            if (playerId >= 0 && playerId < Main.maxPlayers)
            {
                Player player = Main.player[playerId];
                if (player.active && !player.dead)
                    return player;
            }
        }

        return null;
    }

    private void CheckArrival(Player owner)
    {
        Entity target = GetTargetEntity();
        if (target == null)
        {
            State = Returning;
            Projectile.netUpdate = true;
            return;
        }

        bool reachedEnemy = target is NPC && Projectile.Hitbox.Intersects(target.Hitbox);
        bool reachedTeammate = target is Player p && Vector2.Distance(Projectile.Center, p.Center) <= 16f * 9f;
        if (!reachedEnemy && !reachedTeammate)
            return;

        if (State == ToEnemy && target is NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.SimpleStrikeNPC(40, owner.direction, crit: false, knockBack: 0f, damageType: DamageClass.Generic);
                DebugAery($"Hit boss npc={npc.whoAmI}, damage=40, owner={owner.whoAmI}");
            }
            PlayAerySound(new int[] { 1, 4, 6, 9, 12 }, npc.Center);
        }
        else if (State == ToTeammate && target is Player targetPlayer)
        {
            bool healed = ApplyPendingHealToTarget(targetPlayer, "Arrival");
            if (!healed)
                DebugAery($"Arrival teammate but no effective heal target={targetPlayer.whoAmI}, pendingLife={pendingHealLife}, pendingMana={pendingHealMana}");

            PlayAerySound(new int[] { 2, 5, 8, 10, 13 }, targetPlayer.Center);
        }

        State = Returning;
        Projectile.netUpdate = true;
    }

    private void MoveToTarget(Entity target, float speed, float turnRate)
    {
        if (target == null)
        {
            State = Returning;
            Projectile.netUpdate = true;
            return;
        }

        Vector2 toTarget = target.Center - Projectile.Center;
        float dist = toTarget.Length();
        if (dist < 6f)
        {
            Projectile.velocity *= 0.7f;
            return;
        }

        Vector2 desired = toTarget / dist * speed;
        float inertia = MathHelper.Lerp(14f, 8f, MathHelper.Clamp(turnRate, 0.05f, 0.5f));
        Projectile.velocity = (Projectile.velocity * (inertia - 1f) + desired) / inertia;
    }

    private void FollowPlayer(Player owner)
    {
        float speed = owner.velocity.Length();
        float t = Main.GlobalTimeWrappedHourly + Projectile.identity * 0.11f;
        Vector2 idlePos;

        if (speed <= 2f)
        {
            float a = 40f;
            float b = 22f;
            float x = a * MathF.Sin(t * 1.2f);
            float y = b * MathF.Sin(t * 1.2f) * MathF.Cos(t * 1.2f);
            idlePos = owner.Center + new Vector2(x, y - 34f);
        }
        else
        {
            Vector2 followOffset = new Vector2(-owner.direction * 36f, -28f);
            idlePos = owner.Center + followOffset;
        }

        Vector2 toIdle = idlePos - Projectile.Center;
        float dist = toIdle.Length();
        if (dist < 4f)
        {
            Projectile.velocity *= 0.85f;
            return;
        }

        float desiredSpeed = speed <= 2f ? 5f : 7.5f;
        Vector2 desired = toIdle / dist * desiredSpeed;
        Projectile.velocity = (Projectile.velocity * 11f + desired) / 12f;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.localAI[0]);
        writer.Write(Projectile.localAI[1]);
        DebugAery($"SendExtraAI life={Projectile.localAI[0]}, mana={Projectile.localAI[1]}");
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.localAI[0] = reader.ReadSingle();
        Projectile.localAI[1] = reader.ReadSingle();
        DebugAery($"ReceiveExtraAI life={Projectile.localAI[0]}, mana={Projectile.localAI[1]}, state={State}, targetId={targetId}");

        // 强制补结算：服务端如果只收到 Returning 状态，也尝试按当前目标补一次治疗
        if (Main.netMode == NetmodeID.Server && State == Returning && targetId < 0 && (pendingHealLife > 0 || pendingHealMana > 0))
        {
            int playerId = -targetId - 1;
            if (playerId >= 0 && playerId < Main.maxPlayers)
            {
                Player targetPlayer = Main.player[playerId];
                if (targetPlayer.active && !targetPlayer.dead)
                {
                    bool healed = ApplyPendingHealToTarget(targetPlayer, "ForcedSync");
                    DebugAery($"ForcedSync apply result={healed}, target={targetPlayer.whoAmI}");
                }
            }

            // 无论是否实际回血，都清空这次缓存，避免满血目标反复重放同一笔治疗
            Projectile.localAI[0] = 0f;
            Projectile.localAI[1] = 0f;
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
            DebugAery("ForcedSync cache cleared (life/mana/target reset)");
        }
    }

    private bool ApplyPendingHealToTarget(Player targetPlayer, string source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return false;

        bool healed = false;

        if (pendingHealLife > 0)
        {
            int healLife = Math.Min(pendingHealLife, targetPlayer.statLifeMax2 - targetPlayer.statLife);
            if (healLife > 0)
            {
                targetPlayer.Heal(healLife);
                healed = true;
                DebugAery($"{source} heal life target={targetPlayer.whoAmI}, amount={healLife}, pending={pendingHealLife}");
            }
            else
            {
                DebugAery($"{source} life pending exists but target full hp, pending={pendingHealLife}, life={targetPlayer.statLife}/{targetPlayer.statLifeMax2}");
            }
        }

        if (pendingHealMana > 0)
        {
            int healMana = Math.Min(pendingHealMana, targetPlayer.statManaMax2 - targetPlayer.statMana);
            if (healMana > 0)
            {
                targetPlayer.statMana += healMana;
                targetPlayer.ManaEffect(healMana);
                healed = true;
                DebugAery($"{source} heal mana target={targetPlayer.whoAmI}, amount={healMana}, pending={pendingHealMana}");
            }
            else
            {
                DebugAery($"{source} mana pending exists but target full mana, pending={pendingHealMana}, mana={targetPlayer.statMana}/{targetPlayer.statManaMax2}");
            }
        }

        if (Main.netMode == NetmodeID.Server && healed)
        {
            NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, targetPlayer.whoAmI);
            var modPacket = ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>().GetPacket();
            modPacket.Write((byte)LeagueOfLegendThings.LeaguePacketType.SyncPlayerVitals);
            modPacket.Write(targetPlayer.whoAmI);
            modPacket.Write(targetPlayer.statLife);
            modPacket.Write(targetPlayer.statMana);
            modPacket.Send();
            DebugAery($"{source} SyncPlayer sent for target={targetPlayer.whoAmI}");
        }

        return healed;
    }

    private void DebugAery(string message)
    {
        if (!EnableAeryDebugLog)
            return;

        ModContent.GetInstance<global::LeagueOfLegendThings.LeagueOfLegendThings>()
            .Logger.Info($"[AeryDebug][Proj:{Projectile.whoAmI}][Owner:{Projectile.owner}] {message} | state={State}, targetId={targetId}, cacheLife={pendingHealLife}, cacheMana={pendingHealMana}, pos={Projectile.Center}");
    }

    private void PlayAerySound(int[] options, Vector2 pos)
    {
        int choice = options[Main.rand.Next(options.Length)];
        SoundEngine.PlaySound(new SoundStyle($"LeagueOfLegendThings/Content/Buffs/Summon_Aery_SFX_{choice}"), pos);
    }
}