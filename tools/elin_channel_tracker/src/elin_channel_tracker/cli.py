from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from . import (
    KIND_CHANNEL_SNAPSHOT,
    KIND_COMPAT_REPORT,
    KIND_COMPAT_TARGETS,
    KIND_SIGNATURE_CATALOG,
    SCHEMA_VERSION,
)
from .evaluator import CompatEvaluator
from .git_client import GitClient
from .schema_validator import SchemaValidator
from .signature_collector import (
    pick_best_game_assembly,
    resolve_assembly_path,
    run_signature_collector,
)
from .target_gap_detector import detect_target_gaps


TOOL_ROOT = Path(__file__).resolve().parents[2]
REPORTS_DIR = TOOL_ROOT / "reports"
CONFIG_DIR = TOOL_ROOT / "config"

LEGACY_COMMAND_ALIASES = {
    "track_channels": "track-channels",
    "verify_compat": "verify-compat",
    "track": "track-channels",
    "verify": "verify-compat",
}


def main(argv: list[str] | None = None) -> int:
    args_list = list(sys.argv[1:] if argv is None else argv)
    args_list = _normalize_legacy_command(args_list)

    parser = _build_parser()
    args = parser.parse_args(args_list)

    try:
        if args.command == "track-channels":
            return _run_track_channels(args)
        if args.command == "collect-signatures":
            return _run_collect_signatures(args)
        if args.command == "detect-target-gaps":
            return _run_detect_target_gaps(args)
        if args.command == "verify-compat":
            return _run_verify_compat(args)
    except Exception as ex:
        print(f"[error] {ex}", file=sys.stderr)
        return 2

    parser.print_help()
    return 2


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="elin-channel-tracker")
    subparsers = parser.add_subparsers(dest="command")

    track = subparsers.add_parser("track-channels", help="Track stable/nightly channel heads.")
    track.add_argument("--stable-ref", default="origin/stable")
    track.add_argument("--nightly-ref", default="origin/nightly")
    track.add_argument("--head-ref", default="HEAD")
    track.add_argument("--stable-sha")
    track.add_argument("--nightly-sha")
    track.add_argument("--head-sha")
    track.add_argument("--skip-fetch", action="store_true")
    track.add_argument(
        "--report-json",
        default=str(REPORTS_DIR / "channel_snapshot.json"),
    )
    track.add_argument(
        "--report-md",
        default=str(REPORTS_DIR / "channel_snapshot.md"),
    )
    track.add_argument("--no-legacy-shim", action="store_true")

    collect = subparsers.add_parser(
        "collect-signatures",
        help="Collect API signatures for verify-compat from a game assembly.",
    )
    collect.add_argument(
        "--targets",
        default=str(CONFIG_DIR / "compat_targets.json"),
    )
    collect.add_argument(
        "--output",
        help="Output signature_catalog JSON path.",
    )
    collect.add_argument("--assembly", help="Path to Assembly-CSharp.dll or Elin.dll.")
    collect.add_argument("--managed-dir", help="Path to Elin_Data/Managed.")
    collect.add_argument("--game-dir", help="Path to game root directory.")
    collect.add_argument("--extra-dir", help="Additional directory to load assemblies from.")
    collect.add_argument("--quiet", action="store_true")

    detect = subparsers.add_parser(
        "detect-target-gaps",
        help="Detect missing compat targets from source declarations.",
    )
    detect.add_argument(
        "--targets",
        default=str(CONFIG_DIR / "compat_targets.json"),
    )
    detect.add_argument(
        "--src-root",
        default="src",
        help="Source root to scan (default: src).",
    )
    detect.add_argument(
        "--report-json",
        default=str(REPORTS_DIR / "target_gap_report.json"),
    )
    detect.add_argument(
        "--report-md",
        default=str(REPORTS_DIR / "target_gap_report.md"),
    )
    detect.add_argument(
        "--include-heuristics",
        action="store_true",
        help="Also scan Harmony/AccessTools/GetMethod patterns heuristically.",
    )
    detect.add_argument(
        "--ignore-target",
        action="append",
        default=[],
        help="Target to ignore (can be specified multiple times).",
    )
    detect.add_argument(
        "--fail-on-missing",
        action="store_true",
        help="Return exit code 1 when missing targets are detected.",
    )
    detect.add_argument(
        "--fail-on-undetected",
        action="store_true",
        help="Return exit code 1 when configured targets are not detected.",
    )

    verify = subparsers.add_parser("verify-compat", help="Evaluate compatibility across channels.")
    verify.add_argument(
        "--targets",
        default=str(CONFIG_DIR / "compat_targets.json"),
    )
    verify.add_argument(
        "--stable-signatures",
        default=str(CONFIG_DIR / "stable_signatures.json"),
    )
    verify.add_argument(
        "--nightly-signatures",
        default=str(CONFIG_DIR / "nightly_signatures.json"),
    )
    verify.add_argument("--scan-scope", default="extended-only")
    verify.add_argument(
        "--report-json",
        default=str(REPORTS_DIR / "compat_report.json"),
    )
    verify.add_argument(
        "--report-md",
        default=str(REPORTS_DIR / "compat_report.md"),
    )
    verify.add_argument("--no-legacy-shim", action="store_true")

    return parser


