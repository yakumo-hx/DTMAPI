#!/usr/bin/env python3
"""Audit the current official tutorials against the installed official example package."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable


TOOL_VERSION = "1.0.2"
FILENAME_RE = re.compile(r"([A-Za-z0-9_$-]+\.json)\b", re.IGNORECASE)
OUTLINE_PREFIX_RE = re.compile(r"^\s*\d+(?:\.\d+)+\s*")
NUMBERED_HEADING_RE = re.compile(r"^\s*\d+[.)]\s+")
PLACEHOLDER_RE = re.compile(r"\b(?:x{4,}|your[_-]?id|example[_-]?id)\b", re.IGNORECASE)
NORMALIZE_RE = re.compile(r"[\s（）()&·、_\-]+")

# Prefixes deliberately avoid the official package's translated suffixes. This
# maps public tutorial identity, not a copied package layout contract.
TUTORIAL_SAMPLE_MAP: dict[str, tuple[str, str]] = {
    "01 主角（本体&帽子&工具）": ("01 美化模组示例", "01 主角"),
    "02 NPC": ("01 美化模组示例", "02 NPC"),
    "03 小动物": ("01 美化模组示例", "03 小动物"),
    "04 道具": ("01 美化模组示例", "04 道具"),
    "05 设备": ("01 美化模组示例", "05 设备"),
    "06 资源": ("01 美化模组示例", "06 资源"),
    "07 建筑": ("01 美化模组示例", "07 建筑"),
    "08 平台": ("01 美化模组示例", "08 平台"),
    "09 载具": ("01 美化模组示例", "09 载具"),
    "01 新增道具": ("02 基础内容模组示例", "01 新增道具"),
    "02 新增帽子": ("02 基础内容模组示例", "02 新增帽子"),
    "03 新增装饰设备": ("02 基础内容模组示例", "03 新增设备"),
    "04 新增配方": ("02 基础内容模组示例", "04 新增配方"),
    "05 新增商店道具": ("02 基础内容模组示例", "05 新增商店道具"),
    "06 综合案例一（新增道具）": ("02 基础内容模组示例", "06 综合案例一"),
    "07 综合案例二（新增帽子）": ("02 基础内容模组示例", "07 综合案例二"),
    "08 综合案例三（新增设备）": ("02 基础内容模组示例", "08 综合案例三"),
    "09 综合案例四（新增墙纸）": ("02 基础内容模组示例", "09 综合案例四"),
    "10 综合案例五（新增平台）": ("02 基础内容模组示例", "10 综合案例五"),
    "01 新增作物（种子+果实）": ("03 进阶内容模组示例", "01 新增作物"),
    "02 新增鱼": ("03 进阶内容模组示例", "02 新增鱼"),
    "03 新增料理": ("03 进阶内容模组示例", "03 新增料理"),
    "04 新增资源": ("03 进阶内容模组示例", "04 新增资源"),
    "05 新增植被": ("03 进阶内容模组示例", "05 新增植被"),
    "06 综合案例一（新增作物）": ("03 进阶内容模组示例", "06 综合案例一"),
    "07 综合案例二（新增鱼)": ("03 进阶内容模组示例", "07 综合案例二"),
    "08 综合案例三（新增资源）": ("03 进阶内容模组示例", "08 综合案例三"),
    "01 文本本地化": ("04 其他内容示例", "01 文本本地化"),
    "02 道具数据模板": ("04 其他内容示例", "02 道具数据模板"),
    "03 备用贴图": ("04 其他内容示例", "03 备用贴图"),
    "04 掉落上限、保底配置": ("04 其他内容示例", "04 掉落上限"),
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--snapshot", type=Path, required=True)
    parser.add_argument("--official-example", type=Path, required=True)
    parser.add_argument("--out-json", type=Path, required=True)
    parser.add_argument("--out-matrix", type=Path, required=True)
    parser.add_argument("--out-runtime-expectations", type=Path, required=True)
    return parser.parse_args()


def utc_now() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def normalized(value: str) -> str:
    return NORMALIZE_RE.sub("", value).lower()


def filename_from_heading(value: str) -> str:
    # Feishu headings sometimes concatenate an outline such as ``1.1`` with
    # the filename. Strip only a leading outline; digits inside real filenames
    # remain untouched.
    without_outline = OUTLINE_PREFIX_RE.sub("", value)
    without_outline = NUMBERED_HEADING_RE.sub("", without_outline)
    match = FILENAME_RE.search(without_outline)
    return match.group(1) if match else ""


def find_prefixed_directory(root: Path, prefix: str) -> Path | None:
    expected = normalized(prefix)
    candidates = [path for path in root.iterdir() if path.is_dir() and normalized(path.name).startswith(expected)]
    return sorted(candidates, key=lambda path: (len(path.name), path.name))[0] if candidates else None


def resolve_sample_directory(content_root: Path, prefixes: tuple[str, str]) -> Path | None:
    parent = find_prefixed_directory(content_root, prefixes[0])
    return find_prefixed_directory(parent, prefixes[1]) if parent else None


def is_ignored_template_path(path: Path, content_root: Path) -> bool:
    return "_IGNORE" in path.relative_to(content_root).parts


def strip_json_comments(value: str) -> str:
    output: list[str] = []
    index = 0
    in_string = False
    escaped = False
    while index < len(value):
        char = value[index]
        if in_string:
            output.append(char)
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            index += 1
            continue
        if char == '"':
            in_string = True
            output.append(char)
            index += 1
            continue
        if char == "/" and index + 1 < len(value) and value[index + 1] == "/":
            index += 2
            while index < len(value) and value[index] not in "\r\n":
                index += 1
            continue
        if char == "/" and index + 1 < len(value) and value[index + 1] == "*":
            index += 2
            while index + 1 < len(value) and value[index : index + 2] != "*/":
                index += 1
            index += 2
            continue
        output.append(char)
        index += 1
    return "".join(output)


def parse_jsonc(value: str) -> tuple[str, Any | None, str]:
    if PLACEHOLDER_RE.search(value):
        return "placeholder", None, "contains an explicit tutorial placeholder"
    try:
        return "valid", json.loads(strip_json_comments(value)), ""
    except Exception as exc:  # JSON reports exact line/column in the durable output.
        return "invalid", None, str(exc)


def top_level_ids(value: Any) -> list[str]:
    records = value if isinstance(value, list) else [value]
    return sorted(
        {
            str(record["id"])
            for record in records
            if isinstance(record, dict) and record.get("id") is not None
        }
    )


def write_tsv(path: Path, fields: list[str], rows: Iterable[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            writer.writerow({field: row.get(field, "") for field in fields})


def inspect_sample_json(content_root: Path) -> tuple[list[dict[str, Any]], dict[Path, Any]]:
    records: list[dict[str, Any]] = []
    parsed_by_path: dict[Path, Any] = {}
    for path in sorted(content_root.rglob("*.json")):
        relative = path.relative_to(content_root).as_posix()
        try:
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            parsed_by_path[path] = value
            records.append(
                {
                    "path": relative,
                    "table": path.name,
                    "status": "valid",
                    "error": "",
                    "ids": top_level_ids(value),
                    "sha256": sha256_file(path),
                }
            )
        except Exception as exc:
            records.append(
                {
                    "path": relative,
                    "table": path.name,
                    "status": "invalid",
                    "error": str(exc),
                    "ids": [],
                    "sha256": sha256_file(path),
                }
            )
    return records, parsed_by_path


def main() -> int:
    args = parse_args()
    snapshot = args.snapshot.resolve()
    official_example = args.official_example.resolve()
    content_root = official_example / "Content"
    manifest_path = snapshot / "manifest.json"
    info_path = official_example / "info.json"
    workshop_path = official_example / "workshop.json"
    for required in (manifest_path, info_path, workshop_path, content_root):
        if not required.exists():
            raise SystemExit(f"required input missing: {required}")

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    info = json.loads(info_path.read_text(encoding="utf-8-sig"))
    workshop = json.loads(workshop_path.read_text(encoding="utf-8-sig"))
    pages = {str(page["title"]): page for page in manifest.get("pages", [])}
    sample_json, parsed_sample = inspect_sample_json(content_root)
    sample_record_by_path = {record["path"]: record for record in sample_json}

    code_records: list[dict[str, Any]] = []
    tutorial_rows: list[dict[str, Any]] = []
    runtime_origins: dict[tuple[str, str], list[str]] = defaultdict(list)

    for title, prefixes in TUTORIAL_SAMPLE_MAP.items():
        page = pages.get(title)
        sample_dir = resolve_sample_directory(content_root, prefixes)
        sample_relative = sample_dir.relative_to(content_root).as_posix() if sample_dir else ""
        sample_json_paths = sorted(sample_dir.rglob("*.json")) if sample_dir else []
        sample_png_paths = sorted(sample_dir.rglob("*.png")) if sample_dir else []
        sample_failures = [
            sample_record_by_path[path.relative_to(content_root).as_posix()]
            for path in sample_json_paths
            if sample_record_by_path[path.relative_to(content_root).as_posix()]["status"] != "valid"
        ]
        for path in sample_json_paths:
            if is_ignored_template_path(path, content_root):
                continue
            value = parsed_sample.get(path)
            for item_id in top_level_ids(value):
                runtime_origins[(path.name, item_id)].append(path.relative_to(content_root).as_posix())

        page_codes = (page or {}).get("code_blocks") or []
        parse_errors = 0
        incomplete_lines = 0
        unmatched_files = 0
        id_mismatches = 0
        for code in page_codes:
            code_path = snapshot / str(code.get("path") or "")
            text = code_path.read_text(encoding="utf-8") if code_path.is_file() else ""
            parse_status, parsed_code, parse_error = parse_jsonc(text)
            if parse_status == "invalid":
                parse_errors += 1
            if not code.get("line_complete"):
                incomplete_lines += 1
            heading = str(code.get("heading") or "")
            filename = filename_from_heading(heading)
            matches = [path for path in sample_json_paths if path.name.lower() == filename.lower()] if filename else []
            if filename and not matches:
                unmatched_files += 1
            documented_ids = top_level_ids(parsed_code)
            sample_ids = sorted({item_id for path in matches for item_id in top_level_ids(parsed_sample.get(path))})
            missing_ids = sorted(set(documented_ids) - set(sample_ids)) if matches else []
            if missing_ids:
                id_mismatches += 1
            code_records.append(
                {
                    "tutorial": title,
                    "token": str((page or {}).get("token") or ""),
                    "index": int(code.get("index") or 0),
                    "heading": heading,
                    "caption": str(code.get("caption") or ""),
                    "language": str(code.get("language") or ""),
                    "path": str(code.get("path") or ""),
                    "line_first": int(code.get("line_first") or 0),
                    "line_last": int(code.get("line_last") or 0),
                    "line_count": int(code.get("line_count") or 0),
                    "line_complete": bool(code.get("line_complete")),
                    "parse_status": parse_status,
                    "parse_error": parse_error,
                    "filename": filename,
                    "sample_matches": [path.relative_to(content_root).as_posix() for path in matches],
                    "documented_ids": documented_ids,
                    "sample_ids": sample_ids,
                    "missing_ids_in_sample": missing_ids,
                }
            )

        if page is None or sample_dir is None or sample_failures or parse_errors or incomplete_lines:
            static_status = "fail"
        elif unmatched_files or id_mismatches:
            static_status = "warning"
        elif not page_codes:
            static_status = "visual-or-reference-only"
        else:
            static_status = "pass"
        tutorial_rows.append(
            {
                "tutorial": title,
                "token": str((page or {}).get("token") or ""),
                "sample_directory": sample_relative,
                "sample_found": bool(sample_dir),
                "sample_json": len(sample_json_paths),
                "sample_png": len(sample_png_paths),
                "sample_json_failures": len(sample_failures),
                "code_blocks": len(page_codes),
                "incomplete_code_blocks": incomplete_lines,
                "invalid_code_blocks": parse_errors,
                "unmatched_code_filenames": unmatched_files,
                "code_id_mismatches": id_mismatches,
                "static_status": static_status,
                "runtime_status": "not-run",
                "player_status": "not-run",
            }
        )

    duplicate_ids = [
        {"table": table, "id": item_id, "origins": sorted(origins)}
        for (table, item_id), origins in sorted(runtime_origins.items())
        if len(origins) > 1
    ]
    runtime_rows = [
        {
            "table": table,
            "id": item_id,
            "origin_count": len(origins),
            "origins": "|".join(sorted(origins)),
            "runtime_status": "not-run",
        }
        for (table, item_id), origins in sorted(runtime_origins.items())
    ]

    result = {
        "schema_version": 1,
        "tool": "audit-official-tutorials.py",
        "tool_version": TOOL_VERSION,
        "generated_at": utc_now(),
        "snapshot": {
            "tool_version": manifest.get("tool_version"),
            "complete": bool(manifest.get("complete")),
            "page_count": int(manifest.get("page_count") or 0),
            "code_blocks": sum(len(page.get("code_blocks") or []) for page in manifest.get("pages", [])),
        },
        "official_example": {
            "workshop_id": workshop.get("workshop_id"),
            "name": info.get("name"),
            "version": info.get("version"),
            "json_files": len(sample_json),
            "ignored_template_json_files": sum(
                is_ignored_template_path(content_root / record["path"], content_root)
                for record in sample_json
            ),
            "png_files": sum(1 for _ in content_root.rglob("*.png")),
            "invalid_json_files": sum(record["status"] != "valid" for record in sample_json),
            "info_sha256": sha256_file(info_path),
            "workshop_sha256": sha256_file(workshop_path),
        },
        "summary": {
            "tutorials": len(tutorial_rows),
            "static_pass": sum(row["static_status"] == "pass" for row in tutorial_rows),
            "static_warning": sum(row["static_status"] == "warning" for row in tutorial_rows),
            "static_fail": sum(row["static_status"] == "fail" for row in tutorial_rows),
            "visual_or_reference_only": sum(row["static_status"] == "visual-or-reference-only" for row in tutorial_rows),
            "tutorial_code_blocks": len(code_records),
            "invalid_tutorial_code_blocks": sum(row["parse_status"] == "invalid" for row in code_records),
            "placeholder_code_blocks": sum(row["parse_status"] == "placeholder" for row in code_records),
            "sample_duplicate_table_ids": len(duplicate_ids),
            "runtime_expectations": len(runtime_rows),
        },
        "tutorials": tutorial_rows,
        "code_blocks": code_records,
        "sample_json": sample_json,
        "duplicate_table_ids": duplicate_ids,
    }
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    write_tsv(
        args.out_matrix,
        [
            "tutorial", "token", "sample_directory", "sample_found", "sample_json", "sample_png",
            "sample_json_failures", "code_blocks", "incomplete_code_blocks", "invalid_code_blocks",
            "unmatched_code_filenames", "code_id_mismatches", "static_status", "runtime_status", "player_status",
        ],
        tutorial_rows,
    )
    write_tsv(
        args.out_runtime_expectations,
        ["table", "id", "origin_count", "origins", "runtime_status"],
        runtime_rows,
    )
    print(json.dumps(result["summary"], ensure_ascii=False))
    return 0 if result["summary"]["static_fail"] == 0 else 2


if __name__ == "__main__":
    raise SystemExit(main())
