from __future__ import annotations

import hashlib
import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


def module_at(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


ROOT = Path(__file__).resolve().parents[2]
compare = module_at("compare", Path(__file__).with_name("compare_baselines.py"))
function_map = module_at("function_map", ROOT / "tools/native-function-map/build_native_function_map.py")


def scene(offset: int, guid: str) -> bytes:
    return f"""%YAML 1.1
--- !u!1 &{1+offset}
GameObject:
  m_Name: Barn
  m_Component:
  - component: {{fileID: {2+offset}}}
--- !u!4 &{2+offset}
Transform:
  m_GameObject: {{fileID: {1+offset}}}
  m_Father: {{fileID: 0}}
  m_LocalPosition: {{x: 1, y: 2, z: 3}}
  m_Font: {{fileID: 11400000, guid: {guid}, type: 2}}
""".encode()


class CompareTests(unittest.TestCase):
    def test_guid_and_internal_ids_resolve_without_erasing_references(self):
        first, second = "a" * 32, "b" * 32
        old, issues = compare.canonical_resource("scene.unity", scene(0, first), {first: "Assets/FontA.asset"})
        new, new_issues = compare.canonical_resource("scene.unity", scene(100, second), {second: "Assets/FontA.asset"})
        self.assertEqual([], issues + new_issues)
        self.assertEqual(old, new)
        changed_font, issues = compare.canonical_resource("scene.unity", scene(100, second), {second: "Assets/FontB.asset"})
        self.assertNotEqual(old, changed_font)
        changed_position, _ = compare.canonical_resource("scene.unity", scene(100, second).replace(b"x: 1", b"x: 9"), {second: "Assets/FontA.asset"})
        self.assertNotEqual(old, changed_position)

    def test_unknown_reference_and_duplicate_identity_remain_unresolved(self):
        _, issues = compare.canonical_resource("scene.unity", scene(0, "a" * 32), {})
        self.assertTrue(any("unresolved-external-guid" in item for item in issues))
        text = "--- !u!1 &1\nGameObject:\n  m_Name: Same\n--- !u!1 &2\nGameObject:\n  m_Name: Same\n"
        _, issues = compare.canonical_resource("scene.unity", text.encode(), {})
        self.assertTrue(any("ambiguous-entity" in item for item in issues))

    def test_only_known_tables_may_use_natural_keys(self):
        old = b'[{"id":1,"v":"a"},{"id":2,"v":"b"}]'
        new = b'[{"id":2,"v":"b"},{"id":1,"v":"a"}]'
        table = "ExportedProject/Assets/Configs/GenDatas/item_tbitem.json"
        self.assertEqual(compare.canonical_resource(table, old, {}), compare.canonical_resource(table, new, {}))
        self.assertNotEqual(compare.canonical_resource("ordered.json", old, {}), compare.canonical_resource("ordered.json", new, {}))

    def test_same_assembly_reuses_without_code_or_asset_inputs(self):
        with tempfile.TemporaryDirectory(prefix="dtmapi-code-compare-") as directory:
            base = Path(directory)
            for side in ("before", "after"):
                build = base / side
                source = build / "raw-snapshot/game/DolocTown_Data/Managed/Assembly-CSharp.dll"
                source.parent.mkdir(parents=True)
                source.write_bytes(b"authored fixture bytes")
                compare.write_json(build / "full-baseline-inventory/raw-snapshot-files.json", [{"path": "DolocTown_Data/Managed/Assembly-CSharp.dll", "bytes": source.stat().st_size, "sha256": hashlib.sha256(source.read_bytes()).hexdigest()}])
            result = subprocess.run([sys.executable, str(Path(compare.__file__)), "--before", str(base / "before"), "--after", str(base / "after"), "--output-dir", str(base / "result")], capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stderr)
            summary = compare.read_json(base / "result/comparison-summary.json")
            self.assertEqual("identical-assembly-reused", summary["code"][0]["status"])
            self.assertFalse((base / "result/resources").exists())
            source.write_bytes(b"corruption same bytes!")
            result = subprocess.run([sys.executable, str(Path(compare.__file__)), "--before", str(base / "before"), "--after", str(base / "after"), "--output-dir", str(base / "result")], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertEqual("failed", compare.read_json(base / "result/comparison-summary.json")["status"])

    def test_resource_raw_guid_diff_is_kept(self):
        with tempfile.TemporaryDirectory(prefix="dtmapi-resource-compare-") as directory:
            base = Path(directory)
            for side, guid, offset in (("before", "a" * 32, 0), ("after", "b" * 32, 100)):
                build = base / side
                export = build / "asset-ripper-unity-project"
                export.mkdir(parents=True)
                (export / "scene.unity").write_bytes(scene(offset, guid))
                (export / "FontA.asset.meta").write_text("guid: " + guid + "\n", encoding="utf-8")
                rows = [{"path": file.name, "bytes": file.stat().st_size, "sha256": hashlib.sha256(file.read_bytes()).hexdigest()} for file in export.iterdir()]
                compare.write_json(build / "full-baseline-inventory/asset-ripper-export-files.json", rows)
            compare.compare_resources(base / "before", base / "after", base / "result", ["scene.unity"])
            report = compare.read_json(base / "result/resources/semantic-delta.json")
            self.assertEqual("stable-entities-and-references-equal", report[0]["status"])
            raw = (base / "result" / report[0]["rawDiff"]).read_text()
            self.assertIn("a" * 32, raw)
            self.assertIn("b" * 32, raw)

    def test_function_map_requires_explicit_complete_identity(self):
        result = subprocess.run([sys.executable, str(Path(function_map.__file__))], capture_output=True, text=True)
        self.assertNotEqual(0, result.returncode)
        with tempfile.TemporaryDirectory(prefix="dtmapi-map-") as directory:
            result = subprocess.run([sys.executable, str(Path(function_map.__file__)), "--build-root", directory, "--output-dir", str(Path(directory) / "output")], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse((Path(directory) / "output").exists())
        self.assertEqual({0}, function_map.match_symbol("Owner::Run", [{"fullName": "void Owner::Run()"}], {}, {}))

    def test_function_map_generation_records_actual_fixture_identity(self):
        with tempfile.TemporaryDirectory(prefix="dtmapi-map-complete-") as directory:
            base = Path(directory)
            metadata = base / "baseline/metadata"
            metadata.mkdir(parents=True)
            (metadata / "types.csv").write_text("full_name,namespace\nFixture.Owner,Fixture\n")
            (metadata / "methods.csv").write_text('full_name,type,name\n"void Fixture.Owner::Run()",Fixture.Owner,Run\n')
            (metadata / "calls.csv").write_text("caller_full_name,target_full_name,opcode\n")
            compare.write_json(metadata / "summary.json", {"fixture": True})
            compare.write_json(metadata / "build-info.json", {"game": "Fixture", "steam_build": "999", "branch": "fixture", "assembly": {"sha256": "1" * 64}})
            reports = base / "reports"
            reports.mkdir()
            (reports / "01-fixture.md").write_text("Authored fixture: `Owner.Run`.")
            result = subprocess.run([sys.executable, str(Path(function_map.__file__)), "--build-root", str(metadata.parent), "--reports-dir", str(reports), "--output-dir", str(base / "output")], capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stderr)
            summary = compare.read_json(base / "output/summary.json")["meta"]
            self.assertEqual(("999", "fixture", "1" * 64, 1, 0), (summary["steamBuild"], summary["branch"], summary["assemblySha256"], summary["methodCount"], summary["internalEdgeCount"]))
            self.assertEqual("complete", summary["generation"]["status"])
            for output in summary["generation"]["outputs"]:
                self.assertEqual(output["sha256"], hashlib.sha256((base / "output" / output["path"]).read_bytes()).hexdigest())


if __name__ == "__main__":
    unittest.main()
