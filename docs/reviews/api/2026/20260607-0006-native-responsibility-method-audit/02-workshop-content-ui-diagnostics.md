# Workshop, Content, UI, Diagnostics Symbol-Level Native Responsibility Audit

- Audit: `20260607-0006-native-responsibility-method-audit`
- Scope: public Abstractions symbols in this volume; docs-only review, no implementation goal.
- Locator rule: `Symbol` is the stable key; `Declaration` line numbers are secondary and may drift.

## Top Risks

- `IContentItemInfo` fields can be mistaken for native runtime truth; file/source index must remain separate from `DolocConfig.Tables` truth.
- `IWorkshopHelper` is observational; ordinary mods must not assume DTMAPI can toggle official/Steam enablement.
- `IInputHelper` uses DTMAPI raw key state, not a full `DolocInputSource` action bridge.
- `IUiHelper` opens DTMAPI pages only; success does not prove official panel lifecycle ownership.

## Audit Blocks

<a id="sym-1595"></a>
### DTMAPI.Abstractions.IWorkshopHelper

- Symbol: `DTMAPI.Abstractions.IWorkshopHelper`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:51`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:50 `IWorkshopHelper.GetOfficialMods` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1596"></a>
### DTMAPI.Abstractions.IWorkshopHelper.GetOfficialMods()

- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.GetOfficialMods()`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:53`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:23`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:50 `IWorkshopHelper.GetOfficialMods` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:23
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1597"></a>
### DTMAPI.Abstractions.IWorkshopHelper.GetDtmApiMods()

- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.GetDtmApiMods()`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:54`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:24`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:50 `IWorkshopHelper.GetOfficialMods` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:24
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1598"></a>
### DTMAPI.Abstractions.IWorkshopHelper.IsOfficialEnablementManaged(IWorkshopModInfo mod)

- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.IsOfficialEnablementManaged(IWorkshopModInfo mod)`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:55`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:25`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:50 `IWorkshopHelper.GetOfficialMods` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:25
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1599"></a>
### DTMAPI.Abstractions.IWorkshopHelper.GetEnablementHint(IWorkshopModInfo mod)

- Symbol: `DTMAPI.Abstractions.IWorkshopHelper.GetEnablementHint(IWorkshopModInfo mod)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:56`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:26`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:50 `IWorkshopHelper.GetOfficialMods` (experimental)
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:26
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1600"></a>
### DTMAPI.Abstractions.IWorkshopModInfo

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:60`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1601"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.UniqueID

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.UniqueID`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:62`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:24`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:24
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1602"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.Name

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.Name`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:63`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:164`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:164
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1603"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.Source

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.Source`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:64`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:25`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:25
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1604"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.RootPath

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.RootPath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:65`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:54`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:54
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1605"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.IsEnabledByOfficialPath

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.IsEnabledByOfficialPath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:66`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:13
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1606"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.CanDTMApiToggle

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.CanDTMApiToggle`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:67`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:25`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:25
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1607"></a>
### DTMAPI.Abstractions.IWorkshopModInfo.WorkshopId

- Symbol: `DTMAPI.Abstractions.IWorkshopModInfo.WorkshopId`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:68`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:213`
- Native owner: Official ModManager.ReloadMods observation plus DTMAPI scanner; no official enable/disable write owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260531-0001-player-visible-hotload-f8-cleanup.md
  - smoke path: docs/debug/evidence/HOOK-PROBE/20260530-081752
  - hook-map entry: docs/hook-map/README.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:213
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1608"></a>
### DTMAPI.Abstractions.IUiHelper

- Symbol: `DTMAPI.Abstractions.IUiHelper`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:72`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:422`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:422
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1609"></a>
### DTMAPI.Abstractions.IUiHelper.OpenDtmApiStatusPage()

- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenDtmApiStatusPage()`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:74`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:465`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:465
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1610"></a>
### DTMAPI.Abstractions.IUiHelper.OpenModListPage()

- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenModListPage()`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:75`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:466`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:466
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1611"></a>
### DTMAPI.Abstractions.IUiHelper.OpenConfigPage(string? uniqueId = null)

- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenConfigPage(string? uniqueId = null)`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:76`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:467`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:467
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1612"></a>
### DTMAPI.Abstractions.IUiHelper.OpenErrorPage()

- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenErrorPage()`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:77`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:472`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:472
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1613"></a>
### DTMAPI.Abstractions.IUiHelper.OpenHookStatusPage()

- Symbol: `DTMAPI.Abstractions.IUiHelper.OpenHookStatusPage()`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:78`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:473`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:473
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1614"></a>
### DTMAPI.Abstractions.IUiHelper.ExportLogs()

- Symbol: `DTMAPI.Abstractions.IUiHelper.ExportLogs()`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:79`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:494`
- Native owner: DTMAPI UI/config registry and reflected title/in-save hosts; no official settings owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:54 `IUiHelper.OpenDtmApiStatusPage` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:494
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1615"></a>
### DTMAPI.Abstractions.IDiagnosticsHelper

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:83`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:11`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:60 `IDiagnosticsHelper.GetErrors` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:11
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1616"></a>
### DTMAPI.Abstractions.IDiagnosticsHelper.GetErrors()

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.GetErrors()`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:85`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:25`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:60 `IDiagnosticsHelper.GetErrors` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:25
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1617"></a>
### DTMAPI.Abstractions.IDiagnosticsHelper.GetHookStatuses()

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.GetHookStatuses()`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:86`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:31`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:60 `IDiagnosticsHelper.GetErrors` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:31
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1618"></a>
### DTMAPI.Abstractions.IDiagnosticsHelper.ExportLogs()

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.ExportLogs()`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:87`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:59`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:60 `IDiagnosticsHelper.GetErrors` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:59
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1619"></a>
### DTMAPI.Abstractions.IDiagnosticsHelper.GetLatestLogPath()

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.GetLatestLogPath()`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:88`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:37`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:60 `IDiagnosticsHelper.GetErrors` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:37
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1620"></a>
### DTMAPI.Abstractions.IDiagnosticsHelper.RecordEvidence(string caseId, string summary)

- Symbol: `DTMAPI.Abstractions.IDiagnosticsHelper.RecordEvidence(string caseId, string summary)`
- Current marker: `stable`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:89`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:77`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:60 `IDiagnosticsHelper.GetErrors` (stable)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:77
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1621"></a>
### DTMAPI.Abstractions.IDtmErrorInfo

- Symbol: `DTMAPI.Abstractions.IDtmErrorInfo`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:93`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:1`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:1
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1622"></a>
### DTMAPI.Abstractions.IDtmErrorInfo.Time

- Symbol: `DTMAPI.Abstractions.IDtmErrorInfo.Time`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:95`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:10`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:10
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1623"></a>
### DTMAPI.Abstractions.IDtmErrorInfo.Owner

- Symbol: `DTMAPI.Abstractions.IDtmErrorInfo.Owner`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:96`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:11`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:11
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1624"></a>
### DTMAPI.Abstractions.IDtmErrorInfo.Message

- Symbol: `DTMAPI.Abstractions.IDtmErrorInfo.Message`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:97`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:12`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:12
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1625"></a>
### DTMAPI.Abstractions.IDtmErrorInfo.Details

- Symbol: `DTMAPI.Abstractions.IDtmErrorInfo.Details`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:98`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:13`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:13
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1626"></a>
### DTMAPI.Abstractions.IHookStatusInfo

- Symbol: `DTMAPI.Abstractions.IHookStatusInfo`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:102`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:1`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:1
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1627"></a>
### DTMAPI.Abstractions.IHookStatusInfo.HookId

- Symbol: `DTMAPI.Abstractions.IHookStatusInfo.HookId`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:104`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:26`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:26
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1628"></a>
### DTMAPI.Abstractions.IHookStatusInfo.Status

- Symbol: `DTMAPI.Abstractions.IHookStatusInfo.Status`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:105`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:27`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:27
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1629"></a>
### DTMAPI.Abstractions.IHookStatusInfo.Source

- Symbol: `DTMAPI.Abstractions.IHookStatusInfo.Source`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:106`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:28`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:28
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1630"></a>
### DTMAPI.Abstractions.IHookStatusInfo.Details

- Symbol: `DTMAPI.Abstractions.IHookStatusInfo.Details`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:107`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:13`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:13
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1631"></a>
### DTMAPI.Abstractions.IHookStatusInfo.UpdatedAt

- Symbol: `DTMAPI.Abstractions.IHookStatusInfo.UpdatedAt`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:108`
- Implementation: `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:30`
- Native owner: DTMAPI diagnostics/log/report state; no Doloc gameplay owner.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs:30
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1632"></a>
### DTMAPI.Abstractions.IContentQueryHelper

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:112`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:64 `IContentQueryHelper.FindAssets` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1633"></a>
### DTMAPI.Abstractions.IContentQueryHelper.FindAssets(string contentType)

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.FindAssets(string contentType)`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:114`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:69`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:64 `IContentQueryHelper.FindAssets` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:69
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1634"></a>
### DTMAPI.Abstractions.IContentQueryHelper.GetKnownContentTypes()

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.GetKnownContentTypes()`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:115`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:74`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:64 `IContentQueryHelper.FindAssets` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:74
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1635"></a>
### DTMAPI.Abstractions.IContentQueryHelper.TryReadTextAsset(string relativePath, out string text)

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.TryReadTextAsset(string relativePath, out string text)`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:116`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:79`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:64 `IContentQueryHelper.FindAssets` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:79
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1636"></a>
### DTMAPI.Abstractions.IContentQueryHelper.GetIndexedItems()

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.GetIndexedItems()`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:117`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:91`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:64 `IContentQueryHelper.FindAssets` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:91
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1637"></a>
### DTMAPI.Abstractions.IContentQueryHelper.GetIndexedItem(string itemId)

- Symbol: `DTMAPI.Abstractions.IContentQueryHelper.GetIndexedItem(string itemId)`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:118`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:93`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:64 `IContentQueryHelper.FindAssets` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:93
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1638"></a>
### DTMAPI.Abstractions.IContentAssetInfo

- Symbol: `DTMAPI.Abstractions.IContentAssetInfo`
- Current marker: `experimental`
- Review advice: 保持当前标记；补证据范围说明
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:122`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1639"></a>
### DTMAPI.Abstractions.IContentAssetInfo.ContentType

