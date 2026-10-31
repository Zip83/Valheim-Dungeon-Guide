"""Validate the versioned first-party shader before building or publishing a plugin."""

import hashlib
import json
import struct
import sys
from pathlib import Path

MANIFEST = "src/DungeonGuide/Content/map-shader-bundle.json"


def source_digest(path):
    """Normalize source line endings and Unity's trailing metadata whitespace."""
    text = "\n".join(line.rstrip() for line in Path(path).read_text(encoding="utf-8").splitlines()) + "\n"
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def verify_header(data, editor):
    """Reject incomplete UnityFS envelopes; this does not validate GPU execution."""
    if not data.startswith(b"UnityFS\0") or struct.unpack_from(">I", data, 8)[0] != 8:
        raise ValueError("Unexpected shader bundle format")

    compatibility_end = data.index(b"\0", 12)
    editor_end = data.index(b"\0", compatibility_end + 1)
    if data[compatibility_end + 1:editor_end].decode("ascii") != editor:
        raise ValueError("Shader bundle was built with a different Unity editor")

    size = struct.unpack_from(">Q", data, editor_end + 1)[0]
    if size != len(data) or size <= editor_end + 21:
        raise ValueError("Shader bundle has an invalid or truncated size")


def verify_bundle(root, bundle):
    """Require the recorded bundle bytes and all recorded shader build inputs."""
    manifest = json.loads((root / MANIFEST).read_text(encoding="utf-8"))
    data = bundle.read_bytes()
    verify_header(data, manifest["unity"])
    if hashlib.sha256(data).hexdigest() != manifest["sha256"]:
        raise ValueError("Shader bundle checksum differs from its manifest")

    for source, expected in manifest["sources"].items():
        if source_digest(root / source) != expected:
            raise ValueError("Rebuild shader bundle after changing " + source)


if __name__ == "__main__":
    try:
        verify_bundle(Path(__file__).resolve().parents[1], Path(sys.argv[1]))
    except (OSError, ValueError, KeyError, struct.error, IndexError) as error:
        sys.exit("Shader bundle verification failed: " + str(error))
    print("Versioned shader bundle and build inputs verified")
