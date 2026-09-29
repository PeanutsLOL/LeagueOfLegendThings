using System.Collections.Generic;

namespace LeagueOfLegendThings.Content.Systems
{
    /// <summary>
    /// 符文稳定 ID。存档与逻辑判断只用这些常量，不再使用可被翻译的显示名。
    /// 显示名仅存在于 RuneRegistry 的映射表中，供本地化查找使用。
    /// </summary>
    public static class RuneIds
    {
        public const string PathPrecision = "precision";
        public const string PathDomination = "domination";
        public const string PathSorcery = "sorcery";
        public const string PathResolve = "resolve";
        public const string PathInspiration = "inspiration";

        public const string RuneAbsoluteFocus = "absolute_focus";
        public const string RuneAbsorbLife = "absorb_life";
        public const string RuneAftershock = "aftershock";
        public const string RuneApproachVelocity = "approach_velocity";
        public const string RuneArcaneComet = "arcane_comet";
        public const string RuneAxiomArcanist = "axiom_arcanist";
        public const string RuneBiscuitDelivery = "biscuit_delivery";
        public const string RuneBonePlating = "bone_plating";
        public const string RuneCashBack = "cash_back";
        public const string RuneCelerity = "celerity";
        public const string RuneCheapShot = "cheap_shot";
        public const string RuneConditioning = "conditioning";
        public const string RuneConqueror = "conqueror";
        public const string RuneCosmicInsight = "cosmic_insight";
        public const string RuneCoupDeGrace = "coup_de_grace";
        public const string RuneCutDown = "cut_down";
        public const string RuneDarkHarvest = "dark_harvest";
        public const string RuneDeathfireTouch = "deathfire_touch";
        public const string RuneDeepWard = "deep_ward";
        public const string RuneDemolish = "demolish";
        public const string RuneElectrocute = "electrocute";
        public const string RuneFirstStrike = "first_strike";
        public const string RuneFleetFootwork = "fleet_footwork";
        public const string RuneFontOfLife = "font_of_life";
        public const string RuneGatheringStorm = "gathering_storm";
        public const string RuneGlacialAugment = "glacial_augment";
        public const string RuneGraspOfTheUndying = "grasp_of_the_undying";
        public const string RuneGrislyMementos = "grisly_mementos";
        public const string RuneGuardian = "guardian";
        public const string RuneHailOfBlades = "hail_of_blades";
        public const string RuneHextechFlashtraption = "hextech_flashtraption";
        public const string RuneJackOfAllTrades = "jack_of_all_trades";
        public const string RuneLastStand = "last_stand";
        public const string RuneLegendAlacrity = "legend_alacrity";
        public const string RuneLegendBloodline = "legend_bloodline";
        public const string RuneLegendHaste = "legend_haste";
        public const string RuneLethalTempo = "lethal_tempo";
        public const string RuneMagicalFootwear = "magical_footwear";
        public const string RuneManaflowBand = "manaflow_band";
        public const string RuneNimbusCloak = "nimbus_cloak";
        public const string RuneOvergrowth = "overgrowth";
        public const string RunePresenceOfMind = "presence_of_mind";
        // Predator：代码中有 PredatorSelected 判定，但当前 UI 未提供该基石。
        // 仍登记 ID 以保证判定与存档语义一致（未来接回 UI 时无需再改）。
        public const string RunePredator = "predator";
        public const string RunePressTheAttack = "press_the_attack";
        public const string RuneRelentlessHunter = "relentless_hunter";
        public const string RuneRevitalize = "revitalize";
        public const string RuneScorch = "scorch";
        public const string RuneSecondWind = "second_wind";
        public const string RuneShieldBash = "shield_bash";
        public const string RuneSixthSense = "sixth_sense";
        public const string RuneStormraidersSurge = "stormraiders_surge";
        public const string RuneSuddenImpact = "sudden_impact";
        public const string RuneSummonAery = "summon_aery";
        public const string RuneTasteOfBlood = "taste_of_blood";
        public const string RuneTimeWarpTonic = "time_warp_tonic";
        public const string RuneTranscendence = "transcendence";
        public const string RuneTreasureHunter = "treasure_hunter";
        public const string RuneTripleTonic = "triple_tonic";
        public const string RuneTriumph = "triumph";
        public const string RuneUltimateHunter = "ultimate_hunter";
        public const string RuneUnflinching = "unflinching";
        public const string RuneUnsealedSpellbook = "unsealed_spellbook";
        public const string RuneIngeniousHunter = "ingenious_hunter";
        public const string RuneRavenousHunter = "ravenous_hunter";
        public const string RuneEyeballCollection = "eyeball_collection";
        public const string RunePhaseRush = "phase_rush";
        public const string RuneWaterwalking = "waterwalking";
    }

