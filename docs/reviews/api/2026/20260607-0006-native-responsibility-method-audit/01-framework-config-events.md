# Framework, Config, Events Symbol-Level Native Responsibility Audit

- Audit: `20260607-0006-native-responsibility-method-audit`
- Scope: public Abstractions symbols in this volume; docs-only review, no implementation goal.
- Locator rule: `Symbol` is the stable key; `Declaration` line numbers are secondary and may drift.

## Top Risks

- `IInputEvents`/raw button symbols are public but only partially native; do not imply official rebinding support.
- `IDtmConfigMenuApi` has many family-only/missing matrix members; pending preview and stale input are still lifecycle-sensitive.
- `DtmMod.AttachContext` and helper root members are ordinary-mod critical but are not matrix-row explicit, so docs must add contract rows.
- `IWorkshopEvents` and save/game-loop event DTOs include data fields that need explicit matrix rows before claiming complete coverage.

## Audit Blocks

<a id="sym-0001"></a>
### DTMAPI.Abstractions.DtmApiStatus

- Symbol: `DTMAPI.Abstractions.DtmApiStatus`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:5`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0002"></a>
### DTMAPI.Abstractions.DtmApiStatus.Proposed

- Symbol: `DTMAPI.Abstractions.DtmApiStatus.Proposed`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:7`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0003"></a>
### DTMAPI.Abstractions.DtmApiStatus.Experimental

- Symbol: `DTMAPI.Abstractions.DtmApiStatus.Experimental`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:8`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0004"></a>
### DTMAPI.Abstractions.DtmApiStatus.Verified

- Symbol: `DTMAPI.Abstractions.DtmApiStatus.Verified`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:9`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0005"></a>
### DTMAPI.Abstractions.DtmApiStatus.Stable

- Symbol: `DTMAPI.Abstractions.DtmApiStatus.Stable`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:10`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0006"></a>
### DTMAPI.Abstractions.DtmApiStatus.Disabled

- Symbol: `DTMAPI.Abstractions.DtmApiStatus.Disabled`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:11`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0007"></a>
### DTMAPI.Abstractions.DtmApiStatusAttribute

- Symbol: `DTMAPI.Abstractions.DtmApiStatusAttribute`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:15`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0008"></a>
### DTMAPI.Abstractions.DtmApiStatusAttribute.DtmApiStatusAttribute(DtmApiStatus status)

- Symbol: `DTMAPI.Abstractions.DtmApiStatusAttribute.DtmApiStatusAttribute(DtmApiStatus status)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:17`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0009"></a>
### DTMAPI.Abstractions.DtmApiStatusAttribute.Status

- Symbol: `DTMAPI.Abstractions.DtmApiStatusAttribute.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:22`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0010"></a>
### DTMAPI.Abstractions.DtmApiStatusAttribute.Since

- Symbol: `DTMAPI.Abstractions.DtmApiStatusAttribute.Since`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:23`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0011"></a>
### DTMAPI.Abstractions.DtmApiStatusAttribute.Notes

