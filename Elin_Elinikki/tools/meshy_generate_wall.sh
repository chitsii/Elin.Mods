#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
REPO_ROOT_WIN="$(cygpath -w "$REPO_ROOT")"
PRESET="${1:-wall_quad_lowpoly}"

python - "$REPO_ROOT_WIN" "$PRESET" <<'PY'
import base64
import json
import os
import sys
import time
import urllib.request
from pathlib import Path


def read_env_value(env_path: Path, key: str) -> str:
    for line in env_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        name, value = line.split("=", 1)
        if name.strip() == key:
            return value.strip()
    raise RuntimeError(f"{key} was not found in {env_path}")


def http_json(url: str, method: str, api_key: str, payload: dict | None = None) -> dict:
    data = None
    headers = {
        "Authorization": f"Bearer {api_key}",
    }
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(url, data=data, method=method, headers=headers)
    with urllib.request.urlopen(request, timeout=120) as response:
        return json.loads(response.read().decode("utf-8"))


def download_file(url: str, dest: Path, api_key: str) -> None:
    request = urllib.request.Request(url, headers={"Authorization": f"Bearer {api_key}"})
    with urllib.request.urlopen(request, timeout=120) as response:
        dest.write_bytes(response.read())


repo_root = Path(sys.argv[1])
preset = sys.argv[2]
env_path = repo_root / ".env"
input_path = repo_root / "Elin_Elinikki" / "_tmp_meshy_inputs_corrected" / "tile_775_frame_0.png"
output_root = repo_root / "Elin_Elinikki" / "_tmp_meshy_run_single"
timestamp = time.strftime("%Y%m%d_%H%M%S")
output_dir = output_root / f"{timestamp}_{preset}"
output_dir.mkdir(parents=True, exist_ok=True)

api_key = read_env_value(env_path, "MESHY_API_KEY")
image_bytes = input_path.read_bytes()
data_uri = "data:image/png;base64," + base64.b64encode(image_bytes).decode("ascii")

payloads = {
    "wall_quad_lowpoly": {
        "image_url": data_uri,
        "model_type": "standard",
        "topology": "quad",
        "target_polycount": 100,
        "should_remesh": True,
        "save_pre_remeshed_model": True,
        "symmetry_mode": "on",
        "should_texture": False,
        "remove_lighting": True,
        "image_enhancement": False,
        "target_formats": ["glb"],
    },
    "defaultish": {
        "image_url": data_uri,
        "model_type": "standard",
        "should_texture": False,
        "target_formats": ["glb"],
    },
}
if preset not in payloads:
    raise RuntimeError(f"Unknown preset: {preset}. Available: {', '.join(sorted(payloads))}")
payload = payloads[preset]

(output_dir / "request.json").write_text(
    json.dumps(
        {
            "preset": preset,
            "input_image": str(input_path),
            "payload": {
                **payload,
                "image_url": f"<data-uri:{input_path.name}>",
            },
        },
        ensure_ascii=False,
        indent=2,
    ),
    encoding="utf-8",
)

create_response = http_json(
    "https://api.meshy.ai/openapi/v1/image-to-3d",
    method="POST",
    api_key=api_key,
    payload=payload,
)
(output_dir / "create_response.json").write_text(
    json.dumps(create_response, ensure_ascii=False, indent=2),
    encoding="utf-8",
)

task_id = create_response["result"]
latest = create_response
for _ in range(90):
    latest = http_json(
        f"https://api.meshy.ai/openapi/v1/image-to-3d/{task_id}",
        method="GET",
        api_key=api_key,
    )
    (output_dir / "latest_task.json").write_text(
        json.dumps(latest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    status = latest.get("status", "")
    if status == "SUCCEEDED":
        break
    if status in {"FAILED", "CANCELED"}:
        raise RuntimeError(f"Meshy task ended with status={status}")
    time.sleep(10)
else:
    raise TimeoutError("Timed out while waiting for Meshy task completion")

thumbnail_url = latest.get("thumbnail_url")
model_urls = latest.get("model_urls") or {}
if thumbnail_url:
    download_file(thumbnail_url, output_dir / "preview.png", api_key)
if model_urls.get("glb"):
    download_file(model_urls["glb"], output_dir / "model.glb", api_key)
if model_urls.get("obj"):
    download_file(model_urls["obj"], output_dir / "model.obj", api_key)
if latest.get("pre_remeshed_model_url"):
    download_file(latest["pre_remeshed_model_url"], output_dir / "pre_remeshed_model.glb", api_key)

summary = {
    "preset": preset,
    "input_image": str(input_path),
    "task_id": task_id,
    "status": latest.get("status"),
    "output_dir": str(output_dir),
    "preview": str(output_dir / "preview.png") if thumbnail_url else None,
    "model_glb": str(output_dir / "model.glb") if model_urls.get("glb") else None,
    "model_obj": str(output_dir / "model.obj") if model_urls.get("obj") else None,
    "pre_remeshed_model_glb": str(output_dir / "pre_remeshed_model.glb") if latest.get("pre_remeshed_model_url") else None,
}
(output_dir / "summary.json").write_text(
    json.dumps(summary, ensure_ascii=False, indent=2),
    encoding="utf-8",
)

print(json.dumps(summary, ensure_ascii=False, indent=2))
PY
