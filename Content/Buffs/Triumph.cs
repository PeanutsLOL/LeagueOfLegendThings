using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs
{
    public class Triumph : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var triumphPlayer = player.GetModPlayer<TriumphPlayer>();
            triumphPlayer.HasTriumphBuff = true;
        }
    }

    /// <summary>
    /// 在 PostSetupContent 阶段构建 NPC type -> BossBag ItemID 的查找表。
    /// 使用 ModContent.TryFind 软引用其他模组的 Boss，不安装也不影响加载。
    /// </summary>
    public class TriumphBossBagSystem : ModSystem
    {
        internal static Dictionary<int, int> BossBagMap = new();

        public override void PostSetupContent()
        {
            BossBagMap.Clear();

            // ============ 原版 Vanilla ============
            BossBagMap[NPCID.KingSlime]          = ItemID.KingSlimeBossBag;
            BossBagMap[NPCID.EyeofCthulhu]       = ItemID.EyeOfCthulhuBossBag;
            BossBagMap[NPCID.EaterofWorldsHead]   = ItemID.EaterOfWorldsBossBag;
            BossBagMap[NPCID.EaterofWorldsBody]   = ItemID.EaterOfWorldsBossBag;
            BossBagMap[NPCID.EaterofWorldsTail]   = ItemID.EaterOfWorldsBossBag;
            BossBagMap[NPCID.BrainofCthulhu]      = ItemID.BrainOfCthulhuBossBag;
            BossBagMap[NPCID.QueenBee]            = ItemID.QueenBeeBossBag;
            BossBagMap[NPCID.SkeletronHead]       = ItemID.SkeletronBossBag;
            BossBagMap[NPCID.WallofFlesh]         = ItemID.WallOfFleshBossBag;
            BossBagMap[NPCID.TheDestroyer]        = ItemID.DestroyerBossBag;
            BossBagMap[NPCID.Retinazer]           = ItemID.TwinsBossBag;
            BossBagMap[NPCID.Spazmatism]          = ItemID.TwinsBossBag;
            BossBagMap[NPCID.SkeletronPrime]      = ItemID.SkeletronPrimeBossBag;
            BossBagMap[NPCID.Plantera]            = ItemID.PlanteraBossBag;
            BossBagMap[NPCID.Golem]               = ItemID.GolemBossBag;
            BossBagMap[NPCID.DukeFishron]         = ItemID.FishronBossBag;
            BossBagMap[NPCID.CultistBoss]         = ItemID.CultistBossBag;
            BossBagMap[NPCID.MoonLordCore]        = ItemID.MoonLordBossBag;
            BossBagMap[NPCID.HallowBoss]          = ItemID.FairyQueenBossBag;
            BossBagMap[NPCID.QueenSlimeBoss]      = ItemID.QueenSlimeBossBag;
            BossBagMap[NPCID.Deerclops]           = ItemID.DeerclopsBossBag;

            // ============ Calamity Mod (软引用，不安装不影响) ============
            TryRegister("CalamityMod", "DesertScourgeHead",          "CalamityMod", "DesertScourgeBag");
            TryRegister("CalamityMod", "Crabulon",                   "CalamityMod", "CrabulonBag");
            TryRegister("CalamityMod", "HiveMind",                   "CalamityMod", "HiveMindBag");
            TryRegister("CalamityMod", "PerforatorHive",             "CalamityMod", "PerforatorBag");
            TryRegister("CalamityMod", "SlimeGodCore",               "CalamityMod", "SlimeGodBag");
            TryRegister("CalamityMod", "Cryogen",                    "CalamityMod", "CryogenBag");
            TryRegister("CalamityMod", "AquaticScourgeHead",         "CalamityMod", "AquaticScourgeBag");
            TryRegister("CalamityMod", "BrimstoneElemental",         "CalamityMod", "BrimstoneElementalBag");
            TryRegister("CalamityMod", "CalamitasClone",             "CalamityMod", "CalamitasCloneBag");
            TryRegister("CalamityMod", "Leviathan",                  "CalamityMod", "LeviathanBag");
            TryRegister("CalamityMod", "AstrumAureus",               "CalamityMod", "AstrumAureusBag");
            TryRegister("CalamityMod", "PlaguebringerGoliath",       "CalamityMod", "PlaguebringerGoliathBag");
            TryRegister("CalamityMod", "RavagerBody",                "CalamityMod", "RavagerBag");
            TryRegister("CalamityMod", "AstrumDeusHead",             "CalamityMod", "AstrumDeusBag");
            TryRegister("CalamityMod", "ProfanedGuardianCommander",  "CalamityMod", "ProfanedGuardianBag");
            TryRegister("CalamityMod", "Bumblefuck",                 "CalamityMod", "DragonfollyBag");
            TryRegister("CalamityMod", "Providence",                 "CalamityMod", "ProvidenceBag");
            TryRegister("CalamityMod", "StormWeaverHead",            "CalamityMod", "StormWeaverBag");
            TryRegister("CalamityMod", "CeaselessVoid",              "CalamityMod", "CeaselessVoidBag");
            TryRegister("CalamityMod", "Signus",                     "CalamityMod", "SignusBag");
            TryRegister("CalamityMod", "Polterghast",                "CalamityMod", "PolterghastBag");
            TryRegister("CalamityMod", "OldDuke",                    "CalamityMod", "OldDukeBag");
            TryRegister("CalamityMod", "DevourerofGodsHead",         "CalamityMod", "DevourerofGodsBag");
            TryRegister("CalamityMod", "Yharon",                     "CalamityMod", "YharonBag");
            TryRegister("CalamityMod", "SupremeCalamitas",           "CalamityMod", "SupremeCalamitasBag");

            // ============ Fargo's Souls Mod (软引用) ============
            TryRegister("FargowiltasSouls", "MutantBoss",  "FargowiltasSouls", "MutantBag");
            TryRegister("FargowiltasSouls", "AbomBoss",    "FargowiltasSouls", "AbomBag");
            TryRegister("FargowiltasSouls", "DeviBoss",    "FargowiltasSouls", "DeviBag");

            // 如需支持更多模组，在此按相同格式添加即可
        }

        private void TryRegister(string npcMod, string npcName, string itemMod, string itemName)
        {
            if (ModContent.TryFind<ModNPC>(npcMod + "/" + npcName, out var npc) &&
                ModContent.TryFind<ModItem>(itemMod + "/" + itemName, out var item))
            {
                BossBagMap[npc.Type] = item.Type;
            }
        }

        public override void Unload()
        {
            BossBagMap = null;
        }
    }

    public class TriumphPlayer : ModPlayer
    {
        // 是否激活凯旋效果
        public bool HasTriumphBuff;

        public override void ResetEffects()
        {
            HasTriumphBuff = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // 检查符文是否选择，直接设置标记
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.TriumphSelected)
            {
                HasTriumphBuff = true;
            }
        }

        public override void UpdateDead()
        {
            HasTriumphBuff = false;
        }
    }

    public class TriumphNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (!npc.boss)
                return;

            // 只在主客户端或服务端处理，避免重复
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // 检查所有玩家，找到有 Triumph buff 且符文已选择的玩家
            bool anyPlayerHasTriumph = false;
            foreach (var player in Main.ActivePlayers)
            {
                var triumphPlayer = player.GetModPlayer<TriumphPlayer>();
                if (!triumphPlayer.HasTriumphBuff)
                    continue;

                var save = ModContent.GetInstance<RuneSaveSystem>();
                if (!save.TriumphSelected)
                    continue;

                anyPlayerHasTriumph = true;

                // 治疗：2.5% 最大生命值
                int healAmount = (int)(player.statLifeMax2 * 0.025f);
                if (healAmount > 0)
                    player.Heal(healAmount);
            }

            // 如果有任何玩家激活了 Triumph，在 Boss 位置额外掉落宝藏袋
            if (anyPlayerHasTriumph)
            {
                int bagType = GetBossBagType(npc);
                if (bagType > 0)
                {
                    // 在 Boss 中心位置生成物品到世界中
                    Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, bagType);
                }
            }
        }

        private int GetBossBagType(NPC target)
        {
            // 纯打表查找（原版 + 已注册的模组 Boss）
            if (TriumphBossBagSystem.BossBagMap != null &&
                TriumphBossBagSystem.BossBagMap.TryGetValue(target.type, out int bagId))
                return bagId;

            return 0;
        }
    }
}