- Symbol: `DTMAPI.Abstractions.DtmApiStatusAttribute.Notes`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/ApiStatus.cs:24`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0012"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:7`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0013"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:9`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:13`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:13
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0014"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddSectionTitle(IManifest mod, Func<string> text)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddSectionTitle(IManifest mod, Func<string> text)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:10`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:18`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:18
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0015"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddParagraph(IManifest mod, Func<string> text)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddParagraph(IManifest mod, Func<string> text)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:11`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:19`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:19
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0016"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:12`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:20`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:20
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0017"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool> canEdit, Func<bool>? isVisible = null)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getValue, Action<bool> setValue, Func<bool> canEdit, Func<bool>? isVisible = null)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:13`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:20`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:20
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0018"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddInlineBoolNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<double> getValue, Action<double> setValue, double min, double max, double interval)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddInlineBoolNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<double> getValue, Action<double> setValue, double min, double max, double interval)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:14`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:22`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:22
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0019"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddInlineBoolBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<string> secondaryName, Func<string> secondaryTooltip, Func<bool> getSecondaryValue, Action<bool> setSecondaryValue, Func<bool>? secondaryVisible = null)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddInlineBoolBoolOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<bool> getEnabled, Action<bool> setEnabled, Func<string> secondaryName, Func<string> secondaryTooltip, Func<bool> getSecondaryValue, Action<bool> setSecondaryValue, Func<bool>? secondaryVisible = null)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:15`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:23`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:23
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0020"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddNumberOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<double> getValue, Action<double> setValue, double min, double max, double interval)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:16`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:24`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:24
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0021"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:17`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:25`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:25
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0022"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<bool> canEdit, Func<bool>? isVisible = null)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddTextOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, Func<bool> canEdit, Func<bool>? isVisible = null)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:18`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:25`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:25
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0023"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddChoiceOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<string> allowedValues)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:19`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:27`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:27
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0024"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddColorPresetOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddColorPresetOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue, IReadOnlyList<DtmColorPreset> presets)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:20`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:28`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:28
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0025"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddKeybindOption(IManifest mod, Func<string> name, Func<string> tooltip, Func<string> getValue, Action<string> setValue)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:21`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:29`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:29
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0026"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.AddButton(IManifest mod, Func<string> name, Func<string> tooltip, Action onPressed)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:22`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:30`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:30
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0027"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.SetDisplayName(IManifest mod, Func<string> name)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.SetDisplayName(IManifest mod, Func<string> name)`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:23`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:31`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:31
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0028"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.GetPages()

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.GetPages()`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:24`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:33`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:33
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0029"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.GetPage(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.GetPage(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:25`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:34`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:34
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0030"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.BeginEditing(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.BeginEditing(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:26`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:35`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:35
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0031"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.Save(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.Save(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:27`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:36`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:36
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0032"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.Reset(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.Reset(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:28`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:37`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:37
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0033"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.Cancel(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.Cancel(string uniqueId)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:29`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:38`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:38
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0034"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.SetPageLock(string uniqueId, bool locked, string reason)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.SetPageLock(string uniqueId, bool locked, string reason)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:30`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:40`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:40
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0035"></a>
### DTMAPI.Abstractions.IDtmConfigMenuApi.GetKeybindConflicts(string? uniqueId = null)

- Symbol: `DTMAPI.Abstractions.IDtmConfigMenuApi.GetKeybindConflicts(string? uniqueId = null)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:31`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:46`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:25 `IDtmConfigMenuApi.SetDisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:46
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0036"></a>
### DTMAPI.Abstractions.IConfigMenuPage

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:34`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0037"></a>
### DTMAPI.Abstractions.IConfigMenuPage.Manifest

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.Manifest`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:36`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:33`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:33
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0038"></a>
### DTMAPI.Abstractions.IConfigMenuPage.DisplayName

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.DisplayName`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:37`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:117`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:117
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0039"></a>
### DTMAPI.Abstractions.IConfigMenuPage.TitleScreenOnly

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.TitleScreenOnly`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:38`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:112`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:112
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0040"></a>
### DTMAPI.Abstractions.IConfigMenuPage.Items

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.Items`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:39`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:124`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:124
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0041"></a>
### DTMAPI.Abstractions.IConfigMenuPage.IsEditing

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.IsEditing`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:40`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:119`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:119
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0042"></a>
### DTMAPI.Abstractions.IConfigMenuPage.HasPendingChanges

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.HasPendingChanges`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:41`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:120`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:120
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0043"></a>
### DTMAPI.Abstractions.IConfigMenuPage.IsLocked

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.IsLocked`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:42`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:121`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:121
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0044"></a>
### DTMAPI.Abstractions.IConfigMenuPage.LockReason

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.LockReason`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:43`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:122`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:122
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0045"></a>
### DTMAPI.Abstractions.IConfigMenuPage.Reset()

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.Reset()`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:44`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:37`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:37
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0046"></a>
### DTMAPI.Abstractions.IConfigMenuPage.Save()

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.Save()`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:45`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:36`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:36
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0047"></a>
### DTMAPI.Abstractions.IConfigMenuPage.Cancel()

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.Cancel()`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:46`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:38`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:38
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0048"></a>
### DTMAPI.Abstractions.IConfigMenuPage.BeginEditing()

- Symbol: `DTMAPI.Abstractions.IConfigMenuPage.BeginEditing()`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:47`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:35`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:26 `IConfigMenuPage.DisplayName` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:35
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0049"></a>
### DTMAPI.Abstractions.IConfigMenuPendingPreview

- Symbol: `DTMAPI.Abstractions.IConfigMenuPendingPreview`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:51`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:27 `IConfigMenuPendingPreview.PreviewPendingValues` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0050"></a>
### DTMAPI.Abstractions.IConfigMenuPendingPreview.PreviewPendingValues()

- Symbol: `DTMAPI.Abstractions.IConfigMenuPendingPreview.PreviewPendingValues()`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:53`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:157`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:27 `IConfigMenuPendingPreview.PreviewPendingValues` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:157
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0051"></a>
### DTMAPI.Abstractions.IConfigMenuItem

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:56`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:9
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0052"></a>
### DTMAPI.Abstractions.IConfigMenuItem.ItemId

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.ItemId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:58`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:245`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:245
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0053"></a>
### DTMAPI.Abstractions.IConfigMenuItem.Kind

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.Kind`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:59`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:52`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:52
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0054"></a>
### DTMAPI.Abstractions.IConfigMenuItem.Name

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.Name`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:60`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:62`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:62
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0055"></a>
### DTMAPI.Abstractions.IConfigMenuItem.Tooltip

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.Tooltip`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:61`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:254`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:254
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0056"></a>
### DTMAPI.Abstractions.IConfigMenuItem.DisplayValue

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.DisplayValue`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:62`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:255`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:255
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0057"></a>
### DTMAPI.Abstractions.IConfigMenuItem.PendingValue

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.PendingValue`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:63`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:54`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:54
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0058"></a>
### DTMAPI.Abstractions.IConfigMenuItem.CanEdit

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.CanEdit`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:64`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:255`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:255
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0059"></a>
### DTMAPI.Abstractions.IConfigMenuItem.IsVisible

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.IsVisible`
- Current marker: `experimental`
- Review advice: 保持 experimental；继续用 pending preview/input lifecycle 证据支撑
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:65`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:124`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:124
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0060"></a>
### DTMAPI.Abstractions.IConfigMenuItem.HasPendingChange

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.HasPendingChange`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:66`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:120`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:120
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0061"></a>
### DTMAPI.Abstractions.IConfigMenuItem.ValidationError

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.ValidationError`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:67`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:260`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:260
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0062"></a>
### DTMAPI.Abstractions.IConfigMenuItem.AllowedValues

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.AllowedValues`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:68`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:261`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:261
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0063"></a>
### DTMAPI.Abstractions.IConfigMenuItem.MinValue

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.MinValue`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:69`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:262`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:262
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0064"></a>
### DTMAPI.Abstractions.IConfigMenuItem.MaxValue

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.MaxValue`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:70`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:263`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:263
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0065"></a>
### DTMAPI.Abstractions.IConfigMenuItem.Interval

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.Interval`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:71`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:264`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:264
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0066"></a>
### DTMAPI.Abstractions.IConfigMenuItem.TrySetPendingValue(string value, out string error)

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.TrySetPendingValue(string value, out string error)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:72`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:268`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:268
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0067"></a>
### DTMAPI.Abstractions.IConfigMenuItem.Invoke()

