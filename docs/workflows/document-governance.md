# DTMAPI Document Governance

Status: active for records created after `20260711-0007`.

This policy keeps one canonical owner per fact, preserves historical evidence, and prevents every implementation from rewriting every ledger.

## Canonical Ownership

| Fact | Canonical owner | Other documents do |
| --- | --- | --- |
| project-wide stable invariants such as Mod identity classes, physical ownership classification, and native-save commit semantics | `PROJECT.md` | architecture/product records project the invariant onto current implementations without redefining it |
| implementation scope, progress, changed files, final validation, rollback | one `docs/updates/YYYY/...` record | link only |
| pre-implementation facts, hypotheses, rejected paths, acceptance gates | one task-specific `docs/reviews/...` record when needed | link only |
| recurring runtime symptom, attempts, rejected hypotheses, current issue state | one `docs/debug/issues/ISSUE-...md` file | link only |
| one actual game/runtime validation result | active smoke matrix row plus evidence path | Update/Issue interprets it |
| Hook signature, patch owner, lifecycle, Hook evidence | focused Hook map | Update links it |
| public API contract and stability | public API matrix | Review/Update links it |
| workspace branch, HEAD, version, lock, and process | Git/version sources/status script | onboarding routes to them |

Historical Goal handoffs and cutoff snapshots are read-only audit material.

## Lifecycle

1. **Discussion**: no file unless the user asks for one.
2. **Review when needed**: record durable reasoning for repeated, high-risk, manual-QA, API/native-owner, lifecycle, Hook, input, save/load, UI, vehicle, machine, or official-content work.
3. **Implementation**: create one Update with `Lifecycle Status: in-progress`. That record is the durable task boundary.
4. **Validation**: update only the specialized ledgers whose owned facts changed.
5. **Acceptance**: when an independent audit is part of the gate, first move the Update to `implemented`; the audit either returns it for correction or admits it to `verified`.
6. **Finish**: set the Update lifecycle and validation dimensions truthfully. Do not append an implementation narrative to the Review.

Code, API, and root-cause Reviews may receive a short resolution link, but completion evidence and changed-file lists belong in the Update. A Manual QA gate remains append-only after implementation begins so later user observations, screenshot/log transcriptions, evidence links, and gate outcomes stay with the original feedback; it must not absorb the implementation narrative or final lifecycle state. Debug issues may remain open after one Update is verified.

An audit-only request creates at most one Review and does not create an Update unless the audit changes project files. Do not mark an Update `verified` before a planned independent acceptance audit and then create a second Update solely to record that audit. Corrections found during acceptance remain in the original implementation Update until the single final verification.

## Assurance Proportionality

- A current invariant has one canonical machine authority. Other records link to it; they do not copy commit IDs, receipt hashes, exact status prose, or the full completion narrative.
- Reuse one generic receipt format and validator across milestones and products where the evidence shape is the same. A new schema/builder/checker family is justified only by a new authority boundary, not by a new Batch number, gate label, or product name.
- Adjacent ownership, reclassification, compatibility, consumer, and package checks for one migration should share one migration evidence manifest and reference existing Catalog, SDK, ABI, Doctor, and release authorities. Consolidation may not replace a live zero-leftover invariant with a net-delta-only proxy.
- Historical milestone receipts remain immutable audit evidence, but they are not automatically permanent default-suite gates. Promote only a small live invariant that can regress on ordinary changes; run full historical replay manually, at a milestone, or for release.
- "Atomic" describes the final externally visible admission or switch. Implementation may be split into small reversible commits while the feature remains blocked or hidden.
- Select validation by changed risk. Documentation-only work uses document/link checks; focused source work uses focused unit/source checks; Loader/package/lifecycle work uses focused integration checks; game smoke and the complete Release suite are reserved for the final relevant integration or release boundary.
- "One bounded game acceptance" describes the minimum final evidence shape, not a maximum number of process launches. A non-acceptance or valid failed run enters the ordinary repair loop: pass affected focused checks, then rerun the smallest relevant game smoke as needed. Only an explicit current user instruction with a numeric/process cap can forbid that rerun; a request to avoid full Release, ladders, long/GC tests, or repeated broad regression cannot be promoted into such a cap.
- A failed complete suite starts a repair loop, not another immediate complete run. Pass the failed gate with its focused entry point, run the still-unreached gates once as a non-acceptance diagnostic tail where safe, and batch the discovered corrections. Only after that tail is green should the frozen final candidate receive one clean from-start complete suite. Re-run earlier gates before then only when the repair can affect them, provenance is uncertain, or there is no safe focused/tail route. Diagnostic partial results cannot be spliced into a formal complete-suite PASS.
- Do not create a checkpoint receipt family, cached-pass authority, or parallel assurance system for this workflow. Existing child scripts or a lightweight non-authoritative `-StartAt`/`-Only` route are sufficient for diagnosis. Formal interruption resume is allowed only when the runner already binds an unchanged exact HEAD, tracked tree, configuration, toolchain, and required artifacts; otherwise the final acceptance run starts cleanly from the beginning.

## Conditional Write Matrix

| Artifact | Write when |
| --- | --- |
| Update record and monthly index | every non-trivial project change |
| Review | durable pre-implementation reasoning is needed |
| Debug issue | a recurring symptom, hypothesis, issue state, or runtime evidence changed |
| Smoke matrix | the game or runtime harness actually ran |
| Hook map | Hook target, signature, owner, lifecycle, status, or evidence changed |
| API matrix | public contract, status, limitation, or evidence changed |
| Onboarding router | a canonical source path moved |
| Root indexes | navigation structure changed, not for ordinary entries |

## Normalized Update Metadata

New Update records use separate fields:

- `Lifecycle Status`: `proposed`, `in-progress`, `implemented`, `verified`, `blocked`, `reverted`, or `superseded`.
- `Validation Level`: comma-separated subset of `not-run`, `docs`, `source`, `unit`, `runtime`, `player`.
- `Runtime Validation`: `not-required`, `not-run`, `passed`, `failed`, `blocked`, or `partial`.
- `Related Issue State`: `none`, `open`, `monitoring`, `mitigated`, `verified`, `closed`, or `deferred`.

Do not encode all four dimensions into one slash-separated `Status` value. Historical records remain unchanged.

## Index Policy

- Root indexes are routers and should stay below the size limits enforced by `tools/scripts/check-doc-governance.ps1`.
- Full historical rows live in monthly ledgers or dated cutoff snapshots.
- New Update rows go only into `docs/updates/INDEX-YYYY-MM.md`.
- For normalized records, the monthly row's Lifecycle, Validation, Runtime, and Issue columns must use the same allowed values and exactly match the owning Update metadata.
- `INDEX.md` routes to years; `INDEX-YYYY.md` routes to months. Neither contains Update rows.
- New Hook details go into `docs/hook-map/focused/<Domain>.md`.
- Runtime smoke after the cutoff goes into the active smoke matrix.
- Do not append to cutoff snapshots.

## Validation

Run:

```powershell
tools/scripts/check-doc-governance.ps1
```

The checker validates new Update metadata, duplicate IDs, unique monthly ownership, root/year routing, retired Goal creation, router/month size, current-truth volatility, active-rule regressions, and Markdown links in governance entry files.
