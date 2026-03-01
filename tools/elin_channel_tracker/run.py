#!/usr/bin/env python3
from pathlib import Path
import sys


THIS_DIR = Path(__file__).resolve().parent
SRC_DIR = THIS_DIR / "src"
if str(SRC_DIR) not in sys.path:
    sys.path.insert(0, str(SRC_DIR))

from elin_channel_tracker.cli import main  # noqa: E402


if __name__ == "__main__":
    raise SystemExit(main())
