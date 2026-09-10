# DTMAPI Reviews

Reviews own durable pre-implementation reasoning, not implementation progress or proof of a fix.

## When needed

Create or reuse a Review for:
- an explicit audit/review request requiring a durable record;
- an unresolved or changed root cause/native owner/architecture;
- a regression surviving a previous claimed fix;
- a named public-contract or independent acceptance decision.

Discussion can stay in chat. Ordinary corrections with a confirmed boundary stay in their Update; mentioning UI, save/load, Hook or Workshop does not alone require another Review. Read task-specific sources, not the full review library.

## Routes and minimum content

| Work | Location | Include |
| --- | --- | --- |
| Manual feedback | manual-qa/YYYY | Original numbered observations, screenshot/log text, facts vs inference, rejected causes, acceptance and missing evidence per item |
| Code/process/architecture audit | code/YYYY | Bounded decision, inspected evidence, alternatives/risks and acceptance |
| API/native owner | api/YYYY | Symbol/domain, current API status, native responsibility/state holder, physical owner, consumer/transition/save risks and exact evidence gaps |

Name records `YYYYMMDD-NNNN-short-slug.md`. Use [feedback workflow](../workflows/codex-feedback-to-goal.md) or [API workflow](../workflows/codex-api-rebuild.md) when applicable.

Research routers are discovery aids only:
- [Native-owner domains](api/native-owner-domains/INDEX.md)
- [Native function map](../../tools/native-function-map/workbench/README.md)
- [Local Mod demand](../archive/reviews/api/2026/local-mods-native-owner/INDEX.md)
- [SMAPI ecosystem semantics](../archive/reviews/api/2026/smapi-ecosystem-map/INDEX.md)
- [Third-party compatibility samples](../archive/reviews/api/2026/third-party-mods/INDEX.md)

Consult the relevant domain only if ownership/contract evidence is missing. These libraries do not prove API stability or authorize copying source.

## Lifecycle

Use `draft`, `recorded`, or `superseded`. Once implementation begins, freeze substantive code/API/root-cause analysis and add only a short resolution link to its Update. Manual QA remains append-only for later observations and acceptance outcomes. Do not repeat changed files, implementation narrative or completion status here.

One implementation has one [Update](../updates/README.md). Issue state, real smoke evidence, Hook/API contracts and release facts keep their existing owners under [document governance](../workflows/document-governance.md).
