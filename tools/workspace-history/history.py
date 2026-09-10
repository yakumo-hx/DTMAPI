"""Bounded, resumable document migration for the approved workspace construction.

The manifest tracks location and review coverage, never project/release status.
Raw originals live in ignored local evidence; public text is also in the baseline
Git commit. Reading a file programmatically does not mark it reviewed.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import base64
from datetime import datetime, timezone
from urllib.parse import unquote, quote

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "docs/archive/migrations/20260908-workspace.json"
SNAPSHOT = ROOT / "docs/debug/evidence/WORKSPACE-CONSTRUCTION/20260908/originals"
FROZEN = {
    "docs/architecture/batch6-managed-mod-identity-contract.md",
    "docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md",
    "docs/debug/regressions/smoke-matrix-history-20260804-through-20260809.md",
    "docs/planning/archive/20260601-dtmapi-original-planning-transcript.md",
    "docs/planning/archive/20260613-dtmapi-debug-system-planning-transcript.md",
}


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def text_sha(data: bytes) -> str:
    return sha(data.decode("utf-8-sig").replace("\r\n", "\n").encode("utf-8"))


def inventory_rows(manifest: dict) -> list[dict]:
    return manifest["files"] + manifest.get("attachments", [])


def link_targets(manifest: dict) -> dict[str, str]:
    """Document moves and explicitly recorded source-file moves share link routing."""
    result = {r["source"]: r["current"] for r in inventory_rows(manifest) if r["source"] != r["current"]}
    for old, new in manifest.get("referenceTargets", {}).items():
        safe(ROOT, old)
        if old in result or old == new or not safe(ROOT, new).is_file():
            raise ValueError(f"Invalid relocated reference target: {old}: {new}")
        result[old] = new
    return result


def safe(root: Path, relative: str) -> Path:
    relative_path = Path(relative)
    if relative_path.is_absolute() or ".." in relative_path.parts:
        raise ValueError(f"Expected bounded relative path: {relative}")
    candidate = root / relative_path
    resolved = candidate.resolve()
    if resolved == root.resolve() or root.resolve() not in resolved.parents:
        raise ValueError(f"Path escapes boundary: {relative}")
    node = candidate
    while node != root:
        if node.exists() and getattr(node.lstat(), "st_file_attributes", 0) & 0x400:
            raise ValueError(f"Reparse point refused: {node}")
        node = node.parent
    return candidate


def write_json(path: Path, value) -> None:
    write_bytes(path, (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))


def write_bytes(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + ".pending")
    if temp.exists():
        raise ValueError(f"Pending write exists; inspect/recover it first: {temp}")
    temp.write_bytes(data)
    os.replace(temp, path)


def rewrite_links(data: bytes, old: str, new: str, moves: dict[str, str], mask: bool = False) -> bytes:
    """Rebase actual Markdown links only; preserve code, prose and link titles."""
    text = data.decode("utf-8")
    in_fence = None
    output = []
    inline = re.compile(r'(!?\[[^\]\n]*\]\()(?P<url><[^>\n]+>|[^\s)]+)(?P<tail>[^)\n]*\))')
    reference = re.compile(r'^(\s{0,3}\[[^\]\n]+\]:\s*)(?P<url><[^>\n]+>|\S+)(?P<tail>.*)$')

    def replace(match):
        if mask:
            return match.group(1) + "<LINK-DESTINATION>" + match.group("tail")
        token = match.group("url")
        angled = token.startswith("<") and token.endswith(">")
        url = token[1:-1] if angled else token
        if not url or url.startswith(("#", "/")) or re.match(r"^[A-Za-z][A-Za-z0-9+.-]*:", url):
            return match.group(0)
        path_part, separator, anchor = url.partition("#")
        decoded = unquote(path_part).replace("\\", "/")
        target = (ROOT / old).parent.joinpath(decoded).resolve()
        try:
            relative = target.relative_to(ROOT).as_posix()
        except ValueError:
            return match.group(0)
        mapped = moves.get(relative, relative)
        if old == new and mapped == relative:
            return match.group(0)
        relative_url = os.path.relpath(ROOT / mapped, (ROOT / new).parent).replace("\\", "/")
        relative_url = quote(relative_url, safe="/.-_~") + (separator + anchor if separator else "")
        token = f"<{relative_url}>" if angled else relative_url
        return match.group(1) + token + match.group("tail")

    for line in text.splitlines(keepends=True):
        fence = re.match(r"^\s{0,3}(`{3,}|~{3,})", line)
        if fence:
            marker = fence.group(1)
            if in_fence is None:
                in_fence = marker
            elif marker[0] == in_fence[0] and len(marker) >= len(in_fence):
                in_fence = None
            output.append(line)
        elif in_fence:
            output.append(line)
        else:
            # A whole link inside code is literal. Code *within its label*, e.g.
            # [`PROJECT.md`](...), does not make the destination literal.
            code = [(m.start(), m.end()) for m in re.finditer(r"(`+)(.+?)\1", line)]
            def guarded(match):
                return match.group(0) if any(start <= match.start() < end for start, end in code) else replace(match)
            output.append(reference.sub(guarded, line) if reference.match(line) else inline.sub(guarded, line))
    return "".join(output).encode("utf-8")


def alias_bytes(old: str, new: str) -> bytes:
    relative = os.path.relpath(ROOT / new, (ROOT / old).parent).replace("\\", "/")
    return f"<!-- archived-document -->\n# Historical document\n\n[Read the complete original]({quote(relative, safe='/.-_~')}).\n".encode("utf-8")


def read_selection(selection: Path) -> dict[str, str]:
    def unique_pairs(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError(f"Duplicate migration key: {key}")
            result[key] = value
        return result
    requested = json.loads(selection.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_pairs)
    if not isinstance(requested, dict) or any(not isinstance(k, str) or not isinstance(v, str) for k, v in requested.items()):
        raise ValueError("Migration selection must map source paths to destination paths.")
    return requested


def private_manifest() -> Path:
    return SNAPSHOT.parent / "private-history.json"


def require_ignored(name: str) -> None:
    tracked = subprocess.check_output(["git", "ls-files", "--", name], cwd=ROOT)
    ignored = subprocess.run(["git", "check-ignore", "-q", "--", name], cwd=ROOT, capture_output=True)
    if tracked.strip() or ignored.returncode != 0:
        raise ValueError(f"Private history must remain untracked and ignored: {name}")


def migrate(selection: Path, apply: bool, include_knowledge: bool = False, private: bool = False, active: bool = False, stable_references: Path | None = None, references: bool = False) -> None:
    requested = read_selection(selection)
    manifest_path = private_manifest() if private else MANIFEST
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    rows = {r["source"]: r for r in inventory_rows(manifest)}
    if len(rows) != len(inventory_rows(manifest)):
        raise ValueError("Duplicate original identity in manifest.")
    if len({r["current"] for r in rows.values()}) != len(rows):
        raise ValueError("Duplicate current body in manifest.")
    if references:
        if private or active or stable_references:
            raise ValueError("Reference relocation is a public, link-only operation.")
        existing = manifest.setdefault("referenceTargets", {})
        targets = set(existing.values())
        for old, new in requested.items():
            if old in rows or old in existing or safe(ROOT, old).exists() or not safe(ROOT, new).is_file() or new in targets:
                raise ValueError(f"Conflicting relocated source reference: {old}: {new}")
            # The source is already moved by its engineering change. Verify its
            # original identity in the construction checkpoint before relinking.
            subprocess.run(["git", "cat-file", "-e", f"{manifest['baselineCommit']}:{old}"], cwd=ROOT, check=True, capture_output=True)
            existing[old] = new
            targets.add(new)
        requested = {}
    moves = {}
    for old, new in requested.items():
        if old in FROZEN or rows.get(old, {}).get("archivedOriginal"):
            raise ValueError(f"Frozen anchor stays in place: {old}")
        if old not in rows or rows[old]["current"] != old:
            raise ValueError(f"Missing/already migrated source: {old}")
        source, target = safe(ROOT, old), safe(ROOT, new)
        allowed_destination = new.startswith("docs/archive/")
        if active:
            allowed_destination = not private and old.startswith("docs/reviews/api/native-function-map/") and new.startswith("tools/native-function-map/")
            if not rows[old].get("attachment") and rows[old]["reading"] != "complete":
                raise ValueError(f"Active tool original has not been fully reviewed: {old}")
        if not allowed_destination or target.exists() or new in moves.values():
            raise ValueError(f"Conflicting or non-archive destination: {new}")
        if sha(source.read_bytes()) != rows[old].get("currentSha256", rows[old]["sha256"]):
            raise ValueError(f"Source changed since baseline: {old}; review before migration.")
        if private:
            if not new.startswith("docs/archive/conversations/"):
                raise ValueError(f"Private archive boundary: {new}")
            require_ignored(old)
            require_ignored(new)
            if rows[old]["reading"] != "complete":
                raise ValueError(f"Private original has not been fully reviewed: {old}")
        moves[old] = new

    # The source list is bounded to repository-owned Markdown and selected texts.
    names = ([r["current"] for r in rows.values()] if private else subprocess.check_output(["git", "-c", "core.quotePath=false", "ls-files", "--cached", "--others", "--exclude-standard", "--", "*.md"], cwd=ROOT, text=True, encoding="utf-8").splitlines())
    excluded = (".codex/wiki-maintenance/", "artifacts/") + (() if include_knowledge else ("docs/knowledge/",))
    names = sorted(set(n for n in names if not n.startswith(excluded)))
    link_moves = link_targets(manifest)
    if private:
        public = json.loads(MANIFEST.read_text(encoding="utf-8"))
        link_moves.update(link_targets(public))
    link_moves.update(moves)
    changes = []
    for old in sorted(set(names) | set(moves)):
        path = safe(ROOT, old)
        if not path.is_file() or old in FROZEN:
            continue
        before = path.read_bytes()
        new = moves.get(old, old)
        after = rewrite_links(before, old, new, link_moves) if old.endswith(".md") else before
        if new != old or after != before:
            changes.append({"old": old, "new": new, "before": base64.b64encode(before).decode(), "after": base64.b64encode(after).decode(), "beforeSha256": sha(before), "afterSha256": sha(after)})
    # Frozen inbound links retain a small compatibility route at the old address.
    aliases = {}
    alias_sources = {}
    if stable_references:
        references = json.loads(stable_references.read_text(encoding="utf-8-sig"))
        for old, owners in references.items():
            if private or old not in moves or not isinstance(owners, list) or not owners:
                raise ValueError(f"Invalid stable path reference: {old}")
            for owner in owners:
                text = safe(ROOT, owner).read_text(encoding="utf-8-sig")
                if owner.endswith(".json"):
                    text += "\n" + json.dumps(json.loads(text), ensure_ascii=False)
                if old not in text and old.replace("/", "\\\\") not in text:
                    raise ValueError(f"Stable owner does not reference the old identity: {owner}: {old}")
            aliases[old] = moves[old]
            alias_sources[old] = owners
    for frozen in (() if private else FROZEN):
        path = ROOT / frozen
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8")
        for old, new in moves.items():
            relative = os.path.relpath(ROOT / old, path.parent).replace("\\", "/")
            if relative in text or old in text:
                aliases[old] = new
                alias_sources.setdefault(old, []).append(frozen)
    plan = {"moves": moves, "aliases": aliases, "changes": changes}
    print(json.dumps({"moves": len(moves), "linkFiles": len(changes) - len(moves), "frozenAliases": len(aliases), "apply": apply}))
    if not apply:
        return
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    journal = SNAPSHOT.parent / "migrations" / f"{stamp}.json"
    plan["manifestPath"] = manifest_path.relative_to(ROOT).as_posix()
    plan["manifestBefore"] = base64.b64encode(manifest_path.read_bytes()).decode()
    changed = {item["old"]: item for item in changes}
    current_rows = {row["current"]: row for row in rows.values()}
    for item in changes:
        if item["old"] in current_rows:
            after = base64.b64decode(item["after"])
            current_rows[item["old"]]["currentSha256"] = sha(after)
            if not current_rows[item["old"]].get("attachment"):
                current_rows[item["old"]]["currentTextSha256"] = text_sha(after)
    for old, new in moves.items():
        after = base64.b64decode(changed[old]["after"])
        rows[old]["current"] = new
        rows[old]["currentSha256"] = sha(after)
        if active:
            rows[old]["activeRelocation"] = "Current native-function-map tool; raw construction original preserved separately."
        if old in alias_sources:
            rows[old]["compatibilityReferences"] = sorted(set(alias_sources[old]))
        if not rows[old].get("attachment"):
            rows[old]["currentTextSha256"] = text_sha(after)
        if rows[old].get("disposition") == "pending":
            rows[old]["disposition"] = "archive"
    plan["manifestAfter"] = base64.b64encode((json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode("utf-8")).decode()
    plan["state"] = "applying"
    write_json(journal, plan)
    for item in changes:
        old, new = safe(ROOT, item["old"]), safe(ROOT, item["new"])
        if sha(old.read_bytes()) != item["beforeSha256"]:
            raise ValueError(f"Concurrent edit; recover journal {journal}: {old}")
        write_bytes(new, base64.b64decode(item["after"]))
        if old != new:
            old.unlink()
    for old, new in aliases.items():
        write_bytes(safe(ROOT, old), alias_bytes(old, new))
    write_json(manifest_path, manifest)
    plan["state"] = "complete"
    write_json(journal, plan)
    print(f"Migration journal: {journal}")


def rollback(journal: Path) -> None:
    plan = json.loads(journal.read_text(encoding="utf-8"))
    manifest_path = safe(ROOT, plan["manifestPath"]) if "manifestPath" in plan else MANIFEST
    if manifest_path not in (MANIFEST, private_manifest()):
        raise ValueError("Unknown migration manifest.")
    if manifest_path.read_bytes() not in [base64.b64decode(plan[k]) for k in ("manifestBefore", "manifestAfter")]:
        raise ValueError("The migration manifest has later changes; do not overwrite them.")
    # Validate all current states before changing a single file.
    for item in plan["changes"]:
        target = safe(ROOT, item["new"])
        source = safe(ROOT, item["old"])
        if target.exists() and sha(target.read_bytes()) not in (item["beforeSha256"], item["afterSha256"]):
            raise ValueError(f"Later edit would be overwritten: {target}")
        if not item.get("copy") and source != target and source.exists():
            allowed = [item["beforeSha256"]]
            if item["old"] in plan["aliases"]:
                allowed.append(sha(alias_bytes(item["old"], plan["aliases"][item["old"]])))
            if sha(source.read_bytes()) not in allowed:
                raise ValueError(f"Source conflict: {source}")
    for item in reversed(plan["changes"]):
        source, target = safe(ROOT, item["old"]), safe(ROOT, item["new"])
        if not item.get("copy"):
            write_bytes(source, base64.b64decode(item["before"]))
        if source != target and target.exists():
            target.unlink()
    write_bytes(manifest_path, base64.b64decode(plan["manifestBefore"]))
    plan["state"] = "rolled-back"
    write_json(journal, plan)
    print("Migration rolled back; unrelated files were preserved.")


def preserve_originals(selection: Path, apply: bool) -> None:
    """Archive baseline bodies while leaving a current owner at its address."""
    requested = read_selection(selection)
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    rows = {r["source"]: r for r in manifest["files"]}
    moves = link_targets(manifest)
    changes, targets = [], set()
    for old, new in requested.items():
        row = rows.get(old)
        if row is None or row["current"] != old or row.get("archivedOriginal") or old in FROZEN:
            raise ValueError(f"Not a current, unfrozen owner: {old}")
        if row["reading"] not in {"complete", "authored-complete/generated-checked"}:
            raise ValueError(f"Original has not been reviewed: {old}")
        target = safe(ROOT, new)
        if not new.startswith("docs/archive/") or target.exists() or new in targets:
            raise ValueError(f"Conflicting or non-archive destination: {new}")
        original = safe(SNAPSHOT, old).read_bytes()
        if sha(original) != row["sha256"]:
            raise ValueError(f"Original changed: {old}")
        after = rewrite_links(original, old, new, moves) if old.endswith(".md") else original
        changes.append({"old": old, "new": new, "copy": True, "before": base64.b64encode(original).decode(), "after": base64.b64encode(after).decode(), "beforeSha256": sha(original), "afterSha256": sha(after)})
        targets.add(new)
        row["archivedOriginal"] = new
    print(json.dumps({"originalCopies": len(changes), "apply": apply}))
    if not apply:
        return
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    journal = SNAPSHOT.parent / "migrations" / f"{stamp}.json"
    plan = {"state": "applying", "changes": changes, "aliases": {}, "manifestPath": MANIFEST.relative_to(ROOT).as_posix(), "manifestBefore": base64.b64encode(MANIFEST.read_bytes()).decode(), "manifestAfter": base64.b64encode((json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode()).decode()}
    write_json(journal, plan)
    for item in changes:
        target = safe(ROOT, item["new"])
        if target.exists():
            raise ValueError(f"Concurrent archive creation; recover journal {journal}: {target}")
        write_bytes(target, base64.b64decode(item["after"]))
    write_json(MANIFEST, manifest)
    plan["state"] = "complete"
    write_json(journal, plan)
    print(f"Migration journal: {journal}")


def repair_archive_links(apply: bool) -> None:
    """Recompute only destinations from immutable originals after parser fixes."""
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    moves = link_targets(manifest)
    changes = []
    for row in manifest["files"]:
        target_name = row.get("archivedOriginal") or (row["current"] if row["current"] != row["source"] and not row.get("activeRelocation") else None)
        if not target_name or not row["source"].endswith(".md"):
            continue
        original = safe(SNAPSHOT, row["source"]).read_bytes()
        if sha(original) != row["sha256"]:
            raise ValueError(f"Original changed: {row['source']}")
        before = safe(ROOT, target_name).read_bytes()
        after = rewrite_links(original, row["source"], target_name, moves)
        # Git may have normalized supplemental baseline text. Keep the archive's
        # existing line endings; this repair is only about link destinations.
        old_lines, new_lines = before.splitlines(keepends=True), after.splitlines(keepends=True)
        if len(old_lines) == len(new_lines):
            after = b"".join(new.rstrip(b"\r\n") + old[len(old.rstrip(b"\r\n")):] for old, new in zip(old_lines, new_lines))
        if before == after:
            continue
        if rewrite_links(before, target_name, target_name, {}, mask=True) != rewrite_links(after, target_name, target_name, {}, mask=True):
            raise ValueError(f"Non-link content differs; manual review required: {target_name}")
        changes.append({"old": target_name, "new": target_name, "before": base64.b64encode(before).decode(), "after": base64.b64encode(after).decode(), "beforeSha256": sha(before), "afterSha256": sha(after)})
        if target_name == row["current"]:
            row["currentSha256"] = sha(after)
            row["currentTextSha256"] = text_sha(after)
    print(json.dumps({"archivedLinkRepairs": len(changes), "apply": apply}))
    if not apply or not changes:
        return
    journal = SNAPSHOT.parent / "migrations" / (datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ") + ".json")
    plan = {"state": "applying", "changes": changes, "aliases": {}, "manifestPath": MANIFEST.relative_to(ROOT).as_posix(), "manifestBefore": base64.b64encode(MANIFEST.read_bytes()).decode(), "manifestAfter": base64.b64encode((json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode()).decode()}
    write_json(journal, plan)
    for item in changes:
        target = safe(ROOT, item["new"])
        if sha(target.read_bytes()) != item["beforeSha256"]:
            raise ValueError(f"Concurrent archive edit; recover {journal}: {target}")
        write_bytes(target, base64.b64decode(item["after"]))
    write_json(MANIFEST, manifest)
    plan["state"] = "complete"
    write_json(journal, plan)
    print(f"Migration journal: {journal}")


def classify(path: str) -> str:
    value = path.lower()
    if value.startswith("docs/goals/") or any(x in value for x in (
        "original-planning", "task-ledger", "document-governance", "workflow", "workspace-structure")):
        return "governance"
    if any(x in value for x in ("equipment", "saveload", "save-slot", "save-repair", "save-recovery", "moresaves", "sidecar", "inventory", "backpack", "player-save", "mail")):
        return "persistence"
    if value.startswith("author-docs/") or any(x in value for x in ("reverse", "capture", "function-map", "official-workshop", "json-png", "json-derived")):
        return "research-tools"
    if "/reviews/api/" in value or any(x in value for x in ("native-owner", "public-api", "api-rebuild", "api-audit", "hook-map", "smapi")):
        return "api"
    if any(x in value for x in ("autofish", "zoom", "chest", "animal", "plant", "crop", "mine", "debugconsole", "y-console", "itemdisplay", "oneaction", "actionspeed", "fishbreed", "product")):
        return "products"
    return "runtime"


def snapshot() -> None:
    if MANIFEST.exists() or SNAPSHOT.exists():
        raise ValueError("Baseline already exists; resume it instead of replacing originals.")
    tracked = subprocess.check_output(["git", "-c", "core.quotePath=false", "ls-files"], cwd=ROOT, text=True, encoding="utf-8").splitlines()
    paths = [p for p in tracked if p.startswith(("docs/", "archive/", "author-docs/")) and p.endswith((".md", ".txt"))]
    files = []
    for name in paths:
        data = safe(ROOT, name).read_bytes()
        dest = safe(SNAPSHOT, name)
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        files.append({"source": name, "current": name, "sha256": sha(data), "textSha256": text_sha(data), "bytes": len(data), "domain": classify(name), "reading": "pending", "disposition": "pending", "knowledge": [], "reason": ""})
    private_files = []
    private_root = ROOT / ".codex/conversation-history"
    for path in sorted(private_root.glob("*.md")):
        if path.name in ("README.md", "_TEMPLATE.md"):
            continue
        name = path.relative_to(ROOT).as_posix()
        data = path.read_bytes()
        dest = safe(SNAPSHOT, name)
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(data)
        private_files.append({"source": name, "current": name, "sha256": sha(data), "bytes": len(data), "domain": classify(name), "reading": "pending", "disposition": "retain-private", "knowledge": [], "reason": "Private conversation history stays excluded from Git."})
    required = ["AGENTS.md", "PROJECT.md", "docs/onboarding/current-state.md"]
    manifest = {"format": "dtmapi.workspace-history/v1", "baselineCommit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(), "originals": SNAPSHOT.relative_to(ROOT).as_posix(), "requiredContextCharacters": sum(len((ROOT / p).read_text(encoding="utf-8-sig")) for p in required), "files": files}
    write_json(MANIFEST, manifest)
    write_json(SNAPSHOT.parent / "private-history.json", {"files": private_files})
    print(json.dumps({"publicTexts": len(files), "privateTexts": len(private_files), "publicBytes": sum(r["bytes"] for r in files), "baselineCommit": manifest["baselineCommit"]}))


def extend_snapshot(selection: Path, apply: bool, attachments: bool = False) -> None:
    """Add discovered historical texts from the same Git baseline, not a new one."""
    selected = json.loads(selection.read_text(encoding="utf-8-sig"))
    if attachments:
        if not isinstance(selected, list) or any(not isinstance(e, dict) or not isinstance(e.get("source"), str) or not e.get("purpose") or not e.get("owner") for e in selected):
            raise ValueError("Attachments require source, owner and purpose entries.")
        metadata = {e["source"]: e for e in selected}
        names = [e["source"] for e in selected]
    else:
        names = selected
    if not isinstance(names, list) or any(not isinstance(p, str) for p in names) or len(set(names)) != len(names):
        raise ValueError("Snapshot extension requires a unique array of paths.")
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    known = {r["source"] for r in inventory_rows(manifest)}
    prepared = []
    for name in names:
        if name in known or (not attachments and not name.endswith((".md", ".txt"))):
            raise ValueError(f"Already tracked or non-text source: {name}")
        current = safe(ROOT, name)
        original = safe(SNAPSHOT, name)
        data = subprocess.check_output(["git", "show", f"{manifest['baselineCommit']}:{name}"], cwd=ROOT)
        text_attachment = attachments and Path(name).suffix.lower() in {".py", ".json", ".html", ".js", ".css", ".tsv"}
        identity = text_sha if not attachments or text_attachment else sha
        if identity(current.read_bytes()) != identity(data):
            raise ValueError(f"Discovered source changed since baseline; review separately: {name}")
        snapshot_data = current.read_bytes() if attachments else data
        if original.exists() and original.read_bytes() != snapshot_data:
            raise ValueError(f"Existing original conflicts: {name}")
        prepared.append((name, snapshot_data))
    if apply:
        for name, data in prepared:
            original = safe(SNAPSHOT, name)
            if not original.exists():
                write_bytes(original, data)
            current_data = safe(ROOT, name).read_bytes()
            if attachments:
                row = {"source": name, "current": name, "sha256": sha(data), "bytes": len(data), "attachment": True, "owner": metadata[name]["owner"], "reason": metadata[name]["purpose"]}
                if Path(name).suffix.lower() in {".py", ".json", ".html", ".js", ".css", ".tsv"}:
                    row["textSha256"] = text_sha(data)
                manifest.setdefault("attachments", []).append(row)
            else:
                manifest["files"].append({"source": name, "current": name, "sha256": sha(data), "textSha256": text_sha(data), "currentSha256": sha(current_data), "bytes": len(data), "domain": classify(name), "reading": "pending", "disposition": "pending", "knowledge": [], "reason": "Discovered historical text added from the original Git checkpoint."})
        write_json(MANIFEST, manifest)
    print(json.dumps({"additionalAttachments" if attachments else "additionalTexts": len(prepared), "apply": apply}))


def merge_readings(directory: Path, apply: bool) -> None:
    """Import explicit human/agent full-read decisions, never infer them from I/O."""
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    rows = {r["source"]: r for r in manifest["files"]}
    if len(rows) != len(manifest["files"]):
        raise ValueError("Duplicate original identity in manifest.")
    reviews = {}
    allowed = {"retain-current", "archive", "extract-and-archive", "retain-frozen"}
    reading_states = {"complete", "authored-complete/generated-checked"}
    for path in sorted(directory.glob("*.json")):
        entries = json.loads(path.read_text(encoding="utf-8-sig"))
        if not isinstance(entries, list):
            raise ValueError(f"Reading file must contain an array: {path}")
        for entry in entries:
            source = entry.get("source")
            if source in reviews or source not in rows:
                raise ValueError(f"Duplicate or unknown reviewed identity: {source}")
            if entry.get("sha256") != rows[source]["sha256"]:
                raise ValueError(f"Review refers to a different original: {source}")
            if entry.get("reading") not in reading_states or entry.get("disposition") not in allowed or not entry.get("reason", "").strip():
                raise ValueError(f"Incomplete reading decision: {source}")
            if entry["reading"] == "authored-complete/generated-checked":
                basis = entry.get("reviewBasis", {})
                for field in ("authoredSections", "generatedSections", "evidence", "checks"):
                    values = basis.get(field)
                    if not isinstance(values, list) or not values or any(not isinstance(v, str) or not v.strip() for v in values):
                        raise ValueError(f"Generated-inventory exception lacks {field}: {source}")
            destinations = entry.get("knowledge")
            if not isinstance(destinations, list) or any(not isinstance(p, str) or not safe(ROOT, p).is_file() for p in destinations):
                raise ValueError(f"Missing knowledge/owner destination: {source}")
            reviews[source] = entry
    for source, entry in reviews.items():
        for key in ("reading", "disposition", "knowledge", "reason"):
            rows[source][key] = entry[key]
        if "reviewBasis" in entry:
            rows[source]["reviewBasis"] = entry["reviewBasis"]
    if apply:
        write_json(MANIFEST, manifest)
    print(json.dumps({"reviewDecisions": len(reviews), "total": len(rows), "apply": apply}))


def check_private(require_full: bool = False) -> dict:
    manifest = json.loads(private_manifest().read_text(encoding="utf-8"))
    rows = manifest["files"]
    if len({r["source"] for r in rows}) != len(rows) or len({r["current"] for r in rows}) != len(rows):
        raise ValueError("Duplicate private identity.")
    moves = {r["source"]: r["current"] for r in rows if r["source"] != r["current"]}
    private_move_count = len(moves)
    public = json.loads(MANIFEST.read_text(encoding="utf-8"))
    moves.update(link_targets(public))
    for row in rows:
        require_ignored(row["current"])
        data = safe(SNAPSHOT, row["source"]).read_bytes()
        if sha(data) != row["sha256"]:
            raise ValueError(f"Private original changed: {row['source']}")
        expected = rewrite_links(data, row["source"], row["current"], moves)
        if safe(ROOT, row["current"]).read_bytes() != expected:
            raise ValueError(f"Private observation changed: {row['current']}")
        if require_full and row["reading"] != "complete":
            raise ValueError(f"Private reading pending: {row['source']}")
    return {"privateTexts": len(rows), "privateFullyRead": sum(r["reading"] == "complete" for r in rows), "privateMoved": private_move_count}


def check(require_full: bool = False, include_private: bool = False) -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    rows = inventory_rows(manifest)
    moves = link_targets(manifest)
    missing_originals = [r["source"] for r in rows if not safe(SNAPSHOT, r["source"]).is_file()]
    git_originals = {}
    if missing_originals:
        # One Git process, also usable on CI without private local snapshots.
        request = "".join(f"{manifest['baselineCommit']}:{name}\n" for name in missing_originals).encode("utf-8")
        output = subprocess.check_output(["git", "cat-file", "--batch"], cwd=ROOT, input=request)
        offset = 0
        for name in missing_originals:
            end = output.index(b"\n", offset)
            header = output[offset:end].split()
            if len(header) != 3 or header[1] != b"blob":
                raise ValueError(f"Baseline original unavailable in Git: {name}")
            size = int(header[2])
            git_originals[name] = output[end + 1:end + 1 + size]
            offset = end + size + 2
    seen = set()
    for row in rows:
        if row["source"] in seen:
            raise ValueError(f"Duplicate original: {row['source']}")
        seen.add(row["source"])
        if row.get("activeRelocation") and not (row["source"].startswith("docs/reviews/api/native-function-map/") and row["current"].startswith("tools/native-function-map/")):
            raise ValueError(f"Unapproved active relocation: {row['source']}")
        original = safe(SNAPSHOT, row["source"])
        original_bytes = original.read_bytes() if original.exists() else git_originals[row["source"]]
        # A Git checkout may normalize line endings; the local original is byte-exact.
        original_valid = sha(original_bytes) == row["sha256"] if original.exists() or "textSha256" not in row else text_sha(original_bytes) == row["textSha256"]
        if not original_valid:
            raise ValueError(f"Original changed: {row['source']}")
        if not safe(ROOT, row["current"]).is_file():
            raise ValueError(f"Current document missing: {row['current']}")
        if row["source"] in FROZEN and sha(safe(ROOT, row["current"]).read_bytes()) != row["sha256"]:
            raise ValueError(f"Frozen original changed: {row['source']}")
        if row["current"] != row["source"] and not row.get("activeRelocation") and not row.get("attachment") and text_sha(safe(ROOT, row["current"]).read_bytes()) != row["currentTextSha256"]:
            raise ValueError(f"Archived body changed outside a recorded mechanical migration: {row['current']}")
        if row["current"] != row["source"] and not row.get("activeRelocation"):
            expected = rewrite_links(original_bytes, row["source"], row["current"], moves) if row["source"].endswith(".md") else original_bytes
            identity = sha if row.get("attachment") and (original.exists() or "textSha256" not in row) else text_sha
            if identity(expected) != identity(safe(ROOT, row["current"]).read_bytes()):
                raise ValueError(f"Archived observation changed beyond mechanical links: {row['current']}")
        if row.get("archivedOriginal"):
            archive = safe(ROOT, row["archivedOriginal"])
            expected = rewrite_links(original_bytes, row["source"], row["archivedOriginal"], moves)
            if not archive.is_file() or text_sha(archive.read_bytes()) != text_sha(expected):
                raise ValueError(f"Preserved owner original changed: {row['archivedOriginal']}")
        if row.get("compatibilityReferences"):
            alias = safe(ROOT, row["source"])
            if not alias.is_file() or text_sha(alias.read_bytes()) != text_sha(alias_bytes(row["source"], row["current"])):
                raise ValueError(f"Stable path compatibility route changed: {row['source']}")
        if require_full and not row.get("attachment") and row["reading"] not in {"complete", "authored-complete/generated-checked"}:
            raise ValueError(f"Full reading still pending: {row['source']}")
    print(json.dumps({"texts": len(manifest["files"]), "attachments": len(manifest.get("attachments", [])), "fullyRead": sum(r["reading"] == "complete" for r in manifest["files"]), "authoredReadWithGeneratedInventoryChecked": sum(r["reading"] == "authored-complete/generated-checked" for r in manifest["files"]), "moved": sum(r["current"] != r["source"] for r in rows)}))
    if include_private:
        print(json.dumps(check_private(require_full)))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=["snapshot", "extend-snapshot", "extend-attachments", "check", "migrate", "relocate-tool", "relocate-references", "repair-links", "preserve-originals", "rollback", "merge-readings"])
    parser.add_argument("--selection", type=Path)
    parser.add_argument("--journal", type=Path)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--include-knowledge", action="store_true", help="Relink knowledge pages after their writers finish.")
    parser.add_argument("--readings", type=Path, default=ROOT / "docs/archive/migrations/readings")
    parser.add_argument("--require-full", action="store_true")
    parser.add_argument("--private", action="store_true", help="Migrate the ignored private inventory instead of public history.")
    parser.add_argument("--include-private", action="store_true", help="Also check local private originals and coverage; not a public CI input.")
    parser.add_argument("--stable-references", type=Path, help="Verified old path identities in immutable receipts or independent delivery metadata.")
    args = parser.parse_args()
    if args.operation in ("migrate", "relocate-tool", "relocate-references"):
        if not args.selection:
            parser.error("migrate requires --selection")
        migrate(args.selection, args.apply, args.include_knowledge, args.private, args.operation == "relocate-tool", args.stable_references, args.operation == "relocate-references")
    elif args.operation == "preserve-originals":
        if not args.selection:
            parser.error("preserve-originals requires --selection")
        preserve_originals(args.selection, args.apply)
    elif args.operation == "repair-links":
        repair_archive_links(args.apply)
    elif args.operation == "rollback":
        if not args.journal:
            parser.error("rollback requires --journal")
        rollback(args.journal)
    elif args.operation == "merge-readings":
        merge_readings(args.readings, args.apply)
    elif args.operation in ("extend-snapshot", "extend-attachments"):
        if not args.selection:
            parser.error("extend-snapshot requires --selection")
        extend_snapshot(args.selection, args.apply, args.operation == "extend-attachments")
    elif args.operation == "check":
        check(args.require_full, args.include_private)
    else:
        snapshot()


if __name__ == "__main__":
    main()
