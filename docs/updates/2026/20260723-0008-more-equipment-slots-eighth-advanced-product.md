# MoreEquipmentSlots Eighth Advanced Product

## Metadata

- Update ID: `20260723-0008`
- Date: `2026-07-23`
- Lifecycle Status: `implemented`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `partial`
- Related Issue State: `deferred`

## Source Request And Authority

Close the stale seven-product Player Doctor evidence, freeze the old
EquipmentSlots ABI, correct native backpack/mail recovery semantics, introduce
a durable prepared/committed product-data journal, and then rehome
MoreEquipmentSlots as the only admitted eighth Advanced ProductNative product.
The original implementation scope requested one complete Release suite and one
protected third-save enabled/cold-recovery transaction. Those historical gates
produced the evidence below; the reopened P1 reacceptance gate is now the
smaller focused matrix defined separately in this Update.

Admission and correction authority:

- [MoreEquipmentSlots admission Review](../../reviews/code/2026/20260723-0009-eighth-product-more-equipment-slots-admission-review.md)
- [Admission/runtime-weight/API-reuse audit](../../reviews/code/2026/20260723-0010-eighth-product-admission-runtime-weight-and-api-reuse-audit.md)
- [Unsaved save-commit regression Review](../../reviews/manual-qa/2026/20260724-0001-moreequipment-unsaved-save-commit-regression.md)
- [Legacy sidecar migration and cold-recovery Review](../../reviews/manual-qa/2026/20260729-0001-moreequipment-legacy-sidecar-migration.md)
- [Legacy migration P1/P2 audit and bounded transition plan](../../reviews/code/2026/20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md)
- [Migration correction independent audit](../../reviews/code/2026/20260729-0005-moreequipment-migration-correction-update-audit.md)
- [Claim concurrency and cold-route re-audit](../../reviews/code/2026/20260729-0006-moreequipment-claim-and-cold-route-reaudit.md)
- [Claim and SaveLoaded reacceptance audit](../../reviews/code/2026/20260729-0007-moreequipment-claim-and-saveloaded-reacceptance-audit.md)
- [Completed-claim round-1 independent review](../../reviews/code/2026/20260729-0008-moreequipment-completed-claim-independent-review.md)
- [Historical-claim/archive-capture round-2 independent review](../../reviews/code/2026/20260729-0009-moreequipment-historical-claim-and-archive-capture-review.md)
- [Final round-3 independent source review](../../reviews/code/2026/20260729-0010-moreequipment-final-independent-source-review.md)
- [Additional independent review round 1](../../reviews/code/2026/20260730-0001-moreequipment-additional-independent-review-round-1.md)
- [Additional independent review round 2](../../reviews/code/2026/20260730-0002-moreequipment-additional-independent-review-round-2.md)
- [Additional independent review round 3](../../reviews/code/2026/20260730-0003-moreequipment-additional-independent-review-round-3.md)
- [Additional independent review round 4](../../reviews/code/2026/20260730-0004-moreequipment-additional-independent-review-round-4.md)
- [Additional independent review round 5](../../reviews/code/2026/20260730-0005-moreequipment-additional-independent-review-round-5.md)
- [Post-freeze commit-effectiveness audit](../../reviews/code/2026/20260730-0006-moreequipment-post-freeze-commit-effectiveness-audit.md)
- [Final post-freeze source/package acceptance](../../reviews/code/2026/20260730-0007-moreequipment-final-post-freeze-source-package-acceptance.md)
- [Post-fix compatibility-entry audit](../../reviews/code/2026/20260730-0008-moreequipment-post-fix-compatibility-entry-audit.md)
- [Transition fix and test audit](../../reviews/code/2026/20260730-0009-moreequipment-transition-fix-and-test-audit.md)
- [Transition and release closeout re-audit](../../reviews/code/2026/20260730-0010-moreequipment-transition-release-closeout-reaudit.md)
- [Final cold-observer route re-audit](../../reviews/code/2026/20260730-0011-moreequipment-final-cold-oracle-route-reaudit.md)
- [Generic cold-oracle fix re-audit](../../reviews/code/2026/20260730-0012-moreequipment-generic-cold-oracle-fix-reaudit.md)
- [Mail-observer final independent acceptance](../../reviews/code/2026/20260730-0013-moreequipment-mail-observer-final-acceptance.md)
- [Post-mail closeout sub-agent audit](../../reviews/code/2026/20260730-0014-moreequipment-post-mail-closeout-audit.md)
- [Production mail transaction sub-agent audit](../../reviews/code/2026/20260730-0015-moreequipment-production-mail-transaction-audit.md)
- [Production transaction three-pass reaudit and publication deferral](../../reviews/code/2026/20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)

Correction history and current boundary:

- The prior Product-v3 Working/Committed and lifecycle evidence remains valid
  only for documents already written in the nested Product format.
- Commit `0331f56d` closes the first audit's exact-format, save-identity,
  retryable-backup and scoped/global priority defects. Independent Review
  `20260729-0005` then found the remaining cross-save crash claim plus three
  cold-route defects and two documentation-state contradictions.
- Commit `4a395398` and coverage commit `3937628b` attempted the first durable
  claim correction. Review `20260729-0006` found two remaining P1 and four P2:
  a claim-publication TOCTOU, ignored claim-only missing states, incomplete
  completion validation, a resident-Host SaveLoaded route, overclaiming docs
  and imprecise test provenance.
- Implementation commit `b2cb5430` and coverage commit `c5fce186` address that
  re-audit attempt. Review `20260729-0007` confirmed its direct corrections,
  but found production SaveLoaded double dispatch, a cross-process late-claim
  window, missing global-absence completion proof and over-broad test/docs
  claims.
- Implementation commit `9c0645bd` removes the duplicate EquipmentSlots
  SaveLoaded/title owner and makes a late cross-process publisher revalidate
  the exact global/archive/Product state and withdraw only the claim it just
  published. Coverage commit `af455c7d` exercises the production Hook route,
  same-session SaveSaved terminal state, an actual second-process late
  publisher and global recreation after archival. Independent Review
  `20260729-0008` then found that a process crash after late claim publication
  but before self-withdrawal still leaves a stale loser, and that deletion
  retained a completion TOCTOU.
- Commit `6c5542ff` replaces deletion with an atomic durable
  `pending -> completed` claim transition; coverage `7adf2b7d` adds the real
  child-process crash needle, pre-publication global recreation, a crash after
  completed publication, restart verification and legacy state-less pending
  claim compatibility. Round-2 Review `20260729-0009` accepted those direct
  corrections but found three remaining P1: historical terminal state without
  a claim, global hash-to-archive byte TOCTOU and completed validation that
  ignored an eligible `.previous`.
- Commits `34f4d20c`/`12dd3c6d` atomically capture and hash the global source,
  add same-source historical completed backfill and reuse Product
  live/previous authority selection. Commit `8efea22e` makes the real
  child-process fixture portable under the repository `dotnet <dll>` host.
  Final Review `20260729-0010` found the remaining cross-source singleton,
  pending replacement, capture-crash recovery and GlobalFlat terminal-state
  gaps. Commits `574fb6f4`/`6270d521` close those implementation/focused-test
  boundaries; `a5e36d2` freezes the new data/control paths. Additional Review
  `20260730-0001` then found stale loser evidence precedence, an incomplete
  pre-winner Product census and capture-only empty/demand gaps. Commit
  `0ffb6a70` corrects those source and focused-test boundaries. Additional
  Reviews `20260730-0002`/`0003` then found no-winner and pending-winner census
  gaps plus over-broad residue/empty/Host semantics; commits
  `0b016b12`/`dcecbf3e` correct those focused boundaries. Review
  `20260730-0004` found that later Product revision invalidated the immutable
  winner proof; `571e5d55` separates terminal and current-load revision
  semantics. Review `20260730-0005` then found A's own T1 completion path and
  empty-state loser ordering; `33772012` makes completed-winner handling
  identity-based and winner-first. All five requested additional review rounds
  are recorded and corrected. Post-freeze audit `20260730-0006` then found
  three remaining P1 authority gaps plus cold-demand/test-causality/document
  P2 gaps; commit `5a6c59b4` implements their bounded source and focused-test
  corrections. Final independent source/package acceptance and the replacement
  deterministic package pass.
- Earlier U2a/U2b/U2c plus U2c cold-restart evidence remains valid only for
  the conversion paths it executed. Corrected U1/U3/U4, targeted C0 and the
  migrated no-save/save/cold chain are recorded below. The optional player
  claim-kill gate is explicitly withdrawn rather than counted as PASS.
- Review `20260730-0010` reopened only the final migrated cold oracle and
  release-document provenance. QA/runner commit `523fa6df` now counts
  `box_hat` and `grandmas_button` independently across backpack, unaccepted
  mail and Committed sidecar, and
  [`GAME-SMOKE/20260730-161355`](../../debug/evidence/GAME-SMOKE/20260730-161355/)
  passes that exact `NoNativeSave` observation. The implementation lifecycle
  is therefore again `verified/closed`; Catalog publication authority remains
  separate and blocked.

Safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The reviewed native owners are:

- `AgentEquipmentManager.functions`,
  `AgentEquipmentFunction.CreateAgentEquipmentFunction` and
  `AgentEquipmentManager.ReloadParams()` for native effects;
- native `CountItem`/`CostItem`, backpack placement and
  `EmailManager.SendItemAsEmail` for item movement;
- `BodyController.OnAttacked(float,bool,Vector2,out bool)` for vanilla
  shield-first damage handling;
- `AccessoriesBar.__Init()`, `AccessoriesBar.OnStartShow()` and native
  `AccessorySlot` objects for the base equipment panel.

The official
`DolocAPI.TryPlaceInBackpack(Item,bool)` method calls
`SendItemAsEmail` for overflow and then returns `false` regardless of mail
success. The implementation must observe backpack success, explicit mail
success and true failure separately.

## Approved Implementation Boundary

- Rehome exactly `DTMAPI.MoreEquipmentSlotsMod`, Workshop item `3744059735`,
  as an SDK-generated `netstandard2.0` Advanced product at version `1.0.0`,
  minimum DTMAPI `0.5.5`.
- ProductNative owns exactly three product slots, protected sidecar/journal
  state, ordering, configuration, four exact Hooks, native effect adapters,
  reflected clone/listener lifecycle and recovery policy.
- Install all or none of:
  `AgentEquipmentManager.ReloadParams` postfix,
  `BodyController.OnAttacked(float,bool,Vector2,out bool)` prefix,
  `AccessoriesBar.__Init` postfix and
  `AccessoriesBar.OnStartShow` postfix under
  `dtmapi.mod.dtmapi.moreequipmentslotsmod`.
- Native equipment state, backpack/mail storage, vanilla shield priority,
  native `hatItem` appearance and base AccessoriesBar remain game-owned.
- Freeze `IEquipmentSlotsApi` plus its six DTOs as
  Experimental/Deprecated/Frozen with non-error source warnings and unchanged
  binary members. Compatibility Host retains the old arbitrary-owner `0..24`
  semantics; the new product never consumes this API.
- Move the old heavy executor and cold-recovery backend into the existing
  single dormant-shipped Compatibility Host. Mandatory GameBridge may retain
  only the frozen provider/proxy, exact owner coordination and a cheap
  sidecar/journal-presence demand route.
- Add no SharedNative component, public capability, Host, receipt family,
  product-specific assurance framework or game-loaded target other than
  `netstandard2.0`.

## Durable Item Transaction Contract

Atomic JSON replacement alone is insufficient because the native save and
product data are different stores. Ordinary gameplay mutations and
owner/orphan recovery must be distinguishable. Gameplay equip, replace,
unequip, shield damage and shield break remain in a working projection until
the native save succeeds; a prepared journal cannot turn them into an implicit
autosave or replay an abandoned session. A management recovery mutation, or
the window after a proven successful native save, may use a versioned,
save-identity-bound persistent journal with explicit `prepared`/`committed`
reconciliation:

1. at `SaveSaving`, write and durably replace a prepared record naming the
   exact protected entry and intended native destination;
2. attempt exactly one native destination operation, distinguishing backpack
   success, explicit mail success and true failure;
3. after the native `SaveSaved` boundary, promote the committed sidecar and
   journal state so the protected entry is removed only when restart
   reconciliation can prove the native destination owns it;
4. keep or replay the prepared record only for explicit management recovery or
   after proving that the matching native save committed; otherwise roll an
   ordinary gameplay candidate back so native storage, committed sidecar and
   journal together represent exactly one logical item.

Focused fault injection must cover at least:

