# Local Mod Native Owner Report Template

Use this template when adding another local mod, third-party sample group, or follow-up review to this library.

## Header

- Report id:
- Date:
- Source paths:
- Review status: `Found`, `Partial`, `Not found`, `Blocked`, `Diagnostic`, or `Demand-only`
- Confidence scope: semantic-to-owner mapping only, not API stability

## Semantic Target

- User-facing behavior:
- Subtargets:
- Existing local mod behavior:
- Behavior that is out of scope:

## Source Boundary

- Local DTMAPI source:
- Reverse reference:
- Official Workshop content/docs:
- Third-party sample evidence:
- License or source-boundary notes:

## Native Owner Table

| Area | Exact native class/member names | Where found | State holder | Responsibility | Lifecycle entry | Risk | API concept | Gap |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
|  |  |  |  |  |  |  |  |  |

Risk should explicitly call out `public`, `private/reflection`, `Harmony patch`, `singleton`, `sidecar`, `diagnostic`, or `third-party demand-only`.

## API Translation

- Proposed DTOs:
- Proposed GameBridge adapter:
- Public API status:
- What must stay internal or diagnostic:
- Why raw decompiled game types are not exposed:

## Review Confidence

| Round | Confidence | Change | Reason |
| --- | ---: | --- | --- |
| Round 1 |  |  |  |
| Round 2 |  |  |  |
| Round 3 |  |  |  |
| Round 4 |  |  |  |

## Blockers

- Native owner not found:
- Method-body review needed:
- Save/load risk:
- Transition risk:
- Disable/restore risk:
- Multi-mod ownership risk:
- Validation still missing:

## Next Action

Choose one:

- document only;
- downgrade or clarify docs;
- targeted native-owner deep dive;
- GameBridge rebuild goal;
- blocked until owner or permission exists.
