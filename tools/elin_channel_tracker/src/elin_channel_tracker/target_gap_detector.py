from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass
import json
import re
from pathlib import Path
from typing import Any

from . import SCHEMA_VERSION


GAME_DEP_KIND_MAP = {
    "patch": "patch_target",
    "reflection": "reflection",
    "dynamic": "dynamic",
}

RE_GAME_DEP = re.compile(
    r'\[GameDependency\s*\(\s*"(?P<dep_type>\w+)"\s*,\s*"(?P<target>[^"]+)"',
    re.MULTILINE,
)

RE_COMPAT_SYMBOL_ID = re.compile(
    r'id:\s*"(?P<target>[A-Za-z_][A-Za-z0-9_.]*\.[A-Za-z_][A-Za-z0-9_]*)"',
    re.MULTILINE,
)

RE_HARMONY_EXPLICIT = re.compile(
    r'\[HarmonyPatch\s*\(\s*typeof\s*\(\s*(?P<type>[A-Za-z_][A-Za-z0-9_.]*)\s*\)\s*,\s*'
    r'(?:nameof\s*\(\s*[A-Za-z_][A-Za-z0-9_.]*\.(?P<method1>[A-Za-z_][A-Za-z0-9_]*)\s*\)|"(?P<method2>[A-Za-z_][A-Za-z0-9_]*)")',
    re.MULTILINE,
)

RE_ACCESSTOOLS_METHOD = re.compile(
    r'AccessTools\.Method\s*\(\s*typeof\s*\(\s*(?P<type>[A-Za-z_][A-Za-z0-9_.]*)\s*\)\s*,\s*'
    r'(?:nameof\s*\(\s*[A-Za-z_][A-Za-z0-9_.]*\.(?P<method1>[A-Za-z_][A-Za-z0-9_]*)\s*\)|"(?P<method2>[A-Za-z_][A-Za-z0-9_]*)")',
    re.MULTILINE,
)

RE_GET_METHOD = re.compile(
    r'typeof\s*\(\s*(?P<type>[A-Za-z_][A-Za-z0-9_.]*)\s*\)\s*\.GetMethod\s*\(\s*"(?P<method>[A-Za-z_][A-Za-z0-9_]*)"',
    re.MULTILINE,
)

RE_VALID_TYPE = re.compile(r"^[A-Za-z_][A-Za-z0-9_.]*$")
RE_VALID_MEMBER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")


@dataclass(frozen=True)
class Detection:
    target: str
    check_kind: str | None
    source: str
    file: str
    line: int


