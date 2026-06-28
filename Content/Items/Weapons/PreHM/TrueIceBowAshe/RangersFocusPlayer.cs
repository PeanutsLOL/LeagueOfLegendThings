using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using LeagueOfLegendThings.Content.Items.Weapons.PreHM.TrueIceBowAshe;

namespace LeagueOfLegendThings.Content.Items.Weapons.PreHM
{
    /// <summary>
    /// 追踪射手的专注层数的玩家类
    /// </summary>
    public class RangersFocusPlayer : ModPlayer
    {
        // 游侠专注层数（最大4层）
        public int FocusStacks = 0;
        
        // 层数衰减计时器（60帧 = 1秒）
        private int decayTimer = 0;
        private const int DecayTime = 60 * 2; // 2秒
        
        // 是否已达到4层（准备就绪状态）
        public bool IsReady = false;
        
        // 是否已播放就绪音效
        private bool readySoundPlayed = false;
        
        // 快速连射状态
        public bool RapidFireMode = false;
        public int RapidFireTimer = 0;
        private const int RapidFireDuration = 5 * 60; // 5秒
        
        public override void ResetEffects()
        {
            // 每帧更新
        }
        
        public override void PostUpdateMiscEffects()
        {
            // 快速连发模式：持续5秒，命中可刷新持续时间
            if (RapidFireMode)
            {
                if (RapidFireTimer > 0)
                    RapidFireTimer--;

                if (RapidFireTimer <= 0)
                {
                    ResetStacks();
                }

                return;
            }
            
            // 处理层数衰减
            if (FocusStacks > 0 && decayTimer > 0)
            {
                decayTimer--;
                
                if (decayTimer <= 0)
                {
                    // 衰减一次，清空所有层数
                    ResetStacks();
                }
            }
            
            // 检查是否达到4层
            if (FocusStacks >= 4 && !IsReady)
            {
                IsReady = true;
                
                // 播放就绪音效
                if (!readySoundPlayed)
                {
                    var readySound = new SoundStyle("LeagueOfLegendThings/Content/Items/Weapons/PreHM/TrueIceBowAshe/RMBATKReady");
                    SoundEngine.PlaySound(readySound, Player.Center);
                    readySoundPlayed = true;
                }
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner != Player.whoAmI)
                return;

            var tag = proj.GetGlobalProjectile<TrueIceBowAsheGlobalProjectile>();
            if (!tag.IsFromTrueIceBow)
                return;

            if (RapidFireMode)
            {
                RefreshRapidFireDuration();
                return;
            }

            AddStack();
        }
        public void AddStack()
        {
            bool holdingTrueIceBow = Player.inventory[Player.selectedItem]?.type == ModContent.ItemType<TrueIceBowAshe.TrueIceBowAshe>();
            
            if (!holdingTrueIceBow)
                return;
                
            if (RapidFireMode)
                return;
                
            if (FocusStacks < 4)
            {
                FocusStacks++;
            }
            
            // 重置衰减计时器
            decayTimer = DecayTime;
        }
        
        public void ResetStacks()
        {
            FocusStacks = 0;
            decayTimer = 0;
            IsReady = false;
            readySoundPlayed = false;
            RapidFireMode = false;
            RapidFireTimer = 0;
        }
        
        public void ActivateRapidFire()
        {
            // 确保只有在持有真冰弓时才能激活
            bool holdingTrueIceBow = Player.inventory[Player.selectedItem]?.type == ModContent.ItemType<TrueIceBowAshe.TrueIceBowAshe>();
            if (!holdingTrueIceBow)
                return;
                
            if (IsReady)
            {
                RapidFireMode = true;
                RefreshRapidFireDuration();
                
                // 播放激活音效
                var activateSound = new SoundStyle("LeagueOfLegendThings/Content/Items/Weapons/PreHM/TrueIceBowAshe/RMBATKActivate");
                SoundEngine.PlaySound(activateSound, Player.Center);
            }
        }

        public void RefreshRapidFireDuration()
        {
            RapidFireTimer = RapidFireDuration;
        }
        
        public override void OnHurt(Player.HurtInfo info)
        {
            // 受到任何伤害（含DoT）都重置层数与模式
            if (FocusStacks > 0 || RapidFireMode)
            {
                ResetStacks();
            }
        }
    }
}
