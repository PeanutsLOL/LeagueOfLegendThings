using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LeagueOfLegendThings.Content.Config;
using LeagueOfLegendThings.Content.Systems;
using LeagueOfLegendThings.Content.UI;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 海克斯大乱斗玩家类 — v2: 集成 StatShardSystem
    /// </summary>
    public class MayhemPlayer : ModPlayer
    {
        // ==================== 存档数据 ====================

        public string SilverAugment = "";
        public string GoldAugment = "";
        public string PrismaticAugment = "";

        /// <summary>Shardholder 增幅倍率 (1.0 = 未激活, 1.2-1.8 = 激活)</summary>
        public float ShardholderMultiplier = 1.0f;
        /// <summary>是否已选择 Shardholder</summary>
        public bool HasShardholder;

        /// <summary>已选择的碎片 ID 列表（用于存档）</summary>
        public List<string> TakenShardIds = new();
        /// <summary>已选择碎片的数值列表（与 IDs 一一对应）</summary>
        public List<float> TakenShardValues = new();
        /// <summary>已购买铁砧总次数</summary>
        public int TotalShardsTaken;

        // ==================== 缓存的碎片属性（一次性计算，避免无限累加） ====================
        private int _cachedDefense, _cachedMaxLife, _cachedMaxMana;
        private float _cachedMaxLifePct; // 棱彩百分比生命值
        private float _cachedMeleeDmg, _cachedRangedDmg, _cachedMagicDmg, _cachedSummonDmg, _cachedAllDmg;
        private float _cachedAtkSpd, _cachedCritChance, _cachedMoveSpd, _cachedLifeSteal;
        internal float CachedLifeSteal => _cachedLifeSteal; // LeechPoolCollector 读取

        // ==================== 运行时状态 ====================
        private int erosionStacks;
        private int cerberusComboCounter;
        private int critRhythmTimer, critRhythmStacks;
        private int escapePlanCooldown;
        private bool getExcitedActive; private int getExcitedTimer;
        private bool cantTouchReady; private int cantTouchTimer;
        private int tierCheckCooldown;

        // ==================== 生命周期 ====================

        public override void OnEnterWorld()
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            var save = ModContent.GetInstance<MayhemSaveSystem>();
            RefreshSaveTiers(save);
            RecalcCachedStats(); // 加载存档后重新计算缓存
            if (Main.myPlayer == Player.whoAmI)
                RequestMissingAugments(save);
        }

        public override void PostUpdate()
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;

            if (Main.myPlayer == Player.whoAmI)
            {
                ConsumeAugmentSelection();
                ConsumeShardSelection();

                if (tierCheckCooldown <= 0)
                {
                    tierCheckCooldown = 60;
                    var save = ModContent.GetInstance<MayhemSaveSystem>();
                    bool oldG = save.GoldTierUnlocked, oldP = save.PrismaticTierUnlocked;
                    RefreshSaveTiers(save);
                    if (oldG != save.GoldTierUnlocked || oldP != save.PrismaticTierUnlocked)
                        RequestMissingAugments(save);
                }
                else tierCheckCooldown--;
            }

            if (PrismaticAugment == "CantTouchThis")
                PrismaticAugments.UpdateCantTouchThis(Player, ref cantTouchReady, ref cantTouchTimer);

        }

        /// <summary>
        /// 在引擎重建装备属性后注入碎片加成。
        /// UpdateEquips 每帧仅调用一次，statDefense 等已从零重建，直接 += 不会累积。
        /// </summary>
        public override void UpdateEquips()
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;

            RecalcCachedStats();

            if (TakenShardIds.Count == 0) return;

            Player.statDefense += _cachedDefense;
            Player.statLifeMax2 += _cachedMaxLife;
            if (_cachedMaxLifePct > 0f)
                Player.statLifeMax2 += (int)(Player.statLifeMax2 * _cachedMaxLifePct);
            Player.statManaMax2 += _cachedMaxMana;
            Player.GetDamage(DamageClass.Melee)   += _cachedMeleeDmg;
            Player.GetDamage(DamageClass.Ranged)  += _cachedRangedDmg;
            Player.GetDamage(DamageClass.Magic)   += _cachedMagicDmg;
            Player.GetDamage(DamageClass.Summon)  += _cachedSummonDmg;
            Player.GetDamage(DamageClass.Generic) += _cachedAllDmg;
            Player.GetAttackSpeed(DamageClass.Melee)  += _cachedAtkSpd;
            Player.GetAttackSpeed(DamageClass.Ranged) += _cachedAtkSpd;
            Player.GetCritChance(DamageClass.Generic) += (int)(_cachedCritChance * 100f);
            Player.moveSpeed += _cachedMoveSpd;
        }

        public override void PostUpdateMiscEffects()
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;

            // 增幅器效果（Shardholder 激活时跳过）
            if (!HasShardholder)
            {
                SilverAugments.ApplyPassive(Player, SilverAugment);
                GoldAugments.ApplyPassive(Player, GoldAugment,
                    ref critRhythmTimer, ref critRhythmStacks, ref escapePlanCooldown);
                PrismaticAugments.ApplyPassive(Player, PrismaticAugment,
                    ref cantTouchTimer, ref cantTouchReady);
                GoldAugments.ApplyGetExcited(Player, getExcitedActive, getExcitedTimer);
            }

            if (getExcitedTimer > 0) getExcitedTimer--;
            else getExcitedActive = false;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            // 碎片生命偷取 → 填入吸血池
            if (_cachedLifeSteal > 0f)
            {
                float stolen = damageDone * _cachedLifeSteal;
                if (stolen > 0f) Player.GetModPlayer<LeechPoolPlayer>().Fill(stolen);
            }

            if (HasShardholder) return; // Shardholder 激活时无增幅器效果

            SilverAugments.OnHitNPC(Player, SilverAugment, target, hit, damageDone);
            if (SilverAugment == "Erosion")
                SilverAugments.OnHitNPCErosion(Player, SilverAugment, target, damageDone, ref erosionStacks);
            GoldAugments.OnHitNPC(Player, GoldAugment, item, target, hit, damageDone,
                ref cerberusComboCounter, ref critRhythmTimer, ref critRhythmStacks,
                ref escapePlanCooldown, ref getExcitedActive, ref getExcitedTimer);
            PrismaticAugments.OnHitNPC(Player, PrismaticAugment, item, target, hit, damageDone);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            if (target.lifeMax <= 5 || target.friendly) return;

            // 碎片生命偷取 → 填入吸血池
            if (_cachedLifeSteal > 0f)
            {
                float stolen = damageDone * _cachedLifeSteal;
                if (stolen > 0f) Player.GetModPlayer<LeechPoolPlayer>().Fill(stolen);
            }

            if (HasShardholder) return;

            SilverAugments.OnHitNPC(Player, SilverAugment, target, hit, damageDone);
            if (SilverAugment == "Erosion")
                SilverAugments.OnHitNPCErosion(Player, SilverAugment, target, damageDone, ref erosionStacks);
            GoldAugments.OnHitNPCWithProj(Player, GoldAugment, proj, target, hit, damageDone,
                ref cerberusComboCounter, ref critRhythmTimer, ref critRhythmStacks,
                ref getExcitedActive, ref getExcitedTimer);
            PrismaticAugments.OnHitNPCWithProj(Player, PrismaticAugment, proj, target, hit, damageDone);
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            if (HasShardholder) return;
            SilverAugments.ModifyHitNPC(Player, SilverAugment, target, ref modifiers, ref erosionStacks);
            PrismaticAugments.ModifyHitNPC(Player, PrismaticAugment, target, ref modifiers,
                ref cantTouchReady, ref cantTouchTimer);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            if (HasShardholder) return;
            SilverAugments.ModifyHitNPCWithProj(Player, SilverAugment, proj, target, ref modifiers, ref erosionStacks);
            PrismaticAugments.ModifyHitNPCWithProj(Player, PrismaticAugment, proj, target, ref modifiers);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            if (target.lifeMax <= 5 || target.friendly) return;
            if (HasShardholder) return;
            if (!hit.InstantKill && target.life <= 0)
                GoldAugments.OnKill(Player, GoldAugment, target, damageDone,
                    ref getExcitedActive, ref getExcitedTimer);
        }

        public override void PostHurt(Player.HurtInfo info) { }

        public override void UpdateDead()
        {
            if (!ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune) return;
            erosionStacks = cerberusComboCounter = critRhythmTimer = critRhythmStacks
                = escapePlanCooldown = getExcitedTimer = 0;
            getExcitedActive = cantTouchReady = false;
            cantTouchTimer = 0;
        }

        // ==================== 存档 ====================

        public override void SaveData(TagCompound tag)
        {
            tag[nameof(SilverAugment)] = SilverAugment;
            tag[nameof(GoldAugment)] = GoldAugment;
            tag[nameof(PrismaticAugment)] = PrismaticAugment;
            tag[nameof(ShardholderMultiplier)] = ShardholderMultiplier;
            tag[nameof(HasShardholder)] = HasShardholder;
            tag[nameof(TakenShardIds)] = TakenShardIds;
            tag[nameof(TakenShardValues)] = TakenShardValues;
            tag[nameof(TotalShardsTaken)] = TotalShardsTaken;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey(nameof(SilverAugment))) SilverAugment = tag.GetString(nameof(SilverAugment));
            if (tag.ContainsKey(nameof(GoldAugment))) GoldAugment = tag.GetString(nameof(GoldAugment));
            if (tag.ContainsKey(nameof(PrismaticAugment))) PrismaticAugment = tag.GetString(nameof(PrismaticAugment));
            if (tag.ContainsKey(nameof(ShardholderMultiplier))) ShardholderMultiplier = tag.GetFloat(nameof(ShardholderMultiplier));
            if (tag.ContainsKey(nameof(HasShardholder))) HasShardholder = tag.GetBool(nameof(HasShardholder));
            if (tag.ContainsKey(nameof(TotalShardsTaken))) TotalShardsTaken = tag.GetInt(nameof(TotalShardsTaken));

            if (tag.ContainsKey(nameof(TakenShardIds)))
            {
                var idList = tag.GetList<string>(nameof(TakenShardIds));
                TakenShardIds = new List<string>(idList);
            }
            if (tag.ContainsKey(nameof(TakenShardValues)))
            {
                var valList = tag.GetList<float>(nameof(TakenShardValues));
                TakenShardValues = new List<float>(valList);
            }
        }

        /// <summary>重置所有属性锻造器数据（调试用）</summary>
        public void ResetAllShards()
        {
            TakenShardIds.Clear();
            TakenShardValues.Clear();
            TotalShardsTaken = 0;
            HasShardholder = false;
            ShardholderMultiplier = 1.0f;
            _cachedDefense = _cachedMaxLife = _cachedMaxMana = 0;
            _cachedMaxLifePct = 0f;
            _cachedMeleeDmg = _cachedRangedDmg = _cachedMagicDmg = _cachedSummonDmg = _cachedAllDmg = 0f;
            _cachedAtkSpd = _cachedCritChance = _cachedMoveSpd = _cachedLifeSteal = 0f;
        }

        /// <summary>获取当前碎片属性的文本摘要（用于物品 tooltip）</summary>
        public List<string> GetShardStatSummary()
        {
            var lines = new List<string>();
            if (TakenShardIds.Count == 0) return lines;

            void Add(string label, float val, string suffix, bool isInt)
            {
                if (val == 0f) return;
                if (isInt) lines.Add($"{label}: +{(int)val}{suffix}");
                else lines.Add($"{label}: +{val:P0}{suffix}");
            }

            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.Defense"),      _cachedDefense,   "", true);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.MaxLife"),     _cachedMaxLife,   $"({_cachedMaxLifePct:P0})", true);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.MaxMana"),     _cachedMaxMana,   "", true);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.MeleeDmg"),    _cachedMeleeDmg,  "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.RangedDmg"),   _cachedRangedDmg, "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.MagicDmg"),    _cachedMagicDmg,  "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.SummonDmg"),   _cachedSummonDmg, "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.AllDmg"),      _cachedAllDmg,    "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.AttackSpeed"), _cachedAtkSpd,    "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.CritChance"),  _cachedCritChance,"", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.MoveSpeed"),   _cachedMoveSpd,   "", false);
            Add(Language.GetTextValue("Mods.LeagueOfLegendThings.MayhemPlayer.Stats.LifeSteal"),   _cachedLifeSteal, "", false);

            return lines;
        }

        // ==================== StatAnvil 交互 ====================

        /// <summary>能否使用铁砧物品</summary>
        public bool CanUseStatAnvil()
        {
            var save = ModContent.GetInstance<MayhemSaveSystem>();
            return save.SilverTierUnlocked; // 始终可以用（只要有解锁）
        }

        /// <summary>打开铁砧 UI（由 StatBonusAnvilItem 调用）</summary>
        public void OpenStatAnvilUI()
        {
            if (Main.myPlayer != Player.whoAmI) return;

            var tier = StatShardSystem.RollTier(TotalShardsTaken);
            var options = StatShardSystem.RollShards(tier, 3, HasShardholder, TotalShardsTaken);
            ModContent.GetInstance<StatAnvilUISystem>().OpenSelection(options, tier);
        }

        // ==================== 属性应用 ====================

        /// <summary>
        /// 从 TakenShardIds + TakenShardValues 直接计算所有缓存属性。
        /// 不经过 ReconstructShards/CalculateStats，避免对象重建导致的潜在 bug。
        /// 在 ConsumeShardSelection 和 UpdateEquips 中调用。
        /// </summary>
        private void RecalcCachedStats()
        {
            _cachedDefense = _cachedMaxLife = _cachedMaxMana = 0;
            _cachedMaxLifePct = 0f;
            _cachedMeleeDmg = _cachedRangedDmg = _cachedMagicDmg = _cachedSummonDmg = _cachedAllDmg = 0f;
            _cachedAtkSpd = _cachedCritChance = _cachedMoveSpd = _cachedLifeSteal = 0f;

            float mult = HasShardholder ? ShardholderMultiplier : 1.0f;

            for (int i = 0; i < TakenShardIds.Count && i < TakenShardValues.Count; i++)
            {
                string id = TakenShardIds[i];
                float val = TakenShardValues[i];

                // Shardholder 本身不产生属性，只影响倍率（已在 mult 中体现）
                if (id == StatShardSystem.SHARDHOLDER_ID) continue;

                val *= mult;

                // 根据 ID 前缀判断归属
                if (id.StartsWith("Sil_") || id.StartsWith("Gold_") || id.StartsWith("Pris_"))
                {
                    string suffix = id.Substring(id.IndexOf('_') + 1);

                    switch (suffix)
                    {
                        case "Melee":  _cachedMeleeDmg  += val; break;
                        case "Ranged": _cachedRangedDmg += val; break;
                        case "Magic":  _cachedMagicDmg  += val; break;
                        case "Summon": _cachedSummonDmg += val; break;
                        case "Def":    _cachedDefense   += (int)val; break;
                        case "Life":
                            if (id.StartsWith("Pris_"))
                                _cachedMaxLifePct += val;  // 棱彩百分比
                            else
                                _cachedMaxLife += (int)val; // 固定值
                            break;
                        case "Mana":   _cachedMaxMana   += (int)val; break;
                        case "AS":     _cachedAtkSpd    += val; break;
                        case "Crit":   _cachedCritChance += val; break;
                        case "Move":   _cachedMoveSpd   += val; break;
                        case "LS":     _cachedLifeSteal += val; break;
                        case "AllDmg": _cachedAllDmg    += val; break;
                        case "ArmPen": /* ArmorPen 不在此处处理 */ break;
                        case "CritDmg": /* CritDmg 在 ModifyHitNPC 中处理 */ break;
                        case "Heal":   break;
                        case "Might":
                            _cachedMeleeDmg  += val;
                            _cachedRangedDmg += val;
                            break;
                        case "Unbreak":
                            _cachedDefense += (int)val;
                            _cachedMaxLife += (int)(val * 2);
                            break;
                        case "Precision":
                            _cachedCritChance += val;
                            _cachedAtkSpd    += val;
                            break;
                        case "Vitality":
                            _cachedMaxLife += (int)val;
                            _cachedMaxMana += (int)val;
                            break;
                        case "Faith":
                            _cachedAllDmg  += val * 0.03f * mult;
                            _cachedDefense += (int)(val * 3f * mult);
                            break;
                    }
                }
            }
        }

        // ==================== 选择消耗 ====================

        private void ConsumeAugmentSelection()
        {
            if (HasShardholder) return;

            var sel = ModContent.GetInstance<MayhemSelectionSystem>();
            if (!sel.SelectionComplete || string.IsNullOrEmpty(sel.SelectedAugment)) return;
            switch (sel.CompletedTier)
            {
                case "Silver": SilverAugment = sel.SelectedAugment; break;
                case "Gold": GoldAugment = sel.SelectedAugment; break;
                case "Prismatic": PrismaticAugment = sel.SelectedAugment; break;
            }
            sel.Reset();
        }

        private void ConsumeShardSelection()
        {
            var ui = ModContent.GetInstance<StatAnvilUISystem>();
            var shard = ui.ConsumeSelection();
            if (shard == null) return;

            // Shardholder 特殊处理
            if (shard.Id == StatShardSystem.SHARDHOLDER_ID)
            {
                HasShardholder = true;
                ShardholderMultiplier = 1.0f + shard.StatValue;
                SilverAugment = ""; GoldAugment = ""; PrismaticAugment = "";
                string shMsg = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.ShardholderActivated");
                Main.NewText(string.Format(shMsg, $"{ShardholderMultiplier:F2}"), 255, 215, 0);
            }

            TakenShardIds.Add(shard.Id);
            TakenShardValues.Add(shard.StatValue);
            TotalShardsTaken++;
            RecalcCachedStats();

            // Faith Shard 在聊天栏告知具体属性
            if (shard.Id.Contains("_Faith"))
            {
                float faithPct = shard.StatValue * 100f;
                float dmg = shard.StatValue * 0.03f * (HasShardholder ? ShardholderMultiplier : 1f);
                int def = (int)(shard.StatValue * 3f * (HasShardholder ? ShardholderMultiplier : 1f));
                string faithMsg = Language.GetTextValue("Mods.LeagueOfLegendThings.UI.StatAnvil.FaithDetails");
                Main.NewText(string.Format(faithMsg, $"{faithPct:F0}", $"{dmg:P0}", $"{def}"), 200, 220, 255);
            }
        }

        // ==================== 辅助 ====================

        private void RefreshSaveTiers(MayhemSaveSystem save)
        {
            save.SilverTierUnlocked = true;
            if (Main.hardMode) save.GoldTierUnlocked = true;
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                save.PrismaticTierUnlocked = true;
        }

        private void RequestMissingAugments(MayhemSaveSystem save)
        {
            // Shardholder 激活后不再弹出增幅器选择
            if (HasShardholder) return;

            var sel = ModContent.GetInstance<MayhemSelectionSystem>();
            if (!string.IsNullOrEmpty(sel.PendingTier)) return;

            if (string.IsNullOrEmpty(SilverAugment) && save.SilverTierUnlocked)
                sel.RequestSelection("Silver");
            else if (string.IsNullOrEmpty(GoldAugment) && save.GoldTierUnlocked)
                sel.RequestSelection("Gold");
            else if (string.IsNullOrEmpty(PrismaticAugment) && save.PrismaticTierUnlocked)
                sel.RequestSelection("Prismatic");
        }
    }
}
