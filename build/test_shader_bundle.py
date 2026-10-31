"""Protect CI from silently publishing missing or stale shaders."""

import hashlib
import json
import struct
import tempfile
import unittest
from pathlib import Path

from verify_shader_bundle import MANIFEST, source_digest, verify_bundle


class ShaderBundleTests(unittest.TestCase):
    """Exercise artifact failures independently of Unity activation."""

    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.source = self.root / "shader.txt"
        self.source.write_text("source\n", encoding="utf-8")
        self.bundle = self.root / "dungeonguide-ui"
        prefix = b"UnityFS\0" + struct.pack(">I", 8) + b"5.x.x\0" + b"6000.0.75f1\0"
        data = prefix + struct.pack(">Q", len(prefix) + 48) + bytes(40)
        self.bundle.write_bytes(data)
        manifest = self.root / MANIFEST
        manifest.parent.mkdir(parents=True)
        manifest.write_text(json.dumps({
            "unity": "6000.0.75f1",
            "sha256": hashlib.sha256(data).hexdigest(),
            "sources": {"shader.txt": source_digest(self.source)},
        }), encoding="utf-8")

    def test_accepts_matching_bundle_and_sources(self):
        verify_bundle(self.root, self.bundle)

    def test_rejects_missing_bundle(self):
        self.bundle.unlink()
        with self.assertRaises(FileNotFoundError):
            verify_bundle(self.root, self.bundle)

    def test_rejects_modified_bundle(self):
        data = bytearray(self.bundle.read_bytes())
        data[-1] = 1
        self.bundle.write_bytes(data)
        with self.assertRaisesRegex(ValueError, "checksum"):
            verify_bundle(self.root, self.bundle)

    def test_rejects_changed_shader_source(self):
        self.source.write_text("changed shader\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Rebuild"):
            verify_bundle(self.root, self.bundle)

    def test_accepts_cross_platform_source_whitespace(self):
        self.source.write_bytes(b"source \r\n")
        verify_bundle(self.root, self.bundle)


if __name__ == "__main__":
    unittest.main()
