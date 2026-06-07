# 20260608-0001: Git Scope Cleanup

## Metadata

- Update ID: 20260608-0001
- Date: 2026-06-08
- Status: implemented
- Source: user request to reorganize local Git scope and commit the current state
- Owner: Codex

## Summary

- Reorganized the local Git scope around source, mod source, tests, scripts, manifests, public docs, and traceable project records.
- Expanded `.gitignore` to exclude build outputs, local settings, runtime logs/evidence, game install artifacts, reverse-engineering data, third-party mod samples, installed SMAPI references, and old own-mod reference copies.
- Kept official Workshop documentation and DTMAPI-authored research notes trackable.
- Removed private/reference folders from the Git index with `git rm --cached`, while preserving local files on disk.

## User-Visible Impact

- Future commits should no longer accidentally include decompiled game code, official game DLLs, third-party mod samples, local logs, or build artifacts.
- The repository becomes closer to a shareable source repository: clone/build inputs stay in Git; local research data stays local.

## Changed Files

- `.gitignore`
- `references/README.md`
- `docs/updates/2026/20260608-0001-git-scope-cleanup.md`
- `docs/updates/INDEX.md`
- Git index only:
  - `references/doloc-town/reverse/`
  - `references/doloc-town/own-mod-sources/`
  - `references/third-party-mods/`
  - `references/stardew-smapi/`

## Validation

- Checked tracked files for binaries, build outputs, reverse/decompiled paths, third-party mod samples, and installed SMAPI references.
- Verified ignored local reference folders remain on disk after `git rm --cached`.
- Did not run build or game smoke; this is a Git scope and documentation update only.

## Evidence

- `git status --short`
- `git ls-files`
- `git ls-files --others --exclude-standard`

## Related Records

- Debug: not applicable
- Hook map: not applicable
- Smoke matrix: not applicable
- API matrix: not applicable

## Rollback Notes

- To re-track any local-only reference folder, remove the matching `.gitignore` rule and explicitly `git add` the needed path.
- Do not re-track `references/doloc-town/reverse`, `references/third-party-mods`, or game binaries unless a separate legal/distribution review approves it.

## Follow-Up

- Future public source exports should reuse this Git scope: source, mod source, tests, scripts, manifests, public docs, and project records only.
