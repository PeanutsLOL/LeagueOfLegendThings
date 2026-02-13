using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using System;
using LeagueOfLegendThings.Content.Systems;
using Terraria.ModLoader.IO;

namespace LeagueOfLegendThings.Content.Buffs
{
	// Grasp of the Undying核心机制
	public class GraspOfUndyingPlayer : ModPlayer
	{
		private const int MaxStacks = 4;
		private const int StackDuration = 3 * 60; // 3秒，60帧每秒
		private const int StackInterval = 60; // 每秒叠加一次
		private const int OnHitWindow = 5 * 60; // 5秒内可触发

		private int stackTimer = 0;
		private int stackDurationTimer = 0;
		private int stacks = 0;
		private bool inCombat = false;

		// 触发窗口
		private int onHitTimer = 0;
		private bool readyToProc = false;

		// 记录永久加成
		private float bonusMaxHealth = 0f; // 小数缓存
		private int bonusMaxHealthInt = 0; // 已加到玩家的整数部分

		public override void ResetEffects()
		{
			// 这里可以根据符文选择情况决定是否激活
			//inCombat = false;
			Player.statLifeMax2 += bonusMaxHealthInt;
		}

		public override void SaveData(TagCompound tag)
		{
			tag["grasp_bonusMaxHealthInt"] = bonusMaxHealthInt;
			tag["grasp_bonusMaxHealthFrac"] = bonusMaxHealth;
		}

		public override void LoadData(TagCompound tag)
		{
			if (tag.ContainsKey("grasp_bonusMaxHealthInt"))
				bonusMaxHealthInt = tag.GetInt("grasp_bonusMaxHealthInt");
			if (tag.ContainsKey("grasp_bonusMaxHealthFrac"))
				bonusMaxHealth = tag.GetFloat("grasp_bonusMaxHealthFrac");
		}

		public void EnterCombat()
		{
			inCombat = true;
			stackDurationTimer = StackDuration;
		}

		public override void PostUpdate()
		{
			if (inCombat)
			{
				if (stacks < MaxStacks)
				{
					stackTimer++;
					if (stackTimer >= StackInterval)
					{
						stackTimer = 0;
						stacks++;
						if (stacks == MaxStacks)
						{
							readyToProc = true;
							onHitTimer = OnHitWindow;
						}
					}
				}
				if (stackDurationTimer > 0)
				{
					stackDurationTimer--;
				}
				if (stackDurationTimer <= 0)
				{
					inCombat = false;
					stackTimer = 0;
					stacks = 0;
				}
			}

			if (readyToProc && onHitTimer > 0)
			{
				onHitTimer--;
				if (onHitTimer <= 0)
				{
					readyToProc = false;
					stacks = 0;
				}
			}
		}

		// 进入战斗的判定（可根据实际需求调整调用时机）
		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!target.friendly && target.lifeMax > 5)
			{
				EnterCombat();
			}
			// 仅对Boss生效，且达到最大层数并在触发窗口内
			if (readyToProc && onHitTimer > 0 && target.boss)
			{
				ProcGraspEffect(item.DamageType, target);
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.owner == Player.whoAmI && !target.friendly && target.lifeMax > 5)
			{
				EnterCombat();
			}
			// 仅对Boss生效，且达到最大层数并在触发窗口内
			if (readyToProc && onHitTimer > 0 && target.boss && (proj.DamageType == DamageClass.Melee || proj.DamageType == DamageClass.Ranged))
			{
				ProcGraspEffect(proj.DamageType, target);
			}
		}

		private void ProcGraspEffect(DamageClass damageType, NPC target)
		{
			float maxHp = Player.statLifeMax2;
			float bonusDmg = 0f;
			float heal = 0f;
			float bonusHealth = 0f;

			if (damageType == DamageClass.Melee)
			{
				bonusDmg = maxHp * 0.035f;
				heal = maxHp * 0.013f;
				bonusHealth = 0.25f;
			}
			else if (damageType == DamageClass.Ranged)
			{
				bonusDmg = maxHp * 0.014f;
				heal = maxHp * 0.0052f;
				bonusHealth = 0.1f;
			}
			else
			{
				// 其他类型不触发
				return;
			}

			// 造成魔法伤害
			int intBonusDmg = (int)MathF.Max(1, bonusDmg);
			target.StrikeNPC(new NPC.HitInfo()
			{
				Damage = intBonusDmg,
				DamageType = DamageClass.Magic,
				Knockback = 0f,
				Crit = false
			});
            var sfx = new SoundStyle("LeagueOfLegendThings/Content/Buffs/Grasp_of_the_Undying_SFX")
            {
                Volume = 0.75f,
                PitchVariance = 0.2f
            };
            SoundEngine.PlaySound(sfx, Player.position);

			// 治疗
			int intHeal = (int)MathF.Max(1, heal);
			Player.statLife += intHeal;
			Player.HealEffect(intHeal, true);

			// 永久生命值奖励：只将整数部分累入缓存，剩余小数保留
			bonusMaxHealth += bonusHealth;
			int addInt = (int)bonusMaxHealth;
			if (addInt > 0)
			{
				bonusMaxHealthInt += addInt;
				bonusMaxHealth -= addInt;
			}

			// 重置状态
			readyToProc = false;
			stacks = 0;
			onHitTimer = 0;
		}

		// 可用于UI显示当前层数
		public int GetGraspStacks() => stacks;
		public bool IsInCombat() => inCombat;
		public bool IsReadyToProc() => readyToProc;
		public float GetBonusMaxHealth() => bonusMaxHealth;
		public int GetBonusMaxHealthInt() => bonusMaxHealthInt;
	}
}
