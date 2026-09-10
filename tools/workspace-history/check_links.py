"""Compare repository Markdown links/anchors with the pre-migration Git tree.

Existing historical defects are reported separately. Commands, code spans,
external URLs and paths outside this workspace are not rewritten or visited.
"""
from __future__ import annotations

import argparse
import html
import json
import os
from pathlib import Path
import re
import subprocess
import unicodedata
from urllib.parse import unquote

import history


def prose_lines(text: str):
    fence = None
    for number, line in enumerate(text.splitlines(), 1):
        match = re.match(r"^\s{0,3}(`{3,}|~{3,})", line)
        if match:
            marker = match[1]
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
        elif fence is None:
            yield number, line


def links(text: str):
    for number, line in prose_lines(text):
        # Markdown destinations cannot originate in an inline command example.
        line = re.sub(r"(`+)(.+?)\1", "", line)
        reference = re.match(r"^\s{0,3}\[[^\]\n]+\]:\s*(<[^>]+>|\S+)", line)
        if reference:
            yield number, reference[1].strip("<>")
        for match in re.finditer(r"!?\[[^\]\n]*\]\(", line):
            start = match.end()
            if start == len(line):
                continue
            if line[start] == "<":
                end = line.find(">", start + 1)
                if end >= 0:
                    yield number, line[start + 1:end]
                continue
            depth, end = 0, start
            while end < len(line):
                char = line[end]
                if char == "\\" and end + 1 < len(line):
                    end += 2
                    continue
                if char.isspace() or (char == ")" and depth == 0):
                    break
                if char == "(":
                    depth += 1
                elif char == ")":
                    depth -= 1
                end += 1
            if end > start:
                yield number, re.sub(r"\\([()])", r"\1", line[start:end])


def anchors(text: str) -> set[str]:
    result, counts = set(), {}
    previous = ""
    for _, line in prose_lines(text):
        result.update(re.findall(r"<(?:a|h[1-6])\b[^>]*\b(?:id|name)=[\"']([^\"']+)[\"']", line, re.I))
        match = re.match(r"^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$", line)
        heading = match[1] if match else (previous if previous and re.match(r"^\s{0,3}(?:=+|-+)\s*$", line) else None)
        if heading:
            heading = re.sub(r"\[([^\]]+)\]\([^)]*\)", r"\1", heading)
            heading = html.unescape(re.sub(r"<[^>]*>", "", heading)).lower()
            slug = "".join(c for c in heading if c in "-_ " or unicodedata.category(c)[0] not in "PS").replace(" ", "-")
            count = counts.get(slug, 0)
            counts[slug] = count + 1
            result.add(slug if count == 0 else f"{slug}-{count}")
        previous = line.strip()
    return result


def local_target(source: str, url: str):
    if re.match(r"^[A-Za-z][A-Za-z0-9+.-]*:", url) or url.startswith("//"):
        return None
    path, _, anchor = url.partition("#")
    path = unquote(path).replace("\\", "/").split("?", 1)[0]
    if not path:
        target = source
    elif path.startswith("/"):
        target = path.lstrip("/")
    else:
        target = os.path.normpath(str(Path(source).parent / path)).replace("\\", "/")
    if target == ".." or target.startswith("../"):
        return None
    return target, unquote(anchor)


def defects(documents: dict[str, str], names: set[str], root: Path):
    anchor_cache = {}
    for source, text in documents.items():
        for line, url in links(text):
            target = local_target(source, url)
            if target is None:
                continue
            path, anchor = target
            exists = path in names or any(n.startswith(path.rstrip("/") + "/") for n in names) or (root / path).exists()
            reason = "missing-target" if not exists else ""
            if exists and anchor and path in documents:
                if path not in anchor_cache:
                    anchor_cache[path] = anchors(documents[path])
                if anchor not in anchor_cache[path]:
                    reason = "missing-anchor"
            if reason:
                yield {"source": source, "line": line, "url": url, "target": path, "anchor": anchor, "reason": reason}


def git_texts(commit: str, names: list[str]) -> dict[str, str]:
    request = "".join(f"{commit}:{name}\n" for name in names).encode("utf-8")
    output = subprocess.check_output(["git", "cat-file", "--batch"], cwd=history.ROOT, input=request)
    documents, offset = {}, 0
    for name in names:
        end = output.index(b"\n", offset)
        header = output[offset:end].split()
        if len(header) != 3 or header[1] != b"blob":
            raise ValueError(f"Missing baseline Markdown: {name}")
        size = int(header[2])
        documents[name] = output[end + 1:end + 1 + size].decode("utf-8-sig")
        offset = end + size + 2
    return documents


def audit() -> dict:
    root = history.ROOT
    manifest = json.loads(history.MANIFEST.read_text(encoding="utf-8"))
    excluded = (".codex/wiki-maintenance/", "artifacts/")
    baseline = subprocess.check_output(["git", "-c", "core.quotePath=false", "ls-tree", "-r", "--name-only", manifest["baselineCommit"]], cwd=root, encoding="utf-8").splitlines()
    before = git_texts(manifest["baselineCommit"], [p for p in baseline if p.endswith(".md") and not p.startswith(excluded)])
    current = subprocess.check_output(["git", "-c", "core.quotePath=false", "ls-files", "--cached", "--others", "--exclude-standard"], cwd=root, encoding="utf-8").splitlines()
    current = {p for p in current if (root / p).is_file()}
    after = {p: (root / p).read_text(encoding="utf-8-sig") for p in current if p.endswith(".md") and not p.startswith(excluded)}
    old_defects = list(defects(before, set(baseline), root))
    old_keys = {(d["source"], d["target"], d["anchor"], d["reason"]) for d in old_defects}
    originals = {r["current"]: r["source"] for r in history.inventory_rows(manifest)}
    originals.update({r["archivedOriginal"]: r["source"] for r in manifest["files"] if r.get("archivedOriginal")})
    existing, new = [], []
    for defect in defects(after, current, root):
        key = (originals.get(defect["source"], defect["source"]), originals.get(defect["target"], defect["target"]), defect["anchor"], defect["reason"])
        (existing if key in old_keys else new).append(defect)
    return {"baselineCommit": manifest["baselineCommit"], "markdownFiles": len(after), "baselineDefects": len(old_defects), "retainedDefects": len(existing), "newDefects": new, "existingDefects": existing}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path, default=history.SNAPSHOT.parent / "link-audit.json")
    args = parser.parse_args()
    result = audit()
    history.write_json(args.report, result)
    print(json.dumps({k: (len(v) if isinstance(v, list) else v) for k, v in result.items()}))
    print(f"Link report: {args.report}")
    raise SystemExit(1 if result["newDefects"] else 0)
