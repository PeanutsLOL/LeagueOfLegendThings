using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using System.Collections.Generic;
using System.Reflection;

namespace LeagueOfLegendThings.Content.Systems
{
    // GlobalNPC - 管理对每个 NPC 的冰川增幅效果与冷却
    public class GlacialAugmentGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // 当减速效果持续时计时（以帧为单位）
        private int glacialSlowTimer = 0;
        // 当减速结束后进入的冷却计时（以帧为单位）
        private int glacialCooldownTimer = 0;
        // 在减速期间用于间歇播放随机音效的计时器（每秒一次）
        private int glacialPlayTimer = 0;

        // 每帧更新计时器
        public override void PostAI(NPC npc)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            // 仅在符文激活时使用此逻辑；未激活时重置计时器并跳过
            if (!runeSave.GlacialAugmentSelected)
            {
                glacialSlowTimer = 0;
                glacialCooldownTimer = 0;
                return;
            }

            if (glacialSlowTimer > 0)
            {
                // 在每帧应用自定义减速
                // 将 NPC 的速度缩小 25%（乘以 0.75）
                if (!npc.dontTakeDamage)
                {
                    npc.velocity *= 0.75f;
                }

                // 播放期间音效计时器，每秒播放一次随机音效
                if (glacialPlayTimer > 0)
                    glacialPlayTimer--;
                if (glacialPlayTimer <= 0)
                {
                    try
                    {
                        string[] choices = { "Glacial_Augment_SFX_6", "Glacial_Augment_SFX_3", "Glacial_Augment_SFX_4" };
                        int idx = Main.rand.Next(choices.Length);
                        SoundEngine.PlaySound(new SoundStyle($"LeagueOfLegendThings/Content/SFX/{choices[idx]}"), npc.Center);
                    }
                    catch { }
                    glacialPlayTimer = 60; // 1 秒
                }

                glacialSlowTimer--;
                if (glacialSlowTimer == 0)
                {
                    // 减速结束时启动 5 秒冷却
                    glacialCooldownTimer = 5 * 60;
                    glacialPlayTimer = 0;
                }
            }
            else if (glacialCooldownTimer > 0)
            {
                glacialCooldownTimer--;
            }
        }

        private static readonly HashSet<int> ColdItemIDs = new();
        private static readonly HashSet<int> ColdProjectileIDs = new();
        private static bool coldIdsInitialized = false;

        private static void EnsureColdIdsInitialized()
        {
            if (coldIdsInitialized) return;
            coldIdsInitialized = true;

            // 尝试从 Terraria.ID.ItemID / ProjectileID 反射获取常量值
            var itemIdType = typeof(ItemID);
            var projIdType = typeof(ProjectileID);

            string[] itemNames = { "IceRod", "IceBow", "IceBlade", "Frostbrand", "Amarok", "IceSickle" };
            foreach (var name in itemNames)
            {
                var f = itemIdType.GetField(name, BindingFlags.Public | BindingFlags.Static);
                if (f != null && f.GetValue(null) is int val)
                    ColdItemIDs.Add(val);
            }

            // 如果需要可在此扩展 ProjectileID 名称列表
            string[] projNames = { "IceRod", "Frostbrand" };
            foreach (var name in projNames)
            {
                var f = projIdType.GetField(name, BindingFlags.Public | BindingFlags.Static);
                if (f != null && f.GetValue(null) is int val)
                    ColdProjectileIDs.Add(val);
            }
        }

        private bool IsColdSource(Item item, Projectile proj)
        {
            EnsureColdIdsInitialized();
            // 手动标记优先
            if (item != null && item.ModItem is IColdDamage)
                return true;
            if (proj != null && proj.ModProjectile is IColdDamage)
                return true;
            // ID 优先匹配（更稳）
            if (item != null && ColdItemIDs.Contains(item.type))
                return true;
            if (proj != null && ColdProjectileIDs.Contains(proj.type))
                return true;

            // 自动识别：根据类名或物品/投射物的 Name 包含关键词（兜底）
            string[] keywords = { "Ice", "Frost", "Snow", "Glacial", "寒", "冰", "霜", "雪" };
            if (item != null)
            {
                string nm = item.Name ?? string.Empty;
                foreach (var k in keywords)
                {
                    if (nm.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            if (proj != null)
            {
                string pnm = proj.Name ?? string.Empty;
                foreach (var k in keywords)
                {
                    if (pnm.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        private void TryTriggerGlacial(NPC npc, Player player, Item item, Projectile proj)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.GlacialAugmentSelected)
                return;

            if (npc.friendly || npc.lifeMax <= 5)
                return;

            // 如果正在减速或处于冷却中则不触发
            if (glacialSlowTimer > 0 || glacialCooldownTimer > 0)
                return;

            if (!IsColdSource(item, proj))
                return;

            // 触发：减速 25%（使用 Chilled Debuff）并附加 Frostburn，持续 3 秒
            int duration = 3 * 60;
            npc.AddBuff(BuffID.Chilled, duration);
            npc.AddBuff(BuffID.Frostburn, duration);

            // 触发时播放 SFX_2
            try
            {
                SoundEngine.PlaySound(new SoundStyle("LeagueOfLegendThings/Content/SFX/Glacial_Augment_SFX_2"), npc.Center);
            }
            catch { }

            // 期间随机播放一个（SFX_6 / SFX_3 / SFX_4）
            try
            {
                string[] choices = { "Glacial_Augment_SFX_6", "Glacial_Augment_SFX_3", "Glacial_Augment_SFX_4" };
                int idx = Main.rand.Next(choices.Length);
                SoundEngine.PlaySound(new SoundStyle($"LeagueOfLegendThings/Content/SFX/{choices[idx]}"), npc.Center);
            }
            catch { }

            // 启动减速计时（冷却将在减速结束后开始）
            glacialSlowTimer = duration;
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.GlacialAugmentSelected)
                return;
            TryTriggerGlacial(npc, player, item, null);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            var runeSave = ModContent.GetInstance<RuneSaveSystem>();
            if (!runeSave.GlacialAugmentSelected)
                return;
            Player player = Main.player[projectile.owner];
            TryTriggerGlacial(npc, player, null, projectile);
        }
    }
}
