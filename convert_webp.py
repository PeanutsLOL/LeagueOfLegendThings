"""将 Content/Icon/Runes/ 下的 .webp 文件转为 .png，自动去除 52px- 前缀和 _rune/_icon 后缀"""
import os, re
from PIL import Image

runes_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Content", "Icon", "Runes")
if not os.path.isdir(runes_dir):
    print(f"目录不存在: {runes_dir}")
    exit(1)

converted = 0
for f in sorted(os.listdir(runes_dir)):
    if not f.lower().endswith(".webp"):
        continue
    webp_path = os.path.join(runes_dir, f)
    # 去掉 52px- 前缀，去掉 _rune / _icon 后缀
    name = re.sub(r'^\d+px-', '', f)
    name = re.sub(r'_(rune|icon)\.webp$', '.png', name)
    png_path = os.path.join(runes_dir, name)
    try:
        img = Image.open(webp_path)
        img.save(png_path, "PNG")
        os.remove(webp_path)
        print(f"  OK  {f} -> {name}")
        converted += 1
    except Exception as e:
        print(f"  FAIL {f}: {e}")

print(f"\n完成: {converted} 个文件")