    /// <summary>
    /// 符文注册表：ID、显示名、本地化键的唯一事实源。
    /// 原先散落在 RuneSaveSystem / RuneUIState 的 300+ 处显示名字面量收敛于此。
    /// </summary>
    public static class RuneRegistry
    {
        public const string LocPrefix = "Mods.LeagueOfLegendThings.";

        /// <summary>稳定 ID -> 英文显示名（仅用于拼本地化键，绝不写入存档）。</summary>
        private static readonly Dictionary<string, string> DisplayNames = new()
        {
            { RuneIds.RuneIngeniousHunter, "Ingenious Hunter" },
            { RuneIds.RuneRavenousHunter, "Ravenous Hunter" },
            { RuneIds.RuneEyeballCollection, "Eyeball Collection" },
            { RuneIds.RunePhaseRush, "Phase Rush" },
            { RuneIds.RuneAbsoluteFocus, "Absolute Focus" },
            { RuneIds.RuneAbsorbLife, "Absorb Life" },
            { RuneIds.RuneAftershock, "Aftershock" },
            { RuneIds.RuneApproachVelocity, "Approach Velocity" },
            { RuneIds.RuneArcaneComet, "Arcane Comet" },
            { RuneIds.RuneAxiomArcanist, "Axiom Arcanist" },
            { RuneIds.RuneBiscuitDelivery, "Biscuit Delivery" },
            { RuneIds.RuneBonePlating, "Bone Plating" },
            { RuneIds.RuneCashBack, "Cash Back" },
            { RuneIds.RuneCelerity, "Celerity" },
            { RuneIds.RuneCheapShot, "Cheap Shot" },
            { RuneIds.RuneConditioning, "Conditioning" },
            { RuneIds.RuneConqueror, "Conqueror" },
            { RuneIds.RuneCosmicInsight, "Cosmic Insight" },
            { RuneIds.RuneCoupDeGrace, "Coup de Grace" },
            { RuneIds.RuneCutDown, "Cut Down" },
            { RuneIds.RuneDarkHarvest, "Dark Harvest" },
            { RuneIds.RuneDeathfireTouch, "Deathfire Touch" },
            { RuneIds.RuneDeepWard, "Deep Ward" },
            { RuneIds.RuneDemolish, "Demolish" },
            { RuneIds.RuneElectrocute, "Electrocute" },
            { RuneIds.RuneFirstStrike, "First Strike" },
            { RuneIds.RuneFleetFootwork, "Fleet Footwork" },
            { RuneIds.RuneFontOfLife, "Font of Life" },
            { RuneIds.RuneGatheringStorm, "Gathering Storm" },
            { RuneIds.RuneGlacialAugment, "Glacial Augment" },
            { RuneIds.RuneGraspOfTheUndying, "Grasp of the Undying" },
            { RuneIds.RuneGrislyMementos, "Grisly Mementos" },
            { RuneIds.RuneGuardian, "Guardian" },
            { RuneIds.RuneHailOfBlades, "Hail of Blades" },
            { RuneIds.RuneHextechFlashtraption, "Hextech Flashtraption" },
            { RuneIds.RuneJackOfAllTrades, "Jack of All Trades" },
            { RuneIds.RuneLastStand, "Last Stand" },
            { RuneIds.RuneLegendAlacrity, "Legend: Alacrity" },
            { RuneIds.RuneLegendBloodline, "Legend: Bloodline" },
            { RuneIds.RuneLegendHaste, "Legend: Haste" },
            { RuneIds.RuneLethalTempo, "Lethal Tempo" },
            { RuneIds.RuneMagicalFootwear, "Magical Footwear" },
            { RuneIds.RuneManaflowBand, "Manaflow Band" },
            { RuneIds.RuneNimbusCloak, "Nimbus Cloak" },
            { RuneIds.RuneOvergrowth, "Overgrowth" },
            { RuneIds.RunePresenceOfMind, "Presence of Mind" },
            { RuneIds.RunePressTheAttack, "Press the Attack" },
            { RuneIds.RunePredator, "Predator" },
            { RuneIds.RuneRelentlessHunter, "Relentless Hunter" },
            { RuneIds.RuneRevitalize, "Revitalize" },
            { RuneIds.RuneScorch, "Scorch" },
            { RuneIds.RuneSecondWind, "Second Wind" },
            { RuneIds.RuneShieldBash, "Shield Bash" },
            { RuneIds.RuneSixthSense, "Sixth Sense" },
            { RuneIds.RuneStormraidersSurge, "Stormraider's Surge" },
            { RuneIds.RuneSuddenImpact, "Sudden Impact" },
            { RuneIds.RuneSummonAery, "Summon Aery" },
            { RuneIds.RuneTasteOfBlood, "Taste of Blood" },
            { RuneIds.RuneTimeWarpTonic, "Time Warp Tonic" },
            { RuneIds.RuneTranscendence, "Transcendence" },
            { RuneIds.RuneTreasureHunter, "Treasure Hunter" },
            { RuneIds.RuneTripleTonic, "Triple Tonic" },
            { RuneIds.RuneTriumph, "Triumph" },
            { RuneIds.RuneUltimateHunter, "Ultimate Hunter" },
            { RuneIds.RuneUnflinching, "Unflinching" },
            { RuneIds.RuneUnsealedSpellbook, "Unsealed Spellbook" },
            { RuneIds.RuneWaterwalking, "Waterwalking" },
        };

