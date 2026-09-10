import contextlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("history", Path(__file__).with_name("history.py"))
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)
WORKSPACE = h.ROOT


class HistoryMigrationTests(unittest.TestCase):
    def setUp(self):
        parent = WORKSPACE / "tmp/workspace-history-tests"
        parent.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=parent, prefix="fixture-")
        self.root = Path(self.temp.name).resolve()
        self.assertIn(parent.resolve(), self.root.parents)
        self.original_globals = h.ROOT, h.MANIFEST, h.SNAPSHOT, h.FROZEN
        h.ROOT = self.root
        h.MANIFEST = self.root / "docs/archive/migrations/test.json"
        h.SNAPSHOT = self.root / "ignored/originals"
        h.FROZEN = {"docs/frozen.md"}
        subprocess.run(["git", "init", "-q", str(self.root)], check=True, capture_output=True)
        self.old = "docs/goals/2026/20260101-0001-中文.md"
        self.new = "docs/archive/goals/2026/20260101-0001-中文.md"
        self.put(self.old, b"# Old\r\n\r\n[owner](../../owner.md#scope)\r\n`[literal](../../owner.md)`\r\n```text\r\n[command](../../owner.md)\r\n```\r\n")
        self.put("docs/owner.md", "# Owner\n\n## scope\n".encode())
        self.put("docs/index.md", f"[old](goals/2026/{Path(self.old).name})\n[asset]: <goals/2026/{Path(self.old).name}> \"title\"\n".encode())
        self.put("docs/frozen.md", f"[old](goals/2026/{Path(self.old).name})\n".encode())
        self.put(".gitignore", b"ignored/\n")
        self.before = {p.relative_to(self.root).as_posix(): p.read_bytes() for p in (self.root / "docs").rglob("*.md")}
        data = self.before[self.old]
        self.put("ignored/originals/" + self.old, data)
        h.write_json(h.MANIFEST, {"files": [{"source": self.old, "current": self.old, "sha256": h.sha(data), "textSha256": h.text_sha(data), "reading": "pending"}]})
        self.selection = self.root / "ignored/selection.json"
        h.write_json(self.selection, {self.old: self.new})

    def put(self, name, data):
        path = h.safe(self.root, name)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)

    def tearDown(self):
        h.ROOT, h.MANIFEST, h.SNAPSHOT, h.FROZEN = self.original_globals
        self.temp.cleanup()

    def apply(self):
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, True)
        return next((h.SNAPSHOT.parent / "migrations").glob("*.json"))

    def test_preview_is_read_only(self):
        before = h.MANIFEST.read_bytes()
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, False)
        self.assertEqual(before, h.MANIFEST.read_bytes())
        self.assertFalse((self.root / self.new).exists())

    def test_source_relocation_relinks_archive_without_changing_historical_commands(self):
        old_source, new_source = "tests/Old 中文.cs", "tests/Product/New 中文.cs"
        self.put(old_source, b"class Example {}\n")
        raw = b"# Old\n\n[source](../../../tests/Old%20%E4%B8%AD%E6%96%87.cs#L1)\n`tests/Old`\n"
        self.put(self.old, raw)
        self.put("ignored/originals/" + self.old, raw)
        manifest = json.loads(h.MANIFEST.read_text())
        manifest['files'][0].update(sha256=h.sha(raw), textSha256=h.text_sha(raw))
        subprocess.run(["git", "add", "."], cwd=self.root, check=True, capture_output=True)
        subprocess.run(["git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "-qm", "baseline"], cwd=self.root, check=True, capture_output=True)
        manifest['baselineCommit'] = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        h.write_json(h.MANIFEST, manifest)
        self.apply()
        self.put(new_source, b"class Example { /* implementation can change */ }\n")
        (self.root / old_source).unlink()
        before = (self.root / self.new).read_bytes()
        before_manifest = h.MANIFEST.read_bytes()
        h.write_json(self.selection, {old_source: new_source})
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, False, references=True)
        self.assertEqual(before_manifest, h.MANIFEST.read_bytes())
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, True, references=True)
            h.check()
        self.assertIn(b"tests/Product/New%20", (self.root / self.new).read_bytes())
        self.assertIn(b"`tests/Old`", (self.root / self.new).read_bytes())
        journal = sorted((h.SNAPSHOT.parent / "migrations").glob("*.json"))[-1]
        with contextlib.redirect_stdout(io.StringIO()): h.rollback(journal)
        self.assertEqual(before, (self.root / self.new).read_bytes())
        self.assertEqual(before_manifest, h.MANIFEST.read_bytes())
        self.assertTrue((self.root / new_source).exists())

    def test_reference_relocation_rejects_existing_source_and_missing_target(self):
        h.write_json(self.selection, {"docs/owner.md": "docs/index.md"})
        with self.assertRaisesRegex(ValueError, "Conflicting relocated"):
            h.migrate(self.selection, True, references=True)
        h.write_json(self.selection, {"tests/old.cs": "tests/missing.cs"})
        with self.assertRaisesRegex(ValueError, "Conflicting relocated"):
            h.migrate(self.selection, True, references=True)

    def test_code_inside_link_label_is_rebased_but_code_example_stays_literal(self):
        raw = b"[`owner`](../../owner.md) [mixed `label`](../../owner.md) `[literal](../../owner.md)`\n"
        actual = h.rewrite_links(raw, self.old, self.new, {})
        self.assertEqual(b"[`owner`](../../../owner.md) [mixed `label`](../../../owner.md) `[literal](../../owner.md)`\n", actual)

    def test_archive_link_repair_uses_original_and_refuses_prose_changes(self):
        data = b"# Old\n\n[`owner`](../../owner.md)\n"
        self.put(self.old, data)
        self.put("ignored/originals/" + self.old, data)
        h.write_json(h.MANIFEST, {"files": [{"source": self.old, "current": self.old, "sha256": h.sha(data), "textSha256": h.text_sha(data), "reading": "complete"}]})
        self.apply()
        self.put(self.new, data)  # Simulate the old parser's missed label destination.
        with contextlib.redirect_stdout(io.StringIO()):
            h.repair_archive_links(True)
            h.check(True)
        self.put(self.new, b"# Rewritten observation\n\n[`owner`](../../owner.md)\n")
        with self.assertRaisesRegex(ValueError, "Non-link content"):
            h.repair_archive_links(True)

    def test_stable_receipt_keeps_old_path_without_rewriting_receipt(self):
        self.put("receipt.json", json.dumps({"review": self.old}).encode())
        refs = self.root / "ignored/stable.json"
        h.write_json(refs, {self.old: ["receipt.json"]})
        before = (self.root / "receipt.json").read_bytes()
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, True, stable_references=refs)
            h.check()
        self.assertEqual(before, (self.root / "receipt.json").read_bytes())
        self.assertEqual(h.alias_bytes(self.old, self.new), (self.root / self.old).read_bytes())
        (self.root / self.old).unlink()
        with self.assertRaisesRegex(ValueError, "compatibility route"):
            h.check()

    def test_active_tool_relocation_preserves_original_and_allows_current_edits(self):
        old = "docs/reviews/api/native-function-map/README.md"
        new = "tools/native-function-map/workbench/README.md"
        data = b"# Historical map tool\n"
        self.put(old, data)
        self.put("ignored/originals/" + old, data)
        h.write_json(h.MANIFEST, {"files": [{"source": old, "current": old, "sha256": h.sha(data), "textSha256": h.text_sha(data), "reading": "complete"}]})
        h.write_json(self.selection, {old: new})
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, True, active=True)
        self.put(new, b"# Current tool after engineering changes\n")
        with contextlib.redirect_stdout(io.StringIO()): h.check(True)
        self.assertEqual(data, (h.SNAPSHOT / old).read_bytes())
        manifest = json.loads(h.MANIFEST.read_text())
        manifest['files'][0]['source'] = self.old
        h.write_json(h.MANIFEST, manifest)
        with self.assertRaisesRegex(ValueError, "Unapproved active"):
            h.check()

    def test_move_rebases_links_preserves_literals_and_frozen_identity_then_rolls_back(self):
        journal = self.apply()
        text = (self.root / self.new).read_text(encoding="utf-8")
        self.assertIn("[owner](../../../owner.md#scope)", text)
        self.assertIn("`[literal](../../owner.md)`", text)
        self.assertIn("[command](../../owner.md)", text)
        self.assertEqual(self.before["docs/frozen.md"], (self.root / "docs/frozen.md").read_bytes())
        self.assertIn("archived-document", (self.root / self.old).read_text())
        self.assertIn("archive/goals/2026/", (self.root / "docs/index.md").read_text())
        with contextlib.redirect_stdout(io.StringIO()):
            h.rollback(journal)
        for name, data in self.before.items():
            self.assertEqual(data, (self.root / name).read_bytes())
        self.assertFalse((self.root / self.new).exists())

    def test_conflicting_destination_and_changed_source_are_rejected(self):
        self.put(self.new, b"existing")
        with self.assertRaises(ValueError): h.migrate(self.selection, True)
        (self.root / self.new).unlink()
        self.put(self.old, b"changed")
        with self.assertRaises(ValueError): h.migrate(self.selection, True)

    def test_rollback_rejects_later_edits(self):
        journal = self.apply()
        self.put(self.new, b"later user edit")
        with self.assertRaises(ValueError): h.rollback(journal)
        self.assertEqual(b"later user edit", (self.root / self.new).read_bytes())

    def test_interrupted_move_can_restore_originals(self):
        journal = self.apply()
        record = json.loads(journal.read_text())
        record["state"] = "applying"
        h.write_json(journal, record)
        self.put("docs/index.md", self.before["docs/index.md"])
        with contextlib.redirect_stdout(io.StringIO()): h.rollback(journal)
        self.assertEqual(self.before[self.old], (self.root / self.old).read_bytes())

    def test_path_escape_and_frozen_moves_are_rejected(self):
        with self.assertRaises(ValueError): h.safe(self.root, "../outside")
        with self.assertRaises(ValueError): h.safe(self.root, str(self.root.parent / "outside"))
        h.write_json(self.selection, {"docs/frozen.md": "docs/archive/frozen.md"})
        with self.assertRaises(ValueError): h.migrate(self.selection, True)

    def test_bom_and_line_endings_have_portable_text_identity(self):
        self.assertEqual(h.text_sha(b"\xef\xbb\xbfalpha\r\n"), h.text_sha(b"alpha\n"))

    def test_duplicate_selection_and_manifest_identity_are_rejected(self):
        self.selection.write_text('{"a":"one","a":"two"}')
        with self.assertRaises(ValueError): h.migrate(self.selection, True)
        h.write_json(self.selection, {self.old: self.new})
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["files"].append(dict(manifest["files"][0]))
        h.write_json(h.MANIFEST, manifest)
        with self.assertRaises(ValueError): h.migrate(self.selection, True)

    def test_rollback_preserves_later_manifest_changes(self):
        journal = self.apply()
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["laterReview"] = True
        h.write_json(h.MANIFEST, manifest)
        with self.assertRaises(ValueError): h.rollback(journal)
        self.assertTrue(json.loads(h.MANIFEST.read_text())["laterReview"])

    def test_read_decisions_require_exact_original_and_resolvable_destination(self):
        readings = self.root / "ignored/readings"
        row = {"source": self.old, "sha256": h.sha(self.before[self.old]), "reading": "complete", "disposition": "archive", "knowledge": ["docs/owner.md"], "reason": "Full original reviewed; current rule has an owner."}
        h.write_json(readings / "review.json", [row])
        before = h.MANIFEST.read_bytes()
        with contextlib.redirect_stdout(io.StringIO()): h.merge_readings(readings, False)
        self.assertEqual(before, h.MANIFEST.read_bytes())
        for changed in ({"sha256": "wrong"}, {"reading": "pending"}, {"knowledge": ["../outside"]}, {"knowledge": ["missing.md"]}):
            h.write_json(readings / "review.json", [dict(row, **changed)])
            with self.assertRaises(ValueError): h.merge_readings(readings, True)
            self.assertEqual(before, h.MANIFEST.read_bytes())
        h.write_json(readings / "review.json", [row, row])
        with self.assertRaises(ValueError): h.merge_readings(readings, True)
        h.write_json(readings / "review.json", [row])
        with contextlib.redirect_stdout(io.StringIO()): h.merge_readings(readings, True)
        self.assertEqual("complete", json.loads(h.MANIFEST.read_text())["files"][0]["reading"])

    def test_later_knowledge_links_can_be_relinked_without_moving_body_again(self):
        self.apply()
        self.put("docs/knowledge/notes.md", f"[source](../goals/2026/{Path(self.old).name}#original)\n".encode())
        h.write_json(self.selection, {})
        with contextlib.redirect_stdout(io.StringIO()): h.migrate(self.selection, True, include_knowledge=True)
        self.assertIn("../archive/goals/2026/", (self.root / "docs/knowledge/notes.md").read_text())
        self.assertIn("#original", (self.root / "docs/knowledge/notes.md").read_text())

    def test_generated_inventory_exception_is_separate_and_needs_reading_basis(self):
        readings = self.root / "ignored/readings"
        row = {"source": self.old, "sha256": h.sha(self.before[self.old]), "reading": "authored-complete/generated-checked", "disposition": "archive", "knowledge": [], "reason": "Authored findings read; generated locators checked."}
        h.write_json(readings / "inventory.json", [row])
        with self.assertRaises(ValueError): h.merge_readings(readings, True)
        row["reviewBasis"] = {"authoredSections": ["Header and all distinct recommendations"], "generatedSections": ["Symbol locator rows"], "evidence": ["Historical generation statement"], "checks": ["Count, baseline hash and representative paths checked"]}
        h.write_json(readings / "inventory.json", [row])
        with contextlib.redirect_stdout(io.StringIO()): h.merge_readings(readings, True)
        stored = json.loads(h.MANIFEST.read_text())["files"][0]
        self.assertNotEqual("complete", stored["reading"])
        self.assertEqual(row["reviewBasis"], stored["reviewBasis"])

    def test_discovered_text_uses_original_git_checkpoint_and_rejects_source_drift(self):
        name = "old-notes.md"
        self.put(name, b"Original discussion.\n")
        for args in (["add", "--", name], ["-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "-qm", "baseline"]):
            subprocess.run(["git", "-C", str(self.root), *args], check=True, capture_output=True)
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["baselineCommit"] = subprocess.check_output(["git", "-C", str(self.root), "rev-parse", "HEAD"], text=True).strip()
        h.write_json(h.MANIFEST, manifest)
        h.write_json(self.selection, [name])
        self.put(name, b"Later edit.\n")
        with self.assertRaises(ValueError): h.extend_snapshot(self.selection, True)
        self.assertFalse((h.SNAPSHOT / name).exists())
        self.put(name, b"Original discussion.\n")
        with contextlib.redirect_stdout(io.StringIO()): h.extend_snapshot(self.selection, True)
        self.assertEqual(b"Original discussion.\n", (h.SNAPSHOT / name).read_bytes())
        with self.assertRaises(ValueError): h.extend_snapshot(self.selection, True)

    def test_recalculated_current_hash_cannot_approve_rewritten_history(self):
        self.apply()
        self.put(self.new, b"Rewritten historical conclusion.\n")
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["files"][0]["currentSha256"] = h.sha((self.root / self.new).read_bytes())
        manifest["files"][0]["currentTextSha256"] = h.text_sha((self.root / self.new).read_bytes())
        h.write_json(h.MANIFEST, manifest)
        with self.assertRaisesRegex(ValueError, "beyond mechanical links"):
            h.check()

    def test_original_copy_keeps_live_owner_and_rollback_preserves_later_owner_edit(self):
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["files"][0]["reading"] = "complete"
        h.write_json(h.MANIFEST, manifest)
        with contextlib.redirect_stdout(io.StringIO()): h.preserve_originals(self.selection, True)
        self.assertEqual(self.before[self.old], (self.root / self.old).read_bytes())
        self.put(self.old, b"Current compact owner.\n")
        with contextlib.redirect_stdout(io.StringIO()): h.check()
        with self.assertRaises(ValueError): h.preserve_originals(self.selection, True)
        journal = next((h.SNAPSHOT.parent / "migrations").glob("*.json"))
        with contextlib.redirect_stdout(io.StringIO()): h.rollback(journal)
        self.assertEqual(b"Current compact owner.\n", (self.root / self.old).read_bytes())
        self.assertFalse((self.root / self.new).exists())

    def test_later_moves_update_previously_archived_links_and_integrity(self):
        owner = "docs/owner.md"
        owner_data = (self.root / owner).read_bytes()
        self.put("ignored/originals/" + owner, owner_data)
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["files"].append({"source": owner, "current": owner, "sha256": h.sha(owner_data), "textSha256": h.text_sha(owner_data), "reading": "complete"})
        h.write_json(h.MANIFEST, manifest)
        self.apply()
        h.write_json(self.selection, {owner: "docs/archive/owner.md"})
        with contextlib.redirect_stdout(io.StringIO()):
            h.migrate(self.selection, True)
            h.check()
        self.assertIn("[owner](../../owner.md#scope)", (self.root / self.new).read_text())

    def test_private_archive_requires_ignore_and_full_read_then_rolls_back(self):
        source = ".codex/conversation-history/20260101-example.md"
        target = "docs/archive/conversations/2026/example.md"
        content = b"# Private\n[public owner](../../docs/owner.md#scope)\n"
        self.put(source, content)
        self.put("ignored/originals/" + source, content)
        self.put(".gitignore", b"ignored/\n.codex/conversation-history/\n")
        private = {"files": [{"source": source, "current": source, "sha256": h.sha(content), "reading": "complete"}]}
        h.write_json(h.private_manifest(), private)
        h.write_json(self.selection, {source: target})
        with self.assertRaisesRegex(ValueError, "ignored"):
            h.migrate(self.selection, True, private=True)
        self.put(".gitignore", b"ignored/\n.codex/conversation-history/\ndocs/archive/conversations/\n")
        private["files"][0]["reading"] = "pending"
        h.write_json(h.private_manifest(), private)
        with self.assertRaisesRegex(ValueError, "fully reviewed"):
            h.migrate(self.selection, True, private=True)
        private["files"][0]["reading"] = "complete"
        h.write_json(h.private_manifest(), private)
        public_before = h.MANIFEST.read_bytes()
        with contextlib.redirect_stdout(io.StringIO()): h.migrate(self.selection, True, private=True)
        self.assertEqual(1, h.check_private(True)["privateMoved"])
        self.assertEqual(public_before, h.MANIFEST.read_bytes())
        journal = next((h.SNAPSHOT.parent / "migrations").glob("*.json"))
        with contextlib.redirect_stdout(io.StringIO()): h.rollback(journal)
        self.assertEqual(content, (self.root / source).read_bytes())

    def test_git_baseline_can_verify_archives_without_local_originals(self):
        for args in (["add", "--", self.old], ["-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "-qm", "baseline"]):
            subprocess.run(["git", "-C", str(self.root), *args], check=True, capture_output=True)
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["baselineCommit"] = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        h.write_json(h.MANIFEST, manifest)
        self.apply()
        (h.SNAPSHOT / self.old).unlink()
        with contextlib.redirect_stdout(io.StringIO()): h.check()

    def test_binary_attachment_is_moved_with_owner_and_checked_without_text_decoding(self):
        asset = "docs/goals/2026/截图.png"
        archived_asset = "docs/archive/goals/2026/截图.png"
        data = b"\x89PNG\x00\xff\xfe\x10"
        self.put(asset, data)
        subprocess.run(["git", "add", "--", asset], cwd=self.root, check=True, capture_output=True)
        subprocess.run(["git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "-qm", "asset baseline"], cwd=self.root, check=True, capture_output=True)
        manifest = json.loads(h.MANIFEST.read_text())
        manifest["baselineCommit"] = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=self.root, text=True).strip()
        h.write_json(h.MANIFEST, manifest)
        h.write_json(self.selection, [{"source": asset, "owner": self.old, "purpose": "Unique screenshot paired with historical record."}])
        with contextlib.redirect_stdout(io.StringIO()): h.extend_snapshot(self.selection, True, attachments=True)
        h.write_json(self.selection, {self.old: self.new, asset: archived_asset})
        journal = self.apply()
        with contextlib.redirect_stdout(io.StringIO()): h.check()
        self.assertEqual(data, (self.root / archived_asset).read_bytes())
        self.put(archived_asset, b"changed binary")
        with self.assertRaises(ValueError): h.check()
        self.put(archived_asset, data)
        with contextlib.redirect_stdout(io.StringIO()): h.rollback(journal)
        self.assertEqual(data, (self.root / asset).read_bytes())


if __name__ == "__main__":
    unittest.main()