def _normalize_legacy_command(args: list[str]) -> list[str]:
    if not args:
        return args

    command = args[0]
    mapped = LEGACY_COMMAND_ALIASES.get(command)
    if mapped:
        print(
            f"[shim] Legacy command '{command}' is deprecated. Use '{mapped}'.",
            file=sys.stderr,
        )
        args = [mapped, *args[1:]]
    return args


def _run_track_channels(args: argparse.Namespace) -> int:
    git = GitClient()
    pull_result = "skipped"
    fallback_notes: list[str] = []

    if not args.skip_fetch and not (args.stable_sha and args.nightly_sha and args.head_sha):
        try:
            git.fetch("origin")
            pull_result = "ok"
        except Exception as ex:
            pull_result = f"failed: {ex}"

    try:
        head = args.head_sha or git.rev_parse(args.head_ref)
    except Exception as ex:
        print(f"[error] Unable to resolve channel commits: {ex}", file=sys.stderr)
        return 2

    stable = args.stable_sha
    if not stable:
        try:
            stable = git.rev_parse(args.stable_ref)
        except Exception:
            stable = head
            fallback_notes.append(f"stable_ref_missing:{args.stable_ref}->HEAD")

    nightly = args.nightly_sha
    if not nightly:
        try:
            nightly = git.rev_parse(args.nightly_ref)
        except Exception:
            nightly = head
            fallback_notes.append(f"nightly_ref_missing:{args.nightly_ref}->HEAD")

    if fallback_notes:
        pull_result = f"{pull_result}; {'; '.join(fallback_notes)}"

    snapshot = {
        "kind": KIND_CHANNEL_SNAPSHOT,
        "schema_version": SCHEMA_VERSION,
        "generated_at_utc": _utc_now(),
        "head": head,
        "stable": stable,
        "nightly": nightly,
        "pull_result": pull_result,
    }
    SchemaValidator.ensure_kind_and_schema(snapshot, KIND_CHANNEL_SNAPSHOT)

    report_json_path = Path(args.report_json)
    report_md_path = Path(args.report_md)
    _write_json(report_json_path, snapshot)
    _write_text(report_md_path, _render_channel_snapshot_markdown(snapshot))

    if not args.no_legacy_shim:
        _write_json(REPORTS_DIR / "channel_snapshot.legacy.json", snapshot)
        _write_text(
            REPORTS_DIR / "channel_snapshot.legacy.md",
            _render_channel_snapshot_markdown(snapshot),
        )

    return 0


def _run_verify_compat(args: argparse.Namespace) -> int:
    targets_doc = _read_json(Path(args.targets))
    stable_doc = _read_json(Path(args.stable_signatures))
    nightly_doc = _read_json(Path(args.nightly_signatures))

    SchemaValidator.ensure_kind_and_schema(targets_doc, KIND_COMPAT_TARGETS)
    SchemaValidator.ensure_kind_and_schema(stable_doc, KIND_SIGNATURE_CATALOG)
    SchemaValidator.ensure_kind_and_schema(nightly_doc, KIND_SIGNATURE_CATALOG)

    targets = targets_doc.get("targets", [])
    stable_symbols = stable_doc.get("symbols", {})
    nightly_symbols = nightly_doc.get("symbols", {})
    if not isinstance(targets, list):
        raise ValueError("targets must be a list")
    if not isinstance(stable_symbols, dict):
        raise ValueError("stable signatures symbols must be an object")
    if not isinstance(nightly_symbols, dict):
        raise ValueError("nightly signatures symbols must be an object")
    if not stable_symbols or not nightly_symbols:
        raise ValueError(
            "signature catalogs are empty. Run collect-signatures for both stable and nightly first."
        )

    report = CompatEvaluator().evaluate(
        targets=targets,
        stable_symbols=stable_symbols,
        nightly_symbols=nightly_symbols,
        scan_scope=str(args.scan_scope),
    )
    SchemaValidator.ensure_kind_and_schema(report, KIND_COMPAT_REPORT)

    report_json_path = Path(args.report_json)
    report_md_path = Path(args.report_md)
    _write_json(report_json_path, report)
    _write_text(report_md_path, _render_compat_markdown(report))

    if not args.no_legacy_shim:
        _write_json(REPORTS_DIR / "compat_report.legacy.json", report)
        _write_text(REPORTS_DIR / "compat_report.legacy.md", _render_compat_markdown(report))

    return 1 if int(report["summary"]["broken"]) > 0 else 0