        private static readonly Dictionary<string, string> BuffKeys = new()
        {
            { RuneIds.RuneIngeniousHunter, "IngeniousHunter" },
            { RuneIds.RuneRavenousHunter, "RavenousHunter" },
            { RuneIds.RuneEyeballCollection, "EyeballCollection" },
            { RuneIds.RunePhaseRush, "PhaseRush" },
            { RuneIds.RuneAbsoluteFocus, "AbsoluteFocus" },
            { RuneIds.RuneAbsorbLife, "AbsorbLife" },
            { RuneIds.RuneAftershock, "Aftershock" },
            { RuneIds.RuneApproachVelocity, "ApproachVelocity" },
            { RuneIds.RuneArcaneComet, "ArcaneComet" },
            { RuneIds.RuneAxiomArcanist, "AxiomArcanist" },
            { RuneIds.RuneBiscuitDelivery, "BiscuitDelivery" },
            { RuneIds.RuneBonePlating, "BonePlating" },
            { RuneIds.RuneCashBack, "CashBack" },
            { RuneIds.RuneCelerity, "Celerity" },
            { RuneIds.RuneCheapShot, "CheapShot" },
            { RuneIds.RuneConditioning, "Conditioning" },
            { RuneIds.RuneConqueror, "Conqueror" },
            { RuneIds.RuneCosmicInsight, "CosmicInsight" },
            { RuneIds.RuneCoupDeGrace, "CoupDeGrace" },
            { RuneIds.RuneCutDown, "CutDown" },
            { RuneIds.RuneDarkHarvest, "DarkHarvest" },
            { RuneIds.RuneDeathfireTouch, "DeathfireTouch" },
            { RuneIds.RuneDeepWard, "DeepWard" },
            { RuneIds.RuneDemolish, "Demolish" },
            { RuneIds.RuneElectrocute, "Electrocute" },
            { RuneIds.RuneFirstStrike, "FirstStrike" },
            { RuneIds.RuneFleetFootwork, "FleetFootwork" },
            { RuneIds.RuneFontOfLife, "FontOfLife" },
            { RuneIds.RuneGatheringStorm, "GatheringStorm" },
            { RuneIds.RuneGlacialAugment, "GlacialAugment" },
            { RuneIds.RuneGraspOfTheUndying, "GraspOfTheUndying" },
            { RuneIds.RuneGrislyMementos, "GrislyMementos" },
            { RuneIds.RuneGuardian, "Guardian" },
            { RuneIds.RuneHailOfBlades, "HailOfBlades" },
            { RuneIds.RuneHextechFlashtraption, "HextechFlashtraption" },
            { RuneIds.RuneJackOfAllTrades, "JackOfAllTrades" },
            { RuneIds.RuneLastStand, "LastStand" },
            { RuneIds.RuneLegendAlacrity, "LegendAlacrity" },
            { RuneIds.RuneLegendBloodline, "LegendBloodline" },
            { RuneIds.RuneLegendHaste, "LegendHaste" },
            { RuneIds.RuneLethalTempo, "LethalTempo" },
            { RuneIds.RuneMagicalFootwear, "MagicalFootwear" },
            { RuneIds.RuneManaflowBand, "ManaflowBand" },
            { RuneIds.RuneNimbusCloak, "NimbusCloak" },
            { RuneIds.RuneOvergrowth, "Overgrowth" },
            { RuneIds.RunePresenceOfMind, "PresenceOfMind" },
            { RuneIds.RunePredator, "Predator" },
            { RuneIds.RunePressTheAttack, "PressTheAttack" },
            { RuneIds.RuneRelentlessHunter, "RelentlessHunter" },
            { RuneIds.RuneRevitalize, "Revitalize" },
            { RuneIds.RuneScorch, "Scorch" },
            { RuneIds.RuneSecondWind, "SecondWind" },
            { RuneIds.RuneShieldBash, "ShieldBash" },
            { RuneIds.RuneSixthSense, "SixthSense" },
            { RuneIds.RuneStormraidersSurge, "StormraidersSurge" },
            { RuneIds.RuneSuddenImpact, "SuddenImpact" },
            { RuneIds.RuneSummonAery, "SummonAery" },
            { RuneIds.RuneTasteOfBlood, "TasteOfBlood" },
            { RuneIds.RuneTimeWarpTonic, "TimeWarpTonic" },
            { RuneIds.RuneTranscendence, "Transcendence" },
            { RuneIds.RuneTreasureHunter, "TreasureHunter" },
            { RuneIds.RuneTripleTonic, "TripleTonic" },
            { RuneIds.RuneTriumph, "Triumph" },
            { RuneIds.RuneUltimateHunter, "UltimateHunter" },
            { RuneIds.RuneUnflinching, "Unflinching" },
            { RuneIds.RuneUnsealedSpellbook, "UnsealedSpellbook" },
            { RuneIds.RuneWaterwalking, "Waterwalking" },
        };

