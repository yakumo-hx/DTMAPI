"""Compare existing local captures. Never discover a game, unpack, or decompile."""
from __future__ import annotations

import argparse
import difflib
import hashlib
import json
import re
import sys
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".writing")
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def child(root: Path, relative: str) -> Path:
    result = (root / relative).resolve()
    if Path(relative).is_absolute() or not result.is_relative_to(root.resolve()):
        raise ValueError(f"Inventory path escapes its input root: {relative}")
    return result


def inventory(build: Path, name: str) -> tuple[dict, dict]:
    path = build / "full-baseline-inventory" / name
    data = path.read_bytes()
    rows = json.loads(data.decode("utf-8-sig"))
    if not isinstance(rows, list):
        raise ValueError(f"Inventory must be an array: {path}")
    result = {}
    for row in rows:
        key = row["path"]
        if key in result or not re.fullmatch(r"[0-9a-fA-F]{64}", row["sha256"]) or row["bytes"] < 0:
            raise ValueError(f"Invalid/duplicate inventory entry: {path}: {key}")
        child(build, key)
        result[key] = row
    return result, {"path": str(path), "sha256": digest(data), "files": len(result)}


def verified_bytes(root: Path, row: dict) -> bytes:
    path = child(root, row["path"])
    data = path.read_bytes()
    if len(data) != row["bytes"] or digest(data).lower() != row["sha256"].lower():
        raise ValueError(f"Input no longer matches its recorded inventory: {path}")
    return data


def file_delta(before: dict, after: dict) -> dict:
    common = before.keys() & after.keys()
    return {"added": sorted(after.keys() - before.keys()), "removed": sorted(before.keys() - after.keys()),
            "changed": sorted(key for key in common if before[key]["sha256"].lower() != after[key]["sha256"].lower()),
            "same": sum(before[key]["sha256"].lower() == after[key]["sha256"].lower() for key in common)}


def decompiler_identity(build: Path, assembly: str):
    stage = build / "stage-receipts" / f"decompile-{assembly}.json"
    if stage.is_file():
        value = read_json(stage)
        if value.get("status") == "complete":
            tool = value.get("inputs", {}).get("tool", {})
            return {"version": tool.get("version"), "packageSha256": tool.get("packageSha256")}
    path = build / "full-baseline-inventory/portable-full-capture-summary.json"
    if path.is_file():
        tool = read_json(path).get("ilSpy") or {}
        if tool.get("version") and tool.get("packageSha256"):
            return {"version": tool["version"], "packageSha256": tool["packageSha256"]}
    return None


def compare_code(before: Path, after: Path, raw_before: dict, raw_after: dict, output: Path) -> list[dict]:
    results = []
    for assembly in ("Assembly-CSharp", "Assembly-CSharp-firstpass"):
        key = f"DolocTown_Data/Managed/{assembly}.dll"
        left, right = raw_before.get(key), raw_after.get(key)
        if left is None or right is None:
            results.append({"assembly": assembly, "status": "source-added-removed-or-absent", "before": left, "after": right})
            continue
        # Only the two relevant DLLs are rehashed, never every capture tree.
        verified_bytes(before / "raw-snapshot/game", left)
        verified_bytes(after / "raw-snapshot/game", right)
        if left["sha256"].lower() == right["sha256"].lower():
            results.append({"assembly": assembly, "status": "identical-assembly-reused", "sha256": left["sha256"],
                            "meaning": "Exact DLL bytes match; no ILSpy invocation or repeated code-tree comparison is needed."})
            continue
        first_tool, second_tool = decompiler_identity(before, assembly), decompiler_identity(after, assembly)
        if not first_tool or first_tool != second_tool:
            results.append({"assembly": assembly, "status": "decompiler-identity-unproven-or-different", "beforeTool": first_tool, "afterTool": second_tool})
            continue
        first, first_receipt = inventory(before, f"{assembly}-decompiled-files.json")
        second, second_receipt = inventory(after, f"{assembly}-decompiled-files.json")
        delta = file_delta(first, second)
        diff_dir = output / "code" / assembly
        diff_dir.mkdir(parents=True, exist_ok=True)
        for path in delta["changed"] + delta["added"] + delta["removed"]:
            old = verified_bytes(before / "decompiled" / assembly, first[path]) if path in first else b""
            new = verified_bytes(after / "decompiled" / assembly, second[path]) if path in second else b""
            patch = difflib.unified_diff(old.decode("utf-8-sig", errors="replace").splitlines(True), new.decode("utf-8-sig", errors="replace").splitlines(True), fromfile=f"before/{path}", tofile=f"after/{path}")
            target = child(diff_dir, path + ".diff")
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("".join(patch), encoding="utf-8")
        results.append({"assembly": assembly, "status": "compared-existing-code", "tool": first_tool,
                        "inputs": [first_receipt, second_receipt], "delta": delta,
                        "integrity": "DLLs and changed/added/removed text verified; unchanged code rows reuse frozen inventories."})
    return results


HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)([^\r\n]*)$", re.M)
REFERENCE = re.compile(r"\{fileID:\s*(-?\d+)(?:,\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*\d+)?\}")


def guid_index(root: Path, rows: dict, wanted: set[str] | None = None) -> dict[str, str | None]:
    index: dict[str, str | None] = {}
    if wanted == set():
        return index
    for key, row in rows.items():
        if not key.endswith(".meta"):
            continue
        # Unity's owning GUID is in the meta header. A bounded comparison reads
        # headers to locate targets, then hashes only meta files it actually uses.
        with child(root, key).open("rb") as handle:
            header = handle.read(1024)
        match = re.search(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$", header)
        if match:
            guid = match[1].decode().lower()
            if wanted is not None and guid not in wanted:
                continue
            verified_bytes(root, row)
            index[guid] = key[:-5] if guid not in index else None
    return index


def yaml_entities(text: str) -> tuple[dict[str, str], dict[str, tuple[str, str]], list[str]]:
    matches = list(HEADER.finditer(text))
    blocks = {match[2]: (match[1], text[match.end(): matches[i + 1].start() if i + 1 < len(matches) else len(text)].strip()) for i, match in enumerate(matches)}
    identities = {}
    game_objects = {}
    transform_owner = {}
    parents = {}
    problems = ["duplicate-fileID"] if len(matches) != len(blocks) else []
    for file_id, (kind, body) in blocks.items():
        name = re.search(r"(?m)^\s*m_Name:\s*(.*)$", body)
        if kind == "1":
            game_objects[file_id] = name[1].strip() if name else "<unnamed>"
        if kind in ("4", "224"):
            owner = re.search(r"m_GameObject:\s*\{fileID:\s*(-?\d+)\}", body)
            father = re.search(r"m_Father:\s*\{fileID:\s*(-?\d+)\}", body)
            if owner:
                transform_owner[file_id] = owner[1]
                parents[owner[1]] = father[1] if father else "0"

    def go_path(file_id: str, visiting: frozenset = frozenset()) -> str:
        if file_id in visiting:
            problems.append("cyclic-transform-hierarchy")
            return "<cyclic>"
        father = parents.get(file_id, "0")
        owner = transform_owner.get(father)
        prefix = go_path(owner, visiting | {file_id}) + "/" if owner else ""
        return prefix + game_objects.get(file_id, "<unknown-game-object>")

    for file_id, (kind, body) in blocks.items():
        if kind == "1":
            identity = "GameObject:" + go_path(file_id)
        else:
            owner = re.search(r"m_GameObject:\s*\{fileID:\s*(-?\d+)\}", body)
            name = re.search(r"(?m)^\s*m_Name:\s*(.*)$", body)
            # External script GUID is resolved later; type/name duplicates remain ambiguous.
            identity = f"class:{kind}|owner:{go_path(owner[1]) if owner and owner[1] != '0' else ''}|name:{name[1].strip() if name else ''}"
        identities[file_id] = identity
    duplicates = {key for key, count in Counter(identities.values()).items() if count > 1}
    for file_id, identity in list(identities.items()):
        if identity in duplicates:
            problems.append("ambiguous-entity:" + identity)
            identities[file_id] = "unresolved-fileID:" + file_id
    return identities, blocks, sorted(set(problems))


def canonical_resource(path: str, data: bytes, guids: dict[str, str | None]) -> tuple[str, list[str]]:
    text = data.decode("utf-8-sig")
    if path.endswith(".json"):
        value = json.loads(text)
        if "/Configs/GenDatas/" in path and isinstance(value, list) and value and all(isinstance(row, dict) for row in value):
            for key in ("id", "name", "Id", "Name"):
                keys = [row.get(key) for row in value]
                if all(isinstance(item, (str, int)) for item in keys) and len(set(map(str, keys))) == len(keys):
                    value = {str(row[key]): row for row in value}
                    break
        return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":")), []
    identities, blocks, problems = yaml_entities(text)

    def reference(match: re.Match) -> str:
        file_id, guid = match[1], match[2]
        if guid and int(guid, 16) != 0:
            target = guids.get(guid.lower())
            if target is None:
                problems.append("unresolved-external-guid:" + guid)
                return match[0]
            # Retain the external subobject fileID: never erase a changed font fallback/asset reference.
            return "{asset:" + target + ", fileID:" + file_id + "}"
        if file_id == "0":
            return "{fileID:0}"
        if file_id in identities:
            return "{entity:" + identities[file_id] + "}"
        problems.append("unresolved-local-fileID:" + file_id)
        return match[0]

    if blocks:
        normalized = []
        headers = list(HEADER.finditer(text))
        flags = {match[2]: match[3].strip() for match in headers}
        for file_id, (kind, body) in blocks.items():
            normalized.append((identities[file_id], kind, flags[file_id], REFERENCE.sub(reference, body)))
        return json.dumps({"preamble": text[:headers[0].start()].strip(), "entities": sorted(normalized)}, ensure_ascii=False), sorted(set(problems))
    if path.endswith(".meta"):
        # These two fields describe the export's identity/time; every reference remains resolved or literal.
        text = re.sub(r"(?m)^guid:\s*[0-9a-fA-F]{32}\s*$", "guid:<self:" + path[:-5] + ">", text)
        text = re.sub(r"(?m)^timeCreated:\s*\d+\s*$", "timeCreated:<export-metadata>", text)
    return REFERENCE.sub(reference, text), sorted(set(problems))