- Symbol: `DTMAPI.Abstractions.IContentAssetInfo.ContentType`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:124`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:71`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:71
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1640"></a>
### DTMAPI.Abstractions.IContentAssetInfo.RelativePath

- Symbol: `DTMAPI.Abstractions.IContentAssetInfo.RelativePath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:125`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:81`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:81
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1641"></a>
### DTMAPI.Abstractions.IContentAssetInfo.SourceModId

- Symbol: `DTMAPI.Abstractions.IContentAssetInfo.SourceModId`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:126`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:290`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:290
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1642"></a>
### DTMAPI.Abstractions.IContentAssetInfo.SourcePath

- Symbol: `DTMAPI.Abstractions.IContentAssetInfo.SourcePath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: missing
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:127`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:82`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: No direct evidence
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:82
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1643"></a>
### DTMAPI.Abstractions.IContentItemInfo

- Symbol: `DTMAPI.Abstractions.IContentItemInfo`
- Current marker: `experimental`
- Review advice: 保持 read-only/observational；文档区分 file index 与 native runtime truth
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:131`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:34
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1644"></a>
### DTMAPI.Abstractions.IContentItemInfo.ItemId

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.ItemId`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:133`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:121`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:121
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1645"></a>
### DTMAPI.Abstractions.IContentItemInfo.ChineseName

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.ChineseName`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:134`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:204`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:204
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1646"></a>
### DTMAPI.Abstractions.IContentItemInfo.EnglishName

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.EnglishName`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:135`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:205`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:205
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1647"></a>
### DTMAPI.Abstractions.IContentItemInfo.Category

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.Category`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:136`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:206`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:206
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1648"></a>
### DTMAPI.Abstractions.IContentItemInfo.Tags

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.Tags`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:137`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:207`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:207
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1649"></a>
### DTMAPI.Abstractions.IContentItemInfo.IconAssetKey

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.IconAssetKey`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:138`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:208`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:208
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1650"></a>
### DTMAPI.Abstractions.IContentItemInfo.IconPath

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.IconPath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:139`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:209`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:209
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1651"></a>
### DTMAPI.Abstractions.IContentItemInfo.SourceKind

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.SourceKind`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:140`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:124`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:124
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1652"></a>
### DTMAPI.Abstractions.IContentItemInfo.SourceModTitle

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.SourceModTitle`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:141`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:211`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:211
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1653"></a>
### DTMAPI.Abstractions.IContentItemInfo.SourceId

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.SourceId`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:142`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:47`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:47
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1654"></a>
### DTMAPI.Abstractions.IContentItemInfo.WorkshopId

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.WorkshopId`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:143`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:213`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:213
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1655"></a>
### DTMAPI.Abstractions.IContentItemInfo.Enabled

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.Enabled`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:144`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:122`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:122
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1656"></a>
### DTMAPI.Abstractions.IContentItemInfo.EnablementKnown

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.EnablementKnown`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:145`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:215`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:215
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1657"></a>
### DTMAPI.Abstractions.IContentItemInfo.IsDtmApiContent

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.IsDtmApiContent`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:146`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:216`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:216
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1658"></a>
### DTMAPI.Abstractions.IContentItemInfo.RootPath

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.RootPath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:147`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:54`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:54
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1659"></a>
### DTMAPI.Abstractions.IContentItemInfo.ContentPath

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.ContentPath`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:148`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:218`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:218
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1660"></a>
### DTMAPI.Abstractions.IContentItemInfo.LoadOrder

- Symbol: `DTMAPI.Abstractions.IContentItemInfo.LoadOrder`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:149`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:123`
- Native owner: DTMAPI file/source index; native truth only after DolocConfig.Tables/DolocAPI.QueryItemProto confirms runtime load.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:68 `IContentItemInfo` (experimental)
  - update record: docs/updates/2026/20260602-0003-content-index-second-motor.md
  - smoke path: docs/debug/evidence/GAME-SMOKE/20260602-122848
  - hook-map entry: references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Assets_Content.md
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:123
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1661"></a>
### DTMAPI.Abstractions.IInputHelper

- Symbol: `DTMAPI.Abstractions.IInputHelper`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:153`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:373`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:373
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1662"></a>
### DTMAPI.Abstractions.IInputHelper.RegisterButton(string button)

- Symbol: `DTMAPI.Abstractions.IInputHelper.RegisterButton(string button)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:155`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:380`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:380
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1663"></a>
### DTMAPI.Abstractions.IInputHelper.UnregisterButton(string button)