        public static readonly string[] Paths =
        {
            RuneIds.PathPrecision,
            RuneIds.PathDomination,
            RuneIds.PathSorcery,
            RuneIds.PathResolve,
            RuneIds.PathInspiration,
        };

        public static readonly Dictionary<string, string[]> Keystones = new()
        {
            { RuneIds.PathPrecision, new[] { RuneIds.RunePressTheAttack, RuneIds.RuneLethalTempo, RuneIds.RuneFleetFootwork, RuneIds.RuneConqueror } },
            { RuneIds.PathDomination, new[] { RuneIds.RuneElectrocute, RuneIds.RuneHailOfBlades, RuneIds.RuneDarkHarvest } },
            { RuneIds.PathSorcery, new[] { RuneIds.RuneSummonAery, RuneIds.RuneArcaneComet, RuneIds.RuneStormraidersSurge, RuneIds.RuneDeathfireTouch } },
            { RuneIds.PathResolve, new[] { RuneIds.RuneGraspOfTheUndying, RuneIds.RuneAftershock, RuneIds.RuneGuardian } },
            { RuneIds.PathInspiration, new[] { RuneIds.RuneGlacialAugment, RuneIds.RuneUnsealedSpellbook, RuneIds.RuneFirstStrike } },
        };

        public static readonly Dictionary<string, string[][]> Rows = new()
        {
            { RuneIds.PathPrecision, new[] { new[] { RuneIds.RuneAbsorbLife, RuneIds.RuneTriumph, RuneIds.RunePresenceOfMind }, new[] { RuneIds.RuneLegendAlacrity, RuneIds.RuneLegendHaste, RuneIds.RuneLegendBloodline }, new[] { RuneIds.RuneCoupDeGrace, RuneIds.RuneCutDown, RuneIds.RuneLastStand } } },
            { RuneIds.PathDomination, new[] { new[] { RuneIds.RuneCheapShot, RuneIds.RuneTasteOfBlood, RuneIds.RuneSuddenImpact }, new[] { RuneIds.RuneGrislyMementos, RuneIds.RuneSixthSense, RuneIds.RuneDeepWard }, new[] { RuneIds.RuneTreasureHunter, RuneIds.RuneRelentlessHunter, RuneIds.RuneUltimateHunter } } },
            { RuneIds.PathSorcery, new[] { new[] { RuneIds.RuneAxiomArcanist, RuneIds.RuneManaflowBand, RuneIds.RuneNimbusCloak }, new[] { RuneIds.RuneTranscendence, RuneIds.RuneCelerity, RuneIds.RuneAbsoluteFocus }, new[] { RuneIds.RuneScorch, RuneIds.RuneWaterwalking, RuneIds.RuneGatheringStorm } } },
            { RuneIds.PathResolve, new[] { new[] { RuneIds.RuneDemolish, RuneIds.RuneFontOfLife, RuneIds.RuneShieldBash }, new[] { RuneIds.RuneConditioning, RuneIds.RuneSecondWind, RuneIds.RuneBonePlating }, new[] { RuneIds.RuneOvergrowth, RuneIds.RuneRevitalize, RuneIds.RuneUnflinching } } },
            { RuneIds.PathInspiration, new[] { new[] { RuneIds.RuneHextechFlashtraption, RuneIds.RuneMagicalFootwear, RuneIds.RuneCashBack }, new[] { RuneIds.RuneTripleTonic, RuneIds.RuneTimeWarpTonic, RuneIds.RuneBiscuitDelivery }, new[] { RuneIds.RuneCosmicInsight, RuneIds.RuneApproachVelocity, RuneIds.RuneJackOfAllTrades } } },
        };

