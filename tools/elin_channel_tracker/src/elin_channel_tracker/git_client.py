from __future__ import annotations

import subprocess
from typing import Optional


class GitClient:
    def __init__(self, repo_root: Optional[str] = None) -> None:
        self._repo_root = repo_root

    def fetch(self, remote: str = "origin") -> str:
        return self._run(["fetch", remote])

    def rev_parse(self, ref: str) -> str:
        return self._run(["rev-parse", ref]).strip()

    def _run(self, args: list[str]) -> str:
        completed = subprocess.run(
            ["git", *args],
            capture_output=True,
            text=True,
            cwd=self._repo_root,
            check=False,
        )
        if completed.returncode != 0:
            stderr = (completed.stderr or completed.stdout or "").strip()
            raise RuntimeError(stderr or f"git {' '.join(args)} failed")
        return completed.stdout
