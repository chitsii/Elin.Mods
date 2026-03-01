import os
import struct
from PIL import Image


def create_transparent_png(path):
    # 1x1 transparent PNG
    data = b"\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x01\x03\x00\x00\x00%\xdbV\xca\x00\x00\x00\x03PLTE\x00\x00\x00\xa7z=\xda\x00\x00\x00\x01tRNS\x00@\xe6\xd8f\x00\x00\x00\nIDAT\x08\xd7c`\x00\x00\x00\x02\x00\x01\xe2!\xbc3\x00\x00\x00\x00IEND\xaeB`\x82"
    with open(path, "wb") as f:
        f.write(data)
    print(f"Created/Ensured: {path}")


def resize_images(directory, target_size=(48, 48)):
    print(f"Resizing images in {directory} to {target_size}...")
    for filename in os.listdir(directory):
        if filename.endswith(".png") and "narrator" not in filename:
            path = os.path.join(directory, filename)
            try:
                with Image.open(path) as img:
                    if img.size != target_size:
                        print(f"Resizing {filename} from {img.size} to {target_size}")
                        img = img.resize(target_size, Image.Resampling.LANCZOS)
                        img.save(path)
                    else:
                        print(f"Skipping {filename} (already {target_size})")
            except Exception as e:
                print(f"Error processing {filename}: {e}")


TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(TOOLS_DIR)
TEXTURE_DIR = os.path.join(PROJECT_ROOT, "Texture")

# Create narrator image if not exists (or overwrite)
create_transparent_png(os.path.join(TEXTURE_DIR, "sukutsu_narrator.png"))

# Resize others
resize_images(TEXTURE_DIR)
