# Native Function Map Workbench

This is a research tool. Its displayed baseline comes from `data/summary.json`, including the full Assembly-CSharp SHA-256 and generation date. The retained data currently belongs to `23465763_workshop_38581E` (42,925 methods; 74,488 internal edges), generated on 2026-06-13. Moving the tool did not regenerate or relabel that data. A newer capture or current API support cannot be inferred from its colors.

`index.html`, `styles.css` and `app.js` display `data/summary.json`, `methods.json` and `links.json`. No official binary or decompiled method body is included.

From the repository root, explicitly select an existing baseline with `metadata/types.csv`, `methods.csv`, `calls.csv`, `summary.json`, and `build-info.json`:

```powershell
tools/scripts/build-native-function-map-data.ps1 -BuildRoot references/doloc-town/reverse/builds/23465763_workshop_38581E
python -m http.server 8765 -d tools/native-function-map/workbench
```

Open `http://localhost:8765/`. The generation command never captures or exports the game. It fails on missing metadata/identity rather than writing an empty graph. New results record exact input hashes, generator hash, parameters, method/link output hashes and completion. The retained 2026-06-13 data predates those receipts and is labelled by its original identity, without inventing retroactive verification.

| Coverage | Meaning |
| --- | --- |
| `native-owner` | A method matched a symbol in a DTMAPI-authored native-owner report. Research coverage, not stability proof. |
| `system-map` | A method appears in a generated reverse map, without an owner-report match. |
| `unmapped` | Present in the selected metadata but not matched by these research inputs. |

For API work, follow the [native-owner domains](../../../docs/reviews/api/native-owner-domains/INDEX.md), [focused Hook maps](../../../docs/hook-map/README.md), and the exact native methods for the matching baseline. Reference handling and full capture routes belong to [references](../../../references/README.md).