        /// <summary>取英文显示名；未知 ID 原样返回，便于暴露问题而不是静默出错。</summary>
        /// <summary>
        /// 稳定 ID -> 英文显示名。路径 ID 走 PathDisplayName，
        /// 否则小写 ID（如 "precision"）会被当成贴图名去找
        /// Content/Icon/Runes/precision，而实际文件是 Precision.png，
        /// tModLoader 资源名大小写敏感 -> 路径图标全部加载失败。
        /// </summary>
        public static string DisplayName(string id)
        {
            if (id == null) return null;
            if (DisplayNames.TryGetValue(id, out var v)) return v;
            return IsPath(id) ? PathDisplayName(id) : id;
        }

        public static bool IsPath(string id)
            => id != null && System.Array.IndexOf(Paths, id) >= 0;

        public static string BuffKey(string id)
            => id != null && BuffKeys.TryGetValue(id, out var v) ? v : id;

        private static readonly Dictionary<string, string> SanitizedNames = BuildSanitized();

        private static Dictionary<string, string> BuildSanitized()
        {
            var d = new Dictionary<string, string>();
            foreach (var kv in DisplayNames)
                d[kv.Key] = Sanitize(kv.Value);
            return d;
        }

        public static string Sanitized(string id)
            => id != null && SanitizedNames.TryGetValue(id, out var v) ? v : id;

