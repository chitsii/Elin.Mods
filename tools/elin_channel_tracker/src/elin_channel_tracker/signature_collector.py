from __future__ import annotations

import os
import subprocess
from pathlib import Path


def find_default_managed_dir() -> Path | None:
    candidates = [
        Path(r"C:\Program Files (x86)\Steam\steamapps\common\Elin\Elin_Data\Managed"),
        Path(r"C:\Program Files\Steam\steamapps\common\Elin\Elin_Data\Managed"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate
    return None


def resolve_assembly_path(
    assembly: str | None,
    managed_dir: str | None,
    game_dir: str | None,
) -> Path:
    if assembly:
        return Path(assembly)

    if managed_dir:
        return Path(managed_dir) / "Assembly-CSharp.dll"

    if game_dir:
        return Path(game_dir) / "Elin_Data" / "Managed" / "Assembly-CSharp.dll"

    env_managed = os.environ.get("ELIN_MANAGED_DIR")
    if env_managed:
        return Path(env_managed) / "Assembly-CSharp.dll"

    env_game = os.environ.get("ELIN_GAME_DIR")
    if env_game:
        return Path(env_game) / "Elin_Data" / "Managed" / "Assembly-CSharp.dll"

    default_managed = find_default_managed_dir()
    if default_managed:
        return default_managed / "Assembly-CSharp.dll"

    raise ValueError(
        "Assembly path is not resolved. Specify --assembly, --managed-dir, "
        "--game-dir, ELIN_MANAGED_DIR, or ELIN_GAME_DIR."
    )


def pick_best_game_assembly(assembly_path: Path) -> Path:
    if not assembly_path.exists():
        return assembly_path

    managed_dir = assembly_path.parent
    elin_dll = managed_dir / "Elin.dll"

    try:
        size = assembly_path.stat().st_size
    except OSError:
        size = 0

    if (
        assembly_path.name.lower() == "assembly-csharp.dll"
        and size < 200_000
        and elin_dll.exists()
    ):
        return elin_dll

    return assembly_path


def run_signature_collector(
    *,
    tool_root: Path,
    assembly_path: Path,
    targets_path: Path,
    output_path: Path,
    extra_dir: str | None,
    quiet: bool,
) -> None:
    collector_project = (
        tool_root
        / "inspector"
        / "SignatureCatalogCollector"
        / "SignatureCatalogCollector.csproj"
    )
    if not collector_project.exists():
        raise ValueError(f"Signature collector project is missing: {collector_project}")

    output_path.parent.mkdir(parents=True, exist_ok=True)

    cmd = [
        "dotnet",
        "run",
        "--project",
        str(collector_project),
        "--",
        "--assembly",
        str(assembly_path),
        "--targets",
        str(targets_path),
        "--output",
        str(output_path),
    ]
    if extra_dir:
        cmd.extend(["--extra-dir", str(extra_dir)])
    if quiet:
        cmd.append("--quiet")

    completed = subprocess.run(
        cmd,
        cwd=str(tool_root),
        capture_output=True,
        text=True,
        check=False,
    )
    if completed.returncode != 0:
        message = (completed.stderr or completed.stdout or "").strip()
        raise RuntimeError(message or "Signature collector failed.")
