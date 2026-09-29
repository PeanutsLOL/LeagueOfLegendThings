# 更新计划 v0.4.0 — "峡谷降临"

> 状态: 规划中 | 目标: 大版本内容更新

---

## 目录

1. [视觉与特效提升](#1-视觉与特效提升)
2. [符文平衡打磨](#2-符文平衡打磨)
3. [英雄装备套装系统](#3-英雄装备套装系统)
4. [Boss/敌怪内容](#4-boss敌怪内容)
5. [版本规划与里程碑](#5-版本规划与里程碑)

---

## 1. 视觉与特效提升

> 核心痛点: 不会自定义 Dust 和贴图，只能引用原版 DustID

### 1.1 可用的原版粒子效果速查

tModLoader 中所有原版粒子通过 `DustID` 静态类访问。以下按场景分类推荐：

#### 远程/弓箭特效
| DustID | 外观 | 推荐用途 |
|--------|------|----------|
| `DustID.Ice` | 蓝白色冰晶碎片 | 艾希冰箭命中、减速 |
| `DustID.IceTorch` | 冰蓝火焰粒子 | 冰系武器挥动 |
| `DustID.BlueCrystalShard` | 蓝色水晶碎片 | 冰箭穿透/暴击 |
| `DustID.Frost` | 霜雾 | 冰系范围减速区域 |
| `DustID.FrostStaff` | 冰刺喷雾 | W 散射箭袋 |

#### 魔法/能量特效
| DustID | 外观 | 推荐用途 |
|--------|------|----------|
| `DustID.MagicMirror` | 白色闪光 | 传送/闪现 |
| `DustID.EnchantedGold` | 金色魔法粒子 | 金身/护盾 |
| `DustID.PurpleCrystalShard` | 紫色水晶碎片 | 虚空/魔法伤害 |
| `DustID.Shadowflame` | 暗紫色火焰 | 黑暗收割、诅咒效果 |
| `DustID.Electric` | 黄色电火花 | 电刑 |

#### 治疗/增益特效
| DustID | 外观 | 推荐用途 |
|--------|------|----------|
| `DustID.HealingPlus` | 绿色十字粒子 | 治疗反馈 |
| `DustID.LifeDrain` | 红色吸血粒子 | 生命偷取 |
| `DustID.GreenBlood` | 绿色血雾 | 征服者叠层 |
| `DustID.EnchantedNightcrawler` | 金色飞虫 | Buff 触发提示 |

#### 爆炸/冲击特效
| DustID | 外观 | 推荐用途 |
|--------|------|----------|
| `DustID.Firework_Red` | 红色烟花 | 先攻触发 |
| `DustID.Explosion` | 烟雾爆炸 | 彗星命中 |
| `DustID.Smoke` | 灰烟 | 通用命中反馈 |
| `DustID.InfernoFork` | 地狱火 | 大型火焰伤害 |

#### 护盾/防御特效
| DustID | 外观 | 推荐用途 |
|--------|------|----------|
| `DustID.GoldCoin` | 金色闪光 | 余震护盾 |
| `DustID.PlatinumCoin` | 白金光 | 守护者 |
| `DustID.SolarFlare` | 橙色火焰 | 不灭之握 |

#### 使用示例（C# 代码）
```csharp
// 在命中位置生成环形粒子爆发
for (int i = 0; i < 12; i++)
{
    float angle = MathHelper.TwoPi * i / 12f;
    var dust = Dust.NewDustPerfect(
        target.Center + new Vector2(20, 0).RotatedBy(angle),
        DustID.Ice,
        new Vector2(2, 0).RotatedBy(angle),
        100, default, 1.5f
    );
    dust.noGravity = true;
    dust.fadeIn = 1f;
}
```

### 1.2 现有符文特效增强清单

每个基石符文命中时应当有可辨识的粒子爆发，而不是使用同一套 Dust：

| 符文 | 当前状况 | 改进方向 |
|------|----------|----------|
| 电刑 | 黄色电光 ✓ | 可加 `DustID.Electric` 链式闪电粒子 |
| 黑暗收割 | ? | 加 `DustID.Shadowflame` + 灵魂上升粒子 |
| 奥术彗星 | 有弹幕 | 命中加 `DustID.MagicMirror` 环形波 |
| 先攻 | ? | 加 `DustID.Firework_Red` 爆发 |
| 余震 | ? | 加 `DustID.SolarFlare` 地面裂纹 |
| 不灭之握 | ? | 加 `DustID.HealingPlus` + 绿色光柱 |
| 强攻 | ? | 加金色冲击波 `DustID.EnchantedGold` |

### 1.3 UI 视觉升级选项

由于你不会自定义 UI 贴图，可以考虑：

1. **利用 `TextureAssets.MagicPixel`**（已在 LeechPoolBar 中使用）— 纯色像素级绘制
2. **复用原版 UI 部件** — `Main.Assets.RequestedTexture` 可以加载原版面板贴图
3. **参考原版 UI 配色** — Terraria 经典棕/金/石风格

具体可改进的 UI：
- Rune 选择面板加简单的像素边框和分隔线（MagicPixel 即可实现）
- StatAnvilUI 卡片面板增加阴影/立体感
- 所有 UI 统一配色方案

---

## 2. 符文平衡打磨

> 核心问题: 所有符文已完成但部分选择率低 — 不是没做完，是不好玩/没用

### 2.1 疑似冷门符文及可能原因

| 符文 | 可能原因 | 解决方向 |
|------|----------|----------|
| 冰川增幅 | 减速在 Terraria 里不关键（Boss 免疫） | 对 Boss 改为降低防御或攻速 |
| 相位猛冲 | 三次攻击才触发，位移量小 | 减少触发次数或加大突进距离 |
| 启封的秘籍 | 功能性太弱 | 加一个常驻小属性 |
| 守护者 | 刚重做完成 | 观察数据 |
| 神奇之鞋 | 移速在 Terraria 不加伤害 | 附加少量伤害加成 |
| 爆破 | 对建筑伤害无意义 | 改为对敌怪额外击退+伤害 |

### 2.2 差异化方向

确保 5 个基石在玩法上有所区分：
- **电刑** → 单体爆发（含 %HP）
- **黑暗收割** → 收割斩杀（低血增伤）
- **奥术彗星** → 远程消耗（投射物）
- **先攻** → 先手优势（第一次伤害翻倍）
- **征服者** → 持久战（叠层）

精密系基石也应差异化：
- **致命节奏** → 投射物流（已完成弧线）
- **强攻** → 团队增伤挂标记
- **迅捷步法** → 治疗+加速的游击风格
- **丛刃** → 前三击爆发攻速

---

## 3. 英雄装备套装系统

> 核心思路: 每个英雄 = 盔甲三件套 + 专属武器 + 套装技能
> 套装效果解锁该英雄的 Q/W/E/R 中的 2-3 个

### 3.1 贴图绘制完全指南

#### 需要绘制什么

tModLoader 中一套完整盔甲需要以下 PNG 文件：

```
Content/Items/Armor/<ChampionName>/
├── <ChampionName>Helmet.png          # 头盔物品图标
├── <ChampionName>Chestplate.png      # 胸甲物品图标
├── <ChampionName>Leggings.png        # 护腿物品图标
├── <ChampionName>Helmet_Head.png     # 装备后头部显示
├── <ChampionName>Chestplate_Body.png # 装备后身体显示
├── <ChampionName>Leggings_Legs.png   # 装备后腿部显示
├── <ChampionName>Chestplate_FemaleBody.png  # 女性身体
└── <ChampionName>Chestplate_Arms.png        # 手臂贴图
```

#### 像素尺寸规格

| 贴图类型 | 尺寸 | 说明 |
|----------|------|------|
| 物品图标（Item） | 40×40 或更小 | 背包里显示的图标 |
| 头部显示 (_Head) | 40×40 最多覆盖 | 玩家头顶装备渲染 |
| 身体显示 (_Body) | 40×56 总共（分多帧） | 最长的一张贴图 |
| 手臂显示 (_Arms) | 40×56 | 手臂覆盖 |
| 腿部显示 (_Legs) | 40×56 | 腿部覆盖 |
| 女性身体 (_FemaleBody) | 40×56 | 仅当玩家为女性时 |
| 武器 | 40×40 或更大 | 根据武器类型调整 |

#### 如何画 — 分步指南

1. **工具推荐**:
   - **Piskel** (免费在线) — https://www.piskelapp.com/ — 最适合像素动画
   - **Aseprite** (付费) — 专业像素画工具
   - **Paint.NET** + 放大镜 — 最基础可用
   - 甚至可以用 **Windows 画图** 放大到 800% 逐像素绘制

2. **参考原版贴图**:
   - tModLoader 安装后，原版贴图在: `<tModLoader目录>/Content/Images/`
   - 可以直接看原版盔甲怎么画的: `Armor_Head_XX.png`, `Armor_Body_XX.png`
   - 推荐参考: 钯金套 (Palladium)、冰霜套 (Frost)、钛金套 (Titanium)

3. **盔甲绘制流程**:
   ```
   步骤1: 用 Terraria 角色模板作为底层参考
   步骤2: 在上面绘制你的盔甲外形（覆盖身体部分）
   步骤3: 用 2-3 种色调给出立体感（亮面/暗面/中间色）
   步骤4: 导出为 PNG，确保透明背景
   步骤5: 放入对应文件夹，tModLoader 自动加载
   ```

4. **关键技巧**:
   - Terraria 光照对盔甲影响很大 → 用中等亮度颜色，不要太暗
   - 物品图标的 40×40 里实际只有中间 ~30×30 有用
   - 武器放在 40×40 画布里，方向朝右上
   - 可以先画一个 8×8 的极小缩略图确定形状，再放大精修

#### 如果你实在不想画 — 替代方案

1. **利用原版贴图染色**: 用 `ModItem.SetStaticDefaults()` 给盔甲指定原版外观
   ```csharp
   // 借用原版盔甲外观，但功能不同
   Item.vanity = false; // 不是时装
   ```
2. **程序化生成**: 使用 tModLoader 的 `EquipTexture` + `DrawLayer` 在渲染时叠加纯色形状
3. **使用现有 Icon 目录的方法**: 项目中已有 100+ 个符文图标 PNG，可以作为物品图标参考

### 3.2 第一套：艾希完整套装（True Ice 系列）

#### 已有资产
- ✅ **真冰弓** (TrueIceBowAshe) — 左键正常射箭 + 右键 Q 技能（游侠专注 → 快速连射）
- ✅ **冰箭弹幕** (TrueIceBowAsheProjectile) — 带轨迹拖尾的直线弹道
- ✅ **专注层数系统** (RangersFocusPlayer) — 4层叠满后触发

#### 需要新增

**贴图清单**（待绘制）:
```
Content/Items/Armor/Ashe/
├── AsheHelmet.png              (40×40 物品图标)
├── AsheHelmet_Head.png         (40×40 头部覆盖)
├── AsheChestplate.png          (40×40 物品图标)
├── AsheChestplate_Body.png     (40×56 身体覆盖)
├── AsheChestplate_FemaleBody.png (40×56 女性身体)
├── AsheChestplate_Arms.png     (40×56 手臂)
├── AsheLeggings.png            (40×40 物品图标)
└── AsheLeggings_Legs.png       (40×56 腿部覆盖)
```

**设计方向**:
- 参考 LoL 艾希经典皮肤配色: 深蓝 + 银白 + 金色点缀
- 头盔: 兜帽风格，半遮面
- 胸甲: 轻甲皮甲风格
- 护腿: 修身设计

**代码清单**:

| 文件 | 说明 |
|------|------|
| `Content/Items/Armor/Ashe/AsheHelmet.cs` | 头盔 ModItem — 提供远程伤害加成 |
| `Content/Items/Armor/Ashe/AsheChestplate.cs` | 胸甲 ModItem — 提供远程暴击+攻速 |
| `Content/Items/Armor/Ashe/AsheLeggings.cs` | 护腿 ModItem — 提供移速+弹药节约 |
| `Content/Items/Armor/Ashe/AsheSetPlayer.cs` | ModPlayer — 套装效果逻辑 |
| `Content/Items/Armor/Ashe/AsheWProjectile.cs` | W 技能弹幕（散射箭袋） |
| `Content/Items/Armor/Ashe/AsheRProjectile.cs` | R 技能弹幕（魔法水晶箭） |

**套装效果设计**:

| 件数 | 效果 |
|------|------|
| 2件 | 远程伤害 +12% |
| 3件（全套） | 解锁以下技能: |
| └ **W — 万箭齐发 (Volley)** | 左键蓄力/右键释放：向鼠标方向射出 5 支锥形冰箭，各造成 60% 武器伤害 + 减速 |
| └ **R — 魔法水晶箭 (Enchanted Crystal Arrow)** | 冷却 60s：射出一发巨型冰晶箭矢，伤害 300% + 命中后 3 秒范围晕眩 + 爆炸 |

**与现有真冰弓联动**:
- 持有真冰弓 + 全套盔甲 → Q 技能（快速连射）持续时间从 5s → 8s，每箭伤害从 33% → 45%
- W 和 R 技能绑定到特定按键（例如 W = 右下角技能按钮，R = 特殊快捷键）

**合成配方**:
```
艾希头盔:
  真冰弓 × 1 + 冰晶块 × 20 + 蓝宝石 × 5
  在铁砧合成

艾希胸甲:
  真冰弓 × 1 + 冰晶块 × 30 + 蓝宝石 × 8
  在铁砧合成

艾希护腿:
  冰晶块 × 20 + 丝绸 × 10 + 蓝宝石 × 5
  在铁砧合成
```
> 注意: 真冰弓本身需要消耗来做盔甲 → 意味着需要额外刷材料多做一把弓

**阶段目标**: `v0.3.50` — 艾希套装完成并可玩

### 3.3 后续英雄装备规划

按难度和贴图复杂度排序：

| 优先级 | 英雄 | 套装名称 | 武器 | 特色技能 | 贴图难度 |
|--------|------|----------|------|----------|----------|
| 1 | **艾希** | True Ice | 弓 (已完成) | Q/W/R | ⭐⭐ |
| 2 | **盖伦** | Might of Demacia | 大剑 | Q(沉默冲刺)/W(护盾)/E(旋转) | ⭐ |
| 3 | **拉克丝** | Illumination | 法杖 | Q(束缚)/E(范围)/R(激光) | ⭐⭐ |
| 4 | **亚索** | Way of the Wanderer | 武士刀 | Q(龙卷)/W(风墙)/R(空中连斩) | ⭐⭐⭐ |
| 5 | **劫** | Shadow Order | 手里剑/爪 | W(影分身)/R(死亡标记) | ⭐⭐⭐ |

**盖伦** 贴图最简单（全盔甲遮盖身体，不需要精准匹配体型），建议作为第 2 套。

---

## 4. Boss/敌怪内容

> 核心思路: 把峡谷中立生物做成 Terraria Boss/小 Boss

### 4.1 Boss 贴图要求

Boss NPC 贴图比盔甲复杂得多：

| 需求 | 说明 |
|------|------|
| 主贴图 (PNG) | 单个 PNG 或精灵表 (Spritesheet) |
| 精灵表格式 | 多帧动画水平排列，帧数自定义 |
| Boss 体型 | 通常 80×80 到 200×200+ |
| 帧数 | 飞行/行走帧 (~6-8 帧)，攻击帧 (~4-6 帧) |
| 推荐工具 | Piskel (支持动画帧层) / Aseprite |
| 简化方案 | 单帧静态 + 代码旋转/缩放实现动态感 |

**简化版 Boss 制作方案**（适合画不好多帧动画的情况）:
- 只画 1-2 帧
- 用代码实现 Boss 的移动和旋转来掩盖缺帧
- 参考原版克苏鲁之眼（只有几张图，靠代码弥补）
- 粒子特效遮盖贴图不足（大量 Dust 爆发）

### 4.2 峡谷生物 Boss 设计

#### 阶段 1: 红蓝 Buff 怪（小 Boss，肉前）

**红 Brambleback**:
- 肉前晚期 Boss
- 召唤方式: 在猩红/腐化之地使用特殊物品
- 行为: 缓慢追踪 + 间歇近战重击 + 低于 50% HP 狂暴加速
- 掉落: 红 Buff 饰品（攻击附带燃烧 DoT）
- 贴图: 约 80×80，类似岩石巨像 + 燃烧纹理

**蓝 Sentinel**:
- 与红对应，侧重魔法
- 召唤方式: 在雪原/地下使用特殊物品
- 行为: 保持距离 + 远程魔法弹幕 + 低于 50% HP 释放减速光环
- 掉落: 蓝 Buff 饰品（加快法力回复 + 技能冷却）
- 贴图: 约 80×80，石像鬼 + 冰蓝符文纹理

#### 阶段 2: 元素巨龙（中 Boss，肉后）

- 四条龙按游戏阶段解锁：云 (肉后前期) → 山 (机械后) → 海 (世纪之花后) → 炼狱 (石巨人后)
- 每条龙一种元素类型: 风/土/水/火
- 掉落: 对应元素的龙魂材料 → 用于合成后期装备

#### 阶段 3: 纳什男爵（大 Boss，月后）

- 月总后的终局 Boss
- 召唤方式: 特定物品在夜晚召唤
- 多个阶段: 远程酸液弹幕 → 近战触手横扫 → 狂暴全屏 AOE
- 掉落: 男爵之眼（终极饰品，提供大幅全属性 + 特殊效果）

### 4.3 开始策略

**最容易上手的是红/蓝 Buff 怪**:
1. 单帧或双帧贴图（80×80 够大，画着容易）
2. 不需要复杂 AI（参考原版克苏鲁之眼的行为模式）
3. 可以作为测试 Boss 制作流程的试验品

---

## 5. 版本规划与里程碑

### 版本号规则
- 当前: `v0.3.43`
- 规则: 第三位按提交数递增，内容更新加第二位
- 下一个内容版本: `v0.4.0`

### 里程碑

| 版本 | 内容 | 预计提交 |
|------|------|----------|
| **v0.3.45** | 符文特效增强 (1.2) + 冷门符文数值调整 (2.1-2.2) | ~6 commits |
| **v0.3.48** | 艾希盔甲套 + W/R 技能完整实现 | ~10 commits |
| **v0.3.50** | 红蓝 Buff Boss + 配套掉落 | ~12 commits |
| **v0.3.52** | UI 像素美化 + 音效补全 | ~5 commits |
| **v0.3.55** | 盖伦盔甲套 | ~8 commits |
| **v0.4.0** | 首个元素巨龙 Boss + 整合测试 | ~15 commits |

### 建议推进顺序

```
v0.3.43 (当前)
  │
  ├─→ 贴图先行: 绘制艾希盔甲 + 红蓝 Buff 怪贴图
  │     (不需要写代码，先画图 — 画好后能极大推动后续)
  │
  ├─→ v0.3.45: 符文特效 + 平衡调整
  │     不依赖贴图，纯代码改动，可立即开始
  │
  ├─→ v0.3.48: 艾希套装代码
  │     依赖贴图完成
  │
  ├─→ v0.3.50: 红蓝 Buff Boss
  │     依赖贴图完成
  │
  └─→ v0.4.0: 整合发布
```

### 并行工作建议
- **等贴图时**: 做符文平衡 + 符文特效（纯代码）
- **贴图完成时**: 做装备套装 + Boss
- **测试时**: UI 美化 + 音效

---

## 附录 A: 贴图速查卡

```
┌─────────────────────────────────────────────────┐
│  物品图标:  40×40 PNG                            │
│  头部覆盖:  40×40 PNG                            │
│  身体覆盖:  40×56 PNG (男女共用/分开发)           │
│  手臂覆盖:  40×56 PNG                            │
│  腿部覆盖:  40×56 PNG                            │
│  武器图标:  40×40 PNG (或更大)                    │
│  弹幕贴图:  任意 (推荐 16×16 到 32×32)            │
│  Boss贴图:  80×80 ~ 200×200 PNG (单帧或精灵表)    │
│  符文图标:  32×32 PNG (已有标准)                   │
└─────────────────────────────────────────────────┘
```

### 参考原版贴图路径
```
<tModLoader>\Content\Images\
  ├── Armor_Head_XX.png    ← 头盔外观参考
  ├── Armor_Body_XX.png    ← 身体外观参考
  ├── Armor_Legs_XX.png    ← 腿部外观参考
  ├── Item_XX.png          ← 物品图标参考
  ├── NPC_XX.png           ← NPC/Boss 贴图参考
  └── Projectile_XX.png    ← 弹幕贴图参考
```

### 推荐像素画教程
- [A Beginner's Guide to Pixel Art](https://www.pixilart.com/blog/terms/pixel-art/) — 基础概念
- [Pixel Art for Terraria Modding](https://steamcommunity.com/sharedfiles/filedetails/?id=2469508156) — 专门针对 Terraria 模组

---

## 附录 B: 贴图外包/替代方案

如果自己画实在困难，可考虑：
1. **开源像素画库**: OpenGameArt.org 搜索 "top-down armor" → 修改配色
2. **调色板限制**: 限定 8 色以内，即使画工一般也容易出效果
3. **几何简化**: 方形盔甲（盖伦式）比曲线盔甲（艾希式）好画 3 倍
4. **AI 辅助线稿 + 手动精修**: 用任何像素生成工具出草图 → 手动修改细节