- Symbol: `DTMAPI.Abstractions.IConfigMenuItem.Invoke()`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:73`
- Implementation: `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:286`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:33 `IConfigMenuItem.IsVisible/CanEdit` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:286
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0068"></a>
### DTMAPI.Abstractions.DtmColorPreset

- Symbol: `DTMAPI.Abstractions.DtmColorPreset`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:76`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:34 `DtmColorPreset` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0069"></a>
### DTMAPI.Abstractions.DtmColorPreset.DtmColorPreset(string id, string label, string hexColor)

- Symbol: `DTMAPI.Abstractions.DtmColorPreset.DtmColorPreset(string id, string label, string hexColor)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:78`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:34 `DtmColorPreset` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0070"></a>
### DTMAPI.Abstractions.DtmColorPreset.Id

- Symbol: `DTMAPI.Abstractions.DtmColorPreset.Id`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:85`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:34 `DtmColorPreset` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0071"></a>
### DTMAPI.Abstractions.DtmColorPreset.Label

- Symbol: `DTMAPI.Abstractions.DtmColorPreset.Label`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:86`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:34 `DtmColorPreset` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0072"></a>
### DTMAPI.Abstractions.DtmColorPreset.HexColor

- Symbol: `DTMAPI.Abstractions.DtmColorPreset.HexColor`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/ConfigMenu.cs:87`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:34 `DtmColorPreset` (experimental)
  - update record: docs/updates/2026/20260606-0007-031-regression-new-content-round.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260606-150928
  - hook-map entry: src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0723"></a>
### DTMAPI.Abstractions.DtmMod

- Symbol: `DTMAPI.Abstractions.DtmMod`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/DtmMod.cs:3`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:1`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:1
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0724"></a>
### DTMAPI.Abstractions.DtmMod.Manifest

- Symbol: `DTMAPI.Abstractions.DtmMod.Manifest`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/DtmMod.cs:5`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:258`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:258
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0725"></a>
### DTMAPI.Abstractions.DtmMod.Monitor

- Symbol: `DTMAPI.Abstractions.DtmMod.Monitor`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/DtmMod.cs:6`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:1`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:1
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0726"></a>
### DTMAPI.Abstractions.DtmMod.AttachContext(IManifest manifest, IMonitor monitor)

- Symbol: `DTMAPI.Abstractions.DtmMod.AttachContext(IManifest manifest, IMonitor monitor)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/DtmMod.cs:8`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:378`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:378
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0727"></a>
### DTMAPI.Abstractions.DtmMod.Entry(IDtmHelper helper)

- Symbol: `DTMAPI.Abstractions.DtmMod.Entry(IDtmHelper helper)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/DtmMod.cs:14`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:98`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:98
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0728"></a>
### DTMAPI.Abstractions.IEventsHelper

- Symbol: `DTMAPI.Abstractions.IEventsHelper`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:6`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0729"></a>
### DTMAPI.Abstractions.IEventsHelper.GameLoop

- Symbol: `DTMAPI.Abstractions.IEventsHelper.GameLoop`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:8`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:115`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:115
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0730"></a>
### DTMAPI.Abstractions.IEventsHelper.Input

- Symbol: `DTMAPI.Abstractions.IEventsHelper.Input`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:9`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:116`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:116
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0731"></a>
### DTMAPI.Abstractions.IEventsHelper.Save

- Symbol: `DTMAPI.Abstractions.IEventsHelper.Save`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:10`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:117`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:117
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0732"></a>
### DTMAPI.Abstractions.IEventsHelper.UI

- Symbol: `DTMAPI.Abstractions.IEventsHelper.UI`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:11`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:118`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:118
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0733"></a>
### DTMAPI.Abstractions.IEventsHelper.Workshop

- Symbol: `DTMAPI.Abstractions.IEventsHelper.Workshop`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:12`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:119`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:119
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0734"></a>
### DTMAPI.Abstractions.IEventsHelper.Diagnostics

- Symbol: `DTMAPI.Abstractions.IEventsHelper.Diagnostics`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:13`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:4`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:4
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0735"></a>
### DTMAPI.Abstractions.IGameLoopEvents

