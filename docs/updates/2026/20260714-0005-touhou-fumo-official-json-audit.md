# 20260714-0005 Touhou Fumo Official JSON Audit

## Metadata

- Update ID: `20260714-0005`
- Date: 2026-07-14
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: review/third-party/official-json/content-pack/equipment/store
- Source: user requested a read-only official-example review of `D:\下载\touhou_fumo.zip`, a third-party pure-JSON Mod unrelated to DTMAPI

## Scope

This documentation-only Update records the static audit of the supplied archive against the current local official example, official Workshop authoring pages, and the current public-build JSON loader/table behavior.

It does not modify or copy the third-party archive, install or enable the Mod, launch the game, alter DTMAPI, or claim player-visible verification.

## Changed Files

- `docs/reviews/code/2026/20260714-0002-touhou-fumo-official-json-audit.md`
  - records the artifact identity, one blocking table-filename typo, downstream no-error behavior, non-blocking source/version/tag findings, confirmed-valid fields and paths, and static/runtime acceptance gates.
- `docs/updates/2026/20260714-0005-touhou-fumo-official-json-audit.md`
  - owns the documentation lifecycle and validation boundary for this audit.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update from the July ledger.

## Validation

- `touhou_fumo.zip` was inspected read-only at SHA-256 `BE7346EC8AEB00B3915CB637EEF3C0F55536FF8338E99F4AC6610DDDA6F0EC7D`.
- All four JSON documents parsed successfully.
- The three Content JSON basenames were compared with current public-build configuration tables: `equipment_tbequipment` and `mod_tbmodstoreextension` matched; `item_tbtiem` did not, while the official/current required name is `item_tbitem`.
- The locally installed official `0.96.06` decoration example and official authoring pages were compared field by field.
- Current public-build source/config references confirmed filename-keyed loading, silent non-consumption of an unknown basename, null item-reference skipping in the store-extension handler, and validity of `hult_shop`.
- All five PNGs decoded; referenced asset names resolved; dimensions and alpha bounds were inspected; the normal and `_flip` equipment sprites differ by zero pixels after horizontal mirroring.
- Documentation governance and diff checks are the final repository validation gates.

No game was launched. Runtime validation is not required to record this static audit, and the review explicitly preserves a separate runtime acceptance gate for the corrected author package.

## Evidence

The external third-party archive remains at its supplied path and was not copied into the repository. Durable findings and reproduction logic are owned by the related review rather than by a redistributed artifact.

## Rollback

Remove this Update, its July ledger row, and the related review together. There is no runtime, game-file, package, API, or third-party content rollback.

## Follow-Up

The third-party author should rename `item_tbtiem.json` to `item_tbitem.json`, then choose whether the item is shop-only or craftable and align its handbook `source`. A rebuilt archive should pass the review's static gates before the optional fresh-process Hult shop/place/flip/restart runtime check.
