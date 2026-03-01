from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from . import KIND_COMPAT_REPORT, SCHEMA_VERSION


@dataclass(frozen=True)
class BranchMatch:
    found: dict[str, list[str]]

    @property
    def all_signatures(self) -> list[str]:
        values: list[str] = []
        seen: set[str] = set()
        for signatures in self.found.values():
            for signature in signatures:
                if signature not in seen:
                    values.append(signature)
                    seen.add(signature)
        return values


class CompatEvaluator:
    def evaluate(
        self,
        targets: list[dict[str, Any]],
        stable_symbols: dict[str, list[str]],
        nightly_symbols: dict[str, list[str]],
        scan_scope: str = "extended-only",
    ) -> dict[str, Any]:
        checks = [
            self._evaluate_target(target, stable_symbols, nightly_symbols)
            for target in targets
        ]

        summary = {"broken": 0, "risky": 0, "ok": 0}
        for check in checks:
            summary[check["status"]] += 1

        return {
            "kind": KIND_COMPAT_REPORT,
            "schema_version": SCHEMA_VERSION,
            "scan_scope": scan_scope,
            "summary": summary,
            "checks": checks,
            "mod_summary": {
                "status": "failed" if summary["broken"] > 0 else "passed",
                "fail_gate": "broken",
            },
        }

    def _evaluate_target(
        self,
        target: dict[str, Any],
        stable_symbols: dict[str, list[str]],
        nightly_symbols: dict[str, list[str]],
    ) -> dict[str, Any]:
        check_kind = str(target.get("check_kind", "reflection"))
        target_name = str(target.get("target", "unknown"))
        type_name = str(target.get("type_name", ""))
        canonical_name = str(target.get("canonical_name", ""))
        candidate_names = [str(x) for x in target.get("candidate_names", []) if str(x)]
        if not candidate_names and canonical_name:
            candidate_names = [canonical_name]
        if not canonical_name and candidate_names:
            canonical_name = candidate_names[0]
        candidate_signatures = [
            str(x) for x in target.get("candidate_signatures", []) if str(x)
        ]
        expected_signatures = set(candidate_signatures)

        stable_match = self._resolve_branch(type_name, candidate_names, stable_symbols)
        nightly_match = self._resolve_branch(type_name, candidate_names, nightly_symbols)

        if check_kind == "dynamic":
            return self._make_check(
                target_name=target_name,
                check_kind=check_kind,
                status="risky",
                reason_code="unresolved_dynamic",
                stable_signatures=stable_match.all_signatures,
                nightly_signatures=nightly_match.all_signatures,
                candidate_signatures=candidate_signatures,
            )

        if not stable_match.found or not nightly_match.found:
            return self._make_check(
                target_name=target_name,
                check_kind=check_kind,
                status="broken",
                reason_code="missing_symbol",
                stable_signatures=stable_match.all_signatures,
                nightly_signatures=nightly_match.all_signatures,
                candidate_signatures=candidate_signatures,
            )

        stable_all = stable_match.all_signatures
        nightly_all = nightly_match.all_signatures

        if expected_signatures:
            if not expected_signatures.intersection(stable_all) or not expected_signatures.intersection(nightly_all):
                return self._make_check(
                    target_name=target_name,
                    check_kind=check_kind,
                    status="broken",
                    reason_code="signature_mismatch",
                    stable_signatures=stable_all,
                    nightly_signatures=nightly_all,
                    candidate_signatures=candidate_signatures,
                )

        stable_has_canonical = canonical_name in stable_match.found
        nightly_has_canonical = canonical_name in nightly_match.found
        if not (stable_has_canonical and nightly_has_canonical):
            return self._make_check(
                target_name=target_name,
                check_kind=check_kind,
                status="risky",
                reason_code="renamed_candidate",
                stable_signatures=stable_all,
                nightly_signatures=nightly_all,
                candidate_signatures=candidate_signatures,
            )

        return self._make_check(
            target_name=target_name,
            check_kind=check_kind,
            status="ok",
            reason_code="matched",
            stable_signatures=stable_all,
            nightly_signatures=nightly_all,
            candidate_signatures=candidate_signatures,
        )

    @staticmethod
    def _resolve_branch(
        type_name: str,
        candidate_names: list[str],
        symbol_catalog: dict[str, list[str]],
    ) -> BranchMatch:
        found: dict[str, list[str]] = {}
        for name in candidate_names:
            key = f"{type_name}.{name}" if type_name else name
            signatures = symbol_catalog.get(key)
            if not signatures:
                continue
            normalized = [str(sig) for sig in signatures if str(sig)]
            if normalized:
                found[name] = normalized
        return BranchMatch(found=found)

    @staticmethod
    def _make_check(
        *,
        target_name: str,
        check_kind: str,
        status: str,
        reason_code: str,
        stable_signatures: list[str],
        nightly_signatures: list[str],
        candidate_signatures: list[str],
    ) -> dict[str, Any]:
        return {
            "target": target_name,
            "check_kind": check_kind,
            "status": status,
            "reason_code": reason_code,
            "stable_signatures": stable_signatures,
            "nightly_signatures": nightly_signatures,
            "candidate_signatures": candidate_signatures,
        }