- Symbol: `DTMAPI.Abstractions.IGameLoopEvents`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:17`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:9 `IGameLoopEvents.GameLaunched` (stable)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0736"></a>
### DTMAPI.Abstractions.IGameLoopEvents.GameLaunched

- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.GameLaunched`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:19`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:136`
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:9 `IGameLoopEvents.GameLaunched` (stable)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: src/DTMAPI.Core/Services/EventManager.cs:136
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0737"></a>
### DTMAPI.Abstractions.IGameLoopEvents.UpdateTicked

- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.UpdateTicked`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:20`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:137`
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:9 `IGameLoopEvents.GameLaunched` (stable)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: src/DTMAPI.Core/Services/EventManager.cs:137
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0738"></a>
### DTMAPI.Abstractions.IGameLoopEvents.OneSecondUpdateTicked

- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.OneSecondUpdateTicked`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:21`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:138`
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:9 `IGameLoopEvents.GameLaunched` (stable)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: src/DTMAPI.Core/Services/EventManager.cs:138
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0739"></a>
### DTMAPI.Abstractions.IGameLoopEvents.ReturnedToTitle

- Symbol: `DTMAPI.Abstractions.IGameLoopEvents.ReturnedToTitle`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:22`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:139`
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:9 `IGameLoopEvents.GameLaunched` (stable)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: src/DTMAPI.Core/Services/EventManager.cs:139
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-0740"></a>
### DTMAPI.Abstractions.IInputEvents

- Symbol: `DTMAPI.Abstractions.IInputEvents`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:26`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:13 `IInputEvents.ButtonPressed` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0741"></a>
### DTMAPI.Abstractions.IInputEvents.ButtonPressed

- Symbol: `DTMAPI.Abstractions.IInputEvents.ButtonPressed`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:28`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:147`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:13 `IInputEvents.ButtonPressed` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:147
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0742"></a>
### DTMAPI.Abstractions.IInputEvents.ButtonReleased

- Symbol: `DTMAPI.Abstractions.IInputEvents.ButtonReleased`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:29`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:148`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:13 `IInputEvents.ButtonPressed` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:148
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0743"></a>
### DTMAPI.Abstractions.ISaveEvents

- Symbol: `DTMAPI.Abstractions.ISaveEvents`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:33`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:18 `ISaveEvents.SaveLoaded` (experimental)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0744"></a>
### DTMAPI.Abstractions.ISaveEvents.SaveLoaded

- Symbol: `DTMAPI.Abstractions.ISaveEvents.SaveLoaded`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:35`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:156`
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:18 `ISaveEvents.SaveLoaded` (experimental)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: src/DTMAPI.Core/Services/EventManager.cs:156
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0745"></a>
### DTMAPI.Abstractions.ISaveEvents.SaveSaving

- Symbol: `DTMAPI.Abstractions.ISaveEvents.SaveSaving`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:36`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:157`
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:18 `ISaveEvents.SaveLoaded` (experimental)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: src/DTMAPI.Core/Services/EventManager.cs:157
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0746"></a>
### DTMAPI.Abstractions.ISaveEvents.SaveSaved

- Symbol: `DTMAPI.Abstractions.ISaveEvents.SaveSaved`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:37`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:158`
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:18 `ISaveEvents.SaveLoaded` (experimental)
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: src/DTMAPI.Core/Services/EventManager.cs:158
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0747"></a>
### DTMAPI.Abstractions.IUiEvents

- Symbol: `DTMAPI.Abstractions.IUiEvents`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:41`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0748"></a>
### DTMAPI.Abstractions.IUiEvents.MenuOpened

- Symbol: `DTMAPI.Abstractions.IUiEvents.MenuOpened`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:43`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:166`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:166
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0749"></a>
### DTMAPI.Abstractions.IUiEvents.MenuClosed

- Symbol: `DTMAPI.Abstractions.IUiEvents.MenuClosed`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:44`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:167`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:167
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0750"></a>
### DTMAPI.Abstractions.IWorkshopEvents

- Symbol: `DTMAPI.Abstractions.IWorkshopEvents`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:48`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:53 `IWorkshopEvents.ModListChanged` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0751"></a>
### DTMAPI.Abstractions.IWorkshopEvents.ModListChanged

- Symbol: `DTMAPI.Abstractions.IWorkshopEvents.ModListChanged`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Events.cs:50`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:175`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:53 `IWorkshopEvents.ModListChanged` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/EventManager.cs:175
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0752"></a>
### DTMAPI.Abstractions.IDiagnosticsEvents

- Symbol: `DTMAPI.Abstractions.IDiagnosticsEvents`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:54`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:111`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:111
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0753"></a>
### DTMAPI.Abstractions.IDiagnosticsEvents.LogExported

- Symbol: `DTMAPI.Abstractions.IDiagnosticsEvents.LogExported`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:56`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:183`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:183
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0754"></a>
### DTMAPI.Abstractions.IDiagnosticsEvents.HookStatusChanged

