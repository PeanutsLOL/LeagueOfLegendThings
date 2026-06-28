using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LeagueOfLegendThings.Content.Buffs.Mayhem
{
    /// <summary>
    /// 吸血池 —— 所有生命偷取填入池子，再按规则回血。
    /// 池上限 = 50 + 吸血% × 250 / (吸血% + 30)，极限 ~300。
    /// 每秒恢复 8%，满血不消耗，池空不触发。
    /// </summary>
    public class LeechPoolPlayer : ModPlayer
    {
        private const float PoolBase = 50f;
        private const float PoolA = 250f;
        private const float PoolB = 30f;
        private const float RegenRatePerSec = 0.08f;

        public float PoolCurrent { get; private set; }
        private float _regenBuffer;

        public override void Initialize()
        {
            PoolCurrent = 0f;
            _regenBuffer = 0f;
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

        /// <summary>计算当前吸血%对应的池上限</summary>
        public float PoolMax()
        {
            var leech = Player.GetModPlayer<LeechPoolCollector>();
            float pct = leech.TotalLeechPercent;
            if (pct <= 0f) return PoolBase;
            return PoolBase + pct * PoolA / (pct + PoolB);
        }

        public override void PostUpdateMiscEffects()
        {
            float max = PoolMax();
            if (PoolCurrent > max) PoolCurrent = max;
            if (PoolCurrent < 0f) PoolCurrent = 0f;

            // 池子每秒恢复
            if (PoolCurrent < max)
            {
                _regenBuffer += max * RegenRatePerSec / 60f;
                int regen = (int)_regenBuffer;
                if (regen > 0)
                {
                    PoolCurrent = System.Math.Min(PoolCurrent + regen, max);
                    _regenBuffer -= regen;
                }
            }

            // 自动回血：玩家不满血时从池子消耗
            int missing = Player.statLifeMax2 - Player.statLife;
            if (missing > 0 && PoolCurrent > 0)
            {
                int heal = (int)System.Math.Min(missing, PoolCurrent);
                Player.statLife += heal;
                PoolCurrent -= heal;
                Player.HealEffect(heal, false);
            }
        }

        /// <summary>向池子填入生命偷取量</summary>
        public void Fill(float amount)
        {
            if (amount <= 0f) return;
            float max = PoolMax();
            if (PoolCurrent >= max) return;
            PoolCurrent = System.Math.Min(PoolCurrent + amount, max);
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
                var mp = Player.GetModPlayer<MayhemPlayer>();
                total += mp.CachedLifeSteal * 100f;
                return total;
            }
        }
    }
}
