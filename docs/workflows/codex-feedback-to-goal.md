# Codex Feedback Review Workflow

Active workflow under a legacy filename. Use for manual feedback or repeated failures needing analysis; ordinary fixes with established causes use their existing Review and Update.

## Read only what resolves this feedback

Start with the user's numbered observations, affected code, latest relevant Update and matching Issue/Review. For new Hook/native ownership read the focused map and matching native methods; for public API redesign use [API workflow](codex-api-rebuild.md). Indexes route to records, not whole histories.

## Record, decide, implement

Preserve each user number and place its analysis directly below it:
- original feedback and screenshot/log transcription;
- user-confirmed facts, code/log facts, inference and rejected/unproven causes;
- affected owner, expected behavior and exact acceptance condition;
- missing evidence or concrete blocker.

Create/reuse a durable Review when the cause or boundary is unresolved/changed, a prior fix failed, or the user requests an audit. A familiar domain name alone is not a gate. The same agent may then implement if authorized; use one Update through corrections and validation.

Save-related analysis distinguishes gameplay data, configuration/diagnostics and explicit owner/orphan recovery, using [PROJECT save semantics](../../PROJECT.md#游戏存档提交语义规范). Test setup and the direct/disposable/isolated choice follow [product validation](product-change-validation.md); do not restate those rules in every feedback record.

## Finish

Keep automated smoke and user confirmation distinct. State exactly what an unrun or partial check leaves open. Preserve later manual observations append-only; completion/files/rollback belong in the Update. Update Issue, smoke, Hook or API records only when their facts changed and link the implementation instead of repeating it.

Use [Update template and ledger sync](../updates/README.md). A fixed requested target is not bumped again just because work continues in another turn.