def detect_target_gaps(
    *,
    targets_path: Path,
    src_root: Path,
    report_json_path: Path,
    report_md_path: Path,
    include_heuristics: bool,
    ignore_targets: set[str] | None = None,
) -> dict[str, Any]:
    if ignore_targets is None:
        ignore_targets = set()

    targets_doc = json.loads(targets_path.read_text(encoding="utf-8"))
    targets = targets_doc.get("targets", [])
    if not isinstance(targets, list):
        raise ValueError("targets must be a list")

    config_ignore = targets_doc.get("auto_detect_ignore_targets", [])
    if isinstance(config_ignore, list):
        ignore_targets = set(ignore_targets).union(
            str(x) for x in config_ignore if isinstance(x, str)
        )

    configured_by_target: dict[str, str] = {}
    for target in targets:
        if not isinstance(target, dict):
            continue
        target_name = str(target.get("target", ""))
        if not target_name:
            continue
        check_kind = str(target.get("check_kind", "reflection"))
        configured_by_target[target_name] = check_kind

    detections = _scan_sources(src_root=src_root, include_heuristics=include_heuristics)

    detected_map: dict[str, dict[str, Any]] = defaultdict(
        lambda: {"check_kinds": set(), "sources": set(), "evidence": []}
    )
    for detection in detections:
        if detection.target in ignore_targets:
            continue
        bucket = detected_map[detection.target]
        if detection.check_kind:
            bucket["check_kinds"].add(detection.check_kind)
        bucket["sources"].add(detection.source)
        bucket["evidence"].append(
            {
                "source": detection.source,
                "file": detection.file,
                "line": detection.line,
            }
        )

    detected_targets = set(detected_map.keys())
    configured_targets = set(
        target for target in configured_by_target.keys() if target not in ignore_targets
    )

    missing_candidates = detected_targets - configured_targets
    missing_targets = sorted(
        target
        for target in missing_candidates
        if not any(cfg.startswith(f"{target}.") for cfg in configured_targets)
    )
    configured_not_detected = sorted(configured_targets - detected_targets)

    check_kind_mismatches: list[dict[str, Any]] = []
    for target in sorted(detected_targets.intersection(configured_targets)):
        configured_kind = configured_by_target[target]
        detected_kinds = sorted(detected_map[target]["check_kinds"])
        if not detected_kinds:
            continue
        if configured_kind not in detected_kinds:
            check_kind_mismatches.append(
                {
                    "target": target,
                    "configured_check_kind": configured_kind,
                    "detected_check_kinds": detected_kinds,
                }
            )

    def _build_entries(target_names: list[str]) -> list[dict[str, Any]]:
        entries: list[dict[str, Any]] = []
        for target in target_names:
            bucket = detected_map.get(target, {})
            evidence = bucket.get("evidence", [])
            entries.append(
                {
                    "target": target,
                    "configured_check_kind": configured_by_target.get(target),
                    "detected_check_kinds": sorted(bucket.get("check_kinds", [])),
                    "detected_by": sorted(bucket.get("sources", [])),
                    "evidence": sorted(
                        evidence,
                        key=lambda x: (str(x.get("file", "")), int(x.get("line", 0))),
                    ),
                }
            )
        return entries

    report = {
        "kind": "target_gap_report",
        "schema_version": SCHEMA_VERSION,
        "src_root": str(src_root),
        "summary": {
            "configured_targets": len(configured_targets),
            "detected_targets": len(detected_targets),
            "missing_targets": len(missing_targets),
            "configured_not_detected": len(configured_not_detected),
            "check_kind_mismatches": len(check_kind_mismatches),
            "ignored_targets": len(ignore_targets),
        },
        "missing_targets": _build_entries(missing_targets),
        "configured_not_detected": [
            {
                "target": target,
                "configured_check_kind": configured_by_target.get(target),
            }
            for target in configured_not_detected
        ],
        "check_kind_mismatches": check_kind_mismatches,
    }

    report_json_path.parent.mkdir(parents=True, exist_ok=True)
    report_json_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    report_md_path.parent.mkdir(parents=True, exist_ok=True)
    report_md_path.write_text(_render_markdown(report), encoding="utf-8")

    return report


def _scan_sources(*, src_root: Path, include_heuristics: bool) -> list[Detection]:
    detections: list[Detection] = []
    if not src_root.exists():
        return detections

    for file_path in src_root.rglob("*.cs"):
        try:
            content = file_path.read_text(encoding="utf-8-sig")
        except Exception:
            continue

        detections.extend(_scan_game_dependency(content, file_path, src_root))
        detections.extend(_scan_compat_symbol(content, file_path, src_root))

        if include_heuristics:
            detections.extend(_scan_harmony_explicit(content, file_path, src_root))
            detections.extend(_scan_access_tools(content, file_path, src_root))
            detections.extend(_scan_get_method(content, file_path, src_root))

    return detections


def _scan_game_dependency(content: str, file_path: Path, src_root: Path) -> list[Detection]:
    rows: list[Detection] = []
    for match in RE_GAME_DEP.finditer(content):
        dep_type = match.group("dep_type").strip().lower()
        check_kind = GAME_DEP_KIND_MAP.get(dep_type)
        if check_kind is None:
            continue
        target = _normalize_target(match.group("target"))
        if not target:
            continue
        rows.append(
            Detection(
                target=target,
                check_kind=check_kind,
                source="game_dependency",
                file=str(file_path.relative_to(src_root)),
                line=_to_line_number(content, match.start()),
            )
        )
    return rows


def _scan_compat_symbol(content: str, file_path: Path, src_root: Path) -> list[Detection]:
    rows: list[Detection] = []
    for match in RE_COMPAT_SYMBOL_ID.finditer(content):
        target = _normalize_target(match.group("target"))
        if not target:
            continue
        rows.append(
            Detection(
                target=target,
                check_kind=None,
                source="compat_symbol",
                file=str(file_path.relative_to(src_root)),
                line=_to_line_number(content, match.start()),
            )
        )
    return rows


