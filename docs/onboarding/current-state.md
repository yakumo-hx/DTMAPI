# DTMAPI Current Truth Router

Open only the matching route. Reuse unchanged context; historical snapshots are evidence.

| Task / fact | Read or run |
| --- | --- |
| Working tree | `git status --short --branch`, `git diff --stat` |
| Toolchain, paths, lock, process | `tools/scripts/status.ps1` |
| Product fix | Affected source/tests, latest relevant Update; [validation](../workflows/product-change-validation.md) |
| Implementation record | [Update template/sync](../updates/README.md); [governance](../workflows/document-governance.md) for ownership/lifecycle changes |
| Known runtime issue | Matching [Issue](../debug/issues/README.md) and [smoke](../debug/regressions/smoke-matrix.md); [Debug router](../debug/INDEX.md) for protocols |
| Manual feedback | [Feedback workflow](../workflows/codex-feedback-to-goal.md), existing Review |
| Player support / save repair | [Support route](../workflows/player-support.md) |
| Game research / unpacking | [Reference boundary and capture entries](../../references/README.md), matching baseline; [knowledge](../knowledge/README.md) |
| Changed Hook boundary | [Focused maps](../hook-map/README.md), matching native methods |
| API/native-owner redesign | [API workflow](../workflows/codex-api-rebuild.md), affected [API rows](../api/public-api-matrix.md); [native-owner research](../reviews/api/native-owner-domains/INDEX.md) when unresolved |
| Product identity/admission | [Catalog](../../tools/release/dtmapi-product-catalog.json), [generated registry](../architecture/managed-product-admission-registry.md) |
| Public/subscription facts | Catalog, [subscription manifest](../../tools/release/current-subscription-manifest.json), its `authority.latestReleaseUpdate`; future upload authority is `releaseStop` |
| Runtime installer/package | [Installer boundary](../architecture/runtime-workshop-installer-boundary.md), matching [matrix lane](../workflows/workshop-package-subscription-test-matrix.md) |
| Runtime/ABI versions | [Version source](../../tools/release/dtmapi-runtime-version.props) |
| Platform implementation | [Platform handoff](../planning/platform-next/README.md), queue and selected task; continue between review nodes |
| History lookup | [Update month](../updates/INDEX.md) or [archive](../archive/README.md) when current evidence needs it |

[PROJECT](../../PROJECT.md) owns stable identity, native ownership and save semantics. This router owns routes, not copied versions, dates, counts or status narratives.
