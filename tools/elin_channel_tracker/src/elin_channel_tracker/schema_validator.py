from __future__ import annotations

from typing import Any


class SchemaValidator:
    @staticmethod
    def ensure_kind_and_schema(document: dict[str, Any], expected_kind: str) -> None:
        if not isinstance(document, dict):
            raise ValueError("Document must be a JSON object.")
        if document.get("kind") != expected_kind:
            raise ValueError(f"Invalid kind: expected {expected_kind}, got {document.get('kind')}")
        schema_version = document.get("schema_version")
        if not isinstance(schema_version, str) or not schema_version:
            raise ValueError("schema_version is required.")