- interruption before native save, which rolls ordinary gameplay intent back;
- prepared journal followed by native-save failure, which does not retry an
  abandoned gameplay command;
- native save committed before sidecar/journal promotion;
- failed sidecar promote/replace;
- interruption after the committed promotion.

The implementation must prove the three canonical crash windows explicitly:
before native commit, after native commit/before sidecar commit, and after
sidecar commit/before journal cleanup. A cold restart must reconcile each
window without item loss or duplication.

## Acceptance Gates

### Focused

- Current Player Doctor rebuild and exact Catalog-to-Doctor policy equality
  for the seven-product starting baseline.
- Exact retained `IEquipmentSlotsApi` TypeRef/MemberRef and warning metadata;
  product fixed-three semantics separated from Compatibility `0..24`.
- Backpack success, mail success and true failure tests.
- Persistent journal state-machine and three crash-window tests.
- Real Harmony four-target atomic install, both owner orders, residual owner,
  rollback failure, config disable/re-enable and real owner-deactivation
  tests.
- Current-save/archive/player/clock isolation, legacy adoption/archive,
  tail-first recovery, corrupt/truncated fallback and no cross-save adoption.
- Passive, defense-only hat, shield hat, official shield-first behavior and
  unchanged native visual hat tests.
- Clone/listener/function/callback/Hook/root zero-leftover tests.
- Catalog, SDK, package, Doctor, Manager, Host, install and uninstall checks
  over the exact eight implemented products.
- Mandatory Runtime source/symbol gate proving the EquipmentSlots executor,
  sidecar/UI policy and four ProductNative Hook bodies are absent.

### Historical Final Integration

- The original plan called for one complete Release suite. The actual final
  candidate reached the stale evidence-allowlist gate after the expensive
  prefix, then passed the repaired gate and previously unreached diagnostic
  tail; it did not receive another from-start complete-suite PASS.
- The original protected third-save transaction under the runtime lock used:
  1. enabled current Advanced product short launch for four-owner behavior,
     native effects/UI, save/title/reload and no duplication;
  2. cold product-disabled short launch where the existing Host loads only
     for protected recovery, completes the journal/native-save transaction,
     and exits with no product/compatibility transient roots.
- Restore exact save, profile, product source/deployment and inventory/mail
  state; require no remaining `DolocTown.exe`.

This section records already-run historical evidence. It is not the acceptance
gate for the reopened save-commit P1.

### Reopened P1 Reacceptance

- Focused ProductNative and Compatibility Host source/Unit checks must separate
  `GameplayMutation` from `OwnerRecovery` and cover no-native-commit rollback,
  proven-native-commit promotion and exactly-one-item interruption windows.
- Before another game acceptance, split the smoke harness into `NoNativeSave`
  and isolated `NativeSaveExpected` modes. The no-save half must prove the
  player archive and committed equipment sidecar unchanged before any runner or
  external file restoration, record only length/hash/mtime by default, and
  create or write back no routine byte backup on a green run. The save half must
  use a disposable fixture isolated from the live Steam AutoCloud state;
  post-exit restoration of the player's third save is not sufficient isolation.
- One bounded acceptance may then exercise shield damage and break plus
  ordinary equip/replace/unequip across no-save return/cold restart, followed
  by the same changed paths with a successful normal native save in the
  isolated fixture.
- A focused native-save failure/interruption fixture may provide the remaining
  failure-window evidence; reuse the existing journal/store harness and runtime
  lock protocol.
- Independent reacceptance follows the focused fix. No complete Release,
  L0-L5, GC gradient or long test is required for this correction.

## Focused Validation

- The stale seventh-product Player Doctor was rebuilt through the tracked
  release path. Its exact seven-policy installation scan passed with `Exit=0`,
  `artifacts=58`, `errors=0`, `warnings=0`; the previous findings/`Exit=2`
  record remains historical evidence for the stale Doctor.
- The generic InstallDoctor Catalog equality Unit now checks the exact current
  Advanced policy set rather than a product-count constant: 12 tests PASS over
  the eight-product candidate.
- `moreequipment-product` focused Unit: PASS. It covers fixed-three product
  behavior, passive/defense/shield/native-visual semantics, backpack/mail/true
  failure, incoming/outgoing replacement rollback, three journal crash
  windows, durable-store failure, the real four-target product installer,
  production UI reachability and native aggregate restoration during owner
  cleanup.
- `compatibility-host` focused Unit: PASS. It covers legacy `0..24`, both real
  Harmony owner orders, all-or-none rollback, residue, disable/restart,
  environment/title cleanup and exact ProductNative-v3 prepared/committed cold
  journal reconciliation. Product-loaded and mismatched-save documents remain
  fail-closed.
- QA Unit and acceptance-route checks: PASS. The enabled route requires slot 3
  and production equip/unequip plus official save; the cold route requires
  `CoreOnly` product isolation and Host-only recovery.
- Advanced SDK/policy/package focused build: PASS. The final candidate product
  DLL is `93,184` bytes, SHA-256
  `08058B922468EDC64F033A9DECE6EA829206C3662169FD8B6366F4771DDE46C7`;
  the deterministic package SHA-256 is
  `40370DAF406E4C6A45B6A86DD0AAA38B8E3B8BAFDB08817A7BB41F8363E8326A`.
- Catalog and Phase 0 focused contracts: PASS over the exact eight-product
  candidate.
- Relative to the committed seven-product baseline, mandatory GameBridge source
  is `31,336 -> 28,592` physical lines and
  `27,762 -> 25,324` non-empty lines. Its focused-built DLL is
  `937,984 -> 851,456` bytes. This is only a reduction in default-loaded
  Runtime; the Host and product still ship, so no download or total-source
  reduction is claimed.
- Official method-body review confirmed the ambiguous overflow return described
  above.
- The historical clean complete Release PASS belongs to candidate `d38af17a`
  and is not reused as final-candidate evidence. Against final code commit
  `034ea5e6`, one from-start Release run passed build, complete Unit, QA,
  Doctor, Catalog, package and portable gates, then stopped because the
  evidence-retention allowlist had not yet incorporated the final Smoke
  references. The existing allowlist was regenerated and its focused
  retention/check pair passed. Per the user's explicit no-repeat instruction,
  the complete suite was not restarted; the exact unreached tail from syntax
  and transaction matrices through deterministic eight-product packaging,
  release contract, QA semantics, static GC/no-demand plans, retained ABI and
  test-artifact governance then passed once in original order.
- `GAME-SMOKE/20260724-053248` passed the final enabled third-save
  transaction:
  four exact patches/four targets, ProductNative fixed-three behavior,
  `grandmas_button` transferred to the native backpack with
  `native=1; committedSidecar=0; journal=0; logicalItems=1`, then real Loader
  deactivation reduced instance, patches, callback, clones, listeners,
  functions and Core roots to zero, including a native parameter refresh when
  no function lease remained. Title, save, profile, Author source and process
  restoration all passed.
- `GAME-SMOKE/20260724-053342` passed the final independent cold launch. A
  smoke-staged exact ProductNative-v3 document was protected by pre-launch
  config-directory and third-save baselines; the product deployment was
  disabled with a receipt-checked marker, the mandatory demand route loaded
  only the existing EquipmentSlots Compatibility Host, and the real backend
  recovered exactly one `grandmas_button` through the native backpack. The
  marker, config tree, three protected save files, profile and Author source
  were restored exactly, title recovery passed, and no process remained.
- `GAME-SMOKE/20260724-031003` is retained only as legacy-ABI orphan-recovery
  evidence; it did not stage the ProductNative-v3 schema and is superseded for
  eighth-product cold acceptance by `053342`.
- `025030`, `025522` and `025932` are non-acceptance orchestration evidence:
  they exposed literal/regex matching, an undisabled Author SDK deployment and
  an empty cold sidecar respectively. The repairs changed only the Smoke
  transaction/routing and were covered by PS 7, Windows PowerShell 5.1 and
  focused route Unit checks; the accepted Runtime/product binaries remained
  the complete-Release candidate.
- `GAME-SMOKE/20260724-040033` and `041318` remain superseded corrected
  integration evidence; the final pair above binds current commit
  `034ea5e6`. Both final runs report Player Doctor
  `status=ok; errors=0; warnings=0`, exact save/profile/source restoration and
  no residual process.
- The independent correction review initially found no remaining P0/P1/P2
  after the durable recovery, shield, real-installer and same-item aggregate
  fixes. The post-close save-semantics correction below supersedes that data-
  safety conclusion without invalidating the physical split or existing
  owner/cold-recovery evidence.

## 2026-07-24 Post-Close Save-Semantics Correction

The user confirmed that normal Doloc Town gameplay rolls unsaved changes back
after return-to-title, no-save exit, crash or force quit. Successful native
`SaveGame` is therefore the technical commit fact; DTMAPI `SaveSaved` is its
normal successful in-process notification.
Review
`docs/reviews/manual-qa/2026/20260724-0001-moreequipment-unsaved-save-commit-regression.md`
finds a P1 migration regression:

- product-shield damage and break currently persist the committed sidecar
  immediately, so durability or a removed shield can survive an official
  no-save rollback;
- ordinary product equip/replace/unequip writes a prepared intent that can be
  retried in a later session even though no native save committed it;
- the frozen old-ABI Host has the same conditional ordinary-intent risk;
- explicit disable/uninstall/orphan recovery remains a separate management
  transaction where persistent retry is allowed to prevent inaccessible items.

The accepted `053248` run covered a saved `grandmas_button` transaction and
`053342` covered product-disabled cold recovery. Neither run tested shield
damage/break or ordinary equip/unequip followed by no-save rollback. The
historical pre-split implementation did test this invariant by keeping
gameplay changes dirty until native save and discarding them at
`SaveLoaded`/`ReturnedToTitle`.

The owning Update is therefore reopened as `in-progress/open/partial`; the
physical migration itself remains implemented.
Physical ProductNative ownership, frozen ABI shape, default-loaded Runtime
reduction, four-owner lifecycle and cold management recovery evidence remain
valid. Behavior/data-safety verification requires:

1. working-versus-committed state for shield and ordinary slot mutations;
2. explicit `GameplayMutation` versus `OwnerRecovery` journal origin;
3. no-save return-title and cold-restart rollback for damage, break,
   equip/replace/unequip;
4. successful native-save promotion and native-success/sidecar-failure
   reconciliation with exactly one logical item;
5. the same ordinary-operation matrix through the old ABI Host, while retaining
   management recovery replay.

This correction changes documentation and workflow constraints only. It adds
the manual-QA Review, makes `PROJECT.md` the save-commit authority, projects the
rule into `AGENTS.md` and the existing feedback/API/document-governance
workflows, and corrects the current Update/index, Batch 6 contract, roadmap,
Hook map, API matrix, smoke evidence scope and closeout audit. No source fix,
game launch, complete Release, L0-L5, GC ladder or long test was performed.
Changed-document links and `git diff --check` pass. Document governance
evaluates 5,853 checks; its only two failures are the unrelated, user-owned
untracked portable reverse-capture Update lacking its monthly row and the
resulting row-count mismatch.

The follow-up test-mode audit narrows “protected save” into two different
obligations. A `NoNativeSave` acceptance now requires unchanged archive and
committed-sidecar evidence before any runner or external file restoration,
with no routine byte backup or archive writeback on the green path. An
intentional native-save or archive-mutation acceptance
requires a disposable fixture isolated from live Steam AutoCloud; restoration
is cleanup, not proof. The current runner's unconditional post-exit restore can
mask an unexpected save and race AutoCloud, so runner classification is a
prerequisite to the reopened game acceptance. This follow-up is docs-only: no
runner/product source, save, Steam state or game process was changed.
Changed-document diff checks pass; the full document-governance scan still has
only the two pre-existing failures caused by the unrelated, user-owned
untracked portable reverse-capture Update and its missing monthly row.

## 2026-07-24 Working/Committed Reacceptance

The reopened P1 is fixed and independently reaccepted.

- ProductNative and the frozen Compatibility Host now keep explicit
  `Working` and `Committed` projections. Shield hit/break and ordinary
  equip/replace/unequip change only Working memory during gameplay.
  `SaveSaving` writes a typed, explicitly uncommitted gameplay candidate;
  only a successful native save followed by `SaveSaved`, or exact proof of the
  interrupted successful native commit, promotes it.
- Return to title, process shutdown or a cold restart without a matching
  native commit discards the gameplay candidate and rehydrates the last
  Committed projection.
