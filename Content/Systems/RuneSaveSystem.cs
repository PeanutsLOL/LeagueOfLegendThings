using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LeagueOfLegendThings.Content.Config;

namespace LeagueOfLegendThings.Content.Systems
{
    public class RuneSaveSystem : ModSystem
    {
        // ---- 符文选择状态 ----
        // 全部以「稳定 ID」存储（见 RuneIds / RuneRegistry），不再存可被翻译的显示名。
        // setter 经 RuneRegistry.ToIdOrKeep 归一化，因此旧存档里的
        // "Lethal Tempo" / "LethalTempo" / "Precision" 等历史值会自动升级为 ID。
        private string _primaryPath = RuneIds.PathPrecision;
        private string _secondaryPath = RuneIds.PathDomination;
        private string _primaryKeystone = RuneIds.RuneLethalTempo;
        private string _primaryRow1 = RuneIds.RuneAbsorbLife;
        private string _primaryRow2 = RuneIds.RuneLegendAlacrity;
        private string _primaryRow3 = RuneIds.RuneCoupDeGrace;
        private string _secondaryPick1 = ""; // not forced, can be empty
        private string _secondaryPick2 = ""; // not forced, can be empty

        public string PrimaryPath
        {
            get => _primaryPath;
            set => _primaryPath = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string SecondaryPath
        {
            get => _secondaryPath;
            set => _secondaryPath = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string PrimaryKeystone
        {
            get => _primaryKeystone;
            set => _primaryKeystone = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string PrimaryRow1
        {
            get => _primaryRow1;
            set => _primaryRow1 = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string PrimaryRow2
        {
            get => _primaryRow2;
            set => _primaryRow2 = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string PrimaryRow3
        {
            get => _primaryRow3;
            set => _primaryRow3 = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string SecondaryPick1
        {
            get => _secondaryPick1;
            set => _secondaryPick1 = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public string SecondaryPick2
        {
            get => _secondaryPick2;
            set => _secondaryPick2 = RuneRegistry.ToIdOrKeep(value) ?? "";
        }

        public int SecondaryPick1Row = -1;
        public int SecondaryPick2Row = -1;

        // 用于传说系列符文：记录已击败的 Boss
        public HashSet<int> DefeatedBosses = new();

        // Mayhem 模式激活时强制禁用所有召唤师峡谷符文
        private bool MayhemActive => ModContent.GetInstance<RuneConfig>().EnableAramMayhemRune;

        public bool PressTheAttackSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryKeystone.Equals(RuneIds.RunePressTheAttack);
        public bool LethalTempoSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryKeystone.Equals(RuneIds.RuneLethalTempo);
        public bool ConquerorSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryKeystone.Equals(RuneIds.RuneConqueror);
        public bool FleetFootworkSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryKeystone.Equals(RuneIds.RuneFleetFootwork);
        public bool AbsorbLifeSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow1.Equals(RuneIds.RuneAbsorbLife)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneAbsorbLife) || SecondaryPick2.Equals(RuneIds.RuneAbsorbLife)));
        public bool TriumphSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow1.Equals(RuneIds.RuneTriumph)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneTriumph) || SecondaryPick2.Equals(RuneIds.RuneTriumph)));
        public bool PresenceOfMindSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow1.Equals(RuneIds.RunePresenceOfMind)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RunePresenceOfMind) || SecondaryPick2.Equals(RuneIds.RunePresenceOfMind)));
        public bool LegendAlacritySelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow2.Equals(RuneIds.RuneLegendAlacrity)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneLegendAlacrity) || SecondaryPick2.Equals(RuneIds.RuneLegendAlacrity)));
        public bool LegendHasteSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow2.Equals(RuneIds.RuneLegendHaste)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneLegendHaste) || SecondaryPick2.Equals(RuneIds.RuneLegendHaste)));
        public bool LegendBloodlineSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow2.Equals(RuneIds.RuneLegendBloodline)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneLegendBloodline) || SecondaryPick2.Equals(RuneIds.RuneLegendBloodline)));
        public bool CoupDeGraceSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow3.Equals(RuneIds.RuneCoupDeGrace)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneCoupDeGrace) || SecondaryPick2.Equals(RuneIds.RuneCoupDeGrace)));
        public bool CutDownSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow3.Equals(RuneIds.RuneCutDown)) || SecondaryPath.Equals(RuneIds.PathPrecision) &&
            (SecondaryPick1.Equals(RuneIds.RuneCutDown) || SecondaryPick2.Equals(RuneIds.RuneCutDown));
        public bool LastStandSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathPrecision) && PrimaryRow3.Equals(RuneIds.RuneLastStand)) ||
            (SecondaryPath.Equals(RuneIds.PathPrecision) && (SecondaryPick1.Equals(RuneIds.RuneLastStand) || SecondaryPick2.Equals(RuneIds.RuneLastStand)));
        public bool ElectrocuteSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryKeystone.Equals(RuneIds.RuneElectrocute);
        public bool PredatorSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryKeystone.Equals(RuneIds.RunePredator);
        public bool DarkHarvestSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryKeystone.Equals(RuneIds.RuneDarkHarvest);
        public bool HailOfBladesSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryKeystone.Equals(RuneIds.RuneHailOfBlades);
        public bool TasteOfBloodSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow1.Equals(RuneIds.RuneTasteOfBlood)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneTasteOfBlood) || SecondaryPick2.Equals(RuneIds.RuneTasteOfBlood)));
        public bool SuddenImpactSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow1.Equals(RuneIds.RuneSuddenImpact)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneSuddenImpact) || SecondaryPick2.Equals(RuneIds.RuneSuddenImpact)));
        public bool GrislyMementosSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow2.Equals(RuneIds.RuneGrislyMementos)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneGrislyMementos) || SecondaryPick2.Equals(RuneIds.RuneGrislyMementos)));
        public bool SixthSenseSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow2.Equals(RuneIds.RuneSixthSense)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneSixthSense) || SecondaryPick2.Equals(RuneIds.RuneSixthSense)));
        public bool DeepWardSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow2.Equals(RuneIds.RuneDeepWard)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneDeepWard) || SecondaryPick2.Equals(RuneIds.RuneDeepWard)));
        public bool TreasureHunterSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow3.Equals(RuneIds.RuneTreasureHunter)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneTreasureHunter) || SecondaryPick2.Equals(RuneIds.RuneTreasureHunter)));
        public bool RelentlessHunterSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow3.Equals(RuneIds.RuneRelentlessHunter)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneRelentlessHunter) || SecondaryPick2.Equals(RuneIds.RuneRelentlessHunter)));
        public bool UltimateHunterSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathDomination) && PrimaryRow3.Equals(RuneIds.RuneUltimateHunter)) ||
            (SecondaryPath.Equals(RuneIds.PathDomination) && (SecondaryPick1.Equals(RuneIds.RuneUltimateHunter) || SecondaryPick2.Equals(RuneIds.RuneUltimateHunter)));
        public bool SummonAerySelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryKeystone.Equals(RuneIds.RuneSummonAery);
        public bool ArcaneCometSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryKeystone.Equals(RuneIds.RuneArcaneComet);
        public bool StormraidersSurgeSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryKeystone.Equals(RuneIds.RuneStormraidersSurge);
        public bool DeathfireTouchSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryKeystone.Equals(RuneIds.RuneDeathfireTouch);
        public bool AxiomArcanistSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow1.Equals(RuneIds.RuneAxiomArcanist)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneAxiomArcanist) || SecondaryPick2.Equals(RuneIds.RuneAxiomArcanist)));
        public bool ManaflowBandSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow1.Equals(RuneIds.RuneManaflowBand)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneManaflowBand) || SecondaryPick2.Equals(RuneIds.RuneManaflowBand)));
        public bool NimbusCloakSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow1.Equals(RuneIds.RuneNimbusCloak)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneNimbusCloak) || SecondaryPick2.Equals(RuneIds.RuneNimbusCloak)));
        public bool TranscendenceSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow2.Equals(RuneIds.RuneTranscendence)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneTranscendence) || SecondaryPick2.Equals(RuneIds.RuneTranscendence)));
        public bool CeleritySelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow2.Equals(RuneIds.RuneCelerity)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneCelerity) || SecondaryPick2.Equals(RuneIds.RuneCelerity)));
        public bool AbsoluteFocusSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow2.Equals(RuneIds.RuneAbsoluteFocus)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneAbsoluteFocus) || SecondaryPick2.Equals(RuneIds.RuneAbsoluteFocus)));
        public bool ScorchSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow3.Equals(RuneIds.RuneScorch)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneScorch) || SecondaryPick2.Equals(RuneIds.RuneScorch)));
        public bool WaterwalkingSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow3.Equals(RuneIds.RuneWaterwalking)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneWaterwalking) || SecondaryPick2.Equals(RuneIds.RuneWaterwalking)));
        public bool GatheringStormSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathSorcery) && PrimaryRow3.Equals(RuneIds.RuneGatheringStorm)) ||
            (SecondaryPath.Equals(RuneIds.PathSorcery) && (SecondaryPick1.Equals(RuneIds.RuneGatheringStorm) || SecondaryPick2.Equals(RuneIds.RuneGatheringStorm)));
        public bool GraspOfTheUndyingSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryKeystone.Equals(RuneIds.RuneGraspOfTheUndying);
        public bool AftershockSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryKeystone.Equals(RuneIds.RuneAftershock);
        public bool GuardianSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryKeystone.Equals(RuneIds.RuneGuardian);
        public bool DemolishSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow1.Equals(RuneIds.RuneDemolish)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneDemolish) || SecondaryPick2.Equals(RuneIds.RuneDemolish)));
        public bool FontOfLifeSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow1.Equals(RuneIds.RuneFontOfLife)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneFontOfLife) || SecondaryPick2.Equals(RuneIds.RuneFontOfLife)));
        public bool ShieldBashSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow1.Equals(RuneIds.RuneShieldBash)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneShieldBash) || SecondaryPick2.Equals(RuneIds.RuneShieldBash)));
        public bool ConditioningSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow2.Equals(RuneIds.RuneConditioning)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneConditioning) || SecondaryPick2.Equals(RuneIds.RuneConditioning)));
        public bool SecondWindSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow2.Equals(RuneIds.RuneSecondWind)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneSecondWind) || SecondaryPick2.Equals(RuneIds.RuneSecondWind)));
        public bool BonePlatingSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow2.Equals(RuneIds.RuneBonePlating)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneBonePlating) || SecondaryPick2.Equals(RuneIds.RuneBonePlating)));
        public bool OvergrowthSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow3.Equals(RuneIds.RuneOvergrowth)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneOvergrowth) || SecondaryPick2.Equals(RuneIds.RuneOvergrowth)));
        public bool RevitalizeSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow3.Equals(RuneIds.RuneRevitalize)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneRevitalize) || SecondaryPick2.Equals(RuneIds.RuneRevitalize)));
        public bool UnflinchingSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathResolve) && PrimaryRow3.Equals(RuneIds.RuneUnflinching)) ||
            (SecondaryPath.Equals(RuneIds.PathResolve) && (SecondaryPick1.Equals(RuneIds.RuneUnflinching) || SecondaryPick2.Equals(RuneIds.RuneUnflinching)));
        public bool GlacialAugmentSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryKeystone.Equals(RuneIds.RuneGlacialAugment);
        public bool UnsealedSpellbookSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryKeystone.Equals(RuneIds.RuneUnsealedSpellbook);
        public bool FirstStrikeSelected => !MayhemActive && PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryKeystone.Equals(RuneIds.RuneFirstStrike);
        public bool HextechFlashtraptionSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow1.Equals(RuneIds.RuneHextechFlashtraption)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneHextechFlashtraption) || SecondaryPick2.Equals(RuneIds.RuneHextechFlashtraption)));
        public bool MagicalFootwearSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow1.Equals(RuneIds.RuneMagicalFootwear)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneMagicalFootwear) || SecondaryPick2.Equals(RuneIds.RuneMagicalFootwear)));
        public bool CashBackSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow1.Equals(RuneIds.RuneCashBack)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneCashBack) || SecondaryPick2.Equals(RuneIds.RuneCashBack)));
        public bool TripleTonicSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow2.Equals(RuneIds.RuneTripleTonic)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneTripleTonic) || SecondaryPick2.Equals(RuneIds.RuneTripleTonic)));
        public bool TimeWarpTonicSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow2.Equals(RuneIds.RuneTimeWarpTonic)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneTimeWarpTonic) || SecondaryPick2.Equals(RuneIds.RuneTimeWarpTonic)));
        public bool BiscuitDeliverySelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow2.Equals(RuneIds.RuneBiscuitDelivery)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneBiscuitDelivery) || SecondaryPick2.Equals(RuneIds.RuneBiscuitDelivery)));
        public bool CosmicInsightSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow3.Equals(RuneIds.RuneCosmicInsight)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneCosmicInsight) || SecondaryPick2.Equals(RuneIds.RuneCosmicInsight)));
        public bool ApproachVelocitySelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow3.Equals(RuneIds.RuneApproachVelocity)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneApproachVelocity) || SecondaryPick2.Equals(RuneIds.RuneApproachVelocity)));
        public bool JackOfAllTradesSelected =>
            !MayhemActive && (PrimaryPath.Equals(RuneIds.PathInspiration) && PrimaryRow3.Equals(RuneIds.RuneJackOfAllTrades)) ||
            (SecondaryPath.Equals(RuneIds.PathInspiration) && (SecondaryPick1.Equals(RuneIds.RuneJackOfAllTrades) || SecondaryPick2.Equals(RuneIds.RuneJackOfAllTrades)));

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
