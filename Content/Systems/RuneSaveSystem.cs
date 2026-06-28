using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LeagueOfLegendThings.Content.Config;

namespace LeagueOfLegendThings.Content.Systems
{
    public class RuneSaveSystem : ModSystem
    {
        public string PrimaryPath = "Precision";
        public string SecondaryPath = "Domination";

        public string PrimaryKeystone = "Lethal Tempo";
        public string PrimaryRow1 = "Absorb Life";
        public string PrimaryRow2 = "Legend: Alacrity";
        public string PrimaryRow3 = "Coup de Grace";

        public string SecondaryPick1 = ""; // not forced, can be empty
        public string SecondaryPick2 = ""; // not forced, can be empty
        public int SecondaryPick1Row = -1;
        public int SecondaryPick2Row = -1;

        // 用于传说系列符文：记录已击败的 Boss
        public HashSet<int> DefeatedBosses = new();

        // Mayhem 模式激活时强制禁用所有召唤师峡谷符文
        private bool MayhemActive => ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune;

        public bool PressTheAttackSelected => !MayhemActive && PrimaryPath == "Precision" && PrimaryKeystone == "Press the Attack";
        public bool LethalTempoSelected => !MayhemActive && PrimaryPath == "Precision" && PrimaryKeystone == "Lethal Tempo";
        public bool ConquerorSelected => !MayhemActive && PrimaryPath == "Precision" && PrimaryKeystone == "Conqueror";
        public bool FleetFootworkSelected => !MayhemActive && PrimaryPath == "Precision" && PrimaryKeystone == "Fleet Footwork";
        public bool AbsorbLifeSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow1 == "Absorb Life") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Absorb Life" || SecondaryPick2 == "Absorb Life"));
        public bool TriumphSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow1 == "Triumph") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Triumph" || SecondaryPick2 == "Triumph"));
        public bool PresenceOfMindSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow1 == "Presence of Mind") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Presence of Mind" || SecondaryPick2 == "Presence of Mind"));
        public bool LegendAlacritySelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow2 == "Legend: Alacrity") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Legend: Alacrity" || SecondaryPick2 == "Legend: Alacrity"));
        public bool LegendHasteSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow2 == "Legend: Haste") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Legend: Haste" || SecondaryPick2 == "Legend: Haste"));
        public bool LegendBloodlineSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow2 == "Legend: Bloodline") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Legend: Bloodline" || SecondaryPick2 == "Legend: Bloodline"));
        public bool CoupDeGraceSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow3 == "Coup de Grace") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Coup de Grace" || SecondaryPick2 == "Coup de Grace"));
        public bool CutDownSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow3 == "Cut Down") || SecondaryPath == "Precision" &&
            (SecondaryPick1 == "Cut Down" || SecondaryPick2 == "Cut Down");
        public bool LastStandSelected =>
            !MayhemActive && (PrimaryPath == "Precision" && PrimaryRow3 == "Last Stand") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Last Stand" || SecondaryPick2 == "Last Stand"));
        public bool ElectrocuteSelected => !MayhemActive && PrimaryPath == "Domination" && PrimaryKeystone == "Electrocute";
        public bool PredatorSelected => !MayhemActive && PrimaryPath == "Domination" && PrimaryKeystone == "Predator";
        public bool DarkHarvestSelected => !MayhemActive && PrimaryPath == "Domination" && PrimaryKeystone == "Dark Harvest";
        public bool HailOfBladesSelected => !MayhemActive && PrimaryPath == "Domination" && PrimaryKeystone == "Hail of Blades";
        public bool TasteOfBloodSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow1 == "Taste of Blood") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Taste of Blood" || SecondaryPick2 == "Taste of Blood"));
        public bool SuddenImpactSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow1 == "Sudden Impact") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Sudden Impact" || SecondaryPick2 == "Sudden Impact"));
        public bool EyeballCollectionSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow2 == "Eyeball Collection") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Eyeball Collection" || SecondaryPick2 == "Eyeball Collection"));
        public bool RavenousHunterSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow2 == "Ravenous Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Ravenous Hunter" || SecondaryPick2 == "Ravenous Hunter"));
        public bool IngeniousHunterSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow2 == "Ingenious Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Ingenious Hunter" || SecondaryPick2 == "Ingenious Hunter"));
        public bool TreasureHunterSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow3 == "Treasure Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Treasure Hunter" || SecondaryPick2 == "Treasure Hunter"));
        public bool RelentlessHunterSelected =>
            !MayhemActive && (PrimaryPath == "Domination" && PrimaryRow3 == "Relentless Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Relentless Hunter" || SecondaryPick2 == "Relentless Hunter"));
        public bool SummonAerySelected => !MayhemActive && PrimaryPath == "Sorcery" && PrimaryKeystone == "Summon Aery";
        public bool ArcaneCometSelected => !MayhemActive && PrimaryPath == "Sorcery" && PrimaryKeystone == "Arcane Comet";
        public bool PhaseRushSelected => !MayhemActive && PrimaryPath == "Sorcery" && PrimaryKeystone == "Phase Rush";
        public bool AxiomArcanistSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow1 == "Axiom Arcanist") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Axiom Arcanist" || SecondaryPick2 == "Axiom Arcanist"));
        public bool ManaflowBandSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow1 == "Manaflow Band") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Manaflow Band" || SecondaryPick2 == "Manaflow Band"));
        public bool NimbusCloakSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow1 == "Nimbus Cloak") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Nimbus Cloak" || SecondaryPick2 == "Nimbus Cloak"));
        public bool TranscendenceSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow2 == "Transcendence") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Transcendence" || SecondaryPick2 == "Transcendence"));
        public bool CeleritySelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow2 == "Celerity") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Celerity" || SecondaryPick2 == "Celerity"));
        public bool AbsoluteFocusSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow2 == "Absolute Focus") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Absolute Focus" || SecondaryPick2 == "Absolute Focus"));
        public bool ScorchSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow3 == "Scorch") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Scorch" || SecondaryPick2 == "Scorch"));
        public bool WaterwalkingSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow3 == "Waterwalking") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Waterwalking" || SecondaryPick2 == "Waterwalking"));
        public bool GatheringStormSelected =>
            !MayhemActive && (PrimaryPath == "Sorcery" && PrimaryRow3 == "Gathering Storm") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Gathering Storm" || SecondaryPick2 == "Gathering Storm"));
        public bool GraspOfTheUndyingSelected => !MayhemActive && PrimaryPath == "Resolve" && PrimaryKeystone == "Grasp of the Undying";
        public bool AftershockSelected => !MayhemActive && PrimaryPath == "Resolve" && PrimaryKeystone == "Aftershock";
        public bool GuardianSelected => !MayhemActive && PrimaryPath == "Resolve" && PrimaryKeystone == "Guardian";
        public bool DemolishSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow1 == "Demolish") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Demolish" || SecondaryPick2 == "Demolish"));
        public bool FontOfLifeSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow1 == "Font of Life") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Font of Life" || SecondaryPick2 == "Font of Life"));
        public bool ShieldBashSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow1 == "Shield Bash") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Shield Bash" || SecondaryPick2 == "Shield Bash"));
        public bool ConditioningSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow2 == "Conditioning") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Conditioning" || SecondaryPick2 == "Conditioning"));
        public bool SecondWindSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow2 == "Second Wind") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Second Wind" || SecondaryPick2 == "Second Wind"));
        public bool BonePlatingSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow2 == "Bone Plating") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Bone Plating" || SecondaryPick2 == "Bone Plating"));
        public bool OvergrowthSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow3 == "Overgrowth") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Overgrowth" || SecondaryPick2 == "Overgrowth"));
        public bool RevitalizeSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow3 == "Revitalize") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Revitalize" || SecondaryPick2 == "Revitalize"));
        public bool UnflinchingSelected =>
            !MayhemActive && (PrimaryPath == "Resolve" && PrimaryRow3 == "Unflinching") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Unflinching" || SecondaryPick2 == "Unflinching"));
        public bool GlacialAugmentSelected => !MayhemActive && PrimaryPath == "Inspiration" && PrimaryKeystone == "Glacial Augment";
        public bool UnsealedSpellbookSelected => !MayhemActive && PrimaryPath == "Inspiration" && PrimaryKeystone == "Unsealed Spellbook";
        public bool FirstStrikeSelected => !MayhemActive && PrimaryPath == "Inspiration" && PrimaryKeystone == "First Strike";
        public bool HextechFlashtraptionSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow1 == "Hextech Flashtraption") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Hextech Flashtraption" || SecondaryPick2 == "Hextech Flashtraption"));
        public bool MagicalFootwearSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow1 == "Magical Footwear") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Magical Footwear" || SecondaryPick2 == "Magical Footwear"));
        public bool CashBackSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow1 == "Cash Back") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Cash Back" || SecondaryPick2 == "Cash Back"));
        public bool TripleTonicSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow2 == "Triple Tonic") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Triple Tonic" || SecondaryPick2 == "Triple Tonic"));
        public bool TimeWarpTonicSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow2 == "Time Warp Tonic") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Time Warp Tonic" || SecondaryPick2 == "Time Warp Tonic"));
        public bool BiscuitDeliverySelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow2 == "Biscuit Delivery") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Biscuit Delivery" || SecondaryPick2 == "Biscuit Delivery"));
        public bool CosmicInsightSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow3 == "Cosmic Insight") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Cosmic Insight" || SecondaryPick2 == "Cosmic Insight"));
        public bool ApproachVelocitySelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow3 == "Approach Velocity") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Approach Velocity" || SecondaryPick2 == "Approach Velocity"));
        public bool JackOfAllTradesSelected =>
            !MayhemActive && (PrimaryPath == "Inspiration" && PrimaryRow3 == "Jack of All Trades") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Jack of All Trades" || SecondaryPick2 == "Jack of All Trades"));

        public override void OnWorldLoad()
        {
            // 符文选择由 RunePlayer.LoadData 负责，此处仅清世界级数据
            DefeatedBosses = new HashSet<int>();
        }

        public override void OnWorldUnload()
        {
            DefeatedBosses = new HashSet<int>();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag[nameof(PrimaryPath)] = PrimaryPath;
            tag[nameof(SecondaryPath)] = SecondaryPath;
            tag[nameof(PrimaryKeystone)] = PrimaryKeystone;
            tag[nameof(PrimaryRow1)] = PrimaryRow1;
            tag[nameof(PrimaryRow2)] = PrimaryRow2;
            tag[nameof(PrimaryRow3)] = PrimaryRow3;
            tag[nameof(SecondaryPick1)] = SecondaryPick1;
            tag[nameof(SecondaryPick2)] = SecondaryPick2;
            tag[nameof(SecondaryPick1Row)] = SecondaryPick1Row;
            tag[nameof(SecondaryPick2Row)] = SecondaryPick2Row;
            tag[nameof(DefeatedBosses)] = DefeatedBosses.ToList();
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.ContainsKey(nameof(PrimaryPath))) PrimaryPath = tag.GetString(nameof(PrimaryPath));
            if (tag.ContainsKey(nameof(SecondaryPath))) SecondaryPath = tag.GetString(nameof(SecondaryPath));
            if (tag.ContainsKey(nameof(PrimaryKeystone))) PrimaryKeystone = tag.GetString(nameof(PrimaryKeystone));
            if (tag.ContainsKey(nameof(PrimaryRow1))) PrimaryRow1 = tag.GetString(nameof(PrimaryRow1));
            if (tag.ContainsKey(nameof(PrimaryRow2))) PrimaryRow2 = tag.GetString(nameof(PrimaryRow2));
            if (tag.ContainsKey(nameof(PrimaryRow3))) PrimaryRow3 = tag.GetString(nameof(PrimaryRow3));
            if (tag.ContainsKey(nameof(SecondaryPick1))) SecondaryPick1 = tag.GetString(nameof(SecondaryPick1));
            if (tag.ContainsKey(nameof(SecondaryPick2))) SecondaryPick2 = tag.GetString(nameof(SecondaryPick2));
            if (tag.ContainsKey(nameof(SecondaryPick1Row))) SecondaryPick1Row = tag.GetInt(nameof(SecondaryPick1Row));
            if (tag.ContainsKey(nameof(SecondaryPick2Row))) SecondaryPick2Row = tag.GetInt(nameof(SecondaryPick2Row));
            if (tag.ContainsKey(nameof(DefeatedBosses)))
            {
                var list = tag.GetList<int>(nameof(DefeatedBosses));
                DefeatedBosses = new HashSet<int>(list);
            }
            else
            {
                DefeatedBosses = new HashSet<int>();
            }
        }
    }
}