- `GameplayMutation`, `OwnerRecovery` and `OrphanRecovery` are distinct journal
  origins. Only explicit management recovery remains persistently retryable.
  The native-success-before-sidecar-promotion and
  committed-tombstone-before-cleanup windows reconcile to exactly one logical
  item.
- Backpack success, explicit mail success despite the official false return,
  and true placement failure remain separate outcomes.

Focused ProductNative, Compatibility Host, real-Harmony, API, QA, save-mode
runner, Catalog and package checks pass. The frozen Host test executes its real
Equip/Unequip, SaveSaving/SaveSaved, title/cold-restart, shield rollback,
native-success-before-SaveSaved, tombstone cleanup, dirty-owner deferral and
typed OwnerRecovery paths; it is not only a source-shape projection.

The current focused Advanced build produced:

- product DLL: `111,104` bytes, SHA-256
  `07F5EF74F85754D92E4BDDEF1970F43B4FD11FDB3C1FE2220973D8F4BAC5157E`;
- deterministic product package SHA-256
  `1C210AFEB2C6776B248D44F09B8623A1690DD34BF97338F8BDEAD1F300708AB6`.

Accepted runtime evidence:

- `GAME-SMOKE/20260724-155216`: third-save `NoNativeSave` Working mutation,
  return to title, four Hooks and unchanged archive/committed-sidecar metadata
  before cleanup, with no backup or writeback;
- `GAME-SMOKE/20260724-155344`: cold process reloaded empty Committed state,
  `workingMatchesCommitted=true`, no journal/candidate and clean exit;
- `GAME-SMOKE/20260724-161422`: Steam-AutoCloud-isolated disposable fixture,
  interrupted candidate reconciliation, successful native saves, sidecar
  promotion, one logical item and clean journal;
- `GAME-SMOKE/20260724-161536`: cold read of the same disposable fixture with
  exact archive and sidecar identity retained, one `SaveLoaded`, zero save
  writes and clean exit.

`154906`, `155811` and `161030` remain non-acceptance diagnostics and are not
folded into the successful evidence. `161030` supplied only the interrupted
candidate later reconciled by `161422`. Historical
`PlayerSaveRestored=Passed` receipts remain historical restoration evidence;
they are not reinterpreted as no-save proof.

The implementation was marked `verified/closed` at this point. The later
independent closeout audit below supersedes that lifecycle conclusion without
invalidating the covered evidence. No complete Release, L0-L5, GC gradient or
long test was run.

## 2026-07-24 Independent Closeout Audit Correction

[Review `20260724-0006`](../../reviews/code/2026/20260724-0006-moreequipment-zoom-closeout-audit.md)
keeps the physical migration and Working/Candidate/Committed implementation,
but returns this Update to `implemented/open/partial`:

- the `NoNativeSave` runner can accept a fixture-root parameter or persistent-
  root environment override even though its QA path does not redirect the
  game, allowing a future run to compare the wrong archive root;
- disposable native-save fixtures do not yet reject junction/symlink roots;
- the retained ProductNative runtime pairs cover equip/title/cold and
  equip/unequip/save, but do not execute the declared damage, break, replace
  and no-save unequip recovery matrix through the real product path.

The recorded `155216`/`155344` archive paths are the real live third-save
paths and remain valid for their covered route. `161422`/`161536` remain valid
for their ordinary-directory isolated route. Reacceptance needs only the
focused runner corrections and the missing bounded product cases; it does not
need a complete Release, L0-L5, GC gradient or long test.

## 2026-07-24 Final Focused Reacceptance

The independent closeout findings are corrected.

- `NoNativeSave` rejects both fixture-root inputs, always observes the live
  persistent root and records only archive/committed-sidecar metadata.
  Disposable fixture validation rejects reparse points at the root, `SAVE`,
  `DTMAPI` and protected archive paths before launch.
- ProductNative and the frozen Compatibility Host use the same deterministic
  native reflection resolution. The real product fixtures now execute
  replacement, shield damage, shield break, post-break equip/unequip,
  no-save rollback and successful normal-save promotion while retaining the
  three placement outcomes and exactly-one-item journal invariants.
- Focused Product/Host/QA Units, save-mode tests, retained ABI, Catalog,
  Author SDK and deterministic package gates pass. The final package SHA-256
  is
  `28263452F853CA515F12CAD2CF53E9C3D22BC9AFBEB8A6250620FD1195E5E267`,
  entry DLL SHA-256 is
  `F7BB2C7A9F39BF60946BD67725A653141958F1A681C7AABDC1E4E0F97EBAE3A0`,
  and Advanced reference receipt SHA-256 is
  `662F4FCD52BA83B824D0EACC2137E559400F9D08DA428E4E58787333A93B2931`.

Accepted runtime evidence, all bound to
`BuildCommit=25403595da3e`:

- `GAME-SMOKE/20260724-202032`: third-save `NoNativeSave` replacement,
  `80 -> 40` shield damage, break, post-break equip/unequip, four exact Hooks,
  no native save, Working dirty and Committed unchanged. Before any cleanup,
  the selected player archives and committed sidecars are unchanged; title
  recovery and process exit pass.
- `GAME-SMOKE/20260724-202146`: independent cold `NoNativeSave` observation
  proves Working equals Committed with no surviving candidate or journal;
  archive/sidecar unchanged-before-cleanup and process-exit gates pass.
- `GAME-SMOKE/20260724-202526`: Steam-AutoCloud-isolated disposable
  `NativeSaveExpected` run receives two real `SaveSaved` notifications,
  commits the damaged shield then the empty sidecar after break/equip/unequip,
  and ends with backpack `1`, committed sidecar `0`, journal `0`, exactly one
  logical item, four targets, fixture cleanup and clean process exit.

`GAME-SMOKE/20260724-202324` remains non-acceptance diagnostic evidence:
the native product matrix passed, but the disposable fixture contained a stale
Compatibility Host/release-manifest pair. `202526` restaged the current Host
and supersedes that run.

Review `20260724-0006` therefore closes this correction and returns
MoreEquipmentSlots to `verified/closed`. No complete Release, L0-L5, GC
gradient or long test was run; complete Release count remains zero.

## 2026-07-24 Second Independent Audit Correction

[Review `20260724-0007`](../../reviews/code/2026/20260724-0007-equipment-zoom-final-closeout-audit.md)
supersedes the lifecycle conclusion above. The physical ProductNative
migration and the behavior actually exercised by the retained runs remain
valid, but no-save acceptance is reopened because the final fixture began and
ended with an empty Committed slot. Reacceptance must begin from a normally
saved, non-empty committed shield and prove damage, break, replacement and
unequip rollback restore the prior durability and exactly one logical item.
Runner environment/fixture cleanup is a shared focused prerequisite. This
Update remains `implemented/open/partial` until those bounded gates pass.

## 2026-07-24 Non-Empty Committed Final Reacceptance

Commit `136a7278ed79614d7dffe259d21789b02329d869` closes the bounded
Review `20260724-0007` evidence gap and the shared runner-lifecycle findings:

- `NoNativeSave` still defaults to the selected live persistent root, but may
  use an explicitly created Steam-AutoCloud-isolated disposable fixture for a
  bounded multi-process rollback proof. The runner owns and restores both
  persistent-root environment variables, rejects reparse-point fixtures, and
  deletes a successful fixture through the same cleanup lifecycle unless
  explicit bounded follow-up retention was requested;
- the PowerShell 5.1 junction-rejection test now removes its test junction
  through a bounded directory-handle operation, without traversing or restoring
  a player save;
- ProductNative and frozen Host focused matrices continue to pass, including
  placement outcomes, save-failure windows, Working/Candidate/Committed
  behavior, typed owner/orphan recovery, exact four-Hook ownership and
  zero-leftover cleanup. The product package remains
  `28263452F853CA515F12CAD2CF53E9C3D22BC9AFBEB8A6250620FD1195E5E267`.

The accepted game sequence uses one disposable AutoCloud-isolated fixture and
is entirely bound to
`BuildCommit=136a7278ed79614d7dffe259d21789b02329d869`:

- `GAME-SMOKE/20260724-222231` normally saves one committed `box_hat` shield
  at durability `80`, with zero native shield copies, one committed logical
  shield, no candidate and no journal;
- `GAME-SMOKE/20260724-222329` cold-loads that baseline, performs real
  ProductNative shield damage `80 -> 40` only in Working memory, returns to
  title without native save, and proves archive plus committed-sidecar
  length/hash/mtime unchanged before cleanup;
- `GAME-SMOKE/20260724-222423` first proves the prior damage rolled back to
  committed durability `80`, then exercises unsaved shield unequip/re-equip,
  `grandmas_button` replacement and replacement unequip, shield re-equip and
  break. Committed remains the exact durability-80 shield and there is exactly
  one logical shield;
- `GAME-SMOKE/20260724-222516` is the independent final cold read:
  `workingMatchesCommitted=true`, `workingDirty=false`, no journal/candidate,
  zero native shield copies, one committed/logical shield at durability `80`,
  unchanged archive/sidecar metadata before cleanup, and successful fixture
  deletion.

All four processes exit cleanly. This is the requested non-empty committed
rollback proof; it does not reinterpret the earlier empty-to-empty runs.
MoreEquipmentSlots returns to `verified/closed`. No complete Release, L0-L5,
GC gradient or long test was run.

## Eight-Product Horizontal And Weight Closeout

The 2026-07-24 documentation closeout measures tracked ProductNative C# `src`
files and actual mandatory compile inputs with physical and non-empty lines
kept as separate units:

| Product | C# files | Physical lines | Non-empty lines |
| --- | ---: | ---: | ---: |
| AutoFishing | 22 | 5,393 | 4,964 |
| OneActionComplete | 7 | 879 | 802 |
| ActionSpeed | 6 | 1,825 | 1,612 |
| FishBreedingAssistant | 5 | 377 | 342 |
| AnimalHusbandryProgress | 7 | 1,192 | 1,088 |
| MoreSaves | 3 | 425 | 391 |
| ChestLocatorEnhancer | 8 | 1,672 | 1,524 |
| MoreEquipmentSlots | 11 | 5,910 | 5,495 |
| **Total** | **69** | **17,673** | **16,218** |

The current five mandatory Runtime projects compile 67,224 physical /
60,195 non-empty source lines; their five DLLs total 2,155,520 bytes. Against
the committed seven-product baseline of
69,948 / 62,613 / 2,240,000, the deltas are
-2,724 (-3.89%) / -2,418 (-3.86%) / -84,480 (-3.77%).
Mandatory GameBridge alone changes
31,336 -> 28,592 physical,
27,762 -> 25,324 non-empty and
937,984 -> 851,456 DLL bytes.

Counting the same product functionality back in gives
mandatory-plus-products 81,711 -> 84,897 physical,
73,336 -> 76,413 non-empty and
2,583,552 -> 2,592,256 DLL bytes. The dormant-shipped Compatibility Host is
364,032 bytes versus 209,408 at the seven-product checkpoint. Therefore the
accepted claim is only **smaller default-loaded Runtime**. Repository source,
download, install and total shipped package size did not become smaller.

The horizontal ownership comparison adds no SharedNative capability. All
eight products reuse Platform ConfigMenu/owner lifecycle, Catalog-driven
Advanced SDK/package, Doctor/Manager and zero-leftover machinery. Housing
eight frozen executors in one optional Host is compatibility assembly reuse,
not a common native owner. The only gameplay/native adapter with two
independent real product consumers remains the Experimental
`IItemDisplayNameApi` shared by FishBreedingAssistant and
AnimalHusbandryProgress over one read-only item-title native owner.

Author-facing truth was reconciled without changing an API signature:
`DTMAPI.Abstractions` is public but not uniformly Stable; stability and
adoption disposition are separate; Frozen/Diagnostic/Disabled/internal/
Proposed surfaces are not recommendations for new ordinary mods; current
Advanced policies are exact first-party identity grants rather than a general
authoring lane; and no general Optional Content Host is currently implemented.

Documentation-only validation for this closeout passed the Catalog projection
(`products=27`, `public=11`, `workshop-items=21`, `api-rows=48`), all 49 local
links in the changed documents, the compact active smoke-router byte limit and
`git diff --check`. The full document-governance scan evaluated 5,828 checks;
its only two remaining failures are the explicitly excluded, user-owned
untracked portable reverse-capture Update being unindexed and the resulting
record/row count mismatch.

## 2026-07-29 Legacy Sidecar Generational Migration

The P1 implementation in commits `edf73dbc` and `cba22720` closes the source,
transaction and cold-routing defects identified by
[Review `20260729-0001`](../../reviews/manual-qa/2026/20260729-0001-moreequipment-legacy-sidecar-migration.md):

