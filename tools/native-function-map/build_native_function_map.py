from __future__ import annotations

import argparse
import csv
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path


DOMAIN_REPORT_RE = re.compile(r"^(\d{2})-(.+)\.md$")
CODE_SPAN_RE = re.compile(r"`([^`\n]+)`")


SKIP_SYMBOL_PARTS = (
    "/",
    "\\",
    ".md",
    ".csv",
    ".json",
    ".cs",
    ".dll",
    ".exe",
    " ",
)


def read_csv(path: Path) -> list[dict[str, str]]:
    if not path.exists():
        return []

    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def read_json(path: Path) -> dict:
    if not path.exists():
        return {}

    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")


def bool_text(value: str) -> bool:
    return str(value).lower() == "true"


def safe_int(value: str | None) -> int:
    try:
        return int(value or 0)
    except ValueError:
        return 0


def normalize_report_title(slug: str) -> str:
    return slug.replace("-", " ").title()


def derive_namespace(type_name: str, type_meta: dict[str, dict[str, str]]) -> str:
    meta = type_meta.get(type_name)
    if meta and meta.get("namespace"):
        return meta["namespace"]

    outer = type_name.split("/", 1)[0]
    if "." in outer:
        return outer.rsplit(".", 1)[0]

    return "(global)"


def short_signature(full_name: str) -> str:
    if "::" not in full_name:
        return full_name

    return full_name.split("::", 1)[1]


def should_keep_symbol(symbol: str) -> bool:
    if not symbol or len(symbol) < 3:
        return False

    if any(part in symbol for part in SKIP_SYMBOL_PARTS):
        return False

    if symbol.startswith(("0x", "-", "#")):
        return False

    return any(ch.isalpha() for ch in symbol)


def extract_domain_reports(reports_dir: Path, repo_root: Path) -> list[dict]:
    reports: list[dict] = []
    for path in sorted(reports_dir.glob("*.md")):
        match = DOMAIN_REPORT_RE.match(path.name)
        if not match:
            continue

        text = path.read_text(encoding="utf-8-sig", errors="replace")
        symbols = []
        for raw in CODE_SPAN_RE.findall(text):
            for piece in re.split(r"[,;]\s*", raw.strip()):
                piece = piece.strip()
                if should_keep_symbol(piece):
                    symbols.append(piece)

        report_id, slug = match.groups()
        reports.append(
            {
                "id": report_id,
                "slug": slug,
                "title": normalize_report_title(slug),
                "path": str(path.relative_to(repo_root).as_posix()),
                "symbols": sorted(set(symbols)),
            }
        )

    return reports


def match_symbol(symbol: str, methods: list[dict], methods_by_full: dict[str, int], type_to_ids: dict[str, list[int]]) -> set[int]:
    matched: set[int] = set()

    if symbol in methods_by_full:
        matched.add(methods_by_full[symbol])
        return matched

    # Full signature fragments, e.g. "System.Void Type::Method(...)".
    if "::" in symbol:
        for index, method in enumerate(methods):
            if symbol in method["full_name"]:
                matched.add(index)
        return matched

    # Class/member tokens, including wildcard variants like ItemDrone* or Equip*.
    if "." in symbol:
        type_part, member_part = symbol.rsplit(".", 1)
        type_part = type_part.strip()
        member_part = member_part.strip()
        wildcard = member_part.endswith("*")
        member_prefix = member_part[:-1] if wildcard else member_part

        for index, method in enumerate(methods):
            method_type = method["type"]
            if not (method_type == type_part or method_type.endswith("." + type_part) or method_type.endswith("/" + type_part)):
                continue

            method_name = method["name"]
            if (wildcard and method_name.startswith(member_prefix)) or method_name == member_part:
                matched.add(index)

        # If the token looks like a property owner, match generated accessors. Field-only state
        # holders are useful details, but should not color every method on a large owner type.
        if not matched:
            for index, method in enumerate(methods):
                method_type = method["type"]
                if not (method_type == type_part or method_type.endswith("." + type_part) or method_type.endswith("/" + type_part)):
                    continue

                if method["name"] in (f"get_{member_part}", f"set_{member_part}"):
                    matched.add(index)
        return matched

    # Type-only tokens.
    type_wildcard = symbol.endswith("*")
    type_prefix = symbol[:-1] if type_wildcard else symbol
    for type_name, ids in type_to_ids.items():
        short_type = type_name.rsplit(".", 1)[-1]
        if type_wildcard:
            if short_type.startswith(type_prefix) or type_name.endswith("." + type_prefix):
                matched.update(ids)
        elif type_name == symbol or short_type == symbol or type_name.endswith("." + symbol) or type_name.endswith("/" + symbol):
            if len(ids) > 80:
                continue
            matched.update(ids)

    return matched


