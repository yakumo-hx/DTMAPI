# 20260809-0002: Document Governance And Product Authority Split

## Metadata

- Update ID: `20260809-0002`
- Date: `2026-08-09`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求整理文件治理系统，修正 `PROJECT.md`、cutoff Smoke Matrix、动态月份检查和月度非权威摘要；冻结 0.6 路线图与 Batch 6 / 0.5.5 历史范围，并把受管产品准入与当前公开/订阅集合分成独立权威。

## Summary

- 建立 Catalog 派生、版本无关的 managed-product admission registry；把当前 Steam subscription membership 放入独立 manifest，并让当前产品发布集合只经 Catalog、该 manifest 与 Catalog 指向的最新 release Update 路由。
- 以 2026-08-11 重新下载后的 Steam 订阅包为玩家制品重新登记 Runtime `0.6.1`，并清除“尚未发布”与已消费 `0.5.5` 上传例外；没有当前上传任务时，release stop 保持零授权入口。
- 把 Batch 6 重新定性为冻结的 0.5.5 历史 annex；把 0.6 大 Update 收敛成未完成事务 handoff 后冻结，不再作为后续直接实施依据。
- 修正月账/年路由规则，增加闭月非权威摘要，cutoff 当前 Smoke Matrix，并使治理 checker 自动发现所有年/月账与校验闭月摘要。

## User-Visible Impact

- 无 Runtime、Mod、存档或游戏行为变化。
- 后续 Codex 不再从 Batch 6 或冻结 0.6 长记录推导当前准入、发布或订阅状态。

## Changed Files

- Current authority and navigation: `AGENTS.md`, `PROJECT.md`, `README.md`, `src/README.md`, `author-sdk/README.md`, `docs/guides/install-dev-preview.md`, `docs/onboarding/current-state.md`, `docs/architecture/README.md`, `docs/planning/README.md`, `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`, `docs/workflows/codex-api-rebuild.md`, `docs/reviews/api/native-owner-domains/INDEX.md`, and `docs/design/dtmapi-manager-ui-mvp.md`.
- Product authority: `tools/release/dtmapi-product-catalog.json`, `tools/release/current-subscription-manifest.json`, `docs/architecture/managed-product-admission-registry.md`, and `docs/architecture/batch6-managed-mod-identity-contract.md`.
- Frozen historical routing: `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md` and `docs/updates/phase-map-20260706.md`.
- Governance and ledgers: `docs/workflows/document-governance.md`, `docs/updates/README.md`, `docs/updates/INDEX-2026-07.md`, and `docs/updates/INDEX-2026-08.md`.
- Smoke/evidence cutoff: `docs/debug/INDEX.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/debug/regressions/smoke-matrix-history-20260804-through-20260809.md`, and `docs/debug/evidence-retention-allowlist.json`.
- Generators and checks: `tools/scripts/generate-managed-product-admission-registry.ps1`, `tools/scripts/check-product-catalog.ps1`, `tools/scripts/check-doc-governance.ps1`, `tools/scripts/check-release-contract.ps1`, `tools/scripts/release-common.ps1`, `tools/scripts/test-dtmapi-060-release-artifact-set.ps1`, `tools/scripts/test-runtime-candidate-published-info-boundary.ps1`, `tools/scripts/test-batch6-moresaves-advanced-product.ps1`, `tools/scripts/test-runtime-evidence-retention.ps1`, and `tests/DTMAPI.UnitTests/Program.cs`.
- Governed-link repairs: the seven July code Reviews whose current relative links were wrong now resolve directly; removed historical source references in the Batch 4 Review are commit-bound to `8bf018505a01` rather than pretending to resolve against current HEAD.
- Lifecycle owner: this Update and its row in the August ledger.

## Validation

- `tools/scripts/generate-managed-product-admission-registry.ps1 -Check`: passed; 12 Catalog-derived Advanced rows match the generated registry.
- `tools/scripts/check-doc-governance.ps1`: passed; 6,640 governance/link/frozen-file checks.
- `tools/scripts/check-product-catalog.ps1`: passed; 27 products, 11 current public products, 22 Workshop items, and 48 API rows.
- `tools/scripts/test-batch6-moresaves-advanced-product.ps1`: passed; the focused historical consumer now follows the generated registry.
- `tools/scripts/test-batch6-phase0-contract.ps1`: passed; the frozen Phase 0 receipt remained reproducible and its historical contract remained intact.
- `tools/scripts/test-runtime-evidence-retention.ps1`: passed; 513 source files, 961 smoke runs, 62 runtime identities, and 35 durable roots.
- `tools/scripts/test-runtime-candidate-published-info-boundary.ps1`: passed against the Steam subscription Runtime with expected build commit `db5e518a6d7f`; the positive projection, six package negatives and two Catalog-owner negatives all closed.
- `tools/scripts/test-dtmapi-060-release-artifact-set.ps1`: passed; current-published Advanced projection is now driven by exact observed Catalog artifacts, remains independent of future `releaseStop` authorization, and admits a later MoreEquipment artifact only after it is observed.
- Final closure checks passed after the lifecycle metadata was closed: nine changed PowerShell scripts parsed cleanly under PowerShell 7 and Windows PowerShell 5.1, three changed JSON files parsed cleanly, generated-output drift checks passed, and `git diff --check` reported no whitespace errors.
- A full `tools/scripts/test.ps1 -Configuration Release` diagnostic run built the tree and advanced through Unit, QA, InstallDoctor, Runtime package and Author SDK gates into the developer local-install transaction, then the outer command timed out after 604 seconds. This is recorded as a non-acceptance partial run, not a complete Release PASS; a complete from-start Release run remains reserved for the later frozen MoreEquipment candidate. No real Runtime installation or game smoke was run for this governance change.