- one side-effect-free classifier distinguishes nested Product v3, legacy
  flat schema 1-3, unsupported future generations, ambiguous shape and invalid
  JSON before either owner may consume a document;
- Product startup converts an exact-owner/exact-save flat schema 2 or Host
  flat schema 3 document into the fixed three-slot Product v3 contract,
  including `isShieldHat -> isShield`, passive/defense/shield traits,
  Working/Committed state and typed gameplay/recovery transaction state;
- a scoped flat file is copied byte-for-byte to the non-authoritative
  `.legacy-migrations` backup area, then replaced only after the temporary
  Product document validates and round-trips. A global legacy file may be
  adopted only when the scoped canonical file is absent; after Product publish,
  the global source is archived only when the recorded source SHA-256 still
  matches;
- interruption before Product publish leaves the legacy authority intact.
  Interruption after Product publish and before global archival converges on
  restart through the exact migration stamp. External source drift, wrong
  owner/save, future schema, duplicate/negative slots and a populated fourth
  slot all fail closed without publishing an empty document;
- when the Product assembly is absent, Compatibility cold recovery routes a
  proven flat schema 2/3 document back to the old parser. Product v3 stays with
  the Product cold backend, while invalid, future or ambiguous documents are
  claimed and rejected rather than crossing parser boundaries.

Focused validation:

- `DTMAPI_UNIT_TEST_FOCUS=moreequipment-product`: PASS, including schema 2/3
  conversion, exact item/trait preservation, legacy gameplay and recovery
  transaction mapping, idempotency, both publish interruption windows,
  external drift and invalid-boundary cases;
- `DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host`: PASS against the physical
  `net48` Compatibility Host. Both flat generations reach the old orphan
  recovery path and move exactly one item; an ambiguous same-file document is
  unchanged and fails closed;
- `DTMAPI_UNIT_TEST_FOCUS=compatibility-host`: PASS;
- Catalog projection: PASS with `products=27`, `public=11`,
  `workshop-items=22`, `api-rows=48`;
- the focused Advanced product build and package transaction pass. The package
  SHA-256 is
  `C8EE0B3B9AD77DDBAB736F11F4A11F14F52BF476DED49F372051F924F0627F31`;
  the frozen Author SDK SHA-256 remains
  `1131840B940D9B366FBC24391449CF739078730655B7409B24720B1579C5F1C7`.

The broad `tools/scripts/test.ps1` was attempted after the focused checks but
exceeded both the 120-second and 300-second command windows without a captured
terminal result. It is not recorded as a PASS. No game was launched and no
complete Release was run for this correction. The P1 implementation is
therefore `implemented`, not `verified`: the prior Product-v3 game evidence
remains valid only for that generation, and one bounded player migration
acceptance remains before the product and 0.5.5 release candidate may return
to `verified/closed`.

The independent
[audit and transition plan `20260729-0004`](../../reviews/code/2026/20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md)
later found four remaining P1 and three P2 gaps in that first implementation.
The coverage paragraph above is therefore historical evidence for
`cba22720`, not the final correction boundary.

Commit `0331f56d` closes those gaps:

- the exact public pre-schema writer shape is recognized without accepting
  arbitrary schema-zero or unknown-field JSON, and its source hash can be
  claimed only once;
- flat schema 1-3 require exact owner, storage scope, archive, effective player
  and native save clock; Product v3 preserves and reload-validates that clock,
  then advances it only after `SaveSaved`;
- legacy backup publication is temp/flush/hash/atomic and a partial final
  backup converges on retry;
- canonical scoped existence claims the owner even when its content is
  invalid, ambiguous or future, so same-owner global data cannot be consumed;
- live-missing or recoverably corrupt Product storage can reach `.previous`,
  while future/ambiguous live authorities remain blocking;
- Compatibility links the small read-only probe, not Product conversion,
  backup or archival implementation.

Pre-Unity automatic validation for commit `0331f56d`:

- `moreequipment-product`, `moreequipment-cold-host`,
  `compatibility-host`, `moreequipment-acceptance-routing`: PASS;
- complete `DTMAPI.UnitTests`: PASS in 93.7 seconds;
- Catalog: PASS with `27/11/22/48` and 278 production source files;
- deterministic Advanced package built twice with 7 files and identical
  SHA-256
  `e3dcaa7f7fa0c1ba34bfb2de7133b0f88075b9d138a01b9f3813bd5ae814ea06`;
  entry DLL SHA-256 is
  `0c11ee787ff955a52b4908487ad1750af4ab6e164565a9d5ca82421a820f7810`;
- retained Workshop `3744059735` rechecks as 9 files, 539,565 bytes and tree
  SHA-256
  `e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`.

No game, Runtime installation, save mutation, complete Release, GC, L0-L5 or
long test belongs to this automatic correction evidence. The bounded Unity
transition plan remains the runtime acceptance owner.

The bounded runtime run then exposed a Unity Mono path-length failure in the
otherwise-correct atomic backup publication. The final correction shortens the
hash-addressed backup to
`.legacy-migrations/<SHA256>.flat.json` and keeps temporary/rejected files as
same-directory `.tmp-<guid>` / `.invalid-<guid>` entries. It also logs an exact
fail-closed storage exception with archive, identity-presence and save-clock
context but without the player name. A realistic full Host flat document and
long per-save paths now participate in the retry matrix.

Superseded pre-claim correction validation and product candidate:

- the four focused Unit routes pass;
- Catalog passes `27/11/22/48`, including public identity/path freeze
  `da8b264478e2a45731bebc7a478f622f2d4bf07c86301c8cf068b23166d10ed9`;
- two frozen-SDK packages are byte-identical: 7 files, 58,798 bytes,
  SHA-256
  `15DB9926A67147012892BE3550368EBDAB63397BEFA888BF979C077C4C2B9633`;
- final entry DLL SHA-256 is
  `75D35F151572298E9FD4630129F36EBD511C0BF6770DEDAEE5B6C7CB68F52D98`;
- frozen Author source tree SHA-256 is
  `5057F9F1834D1575FE071944ED0695DE8D66268E71AF354C9ABF0D4360A58148`.

Runtime-locked, AutoCloud-isolated `ArchiveMutation` evidence:

- [`201712`](../../debug/evidence/GAME-SMOKE/20260729-201712/) passes U2a
  schema 2 with `grandmas_button`, `box_hat`, shield `80/100`, archive 2,
  save clock `229222` and an exact source-byte backup;
- [`201914`](../../debug/evidence/GAME-SMOKE/20260729-201914/) passes U2b
  against the full Compatibility Host flat schema 3 shape;
- [`202212`](../../debug/evidence/GAME-SMOKE/20260729-202212/) passes U2c
  against the exact 435-byte historical no-version global format, leaving one
  hash-addressed backup, one non-authoritative global archive and no active
  global authority;
- [`202551`](../../debug/evidence/GAME-SMOKE/20260729-202551/) cold-restarts
  U2c without changing the scoped Product bytes/hash/mtime or re-adopting the
  global source.

U1 could not be safely executed: DirectExe lacked the native Steam
subscription authority needed to select the exact retained Workshop tree,
while a Local copy correctly failed the frozen Author receipt. The partial
native-save run [`195501`](../../debug/evidence/GAME-SMOKE/20260729-195501/)
moved one `box_hat` through the Product-absent Compatibility route, but its
dedicated cold assertion was skipped and one legacy item remained, so it is
not U1 or U3 acceptance. C0, U1, U3, U4 and the migrated-save gameplay
no-save/save spot check remain open. No complete Release was run.

## 2026-07-29 First Cross-Save Claim Attempt (Superseded)

Independent Review `20260729-0005` reproduced the remaining P1 ordering:

```text
save A publishes Product v3
→ process stops before global archival
→ save B loads first
→ the same identity-free global bytes can be adopted again
```

Commit `4a395398` attempted to close that window without changing ordinary
Product-v3 save semantics:

- before Product publication, the Product atomically writes one
  game-config-root claim under
  `.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/<SHA256>.json`;
  it binds the exact source hash, target Product filename and complete
  archive/player/save-clock scope;
- another scope was intended to fail closed while that claim exists. A pre-fix
  interrupted state with no claim is blocked when another scoped Product
  document already carries the same exact pre-schema migration stamp;
- the original scope was intended to resume, verify/archive the unchanged
  global bytes and clear the claim;
- mandatory cold discovery includes both live `.json` and lone
  `.json.previous`, but generically suppresses only sidecars whose parsed owner
  is already loaded. It does not embed the MoreEquipment product identity in
  mandatory source and does not suppress another orphan owner;
- the Compatibility Host skips normal Product-owned storage without setting
  `orphan-recovery-failed`, and Product cold recovery accepts only the exact
  current archive's canonical scoped path.

Provenance for this superseded attempt:

```text
implementation commit = 4a395398
coverage completion commit = 3937628b
exact committed execution HEAD = not recorded
```

The tests were executed against a worktree containing the later coverage, so
they cannot be attributed to implementation commit `4a395398` alone. Their
historical result was:

```text
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
public identity/path freeze:
  ac96e08bea1f368fbc86cb3122a6946cc0a8e2184d6682f194d9d15839adba0d
```

Coverage commit `3937628b` added stop-after-claim, another-save-first,
stop-after-Product, pre-fix missing-claim, stop-after-archive, same-save resume,
corrupt claim, source drift and conflicting archive. The cold Host focus covers
lone previous demand, loaded-owner suppression, another orphan owner,
Product-owned no-op and non-canonical scoped-path rejection.

The then-current Author SDK output was byte-identical:

```text
files: 7
bytes: 60,135
package SHA-256:
  2933E957E45D768CCDF38749D62A203C9CA5A81F4FF12830CED5F326CAB7AF81
entry DLL SHA-256:
  BCEC89CECF969F26B16D2C34F09565859889B4BE74DFF80E637DFCCCA0AD2A0F
Author SDK SHA-256:
  1131840B940D9B366FBC24391449CF739078730655B7409B24720B1579C5F1C7
```

Both the earlier `15DB...` package and this `2933...` attempt are superseded
and must not be published. Review `20260729-0006` records why the claim source
remained open after these commits.

## 2026-07-29 Claim Concurrency And Resident-Host Correction

This correction remains `implemented/acceptance-open`. It addresses Review
`20260729-0006`, but no independent reacceptance or new player run is claimed.
Review `20260729-0007` later proved that the cross-process and production Hook
conclusions below were too broad; this subsection is retained as a superseded
attempt rather than current acceptance.

Implementation commit
`b2cb543099ec63b475dd1c54dbbee3d9cd2731ff`:

- serializes in-process claim enumeration/publication and always reads any
  concurrently appearing final claim before accepting it;
- repeats complete pending-claim validation immediately before and after
  atomic publication inside one process. This did not close a late publisher
  in another process after the winning claim was deleted;
- checks the owner claim directory before returning a true first-install empty
  state. Claim-only states with source/Product missing, exact archive/Product
  missing or mismatched archive fail closed as recovery-required;
- before claim deletion, revalidates the exact archive bytes and exact live
  Product v3 scope plus pre-schema source stamp;
- when the EquipmentSlots Compatibility backend is already resident,
  a direct `EquipmentSlotsService.SaveLoaded()` call performs lifecycle reset
  and one orphan-recovery attempt instead of returning after notification.
  The production Hook still dispatched that same Service twice.

Coverage commit and exact automatic execution HEAD:

```text
coverage commit =
  c5fce186999a300f9caa168bfaf88a1bc75817d2
execution HEAD =
  c5fce186999a300f9caa168bfaf88a1bc75817d2
```

The Product focus deterministically paused one in-process scope after claim
enumeration, started a competing in-process scope and proved static-lock
serialization only. It also covers:

- claim-only + source/Product missing;
- exact archive + Product missing;
- mismatched archive + Product missing;
- no-claim first-install empty state;
- archive drift and Product deletion after global archival, with the claim
  retained.

The cold Host focus injects an already resident EquipmentSlots backend,
retains only canonical `.json.previous`, directly calls the Service once and
observes one lifecycle notification plus one recovery attempt. It did not
exercise the production Hook plus feature fanout.

Exact-HEAD automatic result:

```text
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
```

The frozen Author SDK built the exact tracked source twice with byte-identical
output:

```text
files: 7
bytes: 60,915
package SHA-256:
  52F5F075D91E84D2A73DF68F24ACC8B7CD375B0FECBA7122126B0E457044EEF6
entry DLL SHA-256:
  0E754093F16965CE91BEC51F658D690BF586EC0B85BE05DAD1B8E25089811F21
Author SDK SHA-256:
  1131840B940D9B366FBC24391449CF739078730655B7409B24720B1579C5F1C7
```

