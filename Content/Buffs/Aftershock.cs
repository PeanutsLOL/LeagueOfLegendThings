using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using System;
using LeagueOfLegendThings.Content.Buffs;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
	public class AftershockPlayer : ModPlayer
	{
		private const int DurationTicks = (int)(2.2f * 60f); // 2.0 seconds (shortened)
		private const int CooldownTicks = 20 * 60; // 20 seconds
		private const int RangePixels = 16 * 5; // 16*5 px

		private int aftershockDuration = 0;
		private int aftershockCooldown = 0;
		private int aftershockResistance = 0; // added to statDefense while active

		public override void ResetEffects()
		{
			// Nothing to reset here; stat changes applied in PostUpdateMiscEffects
		}

		public override void PostUpdateMiscEffects()
		{
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (!save.AftershockSelected)
                return;
			// Cooldown tick
			if (aftershockCooldown > 0)
				aftershockCooldown--;

			// While aftershock active, apply resistance
			if (aftershockDuration > 0)
			{
				Player.statDefense += aftershockResistance;
				aftershockDuration--;

				// When duration ends, release shockwave if off cooldown
				if (aftershockDuration <= 0)
				{
					if (aftershockCooldown <= 0)
					{
						ReleaseShockwave();
						aftershockCooldown = CooldownTicks;
					}
					// clear resistance after duration
					aftershockResistance = 0;
				}
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			var save = ModContent.GetInstance<RuneSaveSystem>();
			if (!save.AftershockSelected)
				return;
			// Only melee items
			if (item.DamageType != DamageClass.Melee)
				return;

			GrantResistance();
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			var save = ModContent.GetInstance<RuneSaveSystem>();
			if (!save.AftershockSelected)
				return;
			// Only melee projectiles
			if (proj.owner != Player.whoAmI)
				return;
			if (proj.DamageType != DamageClass.Melee)
				return;

			GrantResistance();
		}

		private void GrantResistance()
		{
			if (aftershockCooldown > 0)
				return;

			// Calculate base resistance: static 35 + 80% of player's defense
			int baseRes = 35 + (int)(0.8f * Player.statDefense);

			// Cap based on max health: cap = clamp(maxHealth/5, 80, 240)
			int cap = Math.Clamp(Player.statLifeMax2 / 5, 80, 240);
			int finalRes = Math.Min(baseRes, cap);

			// Apply
			aftershockResistance = finalRes;
			aftershockDuration = DurationTicks;

			// play sfx
			var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Aftershock_SFX_1")
			{
				Volume = 0.75f,
				PitchVariance = 0.0f
			};
			SoundEngine.PlaySound(sfx, Player.position);
		}

		private void ReleaseShockwave()
		{
            // play shockwave sfx
			var sfx2 = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Aftershock_SFX_2")
			{
				Volume = 0.75f,
				PitchVariance = 0.3f
			};
			SoundEngine.PlaySound(sfx2, Player.position);
			// Damage calculation: base 25 - 120 based on player maxHP, clamp
			int baseDamage = 25 + (int)(Player.statLifeMax2 / 20f); // e.g. 2000hp -> +100
			baseDamage = Math.Clamp(baseDamage, 25, 120);

			// Add 8% of bonus max health from GraspOfUndying if present
			int extraFromGrasp = 0;
			try
			{
				var grasp = Player.GetModPlayer<GraspOfUndyingPlayer>();
				if (grasp != null)
				{
					extraFromGrasp = (int)(0.08f * grasp.GetBonusMaxHealthInt());
				}
			}
			catch
			{
				// ignore if absent
			}

			int finalDamage = baseDamage + extraFromGrasp;

			// Apply to nearby hostile NPCs
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (npc == null || !npc.active || npc.friendly || npc.life <= 0)
					continue;
				float dist = Vector2.Distance(npc.Center, Player.Center);
				if (dist <= RangePixels)
				{
					npc.SimpleStrikeNPC(finalDamage, Player.direction, crit: false, knockBack: 0f, damageType: DamageClass.Magic);
				}
			}
		}
	}
}