- Symbol: `DTMAPI.Abstractions.IDiagnosticsEvents.HookStatusChanged`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:57`
- Implementation: `src/DTMAPI.Core/Services/EventManager.cs:184`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/EventManager.cs:184
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0755"></a>
### DTMAPI.Abstractions.GameLaunchedEventArgs

- Symbol: `DTMAPI.Abstractions.GameLaunchedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:60`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0756"></a>
### DTMAPI.Abstractions.ReturnedToTitleEventArgs

- Symbol: `DTMAPI.Abstractions.ReturnedToTitleEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:61`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0757"></a>
### DTMAPI.Abstractions.UpdateTickedEventArgs

- Symbol: `DTMAPI.Abstractions.UpdateTickedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:62`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0758"></a>
### DTMAPI.Abstractions.UpdateTickedEventArgs.UpdateTickedEventArgs(ulong tick)

- Symbol: `DTMAPI.Abstractions.UpdateTickedEventArgs.UpdateTickedEventArgs(ulong tick)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:64`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0759"></a>
### DTMAPI.Abstractions.UpdateTickedEventArgs.Tick

- Symbol: `DTMAPI.Abstractions.UpdateTickedEventArgs.Tick`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:65`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0760"></a>
### DTMAPI.Abstractions.OneSecondUpdateTickedEventArgs

- Symbol: `DTMAPI.Abstractions.OneSecondUpdateTickedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:68`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0761"></a>
### DTMAPI.Abstractions.OneSecondUpdateTickedEventArgs.OneSecondUpdateTickedEventArgs(uint second)

- Symbol: `DTMAPI.Abstractions.OneSecondUpdateTickedEventArgs.OneSecondUpdateTickedEventArgs(uint second)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:70`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0762"></a>
### DTMAPI.Abstractions.OneSecondUpdateTickedEventArgs.Second

- Symbol: `DTMAPI.Abstractions.OneSecondUpdateTickedEventArgs.Second`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:71`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI runtime dispatch plus DolocAPI.ReturnHome for title return when available.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-gameloopgamelaunched
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0763"></a>
### DTMAPI.Abstractions.ButtonPressedEventArgs

- Symbol: `DTMAPI.Abstractions.ButtonPressedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:74`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0764"></a>
### DTMAPI.Abstractions.ButtonPressedEventArgs.ButtonPressedEventArgs(string button)

- Symbol: `DTMAPI.Abstractions.ButtonPressedEventArgs.ButtonPressedEventArgs(string button)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:76`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0765"></a>
### DTMAPI.Abstractions.ButtonPressedEventArgs.Button

- Symbol: `DTMAPI.Abstractions.ButtonPressedEventArgs.Button`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:77`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0766"></a>
### DTMAPI.Abstractions.ButtonReleasedEventArgs

- Symbol: `DTMAPI.Abstractions.ButtonReleasedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:80`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0767"></a>
### DTMAPI.Abstractions.ButtonReleasedEventArgs.ButtonReleasedEventArgs(string button)

- Symbol: `DTMAPI.Abstractions.ButtonReleasedEventArgs.ButtonReleasedEventArgs(string button)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:82`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0768"></a>
### DTMAPI.Abstractions.ButtonReleasedEventArgs.Button

- Symbol: `DTMAPI.Abstractions.ButtonReleasedEventArgs.Button`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:83`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0769"></a>
### DTMAPI.Abstractions.SaveLoadedEventArgs

- Symbol: `DTMAPI.Abstractions.SaveLoadedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:86`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0770"></a>
### DTMAPI.Abstractions.SaveLoadedEventArgs.SaveLoadedEventArgs(int? saveSlot, bool isNewGame)

- Symbol: `DTMAPI.Abstractions.SaveLoadedEventArgs.SaveLoadedEventArgs(int? saveSlot, bool isNewGame)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:88`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0771"></a>
### DTMAPI.Abstractions.SaveLoadedEventArgs.SaveSlot

- Symbol: `DTMAPI.Abstractions.SaveLoadedEventArgs.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:94`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0772"></a>
### DTMAPI.Abstractions.SaveLoadedEventArgs.IsNewGame

- Symbol: `DTMAPI.Abstractions.SaveLoadedEventArgs.IsNewGame`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:95`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0773"></a>
### DTMAPI.Abstractions.SaveSavingEventArgs

- Symbol: `DTMAPI.Abstractions.SaveSavingEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:98`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0774"></a>
### DTMAPI.Abstractions.SaveSavingEventArgs.SaveSavingEventArgs(int? saveSlot)

- Symbol: `DTMAPI.Abstractions.SaveSavingEventArgs.SaveSavingEventArgs(int? saveSlot)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:100`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0775"></a>
### DTMAPI.Abstractions.SaveSavingEventArgs.SaveSlot

- Symbol: `DTMAPI.Abstractions.SaveSavingEventArgs.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:101`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0776"></a>
### DTMAPI.Abstractions.SaveSavedEventArgs

- Symbol: `DTMAPI.Abstractions.SaveSavedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:104`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0777"></a>
### DTMAPI.Abstractions.SaveSavedEventArgs.SaveSavedEventArgs(int? saveSlot)

- Symbol: `DTMAPI.Abstractions.SaveSavedEventArgs.SaveSavedEventArgs(int? saveSlot)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:106`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0778"></a>
### DTMAPI.Abstractions.SaveSavedEventArgs.SaveSlot