Review `20260729-0007` supersedes this package and both hashes; these bytes must
not be published or cited as the current candidate. No game, Runtime
installation, save mutation, complete Release, GC, L0-L5 or long test was run.

## 2026-07-29 Production Lifecycle And Cross-Process Claim Correction

This correction remains `implemented/acceptance-open`. It addresses Review
`20260729-0007`; independent source review is running, no new player evidence
is claimed, and no replacement Product/Runtime candidate has been frozen yet.

Implementation commit
`9c0645bd`:

- removes the dedicated EquipmentSlots SaveLoaded and ReturnedToTitle
  callbacks; the normal GameBridge feature fanout is now the single lifecycle
  owner, while the dedicated SaveSaving/SaveSaved transaction boundaries
  remain;
- revalidates the exact global source bytes, absence of its deterministic
  archive and absence of a conflicting stamped Product immediately before and
  after atomic claim publication;
- records whether the current call actually published the claim. If another
  process completed during the remaining TOCTOU window, only that late claim
  is exact-validated and withdrawn before the loser fails closed;
- refuses claim completion while the active global source still exists, in
  addition to the existing exact archive and exact stamped Product checks.

Coverage commit and initial focused execution HEAD:

```text
coverage commit =
  af455c7d
execution HEAD =
  af455c7d
```

The Product focus starts a second `DTMAPI.UnitTests` process and pauses it
after final source-state validation but before atomic claim publication. The
parent process completes the winning migration and deletes its claim; the
second process then publishes late, detects the completed archive/Product,
withdraws its own claim and exits with the winner still readable. The same
focus recreates global at `after-global-archive` and requires Product, global
and claim to remain fail-closed.

The cold Host focus calls the production
`AfterLoadArchiveDataPostfix -> GameBridge feature fanout -> broker` route
against an already resident backend. It proves one lifecycle notification,
one recovery, one SaveSaving and one SaveSaved on the same session, then one
ReturnedToTitle notification. Initial focused results:

```text
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
targeted Release Unit build: PASS (10 existing DebugConsole nullable warnings)
```

The remaining compatibility-host/routing/Catalog/document checks, replacement
package freeze and independent reacceptance are deliberately not claimed in
this initial source checkpoint.

## 2026-07-29 Round-1 Independent Review And Durable Completion

Independent Review `20260729-0008` returned `P0=0 / P1=1 / P2=1`.
It accepted the production lifecycle single-owner change and normal
second-process rejection, but proved that a loser could still crash after
publishing its late claim and before self-withdrawal. It also found that
global could be recreated between completion validation and deletion of the
only claim.

Implementation commit `6c5542ff` changes the same schema-1 claim into a durable
two-state record:

- absent `state` remains backward-compatible and is read as `pending`;
- a new migration atomically publishes `pending`;
- after exact global absence, exact deterministic archive and exact stamped
  Product validation, the winner atomically replaces it with `completed`;
- `completed` is retained rather than deleted, so no other process can publish
  into a claim-deletion gap;
- a restart validates the same archive/Product/global facts against the
  completed scope and returns without another migration;
- completion validation runs both before and after state publication, so
  external global recreation fails closed while the durable owner remains.

Coverage commit `7adf2b7d`:

- arms the child process to exit with code `86` if it ever reaches
  `File.Move(temp, claim)` after the winner completes; PASS requires that the
  completed winner blocks this crash point before publication;
- injects a crash after completed-claim publication and proves restart keeps
  the same winner readable;
- recreates global after the first completion validation and proves the claim
  remains pending;
- removes the optional state field from a real interrupted schema-1 claim and
  proves the historical pending record still blocks another scope and resumes
  for its owner.

Initial source checkpoint:

```text
targeted Release Unit build: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
```

No replacement package, game evidence or independent round-2 acceptance is
claimed here. Product status remains `implemented/acceptance-open`.

## 2026-07-29 Round-2 Independent Review And Authority Capture Correction

Independent Review `20260729-0009` returned `P0=0 / P1=3 / P2=0`. It
confirmed durable completed claims, the production Hook/Host boundary and the
round-1 crash-window fix, then identified:

- a historical exact Product/archive/global-absent state with no claim did not
  backfill the durable barrier;
- global could change between hash validation and `Move`/`Delete`, allowing
  new bytes to be deleted or archived under the old hash;
- completed-claim validation read live Product only and rejected an otherwise
  eligible stamped `.previous`.

Implementation commit `34f4d20c`:

- validates exact completed authorities first, then atomically creates a
  completed tombstone when a historical terminal state has no claim;
- reads any concurrently appearing claim and never replaces a different
  scope/source owner;
- atomically renames global to a unique same-volume capture before hashing,
  restores mismatched bytes when possible and otherwise leaves a unique
  quarantine for explicit recovery;
- uses the existing live/previous Product eligibility rules for completed
  authority validation.

Coverage commits `12dd3c6d` and `8efea22e` add the historical no-claim plus
paused real-process order, existing-archive/replacement-global and
archive-absent/replacement-global windows, and missing/corrupt live with valid
or invalid previous. The latter commit also passes the Unit DLL explicitly
when the parent is hosted by repository `dotnet`.

Exact automatic execution HEAD:

```text
8efea22e
targeted Release Unit build: PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
```

No game, Runtime install, save mutation, complete Release or replacement
package freeze was run. Status remains `implemented/acceptance-open`. Final
independent Review `20260729-0010` supersedes the same-source concurrency
conclusion and records four remaining P1.

## 2026-07-29 Final Independent Review Correction

Final allowed independent Review `20260729-0010` returned
`P0=0 / P1=4 / P2=1` against `df1b8dc4`. It confirmed the production
SaveLoaded/SaveSaving/SaveSaved and eligible previous routes, then found:

- per-hash completed files did not form a source-independent game-root winner;
- the old `claimExists` boolean could drive `File.Replace` over a later
  different pending owner;
- a process exit after global-to-capture rename had no restart route;
- identity-bearing `GlobalFlat` did not reject a global recreated after
  archival;
- Update/README concurrency wording was consequently ahead of source.

Implementation commit `574fb6f4`:

- adds a source-independent `winner.json` and a game-root cross-process
  operation lock while retaining per-hash claim files as evidence;
- creates or validates the singleton under the lock for both new migration and
  historical terminal-state backfill; another source/scope never replaces it;
- transitions pending claims by atomically moving the current bytes to a
  discoverable `.transition-<GUID>` path, validating the captured owner and
  publishing completed with create-if-absent. A different late owner remains
  at the canonical path while the original capture is retained fail-closed;
- discovers exactly one interrupted `.migration-capture-<GUID>`, requires its
  Product-scoped exact legacy backup, source hash and absence of a replacement
  global, then resumes the deterministic archive. Drift or multiple captures
  remain untouched;
- requires active global absence and exact deterministic archive for both
  `PreSchemaGlobal` and `GlobalFlat` before reporting terminal success.

Coverage commit `6270d521` adds:

- two different-hash historical Product/archive/no-claim states loaded in
  sequence;
- two real Unit processes released together against those states, with
  exactly one success and one blocked result while both Product byte streams
  remain unchanged;
- mixed old-process claim deletion plus different-scope pending publication,
  and a different owner appearing after atomic transition capture;
- a real child process exiting with code `87` immediately after atomic global
  capture, followed by exact restart completion;
- mismatch, multiple and replacement-global capture states;
- GlobalFlat exact/different recreation and existing conflicting archive
  states.

Catalog commit `a5e36d2` freezes winner, operation lock, capture and transition
recovery paths. Public identity/path digest:

```text
506ca99ee6abcfad87d08b816b451d7d49507f415b937cbf7a97085df9acc5d5
```

Exact implementation execution HEAD:

```text
a5e36d2d08ba5100384ecc4920785d175a82ef59
targeted Release Unit build: PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
```

Two isolated frozen Author SDK builds were byte-identical:

```text
package entries = 7
package bytes = 63,022
package SHA-256 =
  4D8E4BF05649CF53037EE4150B70E3755F93504358310A2C4F9AFABB528015B6
entry DLL bytes = 161,280
entry DLL SHA-256 =
  CDAD7C681B8ECEBF2CCA2BAA4BE9ABBA38E032CA70224AA22E56F8E76485C3BF
advanced reference receipt SHA-256 =
  6F650A6C5D0E52B97DE2DC3D6AF29B1FB2E1B0B0B735FF4CAA78A7EA536B3ADE
Author SDK SHA-256 =
  1131840B940D9B366FBC24391449CF739078730655B7409B24720B1579C5F1C7
```

This is a post-review implementation checkpoint, not a fourth independent
review. No game, Runtime install, save mutation or complete Release was run.
The package supersedes earlier MoreEquipment candidates but remains
unpublishable while C0/U1/U3/U4, claim/player migration and migrated-save
gameplay acceptance remain open. Status stays `implemented/acceptance-open`.

## 2026-07-30 Additional Review Round-1 Correction

User-requested additional independent Review `20260730-0001` audited clean
HEAD `14b70114625a6f3d7d768d8fd5ab43c916ed4fe2` and returned
`P0=0 / P1=3 / P2=1`. It found that:

- a completed winner was read only after all per-hash evidence/transitions,
  so a late loser artifact could make the winner unreadable;
- pre-winner Product census omitted eligible `.previous` and otherwise valid
  different-source-hash Product authorities;
- capture-only residue permitted empty Product creation and did not wake the
  mandatory Compatibility proxy.

Implementation and focused coverage commit `0ffb6a70`:

- classifies and validates `winner.json` first under the operation lock; exact
  winner authority no longer adopts, overwrites or becomes blocked by optional
  loser evidence/transition bytes;
- applies canonical path, live/previous eligibility, scope, revision and
  PreSchema stamp rules across all scoped Product candidates before publishing
  a new winner; multiple historical authorities remain unchanged and
  fail-closed instead of being selected by load order;
- blocks empty Product creation when any global capture residue remains and
  adds generic mandatory proxy discovery plus explicit dormant Host
  diagnostics for capture, winner/evidence and transition residue;
- adds a real late-writer child process, same-hash evidence occupation,
  sequential and real-process ambiguous historical authority tests,
  eligible-previous/different-hash coverage, capture-only empty prevention and
  true-empty controls.

Exact implementation-side result:

```text
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

The prior `4D8E...15B6` package and `CDAD...C3BF` DLL are superseded by this
source correction and are not publication candidates. No package was rebuilt,
no game or Runtime was launched, no save was mutated and no complete Release
was run. Four additional requested independent review rounds plus the existing
player gates remain open; status stays `implemented/acceptance-open`.

## 2026-07-30 Additional Review Round-2 Correction

Additional independent Review `20260730-0002` audited clean HEAD
`65aa71ac7723026d14da8f6a8979e7af7f481d0a` and recorded
`P0=0 / P1=2 / P2=1`:

- existing pending evidence without `winner.json` returned before the
  operation lock and Product census, allowing a second Product write before a
  late conflict;
- empty-state creation and cold Host demand omitted other eligible PreSchema
  Product, deterministic global archive and scoped backup residue.

Commit `0b016b12` removes that early return, makes empty-state inspection use
the same operation lock, blocks all of those authority/residue classes and
adds generic proxy discovery plus explicit Host diagnostics for archive and
backup residue. Focused coverage proves live/eligible-previous pending-evidence
conflicts mutate no Product/global/archive/backup bytes, and covers
archive-only, current/other-scope backup-only and true-empty controls.

Exact implementation-side result:

```text
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

No package, game, Runtime, save or complete Release operation was run. Three
additional requested reviews and the existing player gates remain open;
status stays `implemented/acceptance-open`.

## 2026-07-30 Additional Review Round-3 Correction

Additional independent Review `20260730-0003` audited clean HEAD
`bed6722441aced4e8dd46e9a4cebcad6a5ff492b` and recorded
`P0=0 / P1=3 / P2=2`:

- an exact pending winner still bypassed the canonical live/eligible-previous
  Product census;
- permanent terminal archive/backup/completed-claim evidence woke the dormant
  Compatibility Host and produced an orphan-failure diagnostic for a healthy
  loaded Product;
- the round-2 empty guard treated terminal data for one save as a permanent
  veto on every new save and left an operation lock on a true-empty check.

Implementation and focused coverage commit `dcecbf3e`:

