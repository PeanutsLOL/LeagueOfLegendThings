using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LeagueOfLegendThings.Content.Systems
{
    public static class LegendSeries
    {
        // 通用叠层规则
        public const float PreHardmodeMaxStacks = 6f;
        public const float HardmodeMaxStacks = 8f;
        public const float MoonLordMaxStacks = 1f;

        public static float CalculateStacks(RuneSaveSystem save, float maxStacks)
        {
            if (save == null || save.DefeatedBosses == null)
                return 0f;

            int preHMCount = 0;
            int hmCount = 0;
            int mlCount = 0;

            foreach (int bossType in save.DefeatedBosses)
            {
                BossStage stage = GetBossStage(bossType);
                switch (stage)
                {
                    case BossStage.PreHardmode:
                        preHMCount++;
                        break;
                    case BossStage.Hardmode:
                        hmCount++;
                        break;
                    case BossStage.MoonLordAndPost:
                        mlCount++;
                        break;
                }
            }

            float totalStacks = 0f;
            totalStacks += System.Math.Min(preHMCount * 0.5f, PreHardmodeMaxStacks);
            totalStacks += System.Math.Min(hmCount * 0.5f, HardmodeMaxStacks);
            totalStacks += System.Math.Min(mlCount * 1f, MoonLordMaxStacks);

            return System.Math.Min(totalStacks, maxStacks);
        }

        public static BossStage GetBossStage(int npcType)
        {
            // Moon Lord 和 Post-ML Bosses
            if (npcType == NPCID.MoonLordCore || npcType == NPCID.MoonLordHead ||
                npcType == NPCID.MoonLordHand || npcType == NPCID.MoonLordLeechBlob)
            {
                return BossStage.MoonLordAndPost;
            }

            // Hardmode Bosses (before Moon Lord)
            if (npcType == NPCID.TheDestroyer ||
                npcType == NPCID.Retinazer || npcType == NPCID.Spazmatism ||
                npcType == NPCID.SkeletronPrime ||
                npcType == NPCID.Plantera ||
                npcType == NPCID.Golem || npcType == NPCID.GolemHead ||
                npcType == NPCID.DukeFishron ||
                npcType == NPCID.HallowBoss ||
                npcType == NPCID.CultistBoss)
            {
                return BossStage.Hardmode;
            }

            return BossStage.PreHardmode;
        }

        public static int GetMainBossType(NPC npc)
        {
            // 世界吞噬者：统一为头部
            if (npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
                return NPCID.EaterofWorldsHead;

            // 双子魔眼：统一为 Retinazer
            if (npc.type == NPCID.Spazmatism)
                return NPCID.Retinazer;

            // 月球领主：统一为核心
            if (npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand ||
                npc.type == NPCID.MoonLordLeechBlob)
                return NPCID.MoonLordCore;

            // 石巨人：统一为身体
            if (npc.type == NPCID.GolemHead || npc.type == NPCID.GolemFistLeft ||
                npc.type == NPCID.GolemFistRight)
                return NPCID.Golem;

            return npc.type;
        }
    }

    public enum BossStage
    {
        PreHardmode,
        Hardmode,
        MoonLordAndPost
    }

    /// <summary>
    /// 全局 NPC 钩子：记录击败的 Boss 用于传说系列符文
    /// </summary>
    public class LegendBossTracker : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (!npc.boss)
                return;

            // 只在服务端或单机处理
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // 记录到 RuneSaveSystem
            var save = ModContent.GetInstance<RuneSaveSystem>();
            if (save.DefeatedBosses == null)
                save.DefeatedBosses = new HashSet<int>();

            int bossType = LegendSeries.GetMainBossType(npc);
            if (!save.DefeatedBosses.Contains(bossType))
            {
                save.DefeatedBosses.Add(bossType);
            }
        }
    }
}
