# 20260714-0001 JSON Derived-Value Authoring Constraint

## Metadata

- Update ID: `20260714-0001`
- Date: 2026-07-14
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `mitigated`
- Area: author-docs/content-json/production-time/native-rounding/manual-qa
- Source: user confirmed DTMAPI is not the cause of the Krank garbage-shredder incident and requested a separate durable rule requiring JSON time/divisor calculations

## Source Request And Boundary

The user confirmed that DTMAPI is effectively excluded from the current incident and requested a standalone record for future JSON authoring, especially production-time values affected by ratios or divisors.

This Update changes documentation and durable authoring constraints only. It does not modify DTMAPI Runtime, ActionSpeed, the game, third-party Workshop content, the repaired/player save, official local `MODS`, packages, APIs, Hooks, or native behavior.

## Changed Files

- `author-docs/content-packs/json-derived-value-validation.md`: canonical derived-value and production-time checklist with the Krank calculation case;
- `author-docs/README.md` and `author-docs/content-packs/README.md`: author-doc navigation;
- `docs/reviews/manual-qa/2026/20260713-0002-garbage-shredder-last-run-log-review.md`: append-only user attribution confirmation and link to the canonical rule;
- this Update and `docs/updates/INDEX-2026-07.md`.

## Recorded Constraint

- Locate the native responsibility function that consumes a JSON value.
- Calculate the final effective value after every multiplier, divisor, global unit and native rounding/truncation step for every target group/device.
- Validate native lower/upper bounds rather than only JSON field types.
- Split shared recipe IDs when different groups cannot safely use the same base value.
- Include minimum/boundary values plus save/reload and disable behavior in production-like QA.

The Krank example records `RoundToInt(TimeRatio * CostTime)`, city `TimeRatio=0.0835`, the integer minimum `CostTime=6`, and why `CostTime=1` works in the farm group but produces a zero interval in the city group.

## Validation

- `tools/scripts/check-doc-governance.ps1`: passed with `4511` checks.
- Scoped Markdown trailing-whitespace inspection: passed for the author guide, navigation, Manual QA review, Update, and monthly ledger.
- Scoped `git diff --check`: passed for tracked author navigation and monthly-ledger changes; only expected LF-to-CRLF working-copy notices were emitted.
- The canonical guide preserves the verified native formula, per-group minimum table, user attribution, and cross-domain authoring checklist without copying cryptographic material or plaintext save data.

No build or game runtime validation was required because this Update changes documentation only.

## Runtime Evidence

Not run. No Runtime lock is required because no shared game/runtime environment is touched.

## Rollback

Remove the standalone author guide and its two navigation links, remove the append-only confirmation block only if the user retracts the attribution, and remove this Update/monthly row. No Runtime, Mod, save, Workshop, or package rollback is required.

## Follow-Up

A future Author SDK/static checker may automate native-derived-value validation and source-package attribution. Runtime clamping or transaction repair is a separate design and implementation task and is not authorized by this documentation update.