## Runtime 0.6.1 Published-Artifact Reconciliation

The Steam-managed subscription root was inspected read-only after Steam had
redownloaded the published Runtime. It is now the current player artifact, not
a source candidate:

- Workshop item `3743016467`, manifest `1868452160403489623`, Steam update
  time `2026-08-08T00:24:14Z`;
- `info.json` version `0.6.1`, 9,192 bytes, SHA-256
  `5d7e4538d34621efffa91b190535e2e274e237c97070aeaf2f46ea2ab17f1171`;
- strict-ordinal Steam tree, excluding generated
  `Content/.tools/bepinex/extract/**`: 28 files, 3,822,058 bytes, SHA-256
  `8280dcfb2bdfae72107cb7e813e057c6f3e76fdd01acc7141ac7b85bbe934aa3`;
- player payload after excluding `workshop.json`: 27 files, 3,822,025 bytes,
  SHA-256
  `c733593445cb784854e3ca105247f4097cdf61efabf1a0c9c309ed63ac174fef`;
- `release-manifest.json` records binary `0.6.1.0`, assembly compatibility
  `0.5.3.0`, build commit `db5e518a6d7f`, five Runtime assemblies and the
  dormant Compatibility Host.

The independent player-package matrix copied this subscription to a managed
temporary path and passed with zero blockers, including Windows PowerShell 5.1
parsing, path-with-spaces/non-ASCII install, missing/invalid target rejection,
post-install status, log collection and post-uninstall truthfulness. Summary:
`tmp/test-runs/runtime-061-published-subscription-audit/DTMAPI Workshop Audit
20260811-015144/Results/stress-summary.md`.

No Steam cache byte, real game directory, official upload folder, player save
or sidecar was modified. Catalog `currentSourceBaseline` and
`currentPublishedArtifact` now agree on version and deterministic `info.json`
bytes while remaining separate authorities. `futureTarget` is unscheduled;
the consumed 0.5.5 upload list is no longer carried as reusable authorization.
The subscription manifest's `latestReleaseUpdate` points here independently
of the empty next-upload `authorizationUpdate`.

## Evidence

- Catalog projection, admission-registry generation, current-subscription-set validation and exact published-artifact digest checks all passed.
- Dynamic year/month discovery, July closed-month synthesis, frozen historical hashes, Smoke cutoff sizing and governed relative-link checks all passed.
- The regenerated evidence-retention allowlist includes the new frozen Smoke history without treating the pending MoreEquipment rebaseline Review as Runtime evidence.
- Final source diff checks passed and remained docs/source-only; no game directory, Steam subscription bytes or player data were mutated.

## Related Records

- Historical Batch 6 annex: `docs/architecture/batch6-managed-mod-identity-contract.md`.
- Frozen 0.6 route: `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`.
- Historical exact product-set upload authorization: `docs/updates/2026/20260801-0002-workshop-upload-release-closeout.md`.
- Historical Runtime 0.5.5 published metadata: `docs/updates/2026/20260801-0003-runtime-published-metadata-authority.md`.
- Current Runtime 0.6.1 published artifact and latest release route: this Update.

## Rollback Notes

- Revert the governance/router/checker changes, remove the generated registry and subscription manifest, restore the prior Catalog pointers and digest, and remove this Update plus its August row.
- Do not roll back or edit historical runtime evidence, product packages, Steam subscription bytes or player data; none were changed.

## Follow-Up

- Future admission changes must change the Catalog through their own bounded Update, regenerate the registry, and pass the Catalog checker.
- Future subscription captures replace the versionless manifest only with fresh native Steam-manifest provenance; they do not change admission.
- Future release work creates or updates its own release Update and Catalog pointer. Frozen Batch 6 and 0.6 records remain audit-only and receive no implementation appendices.
