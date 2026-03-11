using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using LeagueOfLegendThings.Content.Systems;

namespace LeagueOfLegendThings.Content.Items.Weapons.PreHM.TrueIceBowAshe
{
    public class TrueIceBowAshe : ModItem
    {
        // 普通射速（外层节奏）
        private const int NormalUseTime = 32;
        private static readonly int[] RapidFireShotOrder = { 2,1,3,0,4 };

        // 五连发参数：保持外层节奏不变，在一次动画内打出5发
        private const int RapidFireShots = 5;
        private const int RapidUseTime = 2;
        // 五连发结束到下一轮首发的间隔（按住左键时生效）
        private const int RapidBurstGap = NormalUseTime - 5;
        // 首发在0帧，末发在 RapidUseTime*(RapidFireShots-1) 帧，随后留出 RapidBurstGap
        private const int RapidUseAnimation = RapidUseTime * (RapidFireShots - 1) + RapidBurstGap;
        private const float RapidFireDamageMultiplier = 0.33f;
        private const float RapidFireSpeedMultiplier = 1.08f;
        private const float ShootSpeed = 24f;
        private const float RapidFireVerticalSpacing = 18f;
        private const float RemoteAimDistance = 1600f;
        
        public override void SetStaticDefaults()
        {
            
        }

        public override void SetDefaults()
        {
            Item.damage = 36;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 20;
            Item.height = 20;
            Item.useTime = NormalUseTime;
            Item.useAnimation = NormalUseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.value = Item.sellPrice(silver: 7);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = null; // 使用自定义音效
            Item.autoReuse = true;
            Item.useLimitPerAnimation = 1;
            Item.shoot = ModContent.ProjectileType<TrueIceBowAsheProjectile>();
            Item.shootSpeed = ShootSpeed;
        }

        public override float UseSpeedMultiplier(Player player)
        {
            var focusPlayer = player.GetModPlayer<RangersFocusPlayer>();
            return focusPlayer.RapidFireMode ? RapidFireSpeedMultiplier : 1f;
        }
        
        public override bool AltFunctionUse(Player player)
        {
            // 允许右键使用
            return true;
        }
        
        public override bool CanUseItem(Player player)
        {
            var focusPlayer = player.GetModPlayer<RangersFocusPlayer>();
            
            // 右键需要4层才能使用
            if (player.altFunctionUse == 2)
            {
                if (focusPlayer.IsReady && !focusPlayer.RapidFireMode)
                {
                    // 切换到快速连射模式
                    focusPlayer.ActivateRapidFire();
                    
                    // 进入模式后：一次完整攻击=25帧内的5连发
                    Item.useTime = RapidUseTime;
                    Item.useAnimation = RapidUseAnimation;
                    Item.useLimitPerAnimation = RapidFireShots;

                    // 右键仅用于切换模式，不在右键当次发射
                    return false;
                }
                return false;
            }
            
            // 左键正常攻击
            if (focusPlayer.RapidFireMode)
            {
                // 快速连射模式
                Item.useTime = RapidUseTime;
                Item.useAnimation = RapidUseAnimation;
                Item.useLimitPerAnimation = RapidFireShots;
                return true;
            }
            else
            {
                // 正常模式
                Item.useTime = NormalUseTime;
                Item.useAnimation = NormalUseTime;
                Item.useLimitPerAnimation = 1;
                return true;
            }
        }
        
        public override bool? UseItem(Player player)
        {
            var focusPlayer = player.GetModPlayer<RangersFocusPlayer>();

            if (player.whoAmI != Main.myPlayer)
                return base.UseItem(player);
            
            // 播放攻击音效
            if (focusPlayer.RapidFireMode)
            {
                // 快速连射音效：仅每轮5连发的第一箭播放一次
                if (player.ItemUsesThisAnimation == 1)
                {
                    int soundIndex = Main.rand.Next(1, 3); // 1或2
                    var rapidSound = new SoundStyle($"LeagueOfLegendThings/Content/Items/Weapons/PreHM/TrueIceBowAshe/RMBATK{soundIndex}")
                    {
                        MaxInstances = 16
                    };
                    SoundEngine.PlaySound(rapidSound, player.Center);
                }
            }
            else
            {
                // 正常攻击音效（左键）
                int soundIndex = Main.rand.Next(1, 4); // 1, 2, 或3
                var attackSound = new SoundStyle($"LeagueOfLegendThings/Content/Items/Weapons/PreHM/TrueIceBowAshe/LMBATK{soundIndex}")
                {
                    MaxInstances = 16
                };
                SoundEngine.PlaySound(attackSound, player.Center);
            }
            
            return base.UseItem(player);
        }
        
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            var focusPlayer = player.GetModPlayer<RangersFocusPlayer>();

            if (focusPlayer.RapidFireMode)
            {
                damage = (int)(damage * RapidFireDamageMultiplier);
                ApplyRapidFireShotPattern(player, ref position, ref velocity);
            }
        }

        private void ApplyRapidFireShotPattern(Player player, ref Vector2 position, ref Vector2 velocity)
        {
            int shotIndex = player.ItemUsesThisAnimation - 1;
            if (shotIndex < 0)
                shotIndex = 0;
            if (shotIndex >= RapidFireShots)
                shotIndex = RapidFireShots - 1;

            int verticalSlot = RapidFireShotOrder[shotIndex];
            float offsetY = (verticalSlot - 2) * RapidFireVerticalSpacing;

            Vector2 basePosition = position;
            position = basePosition + new Vector2(0f, offsetY);

            Vector2 fallbackDirection = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 targetPoint = player.whoAmI == Main.myPlayer
                ? Main.MouseWorld
                : basePosition + fallbackDirection * RemoteAimDistance;

            Vector2 toTarget = targetPoint - position;
            if (toTarget.LengthSquared() < 0.001f)
                toTarget = fallbackDirection;

            velocity = toTarget.SafeNormalize(fallbackDirection) * ShootSpeed;
        }
        
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI);
            var tag = proj.GetGlobalProjectile<TrueIceBowAsheGlobalProjectile>();
            tag.IsFromTrueIceBow = true;
            tag.LockedVelocityY = velocity.Y;
            tag.NoDropVelocityInitialized = true;
            
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DemonBow)
                .AddIngredient(ItemID.IceBlock, 50)
                .AddIngredient(ItemID.Shiverthorn, 3)
                .AddIngredient(ItemID.Sapphire, 8)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.TendonBow)
                .AddIngredient(ItemID.IceBlock, 50)
                .AddIngredient(ItemID.Shiverthorn, 3)
                .AddIngredient(ItemID.Sapphire, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}