def role_hint(method: dict) -> str:
    name = method["name"]
    if method["isConstructor"]:
        return "Constructor / native instance setup path."
    if name.startswith("get_"):
        return "Getter / snapshot-style read path."
    if name.startswith("set_"):
        return "Setter / native state mutation path."
    if name.startswith("On") or name.startswith("_On"):
        return "Lifecycle or event callback."
    if "Update" in name:
        return "Update/tick path."
    if "Load" in name:
        return "Load or rehydrate path."
    if "Save" in name:
        return "Save/persistence path."
    if "Render" in name:
        return "Render/visual path."
    if "Create" in name or "Generate" in name:
        return "Creation/generation path."
    if "Remove" in name or "Dispose" in name or "Unload" in name:
        return "Cleanup/removal path."
    if "Try" in name:
        return "Guarded operation / partial failure path."
    return "Native method; inspect owner reports and callers before API work."


def main() -> int:
    parser = argparse.ArgumentParser(description="Build the Doloc Town native function map workbench data.")
    parser.add_argument(
        "--build-root",
        default="references/doloc-town/reverse/builds/23465763_workshop_38581E",
        help="Reverse build root containing metadata/ and maps/index/.",
    )
    parser.add_argument(
        "--reports-dir",
        default="docs/reviews/api/native-owner-domains",
        help="Native-owner domain reports used for research coverage tags.",
    )
    parser.add_argument(
        "--output-dir",
        default="docs/reviews/api/native-function-map/data",
        help="Output directory for generated JSON files.",
    )
    args = parser.parse_args()

    repo_root = Path.cwd()
    build_root = (repo_root / args.build_root).resolve()
    reports_dir = (repo_root / args.reports_dir).resolve()
    output_dir = (repo_root / args.output_dir).resolve()

    metadata_dir = build_root / "metadata"
    maps_index_dir = build_root / "maps" / "index"
    diffs_dir = build_root / "diffs"

    type_rows = read_csv(metadata_dir / "types.csv")
    method_rows = read_csv(metadata_dir / "methods.csv")
    call_rows = read_csv(metadata_dir / "calls.csv")
    summary = read_json(metadata_dir / "summary.json")
    build_info = read_json(metadata_dir / "build-info.json")

    type_meta = {row["full_name"]: row for row in type_rows}

    methods: list[dict] = []
    methods_by_full: dict[str, int] = {}
    type_to_ids: dict[str, list[int]] = defaultdict(list)

    added_methods = {row["full_name"] for row in read_csv(diffs_dir / "methods-added.csv")}
    changed_methods = {row["full_name"] for row in read_csv(diffs_dir / "methods-body-size-changed.csv")}

    for index, row in enumerate(method_rows):
        type_name = row["type"]
        method = {
            "id": index,
            "token": row.get("token", ""),
            "type": type_name,
            "name": row["name"],
            "fullName": row["full_name"],
            "signature": short_signature(row["full_name"]),
            "returnType": row.get("return_type", ""),
            "parameters": row.get("parameters", ""),
            "visibility": row.get("visibility", ""),
            "isStatic": bool_text(row.get("is_static", "")),
            "isVirtual": bool_text(row.get("is_virtual", "")),
            "isAbstract": bool_text(row.get("is_abstract", "")),
            "isConstructor": bool_text(row.get("is_constructor", "")),
            "hasBody": bool_text(row.get("has_body", "")),
            "bodySize": safe_int(row.get("body_code_size")),
            "namespace": derive_namespace(type_name, type_meta),
            "systems": [],
            "domains": [],
            "change": "added" if row["full_name"] in added_methods else ("changed" if row["full_name"] in changed_methods else ""),
            "callsOut": 0,
            "callsIn": 0,
            "externalCallsOut": 0,
            "roleHint": "",
            "coverage": "unmapped",
        }
        method["roleHint"] = role_hint(method)
        methods.append(method)
        methods_by_full[row["full_name"]] = index
        type_to_ids[type_name].append(index)

    systems = []
    for path in sorted(maps_index_dir.glob("*-methods.csv")):
        system_id = path.name[: -len("-methods.csv")]
        system_rows = read_csv(path)
        matched_count = 0
        for row in system_rows:
            method_id = methods_by_full.get(row.get("full_name", ""))
            if method_id is None:
                continue
            if system_id not in methods[method_id]["systems"]:
                methods[method_id]["systems"].append(system_id)
            matched_count += 1
        systems.append({"id": system_id, "name": system_id.replace("_", " "), "methodCount": matched_count})

    domain_reports = extract_domain_reports(reports_dir, repo_root)
    domains = []
    method_domain_symbols: dict[int, list[str]] = defaultdict(list)

    for report in domain_reports:
        matched_ids: set[int] = set()
        matched_symbols = []
        for symbol in report["symbols"]:
            ids = match_symbol(symbol, methods, methods_by_full, type_to_ids)
            if ids:
                matched_symbols.append({"symbol": symbol, "methodCount": len(ids)})
                matched_ids.update(ids)
                for method_id in ids:
                    method_domain_symbols[method_id].append(symbol)

        for method_id in matched_ids:
            if report["id"] not in methods[method_id]["domains"]:
                methods[method_id]["domains"].append(report["id"])

        domains.append(
            {
                "id": report["id"],
                "title": report["title"],
                "slug": report["slug"],
                "path": report["path"],
                "symbolCount": len(report["symbols"]),
                "matchedSymbolCount": len(matched_symbols),
                "matchedMethodCount": len(matched_ids),
                "matchedSymbols": matched_symbols[:250],
            }
        )

    edge_counter: Counter[tuple[int, int]] = Counter()
    opcode_counter: dict[tuple[int, int], Counter[str]] = defaultdict(Counter)
    external_call_count = 0

    for row in call_rows:
        source_id = methods_by_full.get(row.get("caller_full_name", ""))
        if source_id is None:
            continue

        target_id = methods_by_full.get(row.get("target_full_name", ""))
        if target_id is None:
            external_call_count += 1
            methods[source_id]["externalCallsOut"] += 1
            continue

        key = (source_id, target_id)
        edge_counter[key] += 1
        opcode_counter[key][row.get("opcode", "")] += 1
        methods[source_id]["callsOut"] += 1
        methods[target_id]["callsIn"] += 1

    links = [
        {
            "source": source,
            "target": target,
            "count": count,
            "opcodes": dict(opcode_counter[(source, target)].most_common()),
        }
        for (source, target), count in edge_counter.items()
    ]

    for method in methods:
        method["systems"].sort()
        method["domains"].sort()
        method["domainSymbols"] = sorted(set(method_domain_symbols.get(method["id"], [])))[:20]
        if method["domains"]:
            method["coverage"] = "native-owner"
        elif method["systems"]:
            method["coverage"] = "system-map"

    coverage_counts = Counter(method["coverage"] for method in methods)
    namespace_counts = Counter(method["namespace"] for method in methods)
    visibility_counts = Counter(method["visibility"] for method in methods)
    change_counts = Counter(method["change"] or "unchanged" for method in methods)

    system_stats = []
    for system in systems:
        method_ids = [method["id"] for method in methods if system["id"] in method["systems"]]
        researched = sum(1 for method_id in method_ids if methods[method_id]["domains"])
        system_stats.append(
            {
                **system,
                "researchedMethodCount": researched,
                "coveragePercent": round((researched / len(method_ids) * 100) if method_ids else 0, 2),
            }
        )

    domain_stats = []
    for domain in domains:
        domain_methods = [method for method in methods if domain["id"] in method["domains"]]
        internal_edges = sum(1 for link in links if domain["id"] in methods[link["source"]]["domains"] or domain["id"] in methods[link["target"]]["domains"])
        domain_stats.append(
            {
                **domain,
                "internalEdgeTouchCount": internal_edges,
                "coveragePercent": round((len(domain_methods) / len(methods) * 100) if methods else 0, 2),
            }
        )

    meta = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "sourceBuildRoot": str(build_root.relative_to(repo_root)).replace("\\", "/"),
        "reportsDir": str(reports_dir.relative_to(repo_root)).replace("\\", "/"),
        "game": build_info.get("game", "Doloc Town"),
        "steamBuild": build_info.get("steam_build", ""),
        "branch": build_info.get("branch", ""),
        "assemblySha256": build_info.get("assembly", {}).get("sha256", ""),
        "methodCount": len(methods),
        "typeCount": len(type_rows),
        "metadataCallCount": len(call_rows),
        "internalEdgeCount": len(links),
        "externalCallCount": external_call_count,
        "coverageCounts": dict(coverage_counts),
        "visibilityCounts": dict(visibility_counts),
        "changeCounts": dict(change_counts),
        "namespaceTop": [{"name": name, "methodCount": count} for name, count in namespace_counts.most_common(40)],
        "sourceSummary": summary,
        "note": "Generated from symbol metadata, call metadata, map indexes, and DTMAPI-authored reports. No decompiled method bodies are copied.",
    }

    summary_json = {
        "meta": meta,
        "systems": sorted(system_stats, key=lambda item: item["methodCount"], reverse=True),
        "domains": sorted(domain_stats, key=lambda item: item["id"]),
    }

    write_json(output_dir / "summary.json", summary_json)
    write_json(output_dir / "methods.json", methods)
    write_json(output_dir / "links.json", links)

    print(f"Wrote {len(methods)} methods and {len(links)} internal links to {output_dir}")
    print(f"Native-owner covered methods: {coverage_counts.get('native-owner', 0)}")
    print(f"System-map covered methods: {coverage_counts.get('system-map', 0)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