        private static string Sanitize(string name)
        {
            var chars = new List<char>();
            foreach (var ch in name)
            {
                if (ch == '\'' || ch == '\u2019') continue;
                chars.Add(char.IsLetterOrDigit(ch) ? ch : '_');
            }
            var s = new string(chars.ToArray());
            while (s.Contains("__")) s = s.Replace("__", "_");
            return s.Trim('_');
        }

        // ---- 本地化键（原先在 RuneUIState 里用字符串拼接，现集中此处） ----
        public static string NameKey(string id)
            => LocPrefix + "Buffs." + BuffKey(id) + ".DisplayName";

        public static string DescKey(string id)
            => LocPrefix + "UI.Runes.Desc." + Sanitized(id);

        public static string PathNameKey(string pathId)
            => LocPrefix + "UI.Runes.Path." + SanitizeNameOf(pathId) + ".Name";

        public static string PathDescKey(string pathId)
            => LocPrefix + "UI.Runes.Path." + SanitizeNameOf(pathId) + ".Desc";

        private static string SanitizeNameOf(string pathId)
        {
            var disp = PathDisplayName(pathId);
            return Sanitize(disp);
        }

        private static string PathDisplayName(string pathId)
        {
            switch (pathId)
            {
                case "precision": return "Precision";
                case "domination": return "Domination";
                case "sorcery": return "Sorcery";
                case "resolve": return "Resolve";
                case "inspiration": return "Inspiration";
                default: return pathId;
            }
        }

        /// <summary>
        /// 旧存档 / 未知输入 -> 稳定 ID。
        /// 同时接受：
        ///   - 新稳定 ID（"lethal_tempo" / "precision"）
        ///   - 旧显示名（"Lethal Tempo" / "Precision"）
        ///   - 旧 BuffKey（"LethalTempo"）
        /// 无法识别时返回 null，由调用方决定回退策略。
        /// </summary>
        public static string ToId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var v = value.Trim();

            if (ReverseLookup.TryGetValue(v, out var hit)) return hit;

            // 不区分大小写的兜底（旧 BuffKey / 路径名的历史大小写差异）
            foreach (var kv in ReverseLookup)
            {
                if (string.Equals(kv.Key, v, System.StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        /// <summary>DisplayName / BuffKey / 路径显示名 -> 稳定 ID（精确匹配）。</summary>
        private static readonly Dictionary<string, string> ReverseLookup = BuildReverseLookup();

        private static Dictionary<string, string> BuildReverseLookup()
        {
            var d = new Dictionary<string, string>();
            foreach (var kv in DisplayNames)
            {
                d[kv.Key] = kv.Key;                 // 稳定 ID 本身（幂等）
                d[kv.Value] = kv.Key;               // 旧显示名
            }
            foreach (var kv in BuffKeys)
            {
                if (!d.ContainsKey(kv.Value)) d[kv.Value] = kv.Key;   // 旧 BuffKey
            }
            foreach (var pid in Paths)
            {
                d[pid] = pid;                       // 路径稳定 ID
                d[PathDisplayName(pid)] = pid;      // 旧路径显示名
            }
            return d;
        }

        /// <summary>ToId 的宽松版本：识别失败时返回原值，保证不丢数据。</summary>
        public static string ToIdOrKeep(string value)
            => ToId(value) ?? value;
    }
}