- reruns Product census when resuming a pending winner and before completing
  that winner, with same/different-source live/eligible-previous rejection
  cases preserving every authority byte;
- validates the completed winner, Product and archive for save A before
  permitting save B's in-memory empty state, while current-scope
  pending/incomplete state, capture and unbound archive remain fail closed;
- treats other-scope Product/backup as terminal identity-bearing data rather
  than adopting or blocking it;
- derives owners for terminal migration artifacts, suppresses them when the
  Product is loaded, retains absent-owner demand, skips healthy terminal
  residue in an already resident Host, and removes newly created true-empty
  lock directories.

Exact implementation-side result:

```text
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

No package, game, Runtime, save or complete Release operation was run. Two
additional requested reviews and the existing player gates remain open;
status stays `implemented/acceptance-open`.

## 2026-07-30 Additional Review Round-4 Correction

Additional independent Review `20260730-0004` audited clean HEAD
`dbb0a45dd761b89c77b3146db4735d566dcb5f92` and recorded
`P0=0 / P1=1 / P2=1`. It found that an immutable completed winner retained
migration clock `T0`, while a normal later SaveSaved advanced Product to
`T1`; once the delta exceeded 300 seconds, the round-3 empty guard reused the
normal anti-ahead rule and incorrectly blocked every new save.

Implementation and focused coverage commit `571e5d55`:

- leaves current-save Product validation and `.previous` eligibility
  unchanged;
- gives terminal winner proof a separate structurally validated live/previous
  route that compares immutable save identity, requires Product revision not
  older than winner, and still proves exact source stamp, archive and active
  global absence;
- targets pending/completed claim residue by save identity rather than mutable
  save clock;
- covers `T0 -> T1 > tolerance -> B empty`, stale Product revision and Product
  identity drift.

Exact implementation-side result:

```text
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

No package, game, Runtime, save or complete Release operation was run. One
additional requested review and the existing player gates remain open; status
stays `implemented/acceptance-open`.

## 2026-07-30 Additional Review Round-5 Correction

Additional independent Review `20260730-0005` audited clean HEAD
`173418f5655e95849da99b86a9c0e0ae8f7d3750` and recorded
`P0=0 / P1=2 / P2=1`:

- save A's own T1 cold load still called completion with current scope and
  exact-matched immutable winner T0;
- the empty guard parsed every retained loser claim/transition before
  validating the completed winner, so B-targeting or malformed optional loser
  evidence could veto B forever.

Implementation and focused coverage commit `33772012`:

- classifies a completed winner before transition/loser evidence in the
  completion path, proves current Product revision against immutable winner
  identity and terminal authorities, preserves winner bytes and optionally
  backfills missing same-hash completed evidence using winner T0;
- keeps pending-winner completion and census semantics unchanged;
- classifies and validates a completed winner first in the empty guard, then
  ignores non-authoritative loser evidence without deleting or changing it;
- keeps capture, unbound archive, current-scope backup and no-completed-winner
  invalid/current-save evidence fail closed;
- covers A T1 cold load followed by B empty with B-targeting loser
  claim/transition and malformed loser evidence retained byte-exact.

Exact implementation-side result:

```text
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

All five requested additional review rounds are now recorded with focused
post-review corrections. Replacement package freeze and the existing player
gates remain open; status stays `implemented/acceptance-open`. No game,
Runtime, save, package or complete Release operation was run.

## 2026-07-30 Post-Review Deterministic Package Freeze

After the five post-review correction commits, the Catalog-driven Author SDK
builder ran twice at clean HEAD
`663cbe1e0fdd689b710753f17bd2d34ac9dcfbfb`. The repeat reused the exact
first-run frozen Author SDK and wrote to a separate repository-temp root.

```text
package entries = 7
package bytes = 64,756
package uncompressed file bytes = 171,310
package SHA-256 =
  1E840AA5CEA1C872AEEEBE3A6FB56A9DD9CB16D1336C99D0AC1F060322BCAC2B
repeat package SHA-256 =
  1E840AA5CEA1C872AEEEBE3A6FB56A9DD9CB16D1336C99D0AC1F060322BCAC2B
entry DLL bytes = 166,912
entry DLL SHA-256 =
  E79D5A5DC9DE8E7CFE9396BDC4BBF4AF9F5481537B5CCF9931DF9A1CE2F96194
repeat entry DLL SHA-256 =
  E79D5A5DC9DE8E7CFE9396BDC4BBF4AF9F5481537B5CCF9931DF9A1CE2F96194
advanced reference receipt SHA-256 =
  B9C4F6D1844FB62AC3ABA7CBE3D1E1F31067935EEA3B34A0B5343A27E083B98C
repeat receipt SHA-256 =
  B9C4F6D1844FB62AC3ABA7CBE3D1E1F31067935EEA3B34A0B5343A27E083B98C
Author SDK SHA-256 =
  1131840B940D9B366FBC24391449CF739078730655B7409B24720B1579C5F1C7
```

Primary frozen candidate:
`temp/batch6-more-equipment-slots-advanced-pilot/DTMAPI-MoreEquipmentSlots-advanced-pilot.zip`.
The repeat root is `temp/moreequipment-additional-review-repeat`.

The new package supersedes every earlier MoreEquipmentSlots candidate. It is
still not publishable: claim crash/resume, C0/U1/U3/U4 and the migrated-save
gameplay spot check remain open. No game, Runtime install, save mutation or
complete Release operation was run.

## 2026-07-30 Post-Freeze Authority Correction

Independent audit `20260730-0006` reviewed the frozen `b3817244` candidate and
returned three P1 plus lifecycle/test/document P2 findings. Implementation and
focused coverage commit `5a6c59b4`:

- permits another save's in-memory empty state only when each deterministic
  `GlobalFlat` archive is exact and bound to exactly one eligible canonical
  Product carrying the matching source stamp; missing, ambiguous and
  wrong-stamp bindings remain fail-closed;
- makes optional per-hash evidence publication non-authoritative after an
  exact completed winner exists. A collision now revalidates immutable winner
  plus terminal Product, so a valid Product advanced from T0 to T1 is not
  rejected by the obsolete strict T0 current-save rule;
- requires non-negative `TotalGameSeconds` in both stored Product v3 and the
  current native scope before Product read/write or global migration authority
  publication. Missing-clock live data is not eligible for `.previous`
  fallback;
- recomputes cold Compatibility demand on each lifecycle probe and logs probe
  failures for retry instead of caching a false or true result for the
  process lifetime;
- releases the real cross-process loser with only `winner.json` present by
  deleting optional per-hash evidence first, so the test isolates the
  permanent winner as the rejection cause;
- updates valid physical Product/Host fixtures to provide an explicit native
  save clock and adds direct stored-clock, current-clock and pre-schema
  no-clock fail-closed cases.

Implementation-side validation:

```text
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

The seven Reviews that had absorbed post-review implementation/PASS blocks,
including their status lines, were restored to review-only findings; Review
`20260729-0007` now states the actual layered Hook-wiring plus independent
physical-Host evidence rather than requiring one combined end-to-end fixture.
The old `1E840AA5...AC2B` package and `E79D5A5D...F96194` DLL are superseded.

The Catalog-driven Author SDK builder then ran twice at clean source-and-test
HEAD `730a49001f7d64ce3a92d68cf9efa85c4387998b`. The repeat reused the exact
first-run frozen Author SDK and wrote to a separate repository-temp root.

```text
package entries = 7
package bytes = 65,237
package uncompressed file bytes = 172,846
package SHA-256 =
  EDB7BF80240CE86C859CBD09BBBD9EE22131B38D9A865142EB3747C1767EEF70
repeat package SHA-256 =
  EDB7BF80240CE86C859CBD09BBBD9EE22131B38D9A865142EB3747C1767EEF70
entry DLL bytes = 168,448
entry DLL SHA-256 =
  03E3651C0E1F5258E4CE44EA38605F7AD047DD507C33B1E76DB0A29E5F639A60
repeat entry DLL SHA-256 =
  03E3651C0E1F5258E4CE44EA38605F7AD047DD507C33B1E76DB0A29E5F639A60
advanced reference receipt bytes = 1,487
advanced reference receipt SHA-256 =
  1F7AB81DD6B521E03858DD2745E84BD47D1DDA65ED15EF1FE4C3654B7D22F6D9
repeat receipt SHA-256 =
  1F7AB81DD6B521E03858DD2745E84BD47D1DDA65ED15EF1FE4C3654B7D22F6D9
Author SDK SHA-256 =
  1131840B940D9B366FBC24391449CF739078730655B7409B24720B1579C5F1C7
```

Primary frozen candidate:
`temp/batch6-more-equipment-slots-advanced-pilot/DTMAPI-MoreEquipmentSlots-advanced-pilot.zip`.
The repeat root is `temp/moreequipment-post-freeze-repeat`. The two ZIPs are
byte-for-byte identical and this candidate supersedes every earlier
MoreEquipmentSlots package. Final independent source/package acceptance passes;
claim crash/resume, C0/U1/U3/U4 and the migrated-save gameplay spot check remain
open, so this package is not publishable. No game, Runtime install, save
mutation, complete Release, L0-L5, GC or long test was run.

## 2026-07-30 Cross-Process Loser Causality Correction

Post-fix audit `20260730-0008` found no production P0/P1 and retained the
accepted Product package, but reopened one focused-test P2. The child process
in `CrossProcessLateClaimIsWithdrawn` caught every `InvalidDataException`, so
its green result did not distinguish the permanent-winner rejection from a
later missing-global or archive/source-state failure.

The focused actor now accepts only the ordinal-exact permanent-claim rejection
reason:

```text
Another save or source revision already owns the pending pre-schema global
migration claim.
```

After that exact assertion it emits
`DTMAPI_MORE_EQUIPMENT_CLAIM_REJECTION=permanent-winner`; the parent process
requires the same reason code in addition to the completed permanent winner,
absent optional per-hash evidence, absent loser Product and absent active
global source. Any other `InvalidDataException` now fails the test rather than
standing in for winner causality.

Focused validation:

```text
DTMAPI.UnitTests Release build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
```

This is a test-evidence correction only. It does not change Product or Runtime
production bytes and does not by itself close the broader claim crash/resume
player gate. No game, Runtime install, native save or complete Release ran for
this correction. The next authorized boundary is a current-HEAD Runtime 0.5.5
test-candidate refreeze followed by U1 and U3/U4 before Runtime R0.

## 2026-07-30 Runtime Transition Acceptance

The exact-cause correction is commit `6751157c`. The focused Product process
test now reaches PASS only after the loser reports the permanent-winner reason,
the parent proves the completed winner still exists, and optional per-hash
evidence, loser Product and active global source are absent. This closes the
focused claim crash/resume causality assertion; it is not a substitute for a
player save test.

The Runtime test candidate frozen from that clean production HEAD is:

```text
dist/prerelease-055-moreequipment-transition-candidate/DTMAPI
BuildCommit = 6751157c2d42
files / bytes = 30 / 71,552,124
DTMAPI-FileTree-SHA256-v1 =
  52dac1e0ec120bb4467645e52fd139decc0ace726ea8eec61c2ed42ac946a868
DTMAPI-CandidateStructure-SHA256-v1 =
  1274239d49d6430123d6d2725f7b06d4fe84841cc2cb5f19162cace213850ffc
normalized retained tree =
  611556795c76a2036103cff34998bc64fcf738afb1a9020e2a9b53b322723533
release-manifest SHA-256 =
  ea184b45bc5b5cc52f8c5315ac6469ce0b66f92d6fce93818d8715771f82a20d
root BAT = exact 1 / 2 / 3 / 4; 0_probe absent
```

The repeat build changed only the non-authoritative manifest `BuildTime`; its
other 29 files were byte-identical and its normalized retained tree matched.
The accepted primary candidate assembly hashes are:

```text
Bootstrap =
  5d536a52b6d467ff7e7026069dc8fc9754f9fb8de5ba0e17449e1fc8198ff18d
Abstractions =
  3cd0eee2815e5b8254c4d9c2b32d101c77364b55eca462759f2244b94add1295
Core =
  148404a5d20805ff9b9f3fffc45ea8f7b3a25ed60f75c0228769ba011f3fc992
GameBridge =
  c4c0a9c211da523f85d6efa3645b00e33f05385853916fa97e1df6f24e336453
ModConfigMenu =
  d0f0d8d0674c26acb55e0fb25e6ad410eb73913a39c94095373351db2227279b
Compatibility Host =
  b37875a535dc11f371a89e3bb3f3372d7263b0af868f95e237126aa4b22c501f
Player Doctor =
  624e4f8571d66708fa35dfff5300b04556fb6d40fdaf751543f84437576a1e4f
```