- Symbol: `DTMAPI.Abstractions.SaveSavedEventArgs.SaveSlot`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:107`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DolocAPI.LoadGame/AfterLoadArchiveData/SaveGame and DataPersistenceManager save lifecycle.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260530-0001-branding-and-update-record-system.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-080759
  - hook-map entry: docs/hook-map/README.md#hook-savesaveloaded
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0779"></a>
### DTMAPI.Abstractions.MenuOpenedEventArgs

- Symbol: `DTMAPI.Abstractions.MenuOpenedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:110`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0780"></a>
### DTMAPI.Abstractions.MenuOpenedEventArgs.MenuOpenedEventArgs(string menuId)

- Symbol: `DTMAPI.Abstractions.MenuOpenedEventArgs.MenuOpenedEventArgs(string menuId)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:112`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0781"></a>
### DTMAPI.Abstractions.MenuOpenedEventArgs.MenuId

- Symbol: `DTMAPI.Abstractions.MenuOpenedEventArgs.MenuId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:113`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0782"></a>
### DTMAPI.Abstractions.MenuClosedEventArgs

- Symbol: `DTMAPI.Abstractions.MenuClosedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:116`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0783"></a>
### DTMAPI.Abstractions.MenuClosedEventArgs.MenuClosedEventArgs(string menuId)

- Symbol: `DTMAPI.Abstractions.MenuClosedEventArgs.MenuClosedEventArgs(string menuId)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:118`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0784"></a>
### DTMAPI.Abstractions.MenuClosedEventArgs.MenuId

- Symbol: `DTMAPI.Abstractions.MenuClosedEventArgs.MenuId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:119`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0785"></a>
### DTMAPI.Abstractions.WorkshopModListChangedEventArgs

- Symbol: `DTMAPI.Abstractions.WorkshopModListChangedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:122`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0786"></a>
### DTMAPI.Abstractions.WorkshopModListChangedEventArgs.WorkshopModListChangedEventArgs(int modCount)

- Symbol: `DTMAPI.Abstractions.WorkshopModListChangedEventArgs.WorkshopModListChangedEventArgs(int modCount)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:124`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0787"></a>
### DTMAPI.Abstractions.WorkshopModListChangedEventArgs.ModCount

- Symbol: `DTMAPI.Abstractions.WorkshopModListChangedEventArgs.ModCount`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:125`
- Implementation: No runtime implementation; data contract member only.
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0788"></a>
### DTMAPI.Abstractions.LogExportedEventArgs

- Symbol: `DTMAPI.Abstractions.LogExportedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:128`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0789"></a>
### DTMAPI.Abstractions.LogExportedEventArgs.LogExportedEventArgs(string reportPath)

- Symbol: `DTMAPI.Abstractions.LogExportedEventArgs.LogExportedEventArgs(string reportPath)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:130`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0790"></a>
### DTMAPI.Abstractions.LogExportedEventArgs.ReportPath

- Symbol: `DTMAPI.Abstractions.LogExportedEventArgs.ReportPath`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:131`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0791"></a>
### DTMAPI.Abstractions.HookStatusChangedEventArgs

- Symbol: `DTMAPI.Abstractions.HookStatusChangedEventArgs`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:134`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-0792"></a>
### DTMAPI.Abstractions.HookStatusChangedEventArgs.HookStatusChangedEventArgs(string hookId, string status)

- Symbol: `DTMAPI.Abstractions.HookStatusChangedEventArgs.HookStatusChangedEventArgs(string hookId, string status)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:136`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0793"></a>
### DTMAPI.Abstractions.HookStatusChangedEventArgs.HookId

- Symbol: `DTMAPI.Abstractions.HookStatusChangedEventArgs.HookId`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:142`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-0794"></a>
### DTMAPI.Abstractions.HookStatusChangedEventArgs.Status

- Symbol: `DTMAPI.Abstractions.HookStatusChangedEventArgs.Status`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Events.cs:143`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1574"></a>
### DTMAPI.Abstractions.IDtmHelper

- Symbol: `DTMAPI.Abstractions.IDtmHelper`
- Current marker: `experimental`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:7`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:30`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:30
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1575"></a>
### DTMAPI.Abstractions.IDtmHelper.ModManifest

- Symbol: `DTMAPI.Abstractions.IDtmHelper.ModManifest`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:9`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:45`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:45
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1576"></a>
### DTMAPI.Abstractions.IDtmHelper.Monitor

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Monitor`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:10`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:46`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:46
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1577"></a>
### DTMAPI.Abstractions.IDtmHelper.Events

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Events`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:11`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:47`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:47
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1578"></a>
### DTMAPI.Abstractions.IDtmHelper.Config

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Config`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:12`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:48`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:48
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1579"></a>
### DTMAPI.Abstractions.IDtmHelper.ModRegistry

- Symbol: `DTMAPI.Abstractions.IDtmHelper.ModRegistry`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:13`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:49`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:49
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1580"></a>
### DTMAPI.Abstractions.IDtmHelper.Workshop

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Workshop`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:14`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:50`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:50
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1581"></a>
### DTMAPI.Abstractions.IDtmHelper.UI

