# 20260721-0006 AutoFishing / OneActionComplete Platform Comparison

Date: `2026-07-21`

Status: `verified — minimum two-consumer promotion implemented`

Scope: Compare the two admitted real Advanced products after AutoFishing D.5 and the focused OneActionComplete migration. This Review decides whether any repeated code has proved Platform or SharedNative ownership. It does not admit a third product, add a public API, implement Content Host G7, publish 0.5.5, or treat textual similarity as shared ownership.

## Compared Products

| Boundary | AutoFishing | OneActionComplete | Decision |
| --- | --- | --- | --- |
| ConfigMenu glue | Registers product-specific fishing options, F6 keybind and live reconfiguration through `IDtmConfigMenuApi`. | Registers product-specific completion options, F11 keybind and live reconfiguration through the same API. | The API, input registration and config persistence are already Platform. Product labels, defaults and callbacks stay local; a second wrapper would add indirection without a shared native responsibility. |
| Lifecycle | Owns an active fishing session, per-frame updater, native cache, input/animation overrides, title/save cleanup and restoration. | Has no update loop or animation state; owns two callbacks, event/input registrations, log dedupe and exact-owner unpatch. | Core owner/event/input cleanup remains Platform. The unlike product state machines stay ProductNative; no shared session/lease layer is justified. |
| Harmony owner | Owns 22 fishing Hooks installed only after full target resolution, with canonical owner rollback. | Owns two completion Postfixes installed only after both targets resolve, with canonical owner rollback. | Canonical owner derivation, receipt binding and Loader cleanup are already SDK/Platform. Hook inventories and callback state are native-domain-specific and remain local. |
| SDK and package | SDK policy, receipt and tracked package filename for the AutoFishing identity. | Independent SDK policy, receipt and tracked package filename for the OneActionComplete identity. | Two real consumers justify one Catalog-driven Advanced validate/build/pack/package implementation plus thin identity wrappers. This is the only new Platform promotion in this comparison. |
| Doctor and Manager | Projected through generic Advanced identity, reference compatibility, native-risk and restart fields. | Uses the same projections without product-specific Doctor/Manager code. | Existing Platform is sufficient. Add Catalog data and rejection checks, not product-aware UI branches or a new receipt family. |
| QA seam | Optional QA assembly activates a bounded reflection observer because fishing has long-running native state, restoration and historical GC risks. No resident player counters are maintained merely for QA. | Stateless callbacks are covered by source/native-anchor and SDK package checks; the bounded runtime acceptance observed product state, native effects and logs externally. | The seams are intentionally different. No shared QA ABI, player counter surface or production observer is promoted. |

## Native-Owner Comparison

AutoFishing consumes the fishing Ready/Cast/Wait/Pull, minigame, `DolocUserInput`, rod-renderer and Hook-physics owners. OneActionComplete consumes `ToolCollider.HandleTools`, `ResourceFellData`, `DungeonResource._Fell`, `AgentStateInteract.OnExit`, `PowerGeneratorFuel`, `Feeder` and item consumption. These sets do not contain a common native state holder, write/restore token, conflict point or global lifecycle invariant.

`AgentStateInteract.OnExit` is also observed by ActionSpeed in mandatory GameBridge, but ActionSpeed restores animator state while OneActionComplete performs product-owned item accounting. Independent owner-scoped Postfixes do not by themselves prove SharedNative ownership. A common dispatch is reconsidered only if real ordering/conflict evidence appears.

## Promotion Decision

Promote only the generic Advanced SDK/package tooling boundary:

- one implementation selects an admitted Advanced/ProductNative/AuthorSDK Catalog row and performs SDK validation, build, pack and package-content checks;
- product wrappers forward only their stable Catalog IDs;
- every managed Author SDK release definition declares its tracked `AuthorSdkBuildScript` and `AuthorSdkPackageFile`;
- release packaging and local installation derive distinct artifact roots per UniqueID;
- the release contract accepts distinct primary/repeat artifacts for the two admitted identities and fails closed for an unadmitted third Author SDK release product;
- the Catalog checker iterates the exact admitted Advanced set for mandatory-Runtime zero-leftover checks instead of branching on product identity;
- player uninstall and developer transaction tests assert the exact two-product Author SDK set and exclude both from the legacy directory-package lane.

Do not promote any new SharedNative adapter, public callback API, common ConfigMenu wrapper, lifecycle/session layer, Harmony dispatcher, Doctor/Manager special case or QA ABI. Repeated surface calls are not a common native owner.

## Result

Comparison: **PASS**. The two real products validate the existing Platform identity/config/input/lifecycle/SDK/Doctor/Manager contracts. The minimum newly justified Platform change is the Catalog-driven Advanced build/package/validation path and generic live zero-leftover loop. SharedNative promotion count is zero because AutoFishing and OneActionComplete have no common native owner.

Implementation and focused validation are owned by [20260721-0005 OneActionComplete Second Advanced Product](../../../updates/2026/20260721-0005-oneactioncomplete-second-advanced-product.md).
