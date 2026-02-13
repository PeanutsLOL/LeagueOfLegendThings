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

        public bool PressTheAttackSelected => PrimaryPath == "Precision" && PrimaryKeystone == "Press the Attack";
        public bool LethalTempoSelected => PrimaryPath == "Precision" && PrimaryKeystone == "Lethal Tempo";
        public bool ConquerorSelected => PrimaryPath == "Precision" && PrimaryKeystone == "Conqueror";
        public bool FleetFootworkSelected => PrimaryPath == "Precision" && PrimaryKeystone == "Fleet Footwork";
        public bool AbsorbLifeSelected =>
            (PrimaryPath == "Precision" && PrimaryRow1 == "Absorb Life") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Absorb Life" || SecondaryPick2 == "Absorb Life"));
        public bool TriumphSelected =>
            (PrimaryPath == "Precision" && PrimaryRow1 == "Triumph") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Triumph" || SecondaryPick2 == "Triumph"));
        public bool PresenceOfMindSelected =>
            (PrimaryPath == "Precision" && PrimaryRow1 == "Presence of Mind") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Presence of Mind" || SecondaryPick2 == "Presence of Mind"));
        public bool LegendAlacritySelected =>
            (PrimaryPath == "Precision" && PrimaryRow2 == "Legend: Alacrity") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Legend: Alacrity" || SecondaryPick2 == "Legend: Alacrity"));
        public bool LegendHasteSelected =>
            (PrimaryPath == "Precision" && PrimaryRow2 == "Legend: Haste") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Legend: Haste" || SecondaryPick2 == "Legend: Haste"));
        public bool LegendBloodlineSelected =>
            (PrimaryPath == "Precision" && PrimaryRow2 == "Legend: Bloodline") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Legend: Bloodline" || SecondaryPick2 == "Legend: Bloodline"));
        public bool CoupDeGraceSelected =>
            (PrimaryPath == "Precision" && PrimaryRow3 == "Coup de Grace") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Coup de Grace" || SecondaryPick2 == "Coup de Grace"));
        public bool CutDownSelected =>
            (PrimaryPath == "Precision" && PrimaryRow3 == "Cut Down") || SecondaryPath == "Precision" &&
            (SecondaryPick1 == "Cut Down" || SecondaryPick2 == "Cut Down");
        public bool LastStandSelected =>
            (PrimaryPath == "Precision" && PrimaryRow3 == "Last Stand") ||
            (SecondaryPath == "Precision" && (SecondaryPick1 == "Last Stand" || SecondaryPick2 == "Last Stand"));
        public bool ElectrocuteSelected => PrimaryPath == "Domination" && PrimaryKeystone == "Electrocute";
        public bool PredatorSelected => PrimaryPath == "Domination" && PrimaryKeystone == "Predator";
        public bool DarkHarvestSelected => PrimaryPath == "Domination" && PrimaryKeystone == "Dark Harvest";
        public bool HailOfBladesSelected => PrimaryPath == "Domination" && PrimaryKeystone == "Hail of Blades";
        public bool TasteOfBloodSelected =>
            (PrimaryPath == "Domination" && PrimaryRow1 == "Taste of Blood") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Taste of Blood" || SecondaryPick2 == "Taste of Blood"));
        public bool SuddenImpactSelected =>
            (PrimaryPath == "Domination" && PrimaryRow1 == "Sudden Impact") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Sudden Impact" || SecondaryPick2 == "Sudden Impact"));
        public bool EyeballCollectionSelected =>
            (PrimaryPath == "Domination" && PrimaryRow2 == "Eyeball Collection") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Eyeball Collection" || SecondaryPick2 == "Eyeball Collection"));
        public bool RavenousHunterSelected =>
            (PrimaryPath == "Domination" && PrimaryRow2 == "Ravenous Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Ravenous Hunter" || SecondaryPick2 == "Ravenous Hunter"));
        public bool IngeniousHunterSelected =>
            (PrimaryPath == "Domination" && PrimaryRow2 == "Ingenious Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Ingenious Hunter" || SecondaryPick2 == "Ingenious Hunter"));
        public bool TreasureHunterSelected =>
            (PrimaryPath == "Domination" && PrimaryRow3 == "Treasure Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Treasure Hunter" || SecondaryPick2 == "Treasure Hunter"));
        public bool RelentlessHunterSelected =>
            (PrimaryPath == "Domination" && PrimaryRow3 == "Relentless Hunter") ||
            (SecondaryPath == "Domination" && (SecondaryPick1 == "Relentless Hunter" || SecondaryPick2 == "Relentless Hunter"));
        public bool SummonAerySelected => PrimaryPath == "Sorcery" && PrimaryKeystone == "Summon Aery";
        public bool ArcaneCometSelected => PrimaryPath == "Sorcery" && PrimaryKeystone == "Arcane Comet";
        public bool PhaseRushSelected => PrimaryPath == "Sorcery" && PrimaryKeystone == "Phase Rush";
        public bool AxiomArcanistSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow1 == "Axiom Arcanist") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Axiom Arcanist" || SecondaryPick2 == "Axiom Arcanist"));
        public bool ManaflowBandSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow1 == "Manaflow Band") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Manaflow Band" || SecondaryPick2 == "Manaflow Band"));
        public bool NimbusCloakSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow1 == "Nimbus Cloak") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Nimbus Cloak" || SecondaryPick2 == "Nimbus Cloak"));
        public bool TranscendenceSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow2 == "Transcendence") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Transcendence" || SecondaryPick2 == "Transcendence"));
        public bool CeleritySelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow2 == "Celerity") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Celerity" || SecondaryPick2 == "Celerity"));
        public bool AbsoluteFocusSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow2 == "Absolute Focus") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Absolute Focus" || SecondaryPick2 == "Absolute Focus"));
        public bool ScorchSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow3 == "Scorch") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Scorch" || SecondaryPick2 == "Scorch"));
        public bool WaterwalkingSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow3 == "Waterwalking") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Waterwalking" || SecondaryPick2 == "Waterwalking"));
        public bool GatheringStormSelected =>
            (PrimaryPath == "Sorcery" && PrimaryRow3 == "Gathering Storm") ||
            (SecondaryPath == "Sorcery" && (SecondaryPick1 == "Gathering Storm" || SecondaryPick2 == "Gathering Storm"));
        public bool GraspOfTheUndyingSelected => PrimaryPath == "Resolve" && PrimaryKeystone == "Grasp of the Undying";
        public bool AftershockSelected => PrimaryPath == "Resolve" && PrimaryKeystone == "Aftershock";
        public bool GuardianSelected => PrimaryPath == "Resolve" && PrimaryKeystone == "Guardian";
        public bool DemolishSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow1 == "Demolish") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Demolish" || SecondaryPick2 == "Demolish"));
        public bool FontOfLifeSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow1 == "Font of Life") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Font of Life" || SecondaryPick2 == "Font of Life"));
        public bool ShieldBashSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow1 == "Shield Bash") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Shield Bash" || SecondaryPick2 == "Shield Bash"));
        public bool ConditioningSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow2 == "Conditioning") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Conditioning" || SecondaryPick2 == "Conditioning"));
        public bool SecondWindSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow2 == "Second Wind") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Second Wind" || SecondaryPick2 == "Second Wind"));
        public bool BonePlatingSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow2 == "Bone Plating") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Bone Plating" || SecondaryPick2 == "Bone Plating"));
        public bool OvergrowthSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow3 == "Overgrowth") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Overgrowth" || SecondaryPick2 == "Overgrowth"));
        public bool RevitalizeSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow3 == "Revitalize") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Revitalize" || SecondaryPick2 == "Revitalize"));
        public bool UnflinchingSelected =>
            (PrimaryPath == "Resolve" && PrimaryRow3 == "Unflinching") ||
            (SecondaryPath == "Resolve" && (SecondaryPick1 == "Unflinching" || SecondaryPick2 == "Unflinching"));
        public bool HextechFlashtraptionSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow1 == "Hextech Flashtraption") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Hextech Flashtraption" || SecondaryPick2 == "Hextech Flashtraption"));
        public bool MagicalFootwearSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow1 == "Magical Footwear") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Magical Footwear" || SecondaryPick2 == "Magical Footwear"));
        public bool CashBackSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow1 == "Cash Back") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Cash Back" || SecondaryPick2 == "Cash Back"));
        public bool TripleTonicSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow2 == "Triple Tonic") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Triple Tonic" || SecondaryPick2 == "Triple Tonic"));
        public bool TimeWarpTonicSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow2 == "Time Warp Tonic") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Time Warp Tonic" || SecondaryPick2 == "Time Warp Tonic"));
        public bool BiscuitDeliverySelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow2 == "Biscuit Delivery") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Biscuit Delivery" || SecondaryPick2 == "Biscuit Delivery"));
        public bool CosmicInsightSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow3 == "Cosmic Insight") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Cosmic Insight" || SecondaryPick2 == "Cosmic Insight"));
        public bool ApproachVelocitySelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow3 == "Approach Velocity") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Approach Velocity" || SecondaryPick2 == "Approach Velocity"));
        public bool JackOfAllTradesSelected =>
            (PrimaryPath == "Inspiration" && PrimaryRow3 == "Jack of All Trades") ||
            (SecondaryPath == "Inspiration" && (SecondaryPick1 == "Jack of All Trades" || SecondaryPick2 == "Jack of All Trades"));

        public override void OnWorldLoad()
        {
            var cfg = ModContent.GetInstance<RuneConfig>();
            PrimaryPath = "Precision";
            SecondaryPath = "Domination";
            PrimaryKeystone = cfg.EnableLethalTempoRune ? "Lethal Tempo" : "Press the Attack";
            PrimaryRow1 = "Absorb Life";
            PrimaryRow2 = "Legend: Alacrity";
            PrimaryRow3 = "Coup de Grace";
            SecondaryPick1 = "";
            SecondaryPick2 = "";
            SecondaryPick1Row = -1;
            SecondaryPick2Row = -1;
            DefeatedBosses = new HashSet<int>();
        }

        public override void OnWorldUnload()
        {
            PrimaryPath = "Precision";
            SecondaryPath = "Domination";
            PrimaryKeystone = "Lethal Tempo";
            PrimaryRow1 = "Absorb Life";
            PrimaryRow2 = "Legend: Alacrity";
            PrimaryRow3 = "Coup de Grace";
            SecondaryPick1 = "";
            SecondaryPick2 = "";
            SecondaryPick1Row = -1;
            SecondaryPick2Row = -1;
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