- Symbol: `DTMAPI.Abstractions.IDtmHelper.UI`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:15`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:51`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:51
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1582"></a>
### DTMAPI.Abstractions.IDtmHelper.Diagnostics

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Diagnostics`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:16`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:52`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:52
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1583"></a>
### DTMAPI.Abstractions.IDtmHelper.Content

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Content`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:17`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:53`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:53
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1584"></a>
### DTMAPI.Abstractions.IDtmHelper.Input

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Input`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:18`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:54`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:54
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1585"></a>
### DTMAPI.Abstractions.IDtmHelper.Translation

- Symbol: `DTMAPI.Abstractions.IDtmHelper.Translation`
- Current marker: `experimental`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:19`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:55`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:35 `IDtmHelper.Translation` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:55
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1586"></a>
### DTMAPI.Abstractions.ITranslationHelper

- Symbol: `DTMAPI.Abstractions.ITranslationHelper`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:25`
- Implementation: `src/DTMAPI.Core/Services/TranslationService.cs:10`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:36 `ITranslationHelper.Language` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/TranslationService.cs:10
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1587"></a>
### DTMAPI.Abstractions.ITranslationHelper.Language

- Symbol: `DTMAPI.Abstractions.ITranslationHelper.Language`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:27`
- Implementation: `src/DTMAPI.Core/Services/TranslationService.cs:17`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:36 `ITranslationHelper.Language` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/TranslationService.cs:17
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1588"></a>
### DTMAPI.Abstractions.ITranslationHelper.Get(string key, string fallback = "")

- Symbol: `DTMAPI.Abstractions.ITranslationHelper.Get(string key, string fallback = "")`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:28`
- Implementation: `src/DTMAPI.Core/Services/TranslationService.cs:27`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:36 `ITranslationHelper.Language` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/TranslationService.cs:27
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1589"></a>
### DTMAPI.Abstractions.IConfigHelper

- Symbol: `DTMAPI.Abstractions.IConfigHelper`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:32`
- Implementation: `src/DTMAPI.Core/Services/ConfigService.cs:10`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:21 `IConfigHelper.ReadConfig<T>` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/ConfigService.cs:10
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1590"></a>
### DTMAPI.Abstractions.IConfigHelper.GetConfigPath(IManifest manifest)

- Symbol: `DTMAPI.Abstractions.IConfigHelper.GetConfigPath(IManifest manifest)`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:36`
- Implementation: `src/DTMAPI.Core/Services/ConfigService.cs:22`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:21 `IConfigHelper.ReadConfig<T>` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/ConfigService.cs:22
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1591"></a>
### DTMAPI.Abstractions.IModRegistry

- Symbol: `DTMAPI.Abstractions.IModRegistry`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:41`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:9`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:41 `IModRegistry.IsLoaded` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:9
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1592"></a>
### DTMAPI.Abstractions.IModRegistry.IsLoaded(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IModRegistry.IsLoaded(string uniqueId)`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:43`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:15`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:41 `IModRegistry.IsLoaded` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:15
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1593"></a>
### DTMAPI.Abstractions.IModRegistry.Get(string uniqueId)

- Symbol: `DTMAPI.Abstractions.IModRegistry.Get(string uniqueId)`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:44`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:16`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:41 `IModRegistry.IsLoaded` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:16
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1594"></a>
### DTMAPI.Abstractions.IModRegistry.GetAll()

- Symbol: `DTMAPI.Abstractions.IModRegistry.GetAll()`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:45`
- Implementation: `src/DTMAPI.Core/Services/RegistryAndHelpers.cs:17`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:41 `IModRegistry.IsLoaded` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/RegistryAndHelpers.cs:17
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1669"></a>
### DTMAPI.Abstractions.LogLevel

- Symbol: `DTMAPI.Abstractions.LogLevel`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:5`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1670"></a>
### DTMAPI.Abstractions.LogLevel.Trace

- Symbol: `DTMAPI.Abstractions.LogLevel.Trace`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:7`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1671"></a>
### DTMAPI.Abstractions.LogLevel.Debug

- Symbol: `DTMAPI.Abstractions.LogLevel.Debug`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:8`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1672"></a>
### DTMAPI.Abstractions.LogLevel.Info

- Symbol: `DTMAPI.Abstractions.LogLevel.Info`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:9`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:24`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Logging/FileMonitor.cs:24
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1673"></a>
### DTMAPI.Abstractions.LogLevel.Warn

- Symbol: `DTMAPI.Abstractions.LogLevel.Warn`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:10`
- Implementation: `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:300`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:300
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1674"></a>
### DTMAPI.Abstractions.LogLevel.Error

- Symbol: `DTMAPI.Abstractions.LogLevel.Error`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:11`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:33`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Logging/FileMonitor.cs:33
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1675"></a>
### DTMAPI.Abstractions.LogLevel.Alert

- Symbol: `DTMAPI.Abstractions.LogLevel.Alert`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:12`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1676"></a>
### DTMAPI.Abstractions.IMonitor

- Symbol: `DTMAPI.Abstractions.IMonitor`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:16`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:1`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:38 `IMonitor.Log` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Logging/FileMonitor.cs:1
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1677"></a>
### DTMAPI.Abstractions.IMonitor.Log(string message, LogLevel level = LogLevel.Info)

- Symbol: `DTMAPI.Abstractions.IMonitor.Log(string message, LogLevel level = LogLevel.Info)`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:18`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:24`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:38 `IMonitor.Log` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Logging/FileMonitor.cs:24
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1678"></a>
### DTMAPI.Abstractions.IMonitor.LogOnce(string key, string message, LogLevel level = LogLevel.Info)

