"""
画像リサイズスクリプト
Texture: 128x128
Portrait: 240x320
"""

from PIL import Image
import os

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)

TEXTURE_DIR = os.path.join(PROJECT_ROOT, "Texture")
PORTRAIT_DIR = os.path.join(PROJECT_ROOT, "Portrait")

TEXTURE_SIZE = (128, 128)
PORTRAIT_SIZE = (240, 320)


def resize_image(src_path, target_size):
    """画像をリサイズして上書き保存"""
    try:
        img = Image.open(src_path)
        # RGBAに変換（透過対応）
        if img.mode != "RGBA":
            img = img.convert("RGBA")
        # リサイズ（高品質）
        resized = img.resize(target_size, Image.Resampling.LANCZOS)
        resized.save(src_path, "PNG")
        print(f"Resized: {src_path} -> {target_size}")
    except Exception as e:
        print(f"Error resizing {src_path}: {e}")


# Texture フォルダ内の画像をリサイズ
if os.path.exists(TEXTURE_DIR):
    for f in os.listdir(TEXTURE_DIR):
        if f.endswith(".png"):
            resize_image(os.path.join(TEXTURE_DIR, f), TEXTURE_SIZE)

# Portrait フォルダ内の画像をリサイズ
if os.path.exists(PORTRAIT_DIR):
    for f in os.listdir(PORTRAIT_DIR):
        if f.endswith(".png"):
            resize_image(os.path.join(PORTRAIT_DIR, f), PORTRAIT_SIZE)

print("Done!")