The packaged Doctor and repository-native package checks passed. The first
player-like audit at
`tmp/test-runs/DTMAPI Workshop Audit 20260730-100020` used a relative output
root and is non-acceptance orchestration evidence. The corrected absolute-root
run at `tmp/test-runs/DTMAPI Workshop Audit 20260730-100109` passed with zero
blockers, including PowerShell 5.1 parsing for all ten packaged scripts and the
offline invalid/missing/valid install, status, log collection and uninstall
matrix.

Acceptance harness commit `fa3283e6` adds one exact G5 owner with explicit
`U1`, `Prepare`, `U3Backpack`, `U3Mail` and `U4` phases. It physically removes
ProductNative for the cold routes, forbids `PostMessage`, waits for the native
`NormalGameState`, publishes the input handshake synchronously before the
modal pauses QA updates, and uses real Enter on native `SleepUiState`.
`U4` is a separate `NoNativeSave` route. QA Release build, QA Unit, the
save-mode/routing focus and PowerShell parsing passed.

Accepted AutoCloud-isolated third-save evidence:

| Phase | Evidence | Accepted result |
| --- | --- | --- |
| U1 | `GAME-SMOKE/20260730-110543` | retained Workshop `3744059735`; exact old ABI; three interactive slots; two applied items; `box_hat` 50/80; real B/Enter; one normal `SaveSaved`; scoped flat schema 3; title/Loader cleanup |
| Prepare | `GAME-SMOKE/20260730-115306` | exactly one free native backpack slot committed by one normal sleep save |
| U3 backpack | `GAME-SMOKE/20260730-115439` | Product and old consumer absent; cold Host recovered `box_hat` to the sole free slot; one deferred `grandmas_button`; terminal journal/candidate |
| U3 mail | `GAME-SMOKE/20260730-115548` | full backpack; cold Host recovered exactly one `grandmas_button` through unaccepted native item mail; empty terminal sidecar |
| U4 | `GAME-SMOKE/20260730-115656` | cold reload retained one shield and one mail; no native save; SAVE and committed sidecar length/hash/mtime unchanged before cleanup; no backup or player writeback |

Runs `110744`, `110855`, `112034`, `112538`, `113102`, `113509` and
`114413` are non-acceptance harness diagnostics. They respectively exposed
Product assembly isolation, forbidden fallback, modal update reachability and
load-transition ordering; none is cited as behavior acceptance.

The frozen Runtime DLLs installed for these runs matched the candidate hashes
above, the eight-file local Product was restored after every run, no
`DolocTown.exe` or temporary hold remained, and the player live Steam
AutoCloud tree was never used as a writeback target. No complete Release,
Steam upload, GC ladder, L0-L5 or long run was performed.

## 2026-07-30 Corrected Transition And Product-Publication Acceptance

Review `20260730-0009` found that the first U1/U3/U4 harness could accept
duplicate native items and did not bind the retained Workshop directory to its
frozen bytes. Commit `3a77b93c` corrects the existing harness without changing
Product or Runtime behavior:

- every phase now asserts `box_hat` and `grandmas_button` independently across
  backpack, unaccepted mail and active sidecar slots, with total count exactly
  one for each item;
- shield mail is included in the conservation equation;
- U1 performs a read-only preflight of Workshop `3744059735` before profile or
  game mutation and requires exactly nine files, `539,565` bytes and tree
  SHA-256
  `e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`;
- the migrated-save phase uses the real native damage method, real
  `SleepUiState` input and one observed `SaveSaved`.

The installed production Runtime/Host bytes remained the frozen `6751157c`
candidate, but the QA fixture had two recorded generations. Corrected
U1/Prepare/U3/U4 used the 1,035,264-byte QA assembly SHA-256
`DF0A93078B63AAE6B5EA69C4BEF013C9B041FAF10640F30BD9ACA12A8C3A69B2`.
After the migrated-save phase was added, `134949` and `135057` used the
1,038,848-byte QA assembly SHA-256
`979E2156E4537AB0F1CC80BCD2EDDFB2037EF7EA8D4DF147405875F8272A7465`.
The evidence therefore binds separate fixture generations rather than one
unchanged QA/runner tree.

The requested minimal rerun passed on fresh AutoCloud-isolated fixtures:

| Phase | Evidence | Accepted result |
| --- | --- | --- |
| U1 | `GAME-SMOKE/20260730-132349` | exact retained Workshop bytes; old `0.3.1-dtmapi` ABI and three-slot UI; active 50/80 `box_hat` plus `grandmas_button`; zero native copies for both items; one normal save; title return and process exit |
| Prepare | `GAME-SMOKE/20260730-132150` | one free backpack slot committed for the fresh recovery chain |
| U3 backpack | `GAME-SMOKE/20260730-132527` | exactly one `box_hat` in backpack and one `grandmas_button` in the committed sidecar; no mail copies and no journal/candidate |
| U3 mail | `GAME-SMOKE/20260730-132623` | exactly one `box_hat` in backpack and one `grandmas_button` in unaccepted mail; empty sidecar |
| U4 | `GAME-SMOKE/20260730-132723` | the same per-item terminal state cold-reloaded under `NoNativeSave`; current/prev/bak archives and committed sidecars stayed length/hash/mtime-identical before cleanup |

C0 used the retained Runtime `0.5.2-alpha` archive at
`E:\Python_project\DTMAPI-retained-artifacts\runtime\DTMAPI-0.5.2-alpha-workshop-3743016467.zip`
(`1,934,142` bytes, SHA-256
`095533CB256E19381D1C51018258B239D53AA01ACCAFA575BD24BDB93E9C4AC6`).
`GAME-SMOKE/20260730-133359` proves the old Runtime rejected the `DTMAPI >=
0.5.5` Product before Product entry/assembly load while remaining in title
startup flow, preserved the protected third-save archives under
`NoNativeSave`, and exited without loading a save. Its generic current-Runtime
health aggregate is `Failed` because
`0.5.2-alpha` cannot emit the `0.5.5` health fields; C0 accepts only the exact
minimum-version rejection and preservation assertions, not that aggregate.

The migrated-save semantic chain uses disposable fixture
`tmp/test-runs/MES-MIGRATED-SEMANTIC-20260730-1342`:

- `134155` migrates the corrected U1 flat sidecar to Product v3 generation 8
  with one 50/80 shield and one button;
- no-save damage `134315` leaves the committed sidecar and native archives
  unchanged, and cold `134412` restores generation 8 and 50/80 exactly;
- real native sleep save `134949` publishes one `SaveSaved` and commits
  generation 11 with shield durability 25/80;
- cold `135057` reloads generation 11 and 25/80 under `NoNativeSave`, with no
  journal/candidate or archive writeback. That fixture generation did not
  count unaccepted mail and therefore is not the final per-item conservation
  proof;
- after Review `20260730-0010`, QA/runner commit `523fa6df` adds the missing
  per-item cold equation and stricter Workshop-tree diagnostics. Final cold
  [`GAME-SMOKE/20260730-161355`](../../debug/evidence/GAME-SMOKE/20260730-161355/)
  uses the 1,039,872-byte QA assembly SHA-256
  `5D3F8A8345C72ED91500B754752A7BD512A8B14316CB279B223A379EC2408EF8`
  and proves, for both items, `0 backpack + 0 unaccepted mail + 1 sidecar = 1`.
  It also passes Working=Committed, generation 11, durability 25/80,
  journal/candidate absence, current/prev/bak and committed-sidecar
  length/hash/mtime preservation before cleanup, zero routine backup/writeback,
  profile/Author-source/QA cleanup and process exit.

The first attempt to collect that final observation, `160935`, was interrupted
by the outer command timeout after the in-game assertion but before runner
collection. It is non-acceptance. Its own pre-run snapshots were used to
restore the official profile and Author source; the exact QA stage was removed
only after byte/membership verification, and the three disposable archives
still matched their pre-run length/hash/mtime before the accepted rerun.

The separately proposed **player** claim crash/resume gate is formally
withdrawn, not marked passed. Claim publication, late loser withdrawal,
restart/resume, permanent-winner causality, source drift and crash windows are
file-transaction invariants and are already exercised deterministically by the
child-process cross-process fault matrix against the production store. A Unity
process kill between selected filesystem operations would require a second,
non-authoritative fault mechanism and would not strengthen that proof. U2,
C0 and the migrated-save chain remain the player/save integration evidence.
This explicit retirement replaces every earlier “claim crash/resume open”
summary.

The final Runtime test candidate was rebuilt from acceptance HEAD `f96c9cc6`
after the source-only semantic-contract and QA status-text corrections:
`dist/prerelease-055-final-candidate/DTMAPI`, 30 files and `71,552,124`
bytes. Its full tree is
`DA1C1980203F81702F97FA07365BDF4358C424DB39F6308AA8A7D3D899AEE0ED`,
structure is
`48388423F87E1F2FE9D87D387A4B5C2E0C63FF3EBFD824160021762D51613825`,
and BuildTime-normalized tree is
`F33FCA39A6942704F98C530AE9BA8EB67111DD5070CA13DB80159AB75C3F1908`.
The repeat differs only in manifest `BuildTime`, and the root BAT set is exact
`1` through `4`. The final player-like Workshop package audit at
`tmp/test-runs/DTMAPI Workshop Audit 20260730-f96-final/DTMAPI Workshop Audit
20260730-144213` reports zero blockers for PowerShell 5.1 parsing and the
offline invalid/missing/valid install, status, log collection and uninstall
matrix.

## 2026-07-30 Generic Cold-Observer Baseline Correction

Review `20260730-0011` confirmed that `523fa6df` and
`GAME-SMOKE/20260730-161355` correctly prove the executed migrated two-item
state, but found a P2 in the reusable QA route: both the reflected cold
observer and runner had frozen that one sample as the only legal state. An
empty Committed document, a shield-only Committed document, or a different
legal backpack baseline could therefore fail before the intended no-save
comparison.

Commit `c2c215e4` corrects only that QA oracle and its runner:

- the observer now parses the supplied expected Committed slot description and
  calculates the expected `grandmas_button` total as the supplied backpack
  baseline plus the expected Committed button count;
- the expected `box_hat` total is the expected Committed shield count;
- both pending native-mail counts must still be zero, and the observed
  backpack/sidecar distribution must match the same supplied baseline;
- the success receipt records expected and observed per-item totals plus
  `itemExpectationsMatch=true` instead of hard-coding both totals to one;
- the PowerShell runner derives its exact second log matcher from the same
  baseline and slot description rather than requiring both items to be in the
  sidecar;
- QA Unit directly executes the shared policy for an empty Committed document
  with a nonzero backpack baseline, a shield-only Committed document, and the
  accepted two-item Committed document. A negative case keeps pending mail and
  duplicate logical authority fail-closed.

Focused validation at `c2c215e4`:

```text
DTMAPI.QaUnitTests Release build/run: PASS
test-game-smoke-save-modes.ps1: PASS
run-game-smoke.ps1 PowerShell parser: PASS
test-batch4-qa-semantic-inventory.ps1: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
check-test-artifact-governance.ps1: PASS
build-evidence-retention-allowlist.ps1 -Check: PASS (489 / 896 / 62 / 19)
check-doc-governance.ps1: PASS
git diff --check: PASS
```

No Runtime or Product production source, package, save data or frozen
candidate bytes changed. No game, complete Release, GC or ladder run was
performed or required for this source-only QA correction. The accepted
`161355` observation remains a valid instance of the generalized rule; this
change does not reinterpret it as evidence for the newly added Unit-only
baseline shapes.

## 2026-07-30 Pending-Mail Fail-Closed Reopen

Independent Review `20260730-0012` accepted the generalized item-total
calculation but found one residual QA evidence-integrity P2:
`CountPendingMoreEquipmentSlotsMail` still translated an unreadable
`archiveHandle -> farmData -> emailManager -> emails` authority chain into a
confirmed zero. It also found that this Update had remained `verified/closed`
before independent acceptance completed.

This Update is therefore deliberately returned to
`implemented / acceptance-open` before the follow-up implementation. Closure
requires isolated negative coverage for button mail, shield mail and an
unreadable mail chain, the bounded focused checks, and a new independent
acceptance. Historical Runtime/Product and `161355` evidence remains unchanged;
no game or package rebuild is required for this QA-only reopen.

## 2026-07-30 Pending-Mail Final Independent Acceptance

Implementation commit `879953d9` closes the residual P2 without changing
Runtime or Product production behavior:

