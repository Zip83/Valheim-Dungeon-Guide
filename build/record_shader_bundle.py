"""Refresh the versioned shader identity after an explicit local Unity rebuild."""

import hashlib
import json
from pathlib import Path

from verify_shader_bundle import MANIFEST, source_digest, verify_header


def record_manifest(root):
    """Record bundle bytes and normalized inputs while preserving the supported target metadata."""
    path = root / MANIFEST
    manifest = json.loads(path.read_text(encoding="utf-8"))
    data = (root / "src/DungeonGuide/Content/dungeonguide-ui").read_bytes()
    verify_header(data, manifest["unity"])
    manifest["sha256"] = hashlib.sha256(data).hexdigest()
    manifest["sources"] = {source: source_digest(root / source) for source in manifest["sources"]}
    with path.open("w", encoding="utf-8", newline="\n") as output:
        output.write(json.dumps(manifest, indent=2) + "\n")


if __name__ == "__main__":
    record_manifest(Path(__file__).resolve().parents[1])
