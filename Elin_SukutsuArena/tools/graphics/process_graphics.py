"""
キャラチップ・ポートレート作成ツール v4

改良点:
1. クロマキー方式: 特定の背景色 (マゼンタ #FF00FF) を透過
2. スプライト縮小: NEAREST (ピクセルアート向け、エッジがくっきり)
3. ポートレート縮小: LANCZOS (滑らか)
"""

from PIL import Image
import os

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

TEXTURE_DIR = os.path.join(PROJECT_ROOT, "Texture")
PORTRAIT_DIR = os.path.join(PROJECT_ROOT, "Portrait")

# 最終出力サイズ
TEXTURE_CANVAS_SIZE = (128, 128)
SPRITE_SIZE = (83, 104)  # キャラクターサイズ (1.3倍)
PORTRAIT_SIZE = (240, 320)

# クロマキー背景色 (マゼンタ)
CHROMA_KEY_COLOR = (255, 0, 255)
COLOR_TOLERANCE = 50  # 色の許容範囲


def chroma_key_transparent(img, key_color=CHROMA_KEY_COLOR, tolerance=COLOR_TOLERANCE):
    """
    クロマキー方式で特定色を透明化
    """
    if img.mode != "RGBA":
        img = img.convert("RGBA")

    pixels = img.load()
    width, height = img.size

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            # キーカラーとの距離を計算
            distance = (
                abs(r - key_color[0]) + abs(g - key_color[1]) + abs(b - key_color[2])
            )
            if distance <= tolerance:
                pixels[x, y] = (r, g, b, 0)  # 透明化

    return img


def process_sprite(src_path, dst_path):
    """
    スプライト画像を処理
    - クロマキーで背景除去
    - NEAREST で縮小（ピクセルアート向け）
    """
    try:
        print(f"  Processing: {os.path.basename(src_path)}...")
        img = Image.open(src_path)

        # 1. クロマキー背景除去
        img = chroma_key_transparent(img)

        # 2. アスペクト比を維持してリサイズ (NEAREST = くっきり)
        original_ratio = img.width / img.height
        target_ratio = SPRITE_SIZE[0] / SPRITE_SIZE[1]

        if original_ratio > target_ratio:
            new_width = SPRITE_SIZE[0]
            new_height = int(SPRITE_SIZE[0] / original_ratio)
        else:
            new_height = SPRITE_SIZE[1]
            new_width = int(SPRITE_SIZE[1] * original_ratio)

        resized = img.resize((new_width, new_height), Image.Resampling.NEAREST)

        # 3. 透明キャンバスに配置 (中央下)
        canvas = Image.new("RGBA", TEXTURE_CANVAS_SIZE, (0, 0, 0, 0))
        x = (TEXTURE_CANVAS_SIZE[0] - new_width) // 2
        y = TEXTURE_CANVAS_SIZE[1] - new_height - 4
        canvas.paste(resized, (x, y), resized)

        canvas.save(dst_path, "PNG")
        print(f"    -> Saved (char size: {new_width}x{new_height}, algo: NEAREST)")

    except Exception as e:
        print(f"Error processing sprite {src_path}: {e}")


def process_portrait(src_path, dst_path):
    """
    ポートレート画像を処理
    - クロマキーで背景除去
    - LANCZOS で縮小（滑らか）
    """
    try:
        print(f"  Processing: {os.path.basename(src_path)}...")
        img = Image.open(src_path)

        # 1. クロマキー背景除去
        img = chroma_key_transparent(img)

        # 2. アスペクト比を維持してリサイズ (LANCZOS = 滑らか)
        original_ratio = img.width / img.height
        target_ratio = PORTRAIT_SIZE[0] / PORTRAIT_SIZE[1]

        if original_ratio > target_ratio:
            new_width = PORTRAIT_SIZE[0]
            new_height = int(PORTRAIT_SIZE[0] / original_ratio)
        else:
            new_height = PORTRAIT_SIZE[1]
            new_width = int(PORTRAIT_SIZE[1] * original_ratio)

        resized = img.resize((new_width, new_height), Image.Resampling.LANCZOS)

        # 3. 透明キャンバスに中央配置
        canvas = Image.new("RGBA", PORTRAIT_SIZE, (0, 0, 0, 0))
        x = (PORTRAIT_SIZE[0] - new_width) // 2
        y = (PORTRAIT_SIZE[1] - new_height) // 2
        canvas.paste(resized, (x, y), resized)

        canvas.save(dst_path, "PNG")
        print(f"    -> Saved (size: {new_width}x{new_height}, algo: LANCZOS)")

    except Exception as e:
        print(f"Error processing portrait {src_path}: {e}")


def main():
    print("=== キャラチップ・ポートレート処理ツール v4 ===")
    print(f"クロマキー色: RGB{CHROMA_KEY_COLOR}, 許容範囲: {COLOR_TOLERANCE}")

    if os.path.exists(TEXTURE_DIR):
        print(f"\n[Texture] Processing sprites (NEAREST scaling)")
        for f in os.listdir(TEXTURE_DIR):
            if f.endswith(".png"):
                src = os.path.join(TEXTURE_DIR, f)
                process_sprite(src, src)

    if os.path.exists(PORTRAIT_DIR):
        print(f"\n[Portrait] Processing portraits (LANCZOS scaling)")
        for f in os.listdir(PORTRAIT_DIR):
            if f.endswith(".png"):
                src = os.path.join(PORTRAIT_DIR, f)
                process_portrait(src, src)

    print("\nDone!")


if __name__ == "__main__":
    main()