- `archiveHandle`, `farmData`, `emailManager`, and `emails` must all be
  readable, and `emails` must be an enumerable non-string authority;
- zero pending mail is returned only after that collection has been
  successfully enumerated;
- the cold-distribution validator rejects button mail and shield mail through
  item-specific checks before logical-total comparison;
- focused QA Unit separately covers button mail, shield mail, readable empty
  mail, null archive, missing farm data, missing manager, missing emails and a
  non-enumerable emails value.

Independent Review `20260730-0013` audited clean HEAD `879953d9`, reran the
bounded build/test/governance set, inspected for additional P0/P1/P2, and
accepted the result with `P0=0 / P1=0 / P2=0`. The Update and July ledger
remained `implemented/open` from pre-implementation commit `66c21bdf` through
that audit. They advance to `verified/closed` only in this post-audit
documentation commit.

The exact commit ownership is:

- `c2c215e4`: Review `0011`, generalized QA/runner implementation, focused
  tests and refreshed allowlist;
- `bb14f75b`: this owning Update only;
- `66c21bdf`: Review `0012`, lifecycle reopen, monthly ledger and refreshed
  allowlist;
- `879953d9`: pending-mail fail-closed implementation and focused tests;
- Review `0013`: independent post-implementation acceptance.

Independent focused validation:

```text
DTMAPI.GameBridge.DolocTown.QA Release build/run: PASS
DTMAPI.QaUnitTests Release build/run: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
test-game-smoke-save-modes.ps1: PASS
PowerShell 7 and Windows PowerShell 5.1 parsing: PASS
test-batch4-qa-semantic-inventory.ps1: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
check-test-artifact-governance.ps1: PASS
check-doc-governance.ps1: PASS
build-evidence-retention-allowlist.ps1 -Check: PASS
git diff --check: PASS
```

No game, native save, Runtime/Product package rebuild, complete Release, GC
or ladder run was performed. The accepted `161355` result and frozen candidate
remain unchanged and retain only their previously recorded evidence scope.

## 2026-07-30 Nested Mail Parser Reopen

The single user-requested sub-agent audit in Review `20260730-0014` accepted
the top-level mail-authority correction and lifecycle/provenance chain, then
found one new QA evidence-integrity P2 below the enumerable `emails`
collection. A partially unreadable mail entry, attachment or reward can still
be skipped or converted to an empty/default value and therefore reported as
confirmed zero mail.

This Update and the July ledger are returned to `implemented/open` before the
follow-up implementation. The bounded correction must strictly classify
mail-template identity, item-attachment collection, acceptance state,
reward type, item identity and non-negative count; it must also execute real
object-graph tests for readable button/shield mail and malformed nested
entries. Existing `161355`, U3Mail, Runtime/Product and package evidence is not
reinterpreted or invalidated by this QA-only reopen.

## 2026-07-30 Nested Mail Parser Implementation

Commit `8526b79a` implements the P2 correction found by the single
post-closeout sub-agent audit:

- every enumerated email must expose a readable non-empty `Id`;
- a `send_item_template` must expose a non-string enumerable
  `emailAttaches`;
- every exact `DolocTown.EmailAttachReward` must expose a readable boolean
  `isAccept`;
- an unaccepted reward attachment must contain an exact
  `DolocTown.RewardItem`, a readable non-empty `itemName` and a non-negative
  integer `itemCount`;
- readable unrelated templates, non-reward attachment types and accepted
  reward attachments remain ignorable;
- null entries, missing/wrong member types, negative counts, reflection
  failures and email/attachment enumeration failures propagate to the QA
  failure path instead of becoming zero mail.

The executable QA Unit now sends a real reflected object graph through the
same reader used by the cold and transition fixtures. It proves readable empty,
button and shield mail, then fault-injects null/missing identity, missing and
non-enumerable attachments, null attachment, unreadable acceptance, missing or
wrong reward, unreadable item name/count, negative count and throwing email or
attachment enumerators. Item-specific cold-policy tests remain separate from
these reader tests.

Focused validation:

```text
DTMAPI.QaUnitTests Release build/run: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
test-game-smoke-save-modes.ps1: PASS
PowerShell parser checks: PASS
test-batch4-qa-semantic-inventory.ps1: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
check-test-artifact-governance.ps1: PASS
check-doc-governance.ps1: PASS
build-evidence-retention-allowlist.ps1 -Check: PASS (490 / 896 / 62 / 19)
git diff --check: PASS
```

No game, native save, Runtime/Product rebuild, package freeze, complete
Release, GC or ladder run was performed. In accordance with the one-round
request, no second sub-agent audit was started after the implementation.
Therefore the Update and July ledger deliberately remain
`implemented/open`; only a later separately authorized independent acceptance
may restore `verified/closed`.

## 2026-07-30 Production Mail Transaction Reopen And Implementation

The one requested independent sub-agent round was recorded in Review
`20260730-0015` against clean HEAD `471cd476`. It accepted the nested QA parser
correction, then found a separate production P1: ProductNative and the linked
Compatibility Host both converted unreadable post-`SendItemAsEmail` evidence
to zero and ordinary failure. Native mail could therefore exist while the
Working slot or recovery escrow remained authoritative and eligible for a
later save or retry.

Commit `10e74ed6` implements the bounded production correction:

- the shared production mail reader now requires a readable archive, farm,
  email manager, enumerable email and attachment collections, exact member
  types, readable acceptance state and non-negative item counts; unreadable
  authority throws and never means zero;
- backpack and mail counts are both read before the first native mutation;
  only exact unchanged, backpack-only `+1` or mail-only `+1` evidence is
  classifiable, while excess, mixed, invocation-error and post-read failure
  states become `NativeMutationOutcomeUnknown`;
- Product gameplay keeps the Working slot and an in-memory guard, blocks
  another placement and `SaveSaving`, verifies the unchanged native-save
  fingerprint, then either retains the slot on exact zero or releases it once
  on exact backpack/mail evidence without replay;
- Product owner/orphan recovery persists `AttemptStarted` before placement,
  keeps incomplete escrow after an unknown outcome, and reconciles its exact
  destination before allowing the synchronous native save;
- the old-ABI Compatibility Host applies the same guard to Working
  mutations, persists durable attempt preimages before native calls, removes
  no journal after an unknown outcome, reconciles before `SaveSaving`, and
  includes the guard in title, Loader/RuntimeShutdown and owner-resource
  cleanup;
- exact absolute after-counts accompany Product placement results so two
  escrow entries with the same item identity do not reuse the first entry's
  baseline;
- the real net48 Product/Host fixture now fault-injects unreadable preflight,
  successful mail followed by unreadable observation, rejected save/no
  replay, later exact `+1` convergence, durable Product recovery, durable
  Compatibility owner recovery, and Product no-save title rollback.

Focused validation at commit `10e74ed6`:

```text
EquipmentSlotsHarmonyOwnerFixture Release build: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
test-game-smoke-save-modes.ps1: PASS
test-batch4-qa-semantic-inventory.ps1: PASS
check-product-catalog.ps1: PASS (27 / 11 / 22 / 48)
git diff --check: PASS
```

These are focused source and transaction checks. No game was launched, no
native player save was mutated, no Runtime/Product candidate was rebuilt, no
package hash was frozen, and no complete Release, GC or ladder was run. The
previous candidate and its complete-Release result remain historical evidence
for their exact bytes; they are not publication authority for the changed
Product/mandatory Host source.

The Update and July ledger remain `implemented/open`. A replacement
deterministic candidate plus a separately authorized independent acceptance
must pass before `verified/closed` can be restored.

## 2026-07-30 Publication deferral

The user paused publication of the new MoreEquipmentSlots `1.0.0` product,
its Product-v3 migration and its Steam update. The implementation and
historical evidence remain in the repository, but the product candidate is
not an input to Runtime 0.5.5 and the five open P1 findings in Review
`20260730-0016` are deferred with it rather than reported as fixed.

Workshop item `3744059735` remains the exact retained
`0.3.1-dtmapi` package. Runtime 0.5.5 continues to ship the frozen
`IEquipmentSlotsApi` ABI and demand-loaded Compatibility Host solely for this
already-published DLL. An exact old-package compatibility smoke may accept the
ordinary legacy path; it does not accept the new Product, cross-generation
migration, U2/U3/U4 fault model, or a new public API.

The proposed generic protected-storage API is also deferred. No
`IProtectedStorageApi` is declared, admitted or promised for 0.5.5. Future
work must begin from a single platform data authority and one production
adapter, not by promoting the current duplicated transaction machinery.

Commits after the frozen `f96c9cc6` Runtime artifact that change
MoreEquipmentSlots Product or Compatibility Host behavior are next-version
source and are excluded from the selected 0.5.5 release artifact. The exact
frozen Runtime candidate and retained Workshop package must still pass their
own release compatibility checks.

## 2026-07-30 Exact Retained-Workshop Compatibility Result

`GAME-SMOKE/20260730-225518` closes only the selected Runtime's ordinary
compatibility target for the already-published Workshop package:

- the read-only preflight matched Workshop `3744059735` as exactly 9 files,
  539,565 bytes and the frozen Catalog-normalized tree;
- the Loader log proves `source=Workshop`, `identity=Strict CodeMod` and the
  old package's DLL path; local Advanced/Product copies were not available to
  the Loader;
- the Compatibility Host became resident specifically for
  `service=EquipmentSlots`;
- the old path rendered three interactive/hoverable slots, held
  `grandmas_button` and a `50/80` `box_hat`, passed real hover and close input,
  completed one ordinary native save, returned to title with UI/storage state
  cleared and exited the process;
- the disposable third-save fixture was Steam-AutoCloud-isolated. No routine
  player-save byte backup or player-archive writeback occurred, and the
  Workshop package stayed read-only.

The scoped field `MoreEquipmentSlotsTransition=Passed`. The aggregate runner
is deliberately not called a PASS because final disposable-root cleanup tried
to delete `runner.stderr.txt` while the outer harness still held that file
open. The earlier input-frame callback warning recovered through the fallback
pump and did not set the runner failure. The active smoke row is therefore
`partial`: it accepts the old ABI/UI/save target without accepting the entire
runner invocation.

This evidence does not close Review `20260730-0016`, validate the new
Product-v3 migration, authorize MoreEquipmentSlots `1.0.0`, or promote a
generic storage API. It is also only a single-Mod, fixed-two-item, one-normal-
save route: no no-save rollback, shield-break, disable/missing, cold-recovery,
update-prompt or multi-Mod claim follows from it. The five Product/Host
transaction P1 findings remain deferred next-version work.

## Changed Areas

- `products/first-party/MoreEquipmentSlots/**`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- mandatory EquipmentSlots proxy/coordination and Compatibility Host sources
- Catalog, Advanced policy, SDK/package, Doctor/Manager and transaction
  projections
- focused Unit/ABI/QA fixtures and one focused EquipmentSlots Hook map
- exact historical format probe, Product-only migration partial and native
  save-clock scope contract
- pre-schema global pending-claim path, mandatory `.previous` cold discovery,
  loaded-owner suppression and exact canonical Product cold routing
- claim publication concurrency, pending-empty barrier, completed-authority
  validation and resident-backend SaveLoaded recovery
- public API matrix, API/author/source/install guides, Batch 6 contract,
  roadmap, smoke matrix and this Update

The historical Strict fixture under `testmods/MoreEquipmentSlotsMod` is removed
as part of the atomic Catalog/source/package switch; the exact old compiled
consumer remains retained by the frozen ABI harness.

## Rollback

Before player-visible cutover, remove the hidden ProductNative/policy and
restore the current mandatory executor while retaining the ABI annotations.
After cutover, revert the product, Host executor/proxy, policy/Catalog and
package transaction as one unit. Never delete or rewrite protected sidecar or
journal data during rollback; cold recovery must remain available until every
prepared record reaches a terminal reconciled state.

## Follow-up

Retain U2a/U2b/U2c, U2c restart, corrected U1/U3/U4, targeted C0 and
migrated-save
evidence only for their executed boundaries. The extra player claim-kill gate
is withdrawn for the reason recorded above and must not reappear as either an
implicit PASS or an unowned open item. Runtime publication still requires the
selected frozen candidate's compatibility check and manual Steam stages.
MoreEquipmentSlots Product publication is deferred and remains blocked by its
Catalog release stop. Preserve the implemented fixed-three ProductNative
source and legacy `0..24` Compatibility semantics as separate future/current
boundaries. Future product work must not reinterpret the default-loaded
Runtime reduction as a repository, download, install or all-products-enabled
reduction.
