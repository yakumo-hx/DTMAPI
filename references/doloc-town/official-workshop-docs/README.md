# Official Doloc Town Workshop Documentation

Canonical live guide: <https://ka7deoo0opr.feishu.cn/wiki/ElmCwXsRCi7OPvkOh8JcvXK9nfc>

The live Feishu Wiki remains authoritative. These folders are dated public-reference snapshots for DTMAPI research, official-JSON authoring checks, and developer documentation. Do not edit a snapshot as if it were the official source.

## Snapshots

- `feishu-crawl-20260715`: current complete browser-rendered snapshot. It contains 55 official Wiki pages, 86 complete code-block exports, 107 embedded-sheet TSV/screenshot exports, 138 rendered document-image captures, original public image responses, bounded sheet API diagnostics, per-page HTML/text/Markdown, and a SHA-256 file inventory.
- `feishu-crawl-20260517`: preserved historical export produced by the predecessor research workflow. It discovered the same 55 Wiki tokens, but its sheet list is not a cell export and its flattened text is not the current authoring reference.
- `pdf`: historical official PDF exports.
- `update-notes`: DTMAPI-authored notes about official Workshop updates.

The 2026-07-15 crawl found no added or removed Wiki tokens relative to 2026-05-17. It found one substantive page-title change (`获取创意工坊权限&查看示例模组` to `查看示例模组`) and 19 pages whose visible source-modified labels are later than 2026-05-17, including a child page dated June 21.

## Refresh

Run from the DTMAPI repository root:

```powershell
python tools\scripts\crawl-official-workshop-docs.py `
  --out-dir references\doloc-town\official-workshop-docs\feishu-crawl-YYYYMMDD `
  --previous-manifest references\doloc-town\official-workshop-docs\feishu-crawl-20260715\manifest.json
```

Requirements:

- Python 3.10 or newer;
- the Python `playwright` package;
- local Google Chrome;
- public network access to the Feishu document.

The crawler uses the real rendered page because Feishu embeds spreadsheets as canvases. It exports each used spreadsheet range through browser clipboard TSV and also saves a rendered canvas screenshot for color/format cues. If a nearby active sheet unloads the target canvas, only that failed sheet is retried in a fresh temporary page.

An interrupted or incomplete crawl can continue without re-fetching verified pages:

```powershell
python tools\scripts\crawl-official-workshop-docs.py `
  --out-dir references\doloc-town\official-workshop-docs\feishu-crawl-YYYYMMDD `
  --previous-manifest references\doloc-town\official-workshop-docs\feishu-crawl-20260715\manifest.json `
  --resume
```

Accept a refresh only when `manifest.json` has `complete: true`, zero page failures, an empty remaining queue, no failed/empty sheet or rendered-image records, and a verified SHA-256 inventory.
