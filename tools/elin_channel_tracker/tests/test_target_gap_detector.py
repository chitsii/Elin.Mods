from __future__ import annotations

import json
from pathlib import Path

from elin_channel_tracker.target_gap_detector import detect_target_gaps


def _write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def test_detect_target_gaps_from_game_dependency_and_compat_symbol(tmp_path: Path) -> None:
    src_root = tmp_path / "src"
    _write(
        src_root / "PatchA.cs",
        """
using HarmonyLib;
[GameDependency("Patch", "Zone.Activate", "High", "")]
[HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
public class PatchA {}
""".strip(),
    )
    _write(
        src_root / "CompatSymbol.cs",
        """
internal class CompatSymbolDef {
    private static readonly object X = new {
        id = "Quest.Create"
    };
}
""".strip(),
    )

    targets_path = tmp_path / "config" / "compat_targets.json"
    _write(
        targets_path,
        json.dumps(
            {
                "kind": "compat_targets",
                "schema_version": "1.0.0",
                "targets": [
                    {"target": "Zone.Activate", "check_kind": "patch_target"},
                ],
            }
        ),
    )

    report = detect_target_gaps(
        targets_path=targets_path,
        src_root=src_root,
        report_json_path=tmp_path / "reports" / "target_gap_report.json",
        report_md_path=tmp_path / "reports" / "target_gap_report.md",
        include_heuristics=False,
        ignore_targets=set(),
    )

    assert report["summary"]["configured_targets"] == 1
    assert report["summary"]["detected_targets"] == 2
    assert report["summary"]["missing_targets"] == 1
    missing = report["missing_targets"]
    assert len(missing) == 1
    assert missing[0]["target"] == "Quest.Create"


def test_detect_target_gaps_with_heuristics(tmp_path: Path) -> None:
    src_root = tmp_path / "src"
    _write(
        src_root / "Heuristic.cs",
        """
using HarmonyLib;
public class H {
    [HarmonyPatch(typeof(Card), nameof(Card.HealHP))]
    public static void Patch() {}
    public void X() {
        var m = AccessTools.Method(typeof(Card), nameof(Card.DamageHP));
        var r = typeof(Point).GetMethod("GetNearestPoint");
    }
}
""".strip(),
    )

    targets_path = tmp_path / "config" / "compat_targets.json"
    _write(
        targets_path,
        json.dumps(
            {
                "kind": "compat_targets",
                "schema_version": "1.0.0",
                "targets": [
                    {"target": "Card.HealHP", "check_kind": "patch_target"},
                    {"target": "Card.DamageHP", "check_kind": "reflection"},
                    {"target": "Point.GetNearestPoint", "check_kind": "reflection"},
                ],
            }
        ),
    )

    report = detect_target_gaps(
        targets_path=targets_path,
        src_root=src_root,
        report_json_path=tmp_path / "reports" / "target_gap_report.json",
        report_md_path=tmp_path / "reports" / "target_gap_report.md",
        include_heuristics=True,
        ignore_targets=set(),
    )

    assert report["summary"]["missing_targets"] == 0
    assert report["summary"]["configured_not_detected"] == 0
