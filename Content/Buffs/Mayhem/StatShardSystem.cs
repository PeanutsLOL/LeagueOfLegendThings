using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 统计铁砧碎片系统 — 仿 LoL ARAM Stat Bonus Anvil 设计
    ///
    /// 每次使用 StatBonusAnvilItem 后随机一个层级 (Silver/Gold/Prismatic)
    /// 展示 3 个随机碎片供选择，中间位有概率出现 Shardholder Value Shard
    ///
    /// Shardholder: 仅在选择棱彩碎片时有概率出现，提升所有已有及未来碎片效果 20%-80%
    /// 仅可选择一次。选择后先前所有 Mayhem 增幅器效果失效。
    /// </summary>
    public static class StatShardSystem
    {
        // ==================== 碎片定义 ====================

        public enum ShardTier { Silver, Gold, Prismatic }

        /// <summary>单个属性碎片</summary>
        public class Shard
        {
            public string Id;          // 内部标识
            public string DisplayName; // 显示名
            public string StatKey;     // 属性键（用于存储）
            public float StatValue;    // 当前数值
            public float BaseValue;    // 基础值
            public float MaxValue;     // 最大值 (Silver 翻倍)
            public ShardTier Tier;
            public bool IsDualStat;    // 双属性

            public string GetDisplayName()
            {
                // 优先从本地化获取
                if (Id == SHARDHOLDER_ID)
                {
                    string key = "Mods.LeagueOfLegendThings.StatShards.Shardholder.DisplayName";
                    string loc = Language.GetTextValue(key);
                    if (!string.IsNullOrEmpty(loc) && loc != key) return loc;
                    return "Shardholder";
                }
                string k = $"Mods.LeagueOfLegendThings.StatShards.{Id}.DisplayName";
                string l = Language.GetTextValue(k);
                if (!string.IsNullOrEmpty(l) && l != k) return l;
                return DisplayName; // 回退到初始值
            }

            public string GetDescription()
            {
                if (Id == "Shardholder") return $"Shardholder: ALL shard effects +{StatValue:P0}";
                return FormatStat(GetDisplayName(), StatValue);
            }

            private static string FormatStat(string name, float val)
            {
                if (name.Contains("%") || name.Contains("Crit") || name.Contains("Steal")
                    || name.Contains("Speed") || name.Contains("Damage") || name.Contains("Power"))
                    return $"{name}\n+{val:P0}";
                if (name.Contains("Life") || name.Contains("Mana") || name.Contains("Defense"))
                    return $"{name}\n+{val:F0}";
                return $"{name}\n+{val:F1}";
            }
        }

        // ==================== 属性键常量 ====================
        public const string KEY_DEFENSE = "Defense";
        public const string KEY_MAX_LIFE = "MaxLife";
        public const string KEY_MAX_MANA = "MaxMana";
        public const string KEY_MELEE_DMG = "MeleeDmg";
        public const string KEY_RANGED_DMG = "RangedDmg";
        public const string KEY_MAGIC_DMG = "MagicDmg";
        public const string KEY_SUMMON_DMG = "SummonDmg";
        public const string KEY_ALL_DMG = "AllDmg";
        public const string KEY_ATK_SPEED = "AtkSpeed";
        public const string KEY_CRIT_CHANCE = "CritChance";
        public const string KEY_CRIT_DMG = "CritDmg";
        public const string KEY_MOVE_SPEED = "MoveSpeed";
        public const string KEY_LIFE_STEAL = "LifeSteal";
        public const string KEY_HEAL_POWER = "HealPower";
        public const string KEY_ARMOR_PEN = "ArmorPen";

        // ==================== 碎片池 ====================

        /// <summary>白银碎片（随机范围值，Base~Max）</summary>
        public static readonly Shard[] SilverShardsPool =
        {
            new() { Id="Sil_Melee",  DisplayName="Melee Damage",    StatKey=KEY_MELEE_DMG,   BaseValue=0.03f, MaxValue=0.05f, Tier=ShardTier.Silver },
            new() { Id="Sil_Ranged", DisplayName="Ranged Damage",   StatKey=KEY_RANGED_DMG,  BaseValue=0.03f, MaxValue=0.05f, Tier=ShardTier.Silver },
            new() { Id="Sil_Magic",  DisplayName="Magic Damage",    StatKey=KEY_MAGIC_DMG,   BaseValue=0.03f, MaxValue=0.05f, Tier=ShardTier.Silver },
            new() { Id="Sil_Summon", DisplayName="Summon Damage",   StatKey=KEY_SUMMON_DMG,  BaseValue=0.03f, MaxValue=0.05f, Tier=ShardTier.Silver },
            new() { Id="Sil_Def",    DisplayName="Defense",         StatKey=KEY_DEFENSE,     BaseValue=2f,    MaxValue=4f,    Tier=ShardTier.Silver },
            new() { Id="Sil_Life",   DisplayName="Max Life",        StatKey=KEY_MAX_LIFE,    BaseValue=10f,   MaxValue=20f,   Tier=ShardTier.Silver },
            new() { Id="Sil_AS",     DisplayName="Attack Speed",    StatKey=KEY_ATK_SPEED,   BaseValue=0.01f, MaxValue=0.02f, Tier=ShardTier.Silver },
            new() { Id="Sil_Crit",   DisplayName="Crit Chance",     StatKey=KEY_CRIT_CHANCE, BaseValue=0.02f, MaxValue=0.03f, Tier=ShardTier.Silver },
            new() { Id="Sil_Move",   DisplayName="Move Speed",      StatKey=KEY_MOVE_SPEED,  BaseValue=0.03f, MaxValue=0.05f, Tier=ShardTier.Silver },
            new() { Id="Sil_LS",     DisplayName="Life Steal",      StatKey=KEY_LIFE_STEAL,  BaseValue=0.005f,MaxValue=0.010f,Tier=ShardTier.Silver },
            new() { Id="Sil_Mana",   DisplayName="Max Mana",        StatKey=KEY_MAX_MANA,    BaseValue=10f,   MaxValue=20f,   Tier=ShardTier.Silver },
            new() { Id="Sil_Might",     DisplayName="Might",           StatKey="Might",          BaseValue=0.02f, MaxValue=0.03f, Tier=ShardTier.Silver, IsDualStat=true },
            new() { Id="Sil_Unbreak",   DisplayName="Unbreakable",     StatKey="Unbreakable",    BaseValue=2f,    MaxValue=3f,    Tier=ShardTier.Silver, IsDualStat=true },
            new() { Id="Sil_Precision", DisplayName="Precision",       StatKey="Precision",      BaseValue=0.015f,MaxValue=0.02f, Tier=ShardTier.Silver, IsDualStat=true },
            new() { Id="Sil_Vitality",  DisplayName="Vitality",        StatKey="Vitality",       BaseValue=6f,    MaxValue=12f,   Tier=ShardTier.Silver, IsDualStat=true },
        };

        /// <summary>黄金碎片（固定值，约为白银的 2 倍）</summary>
        public static readonly Shard[] GoldShardsPool =
        {
            new() { Id="Gold_Melee",  DisplayName="Melee Damage",    StatKey=KEY_MELEE_DMG,   BaseValue=0.06f, MaxValue=0.06f, Tier=ShardTier.Gold },
            new() { Id="Gold_Ranged", DisplayName="Ranged Damage",   StatKey=KEY_RANGED_DMG,  BaseValue=0.06f, MaxValue=0.06f, Tier=ShardTier.Gold },
            new() { Id="Gold_Magic",  DisplayName="Magic Damage",    StatKey=KEY_MAGIC_DMG,   BaseValue=0.06f, MaxValue=0.06f, Tier=ShardTier.Gold },
            new() { Id="Gold_Summon", DisplayName="Summon Damage",   StatKey=KEY_SUMMON_DMG,  BaseValue=0.06f, MaxValue=0.06f, Tier=ShardTier.Gold },
            new() { Id="Gold_Def",    DisplayName="Defense",         StatKey=KEY_DEFENSE,     BaseValue=5f,    MaxValue=5f,    Tier=ShardTier.Gold },
            new() { Id="Gold_Life",   DisplayName="Max Life",        StatKey=KEY_MAX_LIFE,    BaseValue=25f,   MaxValue=25f,   Tier=ShardTier.Gold },
            new() { Id="Gold_AS",     DisplayName="Attack Speed",    StatKey=KEY_ATK_SPEED,   BaseValue=0.03f, MaxValue=0.03f, Tier=ShardTier.Gold },
            new() { Id="Gold_Crit",   DisplayName="Crit Chance",     StatKey=KEY_CRIT_CHANCE, BaseValue=0.03f, MaxValue=0.03f, Tier=ShardTier.Gold },
            new() { Id="Gold_Move",   DisplayName="Move Speed",      StatKey=KEY_MOVE_SPEED,  BaseValue=0.06f, MaxValue=0.06f, Tier=ShardTier.Gold },
            new() { Id="Gold_LS",     DisplayName="Life Steal",      StatKey=KEY_LIFE_STEAL,  BaseValue=0.015f,MaxValue=0.015f,Tier=ShardTier.Gold },
            new() { Id="Gold_Mana",   DisplayName="Max Mana",        StatKey=KEY_MAX_MANA,    BaseValue=25f,   MaxValue=25f,   Tier=ShardTier.Gold },
            new() { Id="Gold_Might",     DisplayName="Might",           StatKey="Might",          BaseValue=0.03f, MaxValue=0.03f, Tier=ShardTier.Gold, IsDualStat=true },
            new() { Id="Gold_Unbreak",   DisplayName="Unbreakable",     StatKey="Unbreakable",    BaseValue=3f,    MaxValue=3f,    Tier=ShardTier.Gold, IsDualStat=true },
            new() { Id="Gold_Precision", DisplayName="Precision",       StatKey="Precision",      BaseValue=0.025f,MaxValue=0.025f,Tier=ShardTier.Gold, IsDualStat=true },
            new() { Id="Gold_Vitality",  DisplayName="Vitality",        StatKey="Vitality",       BaseValue=15f,   MaxValue=15f,   Tier=ShardTier.Gold, IsDualStat=true },
            new() { Id="Gold_Faith",     DisplayName="Faith",           StatKey="Faith",          BaseValue=1.50f, MaxValue=3.00f, Tier=ShardTier.Gold },
        };

        /// <summary>棱彩碎片（强力固定值）</summary>
        public static readonly Shard[] PrismaticShardsPool =
        {
            new() { Id="Pris_AllDmg",  DisplayName="All Damage",     StatKey=KEY_ALL_DMG,     BaseValue=0.05f, MaxValue=0.05f, Tier=ShardTier.Prismatic },
            new() { Id="Pris_ArmPen",  DisplayName="Armor Pen.",     StatKey=KEY_ARMOR_PEN,   BaseValue=5f,    MaxValue=5f,    Tier=ShardTier.Prismatic },
            new() { Id="Pris_CritDmg", DisplayName="Crit Damage",    StatKey=KEY_CRIT_DMG,    BaseValue=0.10f, MaxValue=0.10f, Tier=ShardTier.Prismatic },
            new() { Id="Pris_Life",    DisplayName="Max Life",       StatKey=KEY_MAX_LIFE,    BaseValue=0.08f, MaxValue=0.08f, Tier=ShardTier.Prismatic },
            new() { Id="Pris_Move",    DisplayName="Move Speed",     StatKey=KEY_MOVE_SPEED,  BaseValue=0.06f, MaxValue=0.06f, Tier=ShardTier.Prismatic },
            new() { Id="Pris_LS",      DisplayName="Life Steal",     StatKey=KEY_LIFE_STEAL,  BaseValue=0.02f, MaxValue=0.02f, Tier=ShardTier.Prismatic },
            new() { Id="Pris_Heal",    DisplayName="Heal Power",     StatKey=KEY_HEAL_POWER,  BaseValue=0.10f, MaxValue=0.10f, Tier=ShardTier.Prismatic },
            new() { Id="Pris_Mana",    DisplayName="Max Mana",       StatKey=KEY_MAX_MANA,    BaseValue=40f,   MaxValue=40f,   Tier=ShardTier.Prismatic },
        };

        // ==================== Shardholder ====================

        public const string SHARDHOLDER_ID = "Shardholder";
        /// <summary>Shardholder 在棱彩轮次中出现的概率</summary>
        public const float SHARDHOLDER_CHANCE = 0.20f;
        /// <summary>Shardholder 增幅范围</summary>
        public const float SHARDHOLDER_MIN = 0.20f;
        public const float SHARDHOLDER_MAX = 0.80f;

        /// <summary>创建 Shardholder 碎片</summary>
        public static Shard CreateShardholder()
        {
            float boost = SHARDHOLDER_MIN + (float)Main.rand.NextDouble() * (SHARDHOLDER_MAX - SHARDHOLDER_MIN);
            return new Shard
            {
                Id = SHARDHOLDER_ID,
                DisplayName = "Shardholder",
                StatKey = "Shardholder",
                StatValue = boost,
                BaseValue = boost,
                MaxValue = boost,
                Tier = ShardTier.Prismatic
            };
        }

        // ==================== 抽取逻辑 ====================

        private static readonly Random _rand = new();

        /// <summary>随机选择层级（黄金随总碎片数变多）</summary>
        public static ShardTier RollTier(int totalShardsTaken)
        {
            float r = (float)Main.rand.NextDouble();
            float goldWeight = 0.33f + totalShardsTaken * 0.03f; // 黄金概率随总碎片数增加
            if (goldWeight > 0.55f) goldWeight = 0.55f;
            float prismWeight = 0.10f;

            if (r < prismWeight) return ShardTier.Prismatic;
            if (r < prismWeight + goldWeight) return ShardTier.Gold;
            return ShardTier.Silver;
        }

        /// <summary>25 次锻造后棱彩碎片中间位必定为 Shardholder</summary>
        public const int SHARDHOLDER_GUARANTEED_AT = 25;

        /// <summary>从指定层级抽取 N 个不重复碎片（N=3）</summary>
        public static List<Shard> RollShards(ShardTier tier, int count, bool hasShardholder, int totalForgeCount = 0)
        {
            Shard[] pool = tier switch
            {
                ShardTier.Silver => SilverShardsPool,
                ShardTier.Gold => GoldShardsPool,
                ShardTier.Prismatic => PrismaticShardsPool,
                _ => SilverShardsPool
            };

            var result = new List<Shard>();
            var available = new List<Shard>(pool);

            // Fisher-Yates shuffle
            int n = available.Count;
            while (n > 1) { n--; int k = Main.rand.Next(n + 1); (available[k], available[n]) = (available[n], available[k]); }

            for (int i = 0; i < Math.Min(count, available.Count); i++)
            {
                var template = available[i];
                // 创建副本并随机取值（Silver 在 Base~Max 之间随机）
                var shard = new Shard
                {
                    Id = template.Id,
                    DisplayName = template.DisplayName,
                    StatKey = template.StatKey,
                    BaseValue = template.BaseValue,
                    MaxValue = template.MaxValue,
                    Tier = template.Tier,
                    IsDualStat = template.IsDualStat,
                };

                if (shard.MaxValue > shard.BaseValue)
                {
                    float t_val = (float)Main.rand.NextDouble();
                    shard.StatValue = shard.BaseValue + t_val * (shard.MaxValue - shard.BaseValue);
                }
                else
                {
                    shard.StatValue = shard.BaseValue;
                }

                result.Add(shard);
            }

            // 棱彩轮次：中间位概率插入 Shardholder（25次后必定出现）
            if (tier == ShardTier.Prismatic && !hasShardholder && result.Count >= 2)
            {
                float chance = totalForgeCount >= SHARDHOLDER_GUARANTEED_AT ? 1f : SHARDHOLDER_CHANCE;
                if (Main.rand.NextDouble() < chance)
                {
                    result[1] = CreateShardholder(); // 替换中间位
                }
            }

            return result;
        }

        // ==================== 属性计算 ====================

        /// <summary>
        /// 根据已选碎片列表和 Shardholder 倍率，计算所有属性的累积值
        /// </summary>
        public static Dictionary<string, float> CalculateStats(List<Shard> takenShards, float shardholderMultiplier)
        {
            var stats = new Dictionary<string, float>();

            foreach (var shard in takenShards)
            {
                float val = shard.StatValue;
                // Shardholder 不影响自身以外的 Shardholder（它只影响属性碎片）
                if (shard.Id != SHARDHOLDER_ID)
                    val *= shardholderMultiplier;

                switch (shard.StatKey)
                {
                    case "Might":
                        AddStat(stats, KEY_MELEE_DMG, val);
                        AddStat(stats, KEY_RANGED_DMG, val);
                        break;
                    case "Unbreakable":
                        AddStat(stats, KEY_DEFENSE, val);
                        AddStat(stats, KEY_MAX_LIFE, val * 2); // HP 部分
                        break;
                    case "Precision":
                        AddStat(stats, KEY_CRIT_CHANCE, val);
                        AddStat(stats, KEY_ATK_SPEED, val);
                        break;
                    case "Vitality":
                        AddStat(stats, KEY_MAX_LIFE, val);
                        AddStat(stats, KEY_MAX_MANA, val);
                        break;
                    case "Faith":
                        // Faith: 额外抽取 2 个随机黄金碎片（简化版：直接给两个通用属性）
                        AddStat(stats, KEY_ALL_DMG, 0.05f * shardholderMultiplier);
                        AddStat(stats, KEY_DEFENSE, 5f * shardholderMultiplier);
                        break;
                    default:
                        AddStat(stats, shard.StatKey, val);
                        break;
                }
            }

            return stats;
        }

        private static void AddStat(Dictionary<string, float> dict, string key, float val)
        {
            if (!dict.ContainsKey(key)) dict[key] = 0f;
            dict[key] += val;
        }
    }
}
