#!/usr/bin/env python3
"""Create a dated, browser-rendered snapshot of the public Doloc Town Mod guide."""

from __future__ import annotations

import argparse
import csv
import hashlib
import html
import json
import re
import shutil
import sys
import time
from collections import deque
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable
from urllib.parse import urljoin, urlsplit

from playwright.sync_api import BrowserContext, Locator, Page, Response, sync_playwright


TOOL_VERSION = "1.1.0"
WIKI_HOST = "ka7deoo0opr.feishu.cn"
DEFAULT_ROOT_URL = f"https://{WIKI_HOST}/wiki/ElmCwXsRCi7OPvkOh8JcvXK9nfc"
EXPECTED_ROOT_TITLE = "《多洛可小镇》创意工坊模组制作说明"
WIKI_TOKEN_RE = re.compile(r"/wiki/([A-Za-z0-9]+)")
ZERO_WIDTH_RE = re.compile(r"[\u200b\u200c\u200d\ufeff]")
SAFE_NAME_RE = re.compile(r"[^A-Za-z0-9._-]+")
SHEET_ID_RE = re.compile(r"(?:^|\s)sheet-id-([^\s]+)")
SOURCE_MODIFIED_RE = re.compile(r"\d{1,2}月\d{1,2}日修改")
DOCUMENT_IMAGE_URL_MARKERS = (
    "/space/api/box/stream/",
    "internal-api-drive-stream.feishu.cn/",
    "drive.feishucdn.com/object/",
    "s1-imfile.feishucdn.com/",
    "s3-imfile.feishucdn.com/",
)
SHEET_API_URL_MARKERS = (
    "/space/api/v3/sheet/client_vars",
    "/space/api/v3/sheet/block",
    "/space/api/v3/sheet/resource",
    "/space/api/v2/sheet/block",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out-dir", type=Path, required=True)
    parser.add_argument("--root-url", default=DEFAULT_ROOT_URL)
    parser.add_argument("--previous-manifest", type=Path)
    parser.add_argument("--chrome", type=Path)
    parser.add_argument("--max-pages", type=int, default=120)
    parser.add_argument("--page-timeout-ms", type=int, default=60_000)
    parser.add_argument("--settle-ms", type=int, default=1_500)
    parser.add_argument("--delay-ms", type=int, default=150)
    parser.add_argument("--headed", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--resume", action="store_true")
    return parser.parse_args()


def utc_now() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def clean_text(value: str | None) -> str:
    text = ZERO_WIDTH_RE.sub("", value or "")
    text = text.replace("\xa0", " ")
    lines = [line.rstrip() for line in text.splitlines()]
    while lines and not lines[0].strip():
        lines.pop(0)
    while lines and not lines[-1].strip():
        lines.pop()
    return "\n".join(lines)


def single_line(value: str | None) -> str:
    return re.sub(r"\s+", " ", clean_text(value)).strip()


def canonical_wiki_url(value: str, base_url: str = DEFAULT_ROOT_URL) -> str | None:
    parsed = urlsplit(urljoin(base_url, value))
    if parsed.netloc.lower() != WIKI_HOST:
        return None
    match = WIKI_TOKEN_RE.search(parsed.path)
    if not match:
        return None
    return f"https://{WIKI_HOST}/wiki/{match.group(1)}"


def token_from_url(value: str) -> str:
    match = WIKI_TOKEN_RE.search(urlsplit(value).path)
    if not match:
        raise ValueError(f"not a canonical Wiki URL: {value}")
    return match.group(1)


def safe_component(value: str, fallback: str) -> str:
    safe = SAFE_NAME_RE.sub("-", value).strip("-.")
    return (safe[:100] or fallback).lower()


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def write_text(path: Path, value: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8", newline="\n")


def relative(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def discover_chrome(explicit: Path | None) -> Path:
    candidates: list[Path] = []
    if explicit:
        candidates.append(explicit)
    candidates.extend(
        [
            Path(r"C:\Program Files\Google\Chrome\Application\chrome.exe"),
            Path(r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"),
        ]
    )
    path_candidate = shutil.which("chrome") or shutil.which("chrome.exe")
    if path_candidate:
        candidates.append(Path(path_candidate))
    for candidate in candidates:
        if candidate.is_file():
            return candidate.resolve()
    raise FileNotFoundError("Google Chrome was not found; pass --chrome <path>")


def prepare_output(path: Path, overwrite: bool) -> None:
    if path.exists() and any(path.iterdir()):
        if not overwrite:
            raise FileExistsError(f"output directory is not empty: {path}")
        shutil.rmtree(path)
    path.mkdir(parents=True, exist_ok=True)


def response_extension(content_type: str) -> str:
    media_type = content_type.split(";", 1)[0].strip().lower()
    return {
        "image/png": ".png",
        "image/jpeg": ".jpg",
        "image/jpg": ".jpg",
        "image/webp": ".webp",
        "image/gif": ".gif",
        "image/svg+xml": ".svg",
        "image/bmp": ".bmp",
    }.get(media_type, ".bin")


def is_document_image_response(response: Response) -> bool:
    content_type = (response.headers.get("content-type") or "").lower()
    url = response.url.lower()
    return content_type.startswith("image/") and any(marker in url for marker in DOCUMENT_IMAGE_URL_MARKERS)


def is_sheet_api_response(response: Response) -> bool:
    url = response.url.lower()
    return any(marker in url for marker in SHEET_API_URL_MARKERS)


def capture_response(
    response: Response,
    image_payloads: dict[str, dict[str, Any]],
    sheet_payloads: dict[str, dict[str, Any]],
) -> None:
    try:
        if is_document_image_response(response) and response.url not in image_payloads:
            body = response.body()
            image_payloads[response.url] = {
                "url": response.url,
                "status": response.status,
                "content_type": response.headers.get("content-type", ""),
                "body": body,
            }
        elif is_sheet_api_response(response) and response.url not in sheet_payloads:
            body = response.body()
            if len(body) <= 16 * 1024 * 1024:
                sheet_payloads[response.url] = {
                    "url": response.url,
                    "status": response.status,
                    "content_type": response.headers.get("content-type", ""),
                    "body": body,
                }
    except Exception:
        # A page may close while an unimportant late response is still settling.
        return


COLLECT_BLOCKS_JS = r"""
() => {
  const clean = value => (value || '')
    .replace(/[\u200b\u200c\u200d\ufeff]/g, '')
    .replace(/\u00a0/g, ' ')
    .replace(/[ \t]+\n/g, '\n')
    .trim();
  const cleanCodeLine = value => (value || '')
    .replace(/[\u200b\u200c\u200d\ufeff]/g, '')
    .replace(/\u00a0/g, ' ')
    .replace(/[\r\n]+$/g, '');
  const ownText = element => {
    if (!element) return '';
    const clone = element.cloneNode(true);
    clone.querySelectorAll('.list-children, .page-block-children').forEach(child => child.remove());
    return clean(clone.innerText || clone.textContent);
  };
  const nodes = Array.from(document.querySelectorAll(
    '.page-block-children .render-unit-wrapper > .block'
  ));
  return nodes.map((block, index) => {
    const classes = String(block.className || '');
    let type = block.dataset.blockType || '';
    if (!type) {
      const match = classes.match(/docx-([a-z0-9_-]+)-block/i);
      type = match ? match[1] : 'unknown';
    }
    const id = block.dataset.blockId || block.dataset.recordId || `dom-${index}`;
    const links = Array.from(block.querySelectorAll('a[href]')).map(a => ({
      text: clean(a.innerText || a.textContent),
      url: a.href,
    }));
    const heading = block.querySelector('.heading') || block.querySelector('h1,h2,h3,h4,h5,h6');
    const code = block.querySelector('.code-block-content');
    const codeLines = Array.from(block.querySelectorAll('.code-line-wrapper')).map((line, position) => {
      const parsed = Number.parseInt(line.getAttribute('data-line-num') || '', 10);
      return {
        line_num: Number.isFinite(parsed) ? parsed : position + 1,
        text: cleanCodeLine(line.innerText || line.textContent),
      };
    });
    const caption = block.querySelector('.code-block-caption');
    const language = block.querySelector('.code-block-header-btn');
    const sheetIds = Array.from(block.querySelectorAll('.spreadsheet-wrap')).map(e => {
      const match = String(e.className || '').match(/(?:^|\s)sheet-id-([^\s]+)/);
      return match ? match[1] : '';
    }).filter(Boolean);
    const imageTokens = Array.from(block.querySelectorAll('.image-block[image-token]'))
      .map(e => e.getAttribute('image-token')).filter(Boolean);
    return {
      id,
      record_id: block.dataset.recordId || '',
      type,
      classes,
      text: ownText(block),
      heading: ownText(heading),
      code: clean(code && (code.innerText || code.textContent)),
      code_lines: codeLines,
      caption: clean(caption && (caption.innerText || caption.textContent)),
      language: clean(language && (language.innerText || language.textContent)),
      links,
      sheet_ids: sheetIds,
      image_tokens: imageTokens,
      html: block.outerHTML,
    };
  });
}
"""


def merge_virtualized_code_block(
    existing: dict[str, Any] | None,
    observed: dict[str, Any],
) -> dict[str, Any]:
    """Merge Feishu's viewport-only code lines without duplicating page fragments."""

    merged = dict(existing or {})
    line_map: dict[int, str] = {}
    for source in (existing or {}, observed):
        for item in source.get("code_lines", []):
            try:
                line_number = int(item.get("line_num"))
            except (TypeError, ValueError):
                continue
            if line_number > 0:
                line_map[line_number] = str(item.get("text") or "")

    for key, value in observed.items():
        if key not in {"code", "code_lines", "html", "text"}:
            merged[key] = value
    ordered_lines = [
        {"line_num": line_number, "text": line_map[line_number]}
        for line_number in sorted(line_map)
    ]
    numbers = [item["line_num"] for item in ordered_lines]
    merged["code_lines"] = ordered_lines
    merged["code"] = "\n".join(item["text"] for item in ordered_lines)
    merged["text"] = merged["code"]
    merged["code_line_first"] = numbers[0] if numbers else 0
    merged["code_line_last"] = numbers[-1] if numbers else 0
    merged["code_line_count"] = len(numbers)
    merged["code_line_complete"] = bool(numbers) and numbers == list(range(1, numbers[-1] + 1))
    # Keep one original viewport fragment for diagnostics. The normalized rendered
    # export below uses the merged code instead of presenting this as complete HTML.
    merged["html"] = str((existing or {}).get("html") or observed.get("html") or "")
    return merged


def scroll_state(page: Page) -> dict[str, int]:
    return page.evaluate(
        """
        () => {
          const e = document.querySelector('.bear-web-x-container') || document.scrollingElement;
          return {
            top: Math.round(e ? e.scrollTop : 0),
            height: Math.round(e ? e.scrollHeight : document.body.scrollHeight),
            viewport: Math.round(e ? e.clientHeight : window.innerHeight),
          };
        }
        """
    )


def set_scroll_top(page: Page, top: int) -> None:
    page.evaluate(
        """
        top => {
          const e = document.querySelector('.bear-web-x-container') || document.scrollingElement;
          if (e) {
            e.scrollTop = top;
            e.dispatchEvent(new Event('scroll', { bubbles: true }));
          }
        }
        """,
        top,
    )


def collect_settled_blocks(page: Page, settle_ms: int) -> tuple[list[dict[str, Any]], list[dict[str, int]]]:
    set_scroll_top(page, 0)
    page.wait_for_timeout(500)
    order: list[str] = []
    by_id: dict[str, dict[str, Any]] = {}
    first_scroll: dict[str, int] = {}
    states: list[dict[str, int]] = []
    bottom_stable = 0
    previous_height = -1

    for _ in range(160):
        state = scroll_state(page)
        states.append(state)
        lazy_deadline = time.monotonic() + 5.0
        while time.monotonic() < lazy_deadline:
            pending_sheets = page.evaluate(
                """
                () => Array.from(document.querySelectorAll('.docx-sheet-block'))
                  .filter(block => !block.querySelector('.spreadsheet-wrap')).length
                """
            )
            if not pending_sheets:
                break
            page.wait_for_timeout(350)
        for position, block in enumerate(page.evaluate(COLLECT_BLOCKS_JS)):
            block_id = str(block.get("id") or f"dom-{position}")
            key = str(block.get("record_id") or f"{block_id}-{block.get('type') or 'unknown'}")
            if key in by_id and by_id[key].get("id") != block.get("id"):
                key = f"{key}-{position}"
            if key not in by_id:
                order.append(key)
                first_scroll[key] = state["top"]
            block["key"] = key
            block["first_seen_scroll_top"] = first_scroll[key]
            if str(block.get("type") or "").lower() == "code":
                by_id[key] = merge_virtualized_code_block(by_id.get(key), block)
            else:
                by_id[key] = block

        at_bottom = state["top"] + state["viewport"] >= state["height"] - 4
        if at_bottom and state["height"] == previous_height:
            bottom_stable += 1
        else:
            bottom_stable = 0
        if bottom_stable >= 3:
            break
        previous_height = state["height"]
        next_top = min(
            state["top"] + max(500, int(state["viewport"] * 0.8)),
            max(0, state["height"] - state["viewport"]),
        )
        set_scroll_top(page, next_top)
        page.wait_for_timeout(300 if not at_bottom else settle_ms)

    set_scroll_top(page, 0)
    page.wait_for_timeout(300)
    return [by_id[key] for key in order], states


def page_identity(page: Page, fallback_token: str) -> dict[str, str]:
    identity = page.evaluate(
        """
        () => {
          const clean = value => (value || '').replace(/[\u200b\u200c\u200d\ufeff]/g, '').trim();
          const h1 = document.querySelector('h1.page-block-content')
            || document.querySelector('.page-block-header h1')
            || document.querySelector('.page-main h1');
          const header = document.querySelector('.page-block-header');
          return {
            title: clean(h1 && (h1.innerText || h1.textContent)),
            header_text: clean(header && (header.innerText || header.textContent)),
            document_title: clean(document.title.replace(/ - 飞书云文档$/, '')),
          };
        }
        """
    )
    title = single_line(identity.get("title")) or single_line(identity.get("document_title")) or fallback_token
    modified_match = SOURCE_MODIFIED_RE.search(clean_text(identity.get("header_text")))
    return {
        "title": title,
        "source_modified_label": modified_match.group(0) if modified_match else "",
        "document_title": single_line(identity.get("document_title")),
    }


def merge_links(blocks: Iterable[dict[str, Any]], page: Page, source_url: str) -> list[dict[str, Any]]:
    candidates: list[dict[str, str]] = []
    for block in blocks:
        candidates.extend(block.get("links") or [])
    candidates.extend(
        page.evaluate(
            """
            () => Array.from(document.querySelectorAll('.page-main a[href]')).map(a => ({
              text: (a.innerText || a.textContent || '').replace(/[\u200b\u200c\u200d\ufeff]/g, '').trim(),
              url: a.href,
            }))
            """
        )
    )
    seen: set[tuple[str, str]] = set()
    records: list[dict[str, Any]] = []
    for candidate in candidates:
        url = str(candidate.get("url") or "").strip()
        if not url:
            continue
        text = single_line(candidate.get("text"))
        canonical = canonical_wiki_url(url, source_url)
        final_url = canonical or url
        key = (text, final_url)
        if key in seen:
            continue
        seen.add(key)
        records.append(
            {
                "text": text,
                "url": final_url,
                "kind": "official-wiki" if canonical else "external",
                "target_token": token_from_url(canonical) if canonical else "",
            }
        )
    return records


def locate_block(page: Page, block: dict[str, Any], class_name: str) -> Locator:
    block_id = str(block.get("id") or "")
    scroll_top = int(block.get("first_seen_scroll_top") or 0)
    set_scroll_top(page, scroll_top)
    page.wait_for_timeout(350)
    if block_id and re.fullmatch(r"[A-Za-z0-9_-]+", block_id):
        locator = page.locator(f'.{class_name}[data-block-id="{block_id}"]').first
        if locator.count():
            return locator
    locator = page.locator(f".{class_name}").filter(has_text=clean_text(block.get("text"))[:80]).first
    return locator


def extract_rendered_images(
    page: Page,
    blocks: list[dict[str, Any]],
    page_dir: Path,
    out_dir: Path,
) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    image_blocks = [block for block in blocks if block.get("type") == "image"]
    for index, block in enumerate(image_blocks, start=1):
        block_id = safe_component(str(block.get("id") or index), str(index))
        path = page_dir / "images" / f"rendered-{index:02d}-{block_id}.png"
        record = {
            "block_id": str(block.get("id") or ""),
            "image_tokens": block.get("image_tokens") or [],
            "rendered_path": relative(path, out_dir),
            "status": "pending",
        }
        try:
            locator = locate_block(page, block, "docx-image-block")
            locator.scroll_into_view_if_needed(timeout=5_000)
            page.wait_for_timeout(400)
            image = locator.locator("img.docx-image").first
            if not image.count():
                raise RuntimeError("rendered image element not found")
            path.parent.mkdir(parents=True, exist_ok=True)
            image.screenshot(path=str(path), timeout=10_000)
            record["status"] = "captured"
        except Exception as exc:
            record["status"] = "failed"
            record["error"] = repr(exc)
        records.append(record)
    return records


def sheet_id_from_class(value: str | None, fallback: str) -> str:
    match = SHEET_ID_RE.search(value or "")
    return match.group(1) if match else fallback


def extract_sheets(
    page: Page,
    blocks: list[dict[str, Any]],
    page_dir: Path,
    out_dir: Path,
) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    source_url = page.url
    sheet_blocks = [block for block in blocks if block.get("type") == "sheet"]

    def locate_expected_wrap(target_page: Page, block: dict[str, Any], sheet_id: str) -> Locator:
        first_scroll = int(block.get("first_seen_scroll_top") or 0)
        set_scroll_top(target_page, first_scroll)
        target_page.wait_for_timeout(500)
        block_id = str(block.get("id") or "")
        if block_id and re.fullmatch(r"[A-Za-z0-9_-]+", block_id):
            block_locator = target_page.locator(
                f'.docx-sheet-block[data-block-id="{block_id}"]'
            ).first
            try:
                block_locator.wait_for(state="attached", timeout=8_000)
                block_locator.scroll_into_view_if_needed(timeout=8_000)
                target_page.wait_for_timeout(500)
            except Exception:
                pass
        wrap = target_page.locator(f".spreadsheet-wrap.sheet-id-{sheet_id}").first
        wrap.wait_for(state="attached", timeout=12_000)
        wrap.scroll_into_view_if_needed(timeout=8_000)
        target_page.wait_for_timeout(500)
        return wrap

    def capture_one(target_page: Page, wrap: Locator, screenshot_path: Path) -> str:
        canvas = wrap.locator("canvas.spreadsheet-canvas").first
        canvas.wait_for(state="visible", timeout=12_000)
        screenshot_path.parent.mkdir(parents=True, exist_ok=True)
        canvas.screenshot(path=str(screenshot_path), timeout=15_000)
        clipboard = ""
        for _ in range(3):
            target_page.evaluate("() => navigator.clipboard.writeText('')")
            canvas.click(timeout=8_000)
            target_page.keyboard.press("Control+A")
            target_page.wait_for_timeout(150)
            target_page.keyboard.press("Control+C")
            target_page.wait_for_timeout(450)
            clipboard = clean_text(target_page.evaluate("() => navigator.clipboard.readText()"))
            if clipboard:
                break
        if not clipboard:
            raise RuntimeError("sheet clipboard export was empty")
        return clipboard

    for block_index, block in enumerate(sheet_blocks, start=1):
        expected_sheet_ids = [
            str(value)
            for value in block.get("sheet_ids") or []
            if value and re.fullmatch(r"[A-Za-z0-9_-]+", str(value))
        ]
        if not expected_sheet_ids:
            records.append(
                {
                    "block_id": str(block.get("id") or ""),
                    "sheet_id": "",
                    "status": "failed",
                    "error": "sheet id was not captured during the settled scroll",
                }
            )
            continue
        for wrap_index, sheet_id in enumerate(expected_sheet_ids):
            dedicated_page: Page | None = None
            try:
                sheet_component = safe_component(sheet_id, f"sheet-{block_index:02d}-{wrap_index + 1:02d}")
                base = f"{block_index:02d}-{wrap_index + 1:02d}-{sheet_component}"
                tsv_path = page_dir / "sheets" / f"{base}.tsv"
                screenshot_path = page_dir / "sheets" / f"{base}.png"
                record: dict[str, Any] = {
                    "block_id": str(block.get("id") or ""),
                    "sheet_id": sheet_id,
                    "tsv_path": relative(tsv_path, out_dir),
                    "screenshot_path": relative(screenshot_path, out_dir),
                    "status": "pending",
                }
                try:
                    wrap = locate_expected_wrap(page, block, sheet_id)
                    clipboard = capture_one(page, wrap, screenshot_path)
                except Exception as primary_exc:
                    # Activating a nearby spreadsheet can unmount or hide this one.
                    # A fresh page gives the failed sheet a neutral focus/scroll state.
                    dedicated_page = page.context.new_page()
                    dedicated_page.goto(source_url, wait_until="domcontentloaded", timeout=60_000)
                    dedicated_page.wait_for_selector(".page-main", state="attached", timeout=60_000)
                    dedicated_page.wait_for_timeout(2_500)
                    try:
                        dedicated_wrap = locate_expected_wrap(dedicated_page, block, sheet_id)
                        clipboard = capture_one(dedicated_page, dedicated_wrap, screenshot_path)
                        record["fallback"] = "dedicated-page"
                        record["primary_error"] = repr(primary_exc)
                    except Exception as fallback_exc:
                        raise RuntimeError(
                            f"primary={primary_exc!r}; dedicated={fallback_exc!r}"
                        ) from fallback_exc
                try:
                    write_text(tsv_path, clipboard + "\n")
                    record["status"] = "captured"
                    record["rows"] = len(clipboard.splitlines())
                    record["columns_max"] = max(
                        (len(row.split("\t")) for row in clipboard.splitlines()),
                        default=0,
                    )
                except Exception as exc:
                    record["status"] = "failed"
                    record["error"] = repr(exc)
                records.append(record)
            finally:
                if dedicated_page is not None:
                    dedicated_page.close()
    return records


def save_response_payloads(
    payloads: dict[str, dict[str, Any]],
    directory: Path,
    out_dir: Path,
    prefix: str,
) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    for index, payload in enumerate(sorted(payloads.values(), key=lambda item: item["url"]), start=1):
        body = payload.pop("body")
        digest = hashlib.sha256(payload["url"].encode("utf-8")).hexdigest()[:16]
        extension = response_extension(payload.get("content_type", "")) if prefix == "image" else ".json"
        path = directory / f"{prefix}-{index:02d}-{digest}{extension}"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(body)
        records.append(
            {
                **payload,
                "path": relative(path, out_dir),
                "bytes": len(body),
                "sha256": sha256_bytes(body),
            }
        )
    return records


def markdown_for_page(
    title: str,
    source_url: str,
    modified_label: str,
    blocks: list[dict[str, Any]],
    links: list[dict[str, Any]],
    rendered_images: list[dict[str, Any]],
    sheets: list[dict[str, Any]],
    page_dir: Path,
    out_dir: Path,
) -> str:
    image_by_block = {record["block_id"]: record for record in rendered_images}
    sheets_by_block: dict[str, list[dict[str, Any]]] = {}
    for record in sheets:
        sheets_by_block.setdefault(record.get("block_id", ""), []).append(record)
    lines = [f"# {title}", "", f"Source: <{source_url}>"]
    if modified_label:
        lines.extend(["", f"Source modified label: {modified_label}"])
    lines.extend([""])

    for block in blocks:
        block_type = str(block.get("type") or "unknown").lower()
        text = clean_text(block.get("text"))
        block_id = str(block.get("id") or "")
        if block_type.startswith("heading"):
            match = re.search(r"(\d+)", block_type)
            level = min(6, 1 + (int(match.group(1)) if match else 1))
            heading = single_line(block.get("heading")) or single_line(text)
            if heading:
                lines.extend([f"{'#' * level} {heading}", ""])
        elif block_type == "code":
            caption = single_line(block.get("caption"))
            language = safe_component(single_line(block.get("language")), "")
            code = clean_text(block.get("code"))
            if caption:
                lines.extend([f"_{caption}_", ""])
            lines.extend([f"```{language}", code, "```", ""])
        elif block_type in {"bullet", "todo"}:
            item = text
            item = re.sub(r"^[•·]\s*\n?", "", item).strip()
            if item:
                lines.extend(["- " + item.replace("\n", "\n  "), ""])
        elif block_type in {"ordered", "numbered"}:
            item = re.sub(r"^\d+[.)、]?\s*\n?", "", text).strip()
            if item:
                lines.extend(["1. " + item.replace("\n", "\n   "), ""])
        elif block_type == "image":
            record = image_by_block.get(block_id)
            if record and record.get("status") == "captured":
                target = Path(out_dir / record["rendered_path"])
                lines.extend([f"![Official document image]({relative(target, page_dir)})", ""])
            else:
                lines.extend(["_[Official document image capture failed]_", ""])
        elif block_type == "sheet":
            records = sheets_by_block.get(block_id, [])
            if not records:
                lines.extend(["_[Embedded sheet extraction failed]_", ""])
            for record in records:
                sheet_id = record.get("sheet_id") or "unknown"
                lines.extend([f"#### Embedded sheet `{sheet_id}`", ""])
                if record.get("status") == "captured":
                    tsv = Path(out_dir / record["tsv_path"])
                    shot = Path(out_dir / record["screenshot_path"])
                    table_text = clean_text(tsv.read_text(encoding="utf-8"))
                    lines.extend(
                        [
                            f"[TSV]({relative(tsv, page_dir)}) · [rendered screenshot]({relative(shot, page_dir)})",
                            "",
                            "```tsv",
                            table_text,
                            "```",
                            "",
                        ]
                    )
                else:
                    lines.extend([f"_Extraction failed: {record.get('error', 'unknown error')}_", ""])
        elif block_type in {"divider", "horizontalrule"}:
            lines.extend(["---", ""])
        elif text:
            lines.extend([text, ""])

    if links:
        lines.extend(["## Links", ""])
        for record in links:
            label = record.get("text") or record["url"]
            lines.append(f"- [{label}]({record['url']})")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def export_code_blocks(
    blocks: list[dict[str, Any]],
    page_dir: Path,
    out_dir: Path,
) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    current_heading = ""
    for block in blocks:
        block_type = str(block.get("type") or "").lower()
        if block_type.startswith("heading"):
            current_heading = single_line(block.get("heading")) or single_line(block.get("text"))
            continue
        if block_type != "code":
            continue

        index = len(records) + 1
        language = single_line(block.get("language"))
        extension = ".jsonc" if "json" in language.lower() else ".txt"
        path = page_dir / "code-blocks" / f"{index:02d}-code{extension}"
        code = clean_text(block.get("code"))
        write_text(path, code + ("\n" if code else ""))
        line_numbers = [
            int(item["line_num"])
            for item in block.get("code_lines", [])
            if item.get("line_num") is not None
        ]
        records.append(
            {
                "index": index,
                "block_id": str(block.get("id") or ""),
                "record_id": str(block.get("record_id") or ""),
                "heading": current_heading,
                "caption": single_line(block.get("caption")),
                "language": language,
                "line_first": line_numbers[0] if line_numbers else 0,
                "line_last": line_numbers[-1] if line_numbers else 0,
                "line_count": len(line_numbers),
                "line_complete": bool(block.get("code_line_complete")),
                "path": relative(path, out_dir),
                "bytes": path.stat().st_size,
                "sha256": sha256_file(path),
            }
        )
    return records


def plain_text_for_page(
    title: str,
    source_url: str,
    modified_label: str,
    blocks: list[dict[str, Any]],
    sheets: list[dict[str, Any]],
    out_dir: Path,
) -> str:
    sheets_by_block: dict[str, list[dict[str, Any]]] = {}
    for record in sheets:
        sheets_by_block.setdefault(record.get("block_id", ""), []).append(record)
    chunks = [title, f"Source: {source_url}"]
    if modified_label:
        chunks.append(f"Source modified label: {modified_label}")
    for block in blocks:
        block_type = str(block.get("type") or "")
        if block_type == "sheet":
            for record in sheets_by_block.get(str(block.get("id") or ""), []):
                chunks.append(f"[Embedded sheet: {record.get('sheet_id') or 'unknown'}]")
                if record.get("status") == "captured":
                    chunks.append(clean_text((out_dir / record["tsv_path"]).read_text(encoding="utf-8")))
                else:
                    chunks.append(f"[Extraction failed: {record.get('error', 'unknown error')}]")
            continue
        text = clean_text(block.get("code") if block_type == "code" else block.get("text"))
        if text:
            chunks.append(text)
    return "\n\n".join(chunk for chunk in chunks if chunk).rstrip() + "\n"


def rendered_html_for_page(title: str, source_url: str, blocks: list[dict[str, Any]]) -> str:
    fragments_list: list[str] = []
    for block in blocks:
        if str(block.get("type") or "").lower() == "code":
            caption = single_line(block.get("caption"))
            caption_html = f"<p><em>{html.escape(caption)}</em></p>" if caption else ""
            fragments_list.append(
                '<section class="dtmapi-exported-code-block" '
                f'data-block-id="{html.escape(str(block.get("id") or ""))}" '
                f'data-line-complete="{str(bool(block.get("code_line_complete"))).lower()}">'
                f"{caption_html}<pre><code>{html.escape(clean_text(block.get('code')))}</code></pre></section>"
            )
        else:
            fragments_list.append(str(block.get("html") or ""))
    fragments = "\n".join(fragments_list)
    return f"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{html.escape(title)}</title>
<style>body{{font-family:system-ui,"Microsoft YaHei",sans-serif;max-width:1100px;margin:2rem auto;line-height:1.6}}img,canvas{{max-width:100%}}.source{{color:#555}}</style>
</head>
<body>
<h1>{html.escape(title)}</h1>
<p class="source">Settled fragments from <a href="{html.escape(source_url)}">{html.escape(source_url)}</a>.</p>
{fragments}
</body>
</html>
"""


def process_page(
    context: BrowserContext,
    url: str,
    out_dir: Path,
    page_timeout_ms: int,
    settle_ms: int,
) -> dict[str, Any]:
    token = token_from_url(url)
    page_dir = out_dir / "pages" / token
    page_dir.mkdir(parents=True, exist_ok=True)
    image_payloads: dict[str, dict[str, Any]] = {}
    sheet_payloads: dict[str, dict[str, Any]] = {}
    page = context.new_page()
    page.on("response", lambda response: capture_response(response, image_payloads, sheet_payloads))
    started = time.monotonic()
    try:
        response = page.goto(url, wait_until="domcontentloaded", timeout=page_timeout_ms)
        if response is None:
            raise RuntimeError("navigation returned no main response")
        status = response.status
        if status >= 400:
            raise RuntimeError(f"HTTP {status}")
        source_html = response.text()
        page.wait_for_selector(".page-main", state="attached", timeout=page_timeout_ms)
        page.wait_for_timeout(settle_ms)
        if canonical_wiki_url(page.url) != url:
            raise RuntimeError(f"unexpected final URL: {page.url}")

        blocks, scroll_states = collect_settled_blocks(page, settle_ms)
        identity = page_identity(page, token)
        links = merge_links(blocks, page, url)
        rendered_images = extract_rendered_images(page, blocks, page_dir, out_dir)
        sheets = extract_sheets(page, blocks, page_dir, out_dir)
        page.wait_for_timeout(400)

        original_images = save_response_payloads(
            image_payloads,
            page_dir / "images" / "original",
            out_dir,
            "image",
        )
        raw_sheet_apis = save_response_payloads(
            sheet_payloads,
            page_dir / "raw-api",
            out_dir,
            "sheet-api",
        )
        code_blocks = export_code_blocks(blocks, page_dir, out_dir)

        source_path = page_dir / "source.html"
        rendered_path = page_dir / "rendered.html"
        text_path = page_dir / "content.txt"
        markdown_path = page_dir / "content.md"
        write_text(source_path, source_html)
        write_text(rendered_path, rendered_html_for_page(identity["title"], url, blocks))
        write_text(
            text_path,
            plain_text_for_page(
                identity["title"],
                url,
                identity["source_modified_label"],
                blocks,
                sheets,
                out_dir,
            ),
        )
        write_text(
            markdown_path,
            markdown_for_page(
                identity["title"],
                url,
                identity["source_modified_label"],
                blocks,
                links,
                rendered_images,
                sheets,
                page_dir,
                out_dir,
            ),
        )

        record: dict[str, Any] = {
            "export_schema_version": 2,
            "url": url,
            "final_url": page.url,
            "token": token,
            "title": identity["title"],
            "document_title": identity["document_title"],
            "source_modified_label": identity["source_modified_label"],
            "status_code": status,
            "elapsed_seconds": round(time.monotonic() - started, 3),
            "block_count": len(blocks),
            "blocks_by_type": {},
            "scroll_samples": len(scroll_states),
            "scroll_height_max": max((state["height"] for state in scroll_states), default=0),
            "links": links,
            "children": sorted(
                {
                    record["url"]
                    for record in links
                    if record["kind"] == "official-wiki" and record["url"] != url
                }
            ),
            "rendered_images": rendered_images,
            "original_images": original_images,
            "sheets": sheets,
            "code_blocks": code_blocks,
            "raw_sheet_apis": raw_sheet_apis,
            "paths": {
                "source_html": relative(source_path, out_dir),
                "rendered_html": relative(rendered_path, out_dir),
                "text": relative(text_path, out_dir),
                "markdown": relative(markdown_path, out_dir),
            },
        }
        for block in blocks:
            block_type = str(block.get("type") or "unknown")
            record["blocks_by_type"][block_type] = record["blocks_by_type"].get(block_type, 0) + 1
        page_json_path = page_dir / "page.json"
        record["paths"]["metadata"] = relative(page_json_path, out_dir)
        write_text(page_json_path, json.dumps(record, ensure_ascii=False, indent=2) + "\n")
        return record
    finally:
        page.close()


def load_previous_tokens(path: Path | None) -> dict[str, dict[str, Any]]:
    if not path or not path.is_file():
        return {}
    data = json.loads(path.read_text(encoding="utf-8"))
    return {
        str(page.get("token")): page
        for page in data.get("pages", [])
        if page.get("token")
    }


def load_cached_pages(out_dir: Path) -> dict[str, dict[str, Any]]:
    cached: dict[str, dict[str, Any]] = {}
    pages_dir = out_dir / "pages"
    if not pages_dir.is_dir():
        return cached
    for path in pages_dir.glob("*/page.json"):
        try:
            record = json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            continue
        token = str(record.get("token") or "")
        if token:
            cached[token] = record
    return cached


def cached_page_complete(record: dict[str, Any], out_dir: Path) -> bool:
    if int(record.get("export_schema_version") or 0) < 2:
        return False
    if int(record.get("status_code") or 0) >= 400:
        return False
    if any(sheet.get("status") != "captured" for sheet in record.get("sheets", [])):
        return False
    if any(image.get("status") != "captured" for image in record.get("rendered_images", [])):
        return False
    expected_code_blocks = int((record.get("blocks_by_type") or {}).get("code") or 0)
    code_blocks = record.get("code_blocks") or []
    if expected_code_blocks != len(code_blocks):
        return False
    if any(not code.get("line_complete") for code in code_blocks):
        return False
    if any(not code.get("path") or not (out_dir / code["path"]).is_file() for code in code_blocks):
        return False
    paths = record.get("paths") or {}
    required = [paths.get("source_html"), paths.get("rendered_html"), paths.get("text"), paths.get("markdown")]
    return all(value and (out_dir / value).is_file() for value in required)


def build_comparison(previous: dict[str, dict[str, Any]], pages: list[dict[str, Any]]) -> dict[str, Any]:
    current = {page["token"]: page for page in pages}
    previous_tokens = set(previous)
    current_tokens = set(current)
    title_changes = []
    for token in sorted(previous_tokens & current_tokens):
        old_title = single_line(previous[token].get("title"))
        new_title = single_line(current[token].get("title"))
        if old_title != new_title:
            kind = (
                "formatting-only"
                if re.sub(r"\s+", "", old_title) == re.sub(r"\s+", "", new_title)
                else "substantive"
            )
            title_changes.append(
                {"token": token, "previous": old_title, "current": new_title, "kind": kind}
            )
    substantive_title_changes = [item for item in title_changes if item["kind"] == "substantive"]
    formatting_title_changes = [item for item in title_changes if item["kind"] == "formatting-only"]
    return {
        "previous_page_count": len(previous),
        "current_page_count": len(current),
        "added_tokens": sorted(current_tokens - previous_tokens),
        "removed_tokens": sorted(previous_tokens - current_tokens),
        "retained_tokens": sorted(previous_tokens & current_tokens),
        "title_changes": title_changes,
        "substantive_title_changes": substantive_title_changes,
        "formatting_only_title_changes": formatting_title_changes,
        "content_hash_comparison": "not-comparable-export-format",
    }


def write_tsv(path: Path, fieldnames: list[str], rows: Iterable[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fieldnames, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            writer.writerow({field: row.get(field, "") for field in fieldnames})


def build_report(
    root_url: str,
    pages: list[dict[str, Any]],
    failures: list[dict[str, Any]],
    remaining_queue: list[str],
    comparison: dict[str, Any],
) -> str:
    sheet_records = [sheet for page in pages for sheet in page.get("sheets", [])]
    image_records = [image for page in pages for image in page.get("rendered_images", [])]
    code_records = [code for page in pages for code in page.get("code_blocks", [])]
    external_links = [
        link
        for page in pages
        for link in page.get("links", [])
        if link.get("kind") == "external"
    ]
    complete = not failures and not remaining_queue and all(
        sheet.get("status") == "captured" for sheet in sheet_records
    ) and all(image.get("status") == "captured" for image in image_records) and all(
        code.get("line_complete") for code in code_records
    )
    lines = [
        "# Official Doloc Town Workshop Documentation Crawl Report",
        "",
        f"- Source root: <{root_url}>",
        f"- Crawl time: {utc_now()}",
        f"- Tool version: {TOOL_VERSION}",
        f"- Complete: {'yes' if complete else 'no'}",
        f"- Pages: {len(pages)}",
        f"- Page failures: {len(failures)}",
        f"- Remaining queue: {len(remaining_queue)}",
        f"- Embedded sheets: {len(sheet_records)} ({sum(s.get('status') == 'captured' for s in sheet_records)} captured)",
        f"- Rendered document images: {len(image_records)} ({sum(i.get('status') == 'captured' for i in image_records)} captured)",
        f"- Code blocks: {len(code_records)} ({sum(c.get('line_complete') for c in code_records)} complete line ranges)",
        f"- External links: {len(external_links)}",
        f"- Previous snapshot pages: {comparison.get('previous_page_count', 0)}",
        f"- Added tokens: {len(comparison.get('added_tokens', []))}",
        f"- Removed tokens: {len(comparison.get('removed_tokens', []))}",
        "",
        "## Pages",
        "",
        "| Token | Title | Modified label | Blocks | Code | Sheets | Images |",
        "| --- | --- | --- | ---: | ---: | ---: | ---: |",
    ]
    for page in pages:
        title_cell = page["title"].replace("|", "&#124;")
        lines.append(
            f"| `{page['token']}` | {title_cell} | "
            f"{page.get('source_modified_label') or '-'} | {page.get('block_count', 0)} | "
            f"{len(page.get('code_blocks', []))} | "
            f"{len(page.get('sheets', []))} | {len(page.get('rendered_images', []))} |"
        )
    if comparison.get("added_tokens") or comparison.get("removed_tokens") or comparison.get("title_changes"):
        lines.extend(["", "## Previous snapshot comparison", ""])
        lines.append(f"- Added: {', '.join(comparison.get('added_tokens', [])) or 'none'}")
        lines.append(f"- Removed: {', '.join(comparison.get('removed_tokens', [])) or 'none'}")
        lines.append(
            f"- Formatting-only title changes: {len(comparison.get('formatting_only_title_changes', []))}"
        )
        if comparison.get("substantive_title_changes"):
            lines.append("- Substantive title changes:")
            for item in comparison["substantive_title_changes"]:
                lines.append(f"  - `{item['token']}`: {item['previous']} -> {item['current']}")
    if failures:
        lines.extend(["", "## Page failures", ""])
        for failure in failures:
            lines.append(f"- <{failure['url']}>: `{failure['error']}`")
    failed_sheets = [sheet for sheet in sheet_records if sheet.get("status") != "captured"]
    if failed_sheets:
        lines.extend(["", "## Sheet failures", ""])
        for sheet in failed_sheets:
            lines.append(
                f"- block `{sheet.get('block_id', '')}` / sheet `{sheet.get('sheet_id', '')}`: "
                f"`{sheet.get('error', 'unknown error')}`"
            )
    incomplete_code = [code for code in code_records if not code.get("line_complete")]
    if incomplete_code:
        lines.extend(["", "## Code-block line failures", ""])
        for code in incomplete_code:
            lines.append(
                f"- block `{code.get('block_id', '')}`: first={code.get('line_first', 0)}, "
                f"last={code.get('line_last', 0)}, count={code.get('line_count', 0)}"
            )
    return "\n".join(lines).rstrip() + "\n"


def file_inventory(out_dir: Path) -> list[dict[str, Any]]:
    records = []
    for path in sorted(out_dir.rglob("*")):
        if not path.is_file() or path.name == "manifest.json":
            continue
        records.append(
            {
                "path": relative(path, out_dir),
                "bytes": path.stat().st_size,
                "sha256": sha256_file(path),
            }
        )
    return records


def main() -> int:
    args = parse_args()
    root_url = canonical_wiki_url(args.root_url)
    if not root_url:
        raise SystemExit(f"invalid root Wiki URL: {args.root_url}")
    if args.max_pages <= 0:
        raise SystemExit("--max-pages must be positive")
    if args.resume and args.overwrite:
        raise SystemExit("--resume and --overwrite are mutually exclusive")
    chrome = discover_chrome(args.chrome)
    if args.resume:
        if not (args.out_dir / "pages").is_dir():
            raise SystemExit(f"resume output has no pages directory: {args.out_dir}")
    else:
        prepare_output(args.out_dir, args.overwrite)
    previous = load_previous_tokens(args.previous_manifest)
    cached_pages = load_cached_pages(args.out_dir) if args.resume else {}

    pages: list[dict[str, Any]] = []
    failures: list[dict[str, Any]] = []
    queue: deque[str] = deque([root_url])
    queued = {root_url}
    seen: set[str] = set()
    started_at = utc_now()

    print(f"root={root_url}", flush=True)
    print(f"chrome={chrome}", flush=True)
    print(f"out={args.out_dir.resolve()}", flush=True)

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(
            executable_path=str(chrome),
            headless=not args.headed,
            args=["--disable-gpu", "--no-sandbox"],
        )
        context = browser.new_context(locale="zh-CN", viewport={"width": 1440, "height": 1200})
        context.grant_permissions(
            ["clipboard-read", "clipboard-write"],
            origin=f"https://{WIKI_HOST}",
        )
        try:
            while queue and len(seen) < args.max_pages:
                url = queue.popleft()
                if url in seen:
                    continue
                seen.add(url)
                token = token_from_url(url)
                cached = cached_pages.get(token)
                if cached and cached_page_complete(cached, args.out_dir):
                    record = cached
                    pages.append(record)
                    print(
                        f"[{len(seen):03d}] {token} cached title={record['title']!r} "
                        f"children={len(record.get('children', []))}",
                        flush=True,
                    )
                    for child in record.get("children", []):
                        if child not in seen and child not in queued:
                            queue.append(child)
                            queued.add(child)
                    continue

                print(f"[{len(seen):03d}] {token} fetch", flush=True)
                if args.resume:
                    page_dir = args.out_dir / "pages" / token
                    if page_dir.is_dir():
                        shutil.rmtree(page_dir)
                last_error: Exception | None = None
                record: dict[str, Any] | None = None
                for attempt in range(1, 3):
                    try:
                        record = process_page(
                            context,
                            url,
                            args.out_dir,
                            args.page_timeout_ms,
                            args.settle_ms,
                        )
                        break
                    except Exception as exc:
                        last_error = exc
                        print(f"      attempt={attempt} failed {exc!r}", flush=True)
                        if attempt < 2:
                            time.sleep(1.0)
                if record is None:
                    failures.append({"url": url, "token": token, "error": repr(last_error)})
                    continue
                pages.append(record)
                sheet_ok = sum(sheet.get("status") == "captured" for sheet in record["sheets"])
                image_ok = sum(image.get("status") == "captured" for image in record["rendered_images"])
                print(
                    f"      ok title={record['title']!r} blocks={record['block_count']} "
                    f"children={len(record['children'])} sheets={sheet_ok}/{len(record['sheets'])} "
                    f"images={image_ok}/{len(record['rendered_images'])}",
                    flush=True,
                )
                for child in record["children"]:
                    if child not in seen and child not in queued:
                        queue.append(child)
                        queued.add(child)
                if args.delay_ms:
                    time.sleep(args.delay_ms / 1000)
        finally:
            context.close()
            browser.close()

    pages.sort(key=lambda item: (item["title"], item["token"]))
    remaining_queue = list(queue)
    comparison = build_comparison(previous, pages)
    complete = (
        not failures
        and not remaining_queue
        and bool(pages)
        and any(page["token"] == token_from_url(root_url) for page in pages)
        and all(
            sheet.get("status") == "captured"
            for page in pages
            for sheet in page.get("sheets", [])
        )
        and all(
            image.get("status") == "captured"
            for page in pages
            for image in page.get("rendered_images", [])
        )
        and all(
            code.get("line_complete")
            for page in pages
            for code in page.get("code_blocks", [])
        )
    )
    root_record = next((page for page in pages if page["token"] == token_from_url(root_url)), None)
    if root_url == DEFAULT_ROOT_URL and root_record and root_record["title"] != EXPECTED_ROOT_TITLE:
        complete = False
        failures.append(
            {
                "url": root_url,
                "token": token_from_url(root_url),
                "error": f"unexpected root title: {root_record['title']!r}",
            }
        )

    page_rows = []
    link_rows = []
    for page in pages:
        page_rows.append(
            {
                "token": page["token"],
                "title": page["title"],
                "source_modified_label": page.get("source_modified_label", ""),
                "url": page["url"],
                "status_code": page["status_code"],
                "blocks": page["block_count"],
                "children": len(page["children"]),
                "code_blocks": len(page.get("code_blocks", [])),
                "sheets": len(page["sheets"]),
                "rendered_images": len(page["rendered_images"]),
                "markdown": page["paths"]["markdown"],
            }
        )
        for link in page["links"]:
            link_rows.append(
                {
                    "source_token": page["token"],
                    "source_title": page["title"],
                    **link,
                }
            )
    write_tsv(
        args.out_dir / "pages.tsv",
        [
            "token",
            "title",
            "source_modified_label",
            "url",
            "status_code",
            "blocks",
            "children",
            "code_blocks",
            "sheets",
            "rendered_images",
            "markdown",
        ],
        page_rows,
    )
    write_tsv(
        args.out_dir / "links.tsv",
        ["source_token", "source_title", "kind", "text", "url", "target_token"],
        link_rows,
    )
    write_text(args.out_dir / "comparison.json", json.dumps(comparison, ensure_ascii=False, indent=2) + "\n")
    write_text(
        args.out_dir / "REPORT.md",
        build_report(root_url, pages, failures, remaining_queue, comparison),
    )
    write_text(
        args.out_dir / "README.md",
        "\n".join(
            [
                "# Official Doloc Town Workshop Documentation Snapshot",
                "",
                f"Source: <{root_url}>",
                "",
                f"Captured: {started_at}",
                "",
                f"Crawler: `tools/scripts/crawl-official-workshop-docs.py` v{TOOL_VERSION}",
                "",
                "The live Feishu Wiki is canonical. This dated snapshot is public reference evidence for DTMAPI research and author-documentation checks.",
                "",
                "- `REPORT.md`: completeness and previous-snapshot comparison.",
                "- `pages.tsv`: page inventory.",
                "- `links.tsv`: official and external link inventory.",
                "- `pages/<token>/content.md`: readable page export.",
                "- `pages/<token>/code-blocks`: full code blocks merged by stable block ID and source line number.",
                "- `pages/<token>/sheets`: embedded sheets as TSV plus rendered screenshots.",
                "- `pages/<token>/images`: rendered document images and captured original image responses.",
                "- `pages/<token>/raw-api`: bounded public sheet API responses retained for diagnostics.",
                "- `manifest.json`: machine-readable metadata and SHA-256 inventory.",
                "",
            ]
        ),
    )
    manifest = {
        "schema_version": 1,
        "tool": "crawl-official-workshop-docs.py",
        "tool_version": TOOL_VERSION,
        "started_at": started_at,
        "completed_at": utc_now(),
        "root_url": root_url,
        "chrome": str(chrome),
        "complete": complete,
        "page_count": len(pages),
        "failure_count": len(failures),
        "remaining_queue_count": len(remaining_queue),
        "pages": pages,
        "failures": failures,
        "remaining_queue": remaining_queue,
        "comparison": comparison,
        "file_inventory": file_inventory(args.out_dir),
    }
    write_text(args.out_dir / "manifest.json", json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(
        f"done complete={complete} pages={len(pages)} failures={len(failures)} "
        f"remaining={len(remaining_queue)} files={len(manifest['file_inventory'])}",
        flush=True,
    )
    return 0 if complete else 2


if __name__ == "__main__":
    raise SystemExit(main())