- Symbol: `DTMAPI.Abstractions.IMonitor.LogOnce(string key, string message, LogLevel level = LogLevel.Info)`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:19`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:41`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:38 `IMonitor.Log` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Logging/FileMonitor.cs:41
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1679"></a>
### DTMAPI.Abstractions.IMonitor.LogException(Exception exception, string message)

- Symbol: `DTMAPI.Abstractions.IMonitor.LogException(Exception exception, string message)`
- Current marker: `stable`
- Review advice: 保持
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:20`
- Implementation: `src/DTMAPI.Core/Logging/FileMonitor.cs:51`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:38 `IMonitor.Log` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Logging/FileMonitor.cs:51
- Result: OK
- Recommendation: Keep current public contract; no implementation goal is created by this audit.

<a id="sym-1680"></a>
### DTMAPI.Abstractions.NullMonitor

- Symbol: `DTMAPI.Abstractions.NullMonitor`
- Current marker: `not-in-matrix`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:23`
- Implementation: No runtime implementation; public type is a data/contract surface or enum.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1681"></a>
### DTMAPI.Abstractions.NullMonitor.Instance

- Symbol: `DTMAPI.Abstractions.NullMonitor.Instance`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:25`
- Implementation: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:58`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:58
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1682"></a>
### DTMAPI.Abstractions.NullMonitor.Log(string message, LogLevel level = LogLevel.Info)

- Symbol: `DTMAPI.Abstractions.NullMonitor.Log(string message, LogLevel level = LogLevel.Info)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:26`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1683"></a>
### DTMAPI.Abstractions.NullMonitor.LogOnce(string key, string message, LogLevel level = LogLevel.Info)

- Symbol: `DTMAPI.Abstractions.NullMonitor.LogOnce(string key, string message, LogLevel level = LogLevel.Info)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:27`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1684"></a>
### DTMAPI.Abstractions.NullMonitor.LogException(Exception exception, string message)

- Symbol: `DTMAPI.Abstractions.NullMonitor.LogException(Exception exception, string message)`
- Current marker: `not-in-matrix`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Logging.cs:28`
- Implementation: No runtime implementation; data contract member only.
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: No direct evidence
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1685"></a>
### DTMAPI.Abstractions.IManifest

- Symbol: `DTMAPI.Abstractions.IManifest`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:6`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:1`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:1
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1686"></a>
### DTMAPI.Abstractions.IManifest.Name

- Symbol: `DTMAPI.Abstractions.IManifest.Name`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:8`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:10`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:10
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1687"></a>
### DTMAPI.Abstractions.IManifest.Author

- Symbol: `DTMAPI.Abstractions.IManifest.Author`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:9`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:11`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:11
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1688"></a>
### DTMAPI.Abstractions.IManifest.Version

- Symbol: `DTMAPI.Abstractions.IManifest.Version`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:10`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:12`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:12
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1689"></a>
### DTMAPI.Abstractions.IManifest.Description

- Symbol: `DTMAPI.Abstractions.IManifest.Description`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:11`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:13`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:13
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1690"></a>
### DTMAPI.Abstractions.IManifest.UniqueID

- Symbol: `DTMAPI.Abstractions.IManifest.UniqueID`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:12`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:14`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:14
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1691"></a>
### DTMAPI.Abstractions.IManifest.EntryDll

- Symbol: `DTMAPI.Abstractions.IManifest.EntryDll`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:13`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:15`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:15
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1692"></a>
### DTMAPI.Abstractions.IManifest.MinimumDTMApiVersion

- Symbol: `DTMAPI.Abstractions.IManifest.MinimumDTMApiVersion`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:14`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:16`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:16
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1693"></a>
### DTMAPI.Abstractions.IManifest.MinimumGameVersion

- Symbol: `DTMAPI.Abstractions.IManifest.MinimumGameVersion`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:15`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:18`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:18
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1694"></a>
### DTMAPI.Abstractions.IManifest.Type

- Symbol: `DTMAPI.Abstractions.IManifest.Type`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:16`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:19`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:19
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1695"></a>
### DTMAPI.Abstractions.IManifest.Dependencies

- Symbol: `DTMAPI.Abstractions.IManifest.Dependencies`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:17`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:23`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:23
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1696"></a>
### DTMAPI.Abstractions.IManifest.UpdateKeys

- Symbol: `DTMAPI.Abstractions.IManifest.UpdateKeys`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:18`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:24`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:24
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1697"></a>
### DTMAPI.Abstractions.IManifestDependency

- Symbol: `DTMAPI.Abstractions.IManifestDependency`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:22`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:1`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:1
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1698"></a>
### DTMAPI.Abstractions.IManifestDependency.UniqueID

- Symbol: `DTMAPI.Abstractions.IManifestDependency.UniqueID`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:24`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:14`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:14
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1699"></a>
### DTMAPI.Abstractions.IManifestDependency.MinimumVersion

- Symbol: `DTMAPI.Abstractions.IManifestDependency.MinimumVersion`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:25`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:48`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:48
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1700"></a>
### DTMAPI.Abstractions.IManifestDependency.Required

- Symbol: `DTMAPI.Abstractions.IManifestDependency.Required`
- Current marker: `stable`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Manifest.cs:26`
- Implementation: `src/DTMAPI.Core/Manifesting/ManifestModels.cs:49`
- Native owner: DTMAPI Abstractions/Core contract; no direct Doloc native owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Manifesting/ManifestModels.cs:49
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.
