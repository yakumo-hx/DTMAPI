# Native Function Map Workbench

Status: active, docs-only/tooling-only
Created: 2026-06-13
Primary reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

This directory contains a generated symbol/call-graph data layer and a static browser workbench for Doloc Town native function-map review.

The workbench answers:

- how many native methods exist in the current reverse baseline;
- how many methods are already covered by generated system maps;
- how many methods are directly tagged by DTMAPI native-owner domain reports;
- which methods call each other in the native Assembly-CSharp metadata;
- which owner reports and systems touch a selected method.

## Files

| Path | Purpose |
| --- | --- |
| `index.html` | Static workbench shell. |
| `styles.css` | Workbench visual styles. |
| `app.js` | Browser-side filtering, graph rendering, and detail panels. |
| `data/summary.json` | Generated build, coverage, domain, and system statistics. |
| `data/methods.json` | Generated method symbol records. |
| `data/links.json` | Generated internal method-to-method call links. |

## Generate Data

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build-native-function-map-data.ps1
```

The generator reads only local reverse metadata, map indexes, and DTMAPI-authored review reports. It does not copy decompiled method bodies or official binaries.

## Open Workbench

Serve this directory so the browser can load JSON with `fetch`:

```powershell
python -m http.server 8765 -d docs/reviews/api/native-function-map
```

Then open:

```text
http://localhost:8765/
```

## Coverage Meaning

| Color/status | Meaning |
| --- | --- |
| `native-owner` | Method matched at least one symbol in `docs/reviews/api/native-owner-domains/*.md`. This is research coverage, not stability proof. |
| `system-map` | Method appears in at least one generated reverse map under `maps/index/*-methods.csv`, but not in a native-owner report. |
| `unmapped` | Method exists in metadata but is not currently tagged by a system map or native-owner report. |

## Boundaries

- This is a research index, not a public API stability matrix.
- Do not cite a colored node as proof that a stable GameBridge adapter exists.
- For API work, still start from `../native-owner-domains/INDEX.md` and the exact method bodies named by the chosen domain.