- Symbol: `DTMAPI.Abstractions.IInputHelper.UnregisterButton(string button)`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:156`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:386`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:386
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1664"></a>
### DTMAPI.Abstractions.IInputHelper.GetRegisteredButtons()

- Symbol: `DTMAPI.Abstractions.IInputHelper.GetRegisteredButtons()`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:157`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:392`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:392
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.

<a id="sym-1665"></a>
### DTMAPI.Abstractions.IInputHelper.IsDown(string button)

- Symbol: `DTMAPI.Abstractions.IInputHelper.IsDown(string button)`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:158`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:406`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:406
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1666"></a>
### DTMAPI.Abstractions.IInputHelper.WasPressed(string button)

- Symbol: `DTMAPI.Abstractions.IInputHelper.WasPressed(string button)`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:159`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:407`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:407
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1667"></a>
### DTMAPI.Abstractions.IInputHelper.Suppress(string button)

- Symbol: `DTMAPI.Abstractions.IInputHelper.Suppress(string button)`
- Current marker: `experimental`
- Review advice: 保持 experimental；未来改为 native input action bridge 候选
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: explicit
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:160`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:408`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:408
- Result: Watch
- Recommendation: Keep current marker for now, but preserve evidence scope and ordinary-mod usability notes in future developer docs.

<a id="sym-1668"></a>
### DTMAPI.Abstractions.IInputHelper.GetSuppressedButtons()

- Symbol: `DTMAPI.Abstractions.IInputHelper.GetSuppressedButtons()`
- Current marker: `experimental`
- Review advice: 补 public-api-matrix 显式符号行或明确归入父 contract
- Ordinary mod usability: 普通 mod 可用
- Matrix coverage: family-only
- Declaration: `src/DTMAPI.Abstractions/Helpers.cs:161`
- Implementation: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:409`
- Native owner: DTMAPI raw/reflected input state; future native owner should be DolocInputSource/action maps.
- Evidence:
  - public-api-matrix entry: docs/api/public-api-matrix.md:15 `IInputHelper.IsDown` (experimental)
  - update record: No direct evidence
  - smoke path: No direct evidence
  - hook-map entry: No direct evidence
  - code path: src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:409
- Result: MatrixGap
- Recommendation: MatrixGap: public-api-matrix must add an explicit row or a deliberate grouped contract entry before developer docs claim full API coverage.
