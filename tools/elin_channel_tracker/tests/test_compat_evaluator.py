import json
from pathlib import Path

from elin_channel_tracker.cli import main
from elin_channel_tracker.evaluator import CompatEvaluator


def test_detects_renamed_candidate_as_risky():
    targets = [
        {
            "target": "Chara.ReleaseMinion",
            "check_kind": "reflection",
            "type_name": "Chara",
            "canonical_name": "ReleaseMinion",
            "candidate_names": ["ReleaseMinion", "UnmakeMinion"],
            "candidate_signatures": ["void()"],
        }
    ]
    stable = {"Chara.ReleaseMinion": ["void()"]}
    nightly = {"Chara.UnmakeMinion": ["void()"]}

    report = CompatEvaluator().evaluate(targets, stable, nightly)
    check = report["checks"][0]

    assert check["status"] == "risky"
    assert check["reason_code"] == "renamed_candidate"
    assert report["summary"]["broken"] == 0
    assert report["summary"]["risky"] == 1


def test_detects_signature_diff_as_broken():
    targets = [
        {
            "target": "Chara.SetMainElement",
            "check_kind": "reflection",
            "type_name": "Chara",
            "canonical_name": "SetMainElement",
            "candidate_names": ["SetMainElement"],
            "candidate_signatures": ["void(System.Int32,System.Int32)"],
        }
    ]
    stable = {"Chara.SetMainElement": ["void(System.Int32,System.Int32)"]}
    nightly = {"Chara.SetMainElement": ["void(System.Int32,System.Boolean)"]}

    report = CompatEvaluator().evaluate(targets, stable, nightly)
    check = report["checks"][0]

    assert check["status"] == "broken"
    assert check["reason_code"] == "signature_mismatch"
    assert report["summary"]["broken"] == 1


def test_accepts_optional_parameter_quest_create_signature():
    targets = [
        {
            "target": "Quest.Create",
            "check_kind": "reflection",
            "type_name": "Quest",
            "canonical_name": "Create",
            "candidate_names": ["Create", "CreateQuest"],
            "candidate_signatures": [
                "Quest(System.String,System.String,Chara,System.Boolean)",
                "Quest(System.String)",
            ],
        }
    ]
    stable = {
        "Quest.Create": ["Quest(System.String,System.String,Chara,System.Boolean)"]
    }
    nightly = {
        "Quest.Create": ["Quest(System.String,System.String,Chara,System.Boolean)"]
    }

    report = CompatEvaluator().evaluate(targets, stable, nightly)
    check = report["checks"][0]

    assert check["status"] == "ok"
    assert check["reason_code"] == "matched"
    assert report["summary"]["broken"] == 0


def test_verify_compat_exit_code_fails_on_broken_only(tmp_path: Path):
    targets_path = tmp_path / "targets.json"
    stable_path = tmp_path / "stable.json"
    nightly_path = tmp_path / "nightly.json"
    report_path = tmp_path / "report.json"
    markdown_path = tmp_path / "report.md"

    targets_path.write_text(
        json.dumps(
            {
                "kind": "compat_targets",
                "schema_version": "1.0.0",
                "targets": [
                    {
                        "target": "Chara.ReleaseMinion",
                        "check_kind": "reflection",
                        "type_name": "Chara",
                        "canonical_name": "ReleaseMinion",
                        "candidate_names": ["ReleaseMinion", "UnmakeMinion"],
                        "candidate_signatures": ["void()"],
                    }
                ],
            }
        ),
        encoding="utf-8",
    )
    stable_path.write_text(
        json.dumps({"kind": "signature_catalog", "schema_version": "1.0.0", "symbols": {"Chara.ReleaseMinion": ["void()"]}}),
        encoding="utf-8",
    )
    nightly_path.write_text(
        json.dumps({"kind": "signature_catalog", "schema_version": "1.0.0", "symbols": {"Chara.UnmakeMinion": ["void()"]}}),
        encoding="utf-8",
    )

    rc = main(
        [
            "verify-compat",
            "--targets",
            str(targets_path),
            "--stable-signatures",
            str(stable_path),
            "--nightly-signatures",
            str(nightly_path),
            "--report-json",
            str(report_path),
            "--report-md",
            str(markdown_path),
        ]
    )
    assert rc == 0

    nightly_path.write_text(
        json.dumps(
            {
                "kind": "signature_catalog",
                "schema_version": "1.0.0",
                "symbols": {"Chara.UnmakeMinion": ["void(System.String)"]},
            }
        ),
        encoding="utf-8",
    )
    rc = main(
        [
            "verify-compat",
            "--targets",
            str(targets_path),
            "--stable-signatures",
            str(stable_path),
            "--nightly-signatures",
            str(nightly_path),
            "--report-json",
            str(report_path),
            "--report-md",
            str(markdown_path),
        ]
    )
    assert rc == 1


def test_reports_include_schema_fields(tmp_path: Path):
    report_path = tmp_path / "channel-snapshot.json"
    markdown_path = tmp_path / "channel-snapshot.md"

    rc = main(
        [
            "track-channels",
            "--stable-sha",
            "1111111111111111111111111111111111111111",
            "--nightly-sha",
            "2222222222222222222222222222222222222222",
            "--report-json",
            str(report_path),
            "--report-md",
            str(markdown_path),
        ]
    )

    assert rc == 0

    snapshot = json.loads(report_path.read_text(encoding="utf-8"))
    assert snapshot["kind"] == "channel_snapshot"
    assert snapshot["schema_version"] == "1.0.0"