def _run_collect_signatures(args: argparse.Namespace) -> int:
    targets_path = Path(args.targets).resolve()
    if not targets_path.exists():
        raise ValueError(f"Targets file not found: {targets_path}")

    if not args.output:
        raise ValueError("--output is required for collect-signatures.")
    output_path = Path(args.output).resolve()

    assembly_path = resolve_assembly_path(
        assembly=args.assembly,
        managed_dir=args.managed_dir,
        game_dir=args.game_dir,
    )
    assembly_path = pick_best_game_assembly(assembly_path)
    if not assembly_path.exists():
        raise ValueError(f"Assembly not found: {assembly_path}")

    run_signature_collector(
        tool_root=TOOL_ROOT,
        assembly_path=assembly_path,
        targets_path=targets_path,
        output_path=output_path,
        extra_dir=args.extra_dir,
        quiet=bool(args.quiet),
    )

    catalog = _read_json(output_path)
    SchemaValidator.ensure_kind_and_schema(catalog, KIND_SIGNATURE_CATALOG)
    symbols = catalog.get("symbols")
    if not isinstance(symbols, dict):
        raise ValueError("Collected signature catalog has invalid symbols object.")
    if not symbols:
        raise ValueError(
            f"No symbols were collected from assembly: {assembly_path}. "
            "Check target names and assembly path."
        )

    print(f"[collect-signatures] symbols={len(symbols)} output={output_path}")
    return 0


def _run_detect_target_gaps(args: argparse.Namespace) -> int:
    report = detect_target_gaps(
        targets_path=Path(args.targets).resolve(),
        src_root=Path(args.src_root).resolve(),
        report_json_path=Path(args.report_json).resolve(),
        report_md_path=Path(args.report_md).resolve(),
        include_heuristics=bool(args.include_heuristics),
        ignore_targets={str(x) for x in args.ignore_target if str(x)},
    )

    summary = report["summary"]
    missing = int(summary["missing_targets"])
    undetected = int(summary["configured_not_detected"])
    print(
        "[detect-target-gaps] "
        f"configured={summary['configured_targets']} "
        f"detected={summary['detected_targets']} "
        f"missing={missing} "
        f"configured_not_detected={undetected}"
    )

    if args.fail_on_missing and missing > 0:
        return 1
    if args.fail_on_undetected and undetected > 0:
        return 1
    return 0


def _read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def _write_json(path: Path, document: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def _write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def _render_channel_snapshot_markdown(snapshot: dict[str, Any]) -> str:
    lines = [
        "# Channel Snapshot",
        "",
        f"- generated_at_utc: {snapshot['generated_at_utc']}",
        f"- head: {snapshot['head']}",
        f"- stable: {snapshot['stable']}",
        f"- nightly: {snapshot['nightly']}",
        f"- pull_result: {snapshot['pull_result']}",
        "",
    ]
    return "\n".join(lines)


def _render_compat_markdown(report: dict[str, Any]) -> str:
    summary = report["summary"]
    lines = [
        "# Compatibility Report",
        "",
        f"- broken: {summary['broken']}",
        f"- risky: {summary['risky']}",
        f"- ok: {summary['ok']}",
        f"- fail_gate: broken",
        "",
        "## Broken",
        "",
    ]

    broken = [c for c in report["checks"] if c["status"] == "broken"]
    if broken:
        for check in broken:
            lines.append(
                f"- {check['target']} ({check['check_kind']}): {check['reason_code']}"
            )
    else:
        lines.append("- none")

    lines.extend(["", "## Risky (actionable)", ""])
    risky_actionable = [
        c
        for c in report["checks"]
        if c["status"] == "risky" and c.get("reason_code") != "unresolved_dynamic"
    ]
    if risky_actionable:
        for check in risky_actionable:
            lines.append(
                f"- {check['target']} ({check['check_kind']}): {check['reason_code']}"
            )
    else:
        lines.append("- none")

    lines.extend(["", "## Risky (observation)", ""])
    risky_observation = [
        c
        for c in report["checks"]
        if c["status"] == "risky" and c.get("reason_code") == "unresolved_dynamic"
    ]
    if risky_observation:
        for check in risky_observation:
            lines.append(
                f"- {check['target']} ({check['check_kind']}): {check['reason_code']}"
            )
    else:
        lines.append("- none")

    lines.append("")
    return "\n".join(lines)


def _utc_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