def _scan_harmony_explicit(content: str, file_path: Path, src_root: Path) -> list[Detection]:
    rows: list[Detection] = []
    for match in RE_HARMONY_EXPLICIT.finditer(content):
        method_name = match.group("method1") or match.group("method2") or ""
        target = _compose_target(match.group("type"), method_name)
        if not target:
            continue
        rows.append(
            Detection(
                target=target,
                check_kind="patch_target",
                source="harmony_patch",
                file=str(file_path.relative_to(src_root)),
                line=_to_line_number(content, match.start()),
            )
        )
    return rows


def _scan_access_tools(content: str, file_path: Path, src_root: Path) -> list[Detection]:
    rows: list[Detection] = []
    for match in RE_ACCESSTOOLS_METHOD.finditer(content):
        method_name = match.group("method1") or match.group("method2") or ""
        target = _compose_target(match.group("type"), method_name)
        if not target:
            continue
        rows.append(
            Detection(
                target=target,
                check_kind="reflection",
                source="access_tools_method",
                file=str(file_path.relative_to(src_root)),
                line=_to_line_number(content, match.start()),
            )
        )
    return rows


def _scan_get_method(content: str, file_path: Path, src_root: Path) -> list[Detection]:
    rows: list[Detection] = []
    for match in RE_GET_METHOD.finditer(content):
        target = _compose_target(match.group("type"), match.group("method"))
        if not target:
            continue
        rows.append(
            Detection(
                target=target,
                check_kind="reflection",
                source="typeof_getmethod",
                file=str(file_path.relative_to(src_root)),
                line=_to_line_number(content, match.start()),
            )
        )
    return rows


def _compose_target(type_name: str, member_name: str) -> str | None:
    type_name = type_name.strip()
    member_name = member_name.strip()
    if not RE_VALID_TYPE.fullmatch(type_name):
        return None
    if not RE_VALID_MEMBER.fullmatch(member_name):
        return None
    return f"{type_name}.{member_name}"


def _normalize_target(target: str) -> str | None:
    text = target.strip()
    if "." not in text:
        return None
    if any(x in text for x in (" ", "(", ")", ",", ":")):
        return None
    type_name, member_name = text.rsplit(".", 1)
    return _compose_target(type_name, member_name)


def _to_line_number(content: str, offset: int) -> int:
    return content.count("\n", 0, offset) + 1


def _render_markdown(report: dict[str, Any]) -> str:
    summary = report["summary"]
    lines = [
        "# Target Gap Report",
        "",
        f"- configured_targets: {summary['configured_targets']}",
        f"- detected_targets: {summary['detected_targets']}",
        f"- missing_targets: {summary['missing_targets']}",
        f"- configured_not_detected: {summary['configured_not_detected']}",
        f"- check_kind_mismatches: {summary['check_kind_mismatches']}",
        f"- ignored_targets: {summary['ignored_targets']}",
        "",
        "## Missing Targets (Detected but Not Configured)",
        "",
    ]

    missing_targets = report.get("missing_targets", [])
    if missing_targets:
        for row in missing_targets:
            kinds = ", ".join(row.get("detected_check_kinds", [])) or "unknown"
            lines.append(f"- {row['target']} (detected_kinds: {kinds})")
    else:
        lines.append("- none")

    lines.extend(["", "## Configured but Not Detected", ""])
    configured_not_detected = report.get("configured_not_detected", [])
    if configured_not_detected:
        for row in configured_not_detected:
            lines.append(
                f"- {row['target']} (configured_check_kind: {row['configured_check_kind']})"
            )
    else:
        lines.append("- none")

    lines.extend(["", "## Check Kind Mismatches", ""])
    mismatches = report.get("check_kind_mismatches", [])
    if mismatches:
        for row in mismatches:
            lines.append(
                f"- {row['target']}: configured={row['configured_check_kind']}, detected={','.join(row['detected_check_kinds'])}"
            )
    else:
        lines.append("- none")

    lines.append("")
    return "\n".join(lines)
