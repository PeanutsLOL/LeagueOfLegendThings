using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LeagueOfLegendThings.Content.Config;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 吸血池 —— 所有生命偷取消耗池子余额，立即回血。
    /// 池上限 = 50 + 吸血% × 250 / (吸血% + 30)，极限 ~300。
    /// 每秒恢复上限的 8%，池空时吸血无效果。
    /// </summary>
    public class LeechPoolPlayer : ModPlayer
    {
        private const float PoolBase = 50f;
        private const float PoolA = 250f;
        private const float PoolB = 30f;
        private const float RegenRatePerSec = 0.08f;

        public float PoolCurrent { get; private set; }
        private float _fractionBuffer; // 小数缓存：攒够 1 点就回血
        private int _tickTimer;

        public override void Initialize()
        {
            PoolCurrent = 0f;
            _fractionBuffer = 0f;
            _tickTimer = 0;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["leechPool"] = PoolCurrent;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("leechPool"))
                PoolCurrent = tag.GetFloat("leechPool");
        }

        /// <summary>计算当前吸血%对应的池上限。无偷取时返回 0。</summary>
        public float PoolMax()
        {
            var leech = Player.GetModPlayer<LeechPoolCollector>();
            float pct = leech.TotalLeechPercent;
            if (pct <= 0f) return 0f;
            return PoolBase + pct * PoolA / (pct + PoolB);
        }

        public override void PostUpdateMiscEffects()
        {
            // 每帧裁剪池上限（响应模式/符文切换）
            float max = PoolMax();
            if (PoolCurrent > max) PoolCurrent = max;
            if (PoolCurrent < 0f) PoolCurrent = 0f;

            // 每秒恢复一次
            _tickTimer++;
            if (_tickTimer < 60) return;
            _tickTimer = 0;

            if (PoolCurrent < max)
            {
                PoolCurrent = System.Math.Min(PoolCurrent + max * RegenRatePerSec, max);
            }
        }

        /// <summary>
        /// 从池子消费吸血量，立即回血。
        /// 小额吸血会累积在缓冲区，攒够 1 点才真正回血扣池。
        /// 池空时不产生任何治疗。
        /// </summary>
        /// <summary>
        /// 消耗池子回血。amount = 伤害 × 生命偷取率。
        /// 整数部分即时治疗；小数部分累积，攒够 1 点后一起治疗。
        /// </summary>
        public void TryConsume(float amount)
        {
            if (amount <= 0f || PoolCurrent < 1f) return;

            int missing = Player.statLifeMax2 - Player.statLife;
            if (missing <= 0) return;

            // 整数部分即时回血，小数部分存入缓冲
            int integerPart = (int)amount;
            _fractionBuffer += amount - integerPart;

            // 缓冲攒够 1 点就加到治疗里
            int fromBuffer = (int)_fractionBuffer;
            int heal = integerPart + fromBuffer;

            if (heal <= 0) return;

            _fractionBuffer -= fromBuffer;
            if (heal > (int)PoolCurrent) heal = (int)PoolCurrent;
            if (heal > missing) heal = missing;
            if (heal <= 0) return;

            Player.statLife += heal;
            PoolCurrent -= heal;
            Player.HealEffect(heal, false);
        }
    }

    /// <summary>
    /// 汇总所有来源的吸血百分比（供池子计算上限）
    /// </summary>
    public class LeechPoolCollector : ModPlayer
    {
        /// <summary>所有来源的生命偷取百分比总和（5 = 5%）</summary>
        public float TotalLeechPercent
        {
            get
            {
                float total = 0f;
                var cfg = ModContent.GetInstance<RuneConfig>();

                // Mayhem 碎片偷取（StatShards）
                if (cfg.EnableAramMayhemRune)
                {
                    var mp = Player.GetModPlayer<MayhemPlayer>();
                    total += mp.CachedLifeSteal * 100f;
                }

                // 普通符文偷取（预估最大可能值，用于池上限）
                if (!cfg.EnableAramMayhemRune)
                {
                    var rs = ModContent.GetInstance<RuneSaveSystem>();
                    if (rs.ConquerorSelected) total += 0.5f;
                    if (rs.LegendBloodlineSelected) total += 4.5f; // 满层 15×0.3%
                    if (rs.RavenousHunterSelected) total += 0.25f;  // 满层 5×0.05%
                }

                return total;
            }
        }
    }
}