def compare_resources(before: Path, after: Path, output: Path, selected: list[str]) -> dict:
    output = output.resolve()
    first, first_receipt = inventory(before, "asset-ripper-export-files.json")
    second, second_receipt = inventory(after, "asset-ripper-export-files.json")
    raw = file_delta(first, second)
    if selected and any(path not in first and path not in second for path in selected):
        raise ValueError("An explicitly selected asset is absent from both inventories; check its exact exported path.")
    write_json(output / "resources/raw-file-delta.json", {"delta": raw, "before": first, "after": second})
    roots = [before / "asset-ripper-unity-project", after / "asset-ripper-unity-project"]
    paths = selected or raw["changed"]
    loaded = [{}, {}]
    wanted = [set(), set()]
    if selected:
        for path in paths:
            for side, rows in enumerate((first, second)):
                if path in rows:
                    data = verified_bytes(roots[side], rows[path])
                    loaded[side][path] = data
                    wanted[side].update(match.decode().lower() for match in re.findall(rb"guid:\s*([0-9a-fA-F]{32})", data))
    guids = [guid_index(roots[0], first, wanted[0] if selected else None), guid_index(roots[1], second, wanted[1] if selected else None)]
    results = []
    for path in paths:
        if path not in first or path not in second:
            results.append({"path": path, "status": "added-removed-or-not-present"})
            continue
        old = loaded[0][path] if selected else verified_bytes(roots[0], first[path])
        new = loaded[1][path] if selected else verified_bytes(roots[1], second[path])
        record = {"path": path, "beforeSha256": digest(old), "afterSha256": digest(new)}
        try:
            old_text, new_text = old.decode("utf-8-sig"), new.decode("utf-8-sig")
            if "\x00" in old_text or "\x00" in new_text:
                raise UnicodeError("binary input")
            raw_diff = "".join(difflib.unified_diff(old_text.splitlines(True), new_text.splitlines(True), fromfile=f"before/{path}", tofile=f"after/{path}"))
            target = child(output / "resources/raw-diffs", path + ".diff")
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(raw_diff, encoding="utf-8")
            left, left_problems = canonical_resource(path, old, guids[0])
            right, right_problems = canonical_resource(path, new, guids[1])
            unresolved = sorted(set(left_problems + right_problems))
            record.update(status="unresolved" if unresolved else ("stable-entities-and-references-equal" if left == right else "semantic-change"),
                          canonicalEqual=left == right, unresolved=unresolved, rawDiff=str(target.relative_to(output)))
        except (UnicodeError, json.JSONDecodeError):
            record.update(status="binary-or-unparsed", meaning="Original byte hashes retained; no semantic equality claim.")
        results.append(record)
    write_json(output / "resources/semantic-delta.json", results)
    return {"inputs": [first_receipt, second_receipt], "rawCounts": {key: value if isinstance(value, int) else len(value) for key, value in raw.items()},
            "selection": selected or "all-changed-paths", "compared": len(results), "states": dict(Counter(row["status"] for row in results)),
            "meaning": "GUIDs map to exact asset paths; internal fileIDs map to unambiguous entities. External subobject IDs, unknown references and ambiguous entities are never erased. Original diffs remain available."}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--before", required=True, type=Path)
    parser.add_argument("--after", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--scope", choices=("code", "resources", "all"), default="code")
    parser.add_argument("--asset", action="append", default=[], help="Exact exported path; bounds resource semantic analysis.")
    args = parser.parse_args()
    before, after, output = args.before.resolve(), args.after.resolve(), args.output_dir.resolve()
    if before == after:
        parser.error("Choose two different captured baselines.")
    if args.asset and args.scope == "code":
        parser.error("--asset requires --scope resources or all.")
    repo = Path(__file__).resolve().parents[2]
    if output.is_relative_to(repo) and not output.is_relative_to(repo / "references/doloc-town/reverse"):
        parser.error("Derived official diffs must stay in ignored reverse output or an explicit external directory.")
    for root in (before, after):
        if root == output or output in root.parents or any(output.is_relative_to(root / child_name) for child_name in ("raw-snapshot", "decompiled", "asset-ripper-unity-project", "full-baseline-inventory")):
            parser.error("Comparison output must not overwrite input trees or inventories.")
    first, first_receipt = inventory(before, "raw-snapshot-files.json")
    second, second_receipt = inventory(after, "raw-snapshot-files.json")
    summary = {"schemaVersion": 1, "status": "running", "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
               "before": str(before), "after": str(after), "scope": args.scope, "assets": args.asset,
               "tool": {"python": sys.version.split()[0], "generatorSha256": digest(Path(__file__).read_bytes())},
               "inputs": [first_receipt, second_receipt], "rawDelta": file_delta(first, second),
               "sourceBoundary": "Local derived research only; do not publish official code, assets or textual diffs."}
    write_json(output / "comparison-summary.json", summary)
    try:
        if args.scope in ("code", "all"):
            summary["code"] = compare_code(before, after, first, second, output)
        if args.scope in ("resources", "all"):
            summary["resources"] = compare_resources(before, after, output, args.asset)
        summary["status"] = "complete"
    except Exception as error:
        summary.update(status="failed", error=str(error))
        raise
    finally:
        write_json(output / "comparison-summary.json", summary)
    print(json.dumps({"status": summary["status"], "scope": args.scope, "summary": str(output / "comparison-summary.json"),
                      "code": [{"assembly": item["assembly"], "status": item["status"]} for item in summary.get("code", [])]}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
